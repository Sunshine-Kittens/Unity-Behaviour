using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Behavior.GraphFramework;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIExtras;
using Unity.AppUI.UI;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

namespace Unity.Behavior
{
#if ENABLE_UXML_UI_SERIALIZATION
    [UxmlElement]
#endif
    internal partial class BehaviorGraphEditor : GraphEditor
    {
#if !ENABLE_UXML_UI_SERIALIZATION
        internal new class UxmlFactory : UxmlFactory<BehaviorGraphEditor, UxmlTraits> {}
#endif
        private const string k_PreferencesPrefix = "Muse.Behavior";

        internal BehaviorGraphView BehaviorGraphView => GraphView as BehaviorGraphView;
        internal BehaviorGraphBlackboardView BehaviorBlackboardView => Blackboard as BehaviorGraphBlackboardView;
        internal new BehaviorAuthoringGraph Asset => base.Asset as BehaviorAuthoringGraph;

        internal delegate void OnSaveCallback();
        internal OnSaveCallback OnSave;

        // Auto-saving is set on by default.
        public bool AutoSaveIsEnabled = true;

        internal readonly Dictionary<string, VariableModel> m_RecentlyLinkedVariables = new Dictionary<string, VariableModel>();

        private readonly Dictionary<BehaviorAuthoringGraph, long> m_GraphDependencies = new Dictionary<BehaviorAuthoringGraph, long>();
        private readonly Dictionary<BehaviorBlackboardAuthoringAsset, long> m_BlackboardDependencies = new Dictionary<BehaviorBlackboardAuthoringAsset, long>();
        
        private readonly BehaviorToolbar m_BehaviorGraphToolbar;
        private readonly SubGraphStoryEditor m_StoryEditor;
        private Toast m_PlaceholderNodeWarningToast; 

        internal event Action<int> DebugAgentSelected;

        private static readonly string k_PrefsKeyDefaultGraphOwnerName = "DefaultGraphOwnerName";
        public static readonly string k_SelfDefaultGraphOwnerName = "Self";
        private static readonly string k_PrefsKeySequenceTutorialShown = "SequenceTutorialShown";
        private static readonly string k_PrefsKeyEdgeTutorialShown = "EdgeTutorialShown";
        private bool IsInEditorContext => panel?.contextType == ContextType.Editor;

        private BehaviorGraphAgentBase m_SelectedAgent;
        private DebugAgentElement m_DebugElement;
        
        private int m_PlaceholderNodeIndex;
        private int m_PlaceholderNodeCount;
        private readonly string[] kSearchFolders = new string[] { "Assets" };

        private const string kLayoutFilePath = "Packages/com.unity.behavior/Authoring/UI/AssetEditor/Assets/BehaviorLayout.uxml";
        private const string kStylesheetFilePath = "Packages/com.unity.behavior/Authoring/UI/AssetEditor/Assets/BehaviorStylesheet.uss";

        public BehaviorGraphEditor() : base(kLayoutFilePath, kStylesheetFilePath)
        {
            GraphPrefsUtility.PrefsPrefix = k_PreferencesPrefix;
            AddToClassList("Behavior");

            m_BehaviorGraphToolbar = this.Q<BehaviorToolbar>("GraphToolbar");

#if UNITY_EDITOR
            m_BehaviorGraphToolbar.OpenAssetButton.clicked += OnOpenAssetButtonClick;
#else
            m_BehaviorGraphToolbar.OpenAssetButton.RemoveFromHierarchy();
#endif
            m_StoryEditor = new SubGraphStoryEditor();
            m_BehaviorGraphToolbar.DebugButton.clicked += OnDebugButtonClicked;

            m_DebugElement = new DebugAgentElement();
            m_DebugElement.DebugToggle.RegisterValueChangedCallback(OnDebugToggleValueChanged);

            BehaviorGraphView.ViewState.ViewStateUpdated += OnGraphViewUpdated;
            RegisterCallback<LinkFieldLinkButtonEvent>(OnVariableLinkButton);
            RegisterCallback<FocusInEvent>(_ => IsAssetVersionUpToDate());
            
            BlackboardUtils.AddCustomIconName(typeof(EventChannelBase), "event");
        }

        public void OnSubgraphRepresentationButtonClicked()
        {
#if UNITY_EDITOR
            WizardStepper stepper = new WizardStepper();
            Modal modal = Modal.Build(this, stepper);
            stepper.WizardAppBar.title = "Edit Subgraph Representation";
            stepper.CloseButton.clicked += modal.Dismiss;
            stepper.Add(m_StoryEditor);
            stepper.ConfirmButton.title = "Confirm";
            stepper.ConfirmButton.clicked += modal.Dismiss;
            stepper.ConfirmButton.clicked += SaveSubgraphRepresentation;
            modal.Show();
#endif
        }

        public override void Load(GraphAsset asset)
        {
            BehaviorAuthoringGraph authoringGraph = asset as BehaviorAuthoringGraph;

            // Before loading, check for asset blackboard and the graph owner variable.
            asset.EnsureAssetHasBlackboard();
            authoringGraph.EnsureCorrectModelTypes();
            EnsureNewAssetHasSelfReferenceVariable(asset);
            
            if (authoringGraph.Story != null)
            {
                m_StoryEditor.SetAssetLink(authoringGraph);
            }
            BehaviorBlackboardView.GraphAsset = authoringGraph;
            base.Load(asset);

            if (Inspector is BehaviorInspectorView behaviorInspector)
            {
                if (behaviorInspector.InspectedNode == null)
                {
                    // Display the default inspector if nothing has been selected.
                    Inspector.CreateDefaultInspector();      
                }
                else
                {
                    Inspector.Refresh();
                }
            }

            DispatchOutstandingAssetCommands();
                
            CacheDependentAssets();
        }
        
        private void CacheDependentAssets()
        {
            m_GraphDependencies.Clear();
            m_BlackboardDependencies.Clear();
            
            if (Asset == null)
            {
                return;
            }

            foreach (BehaviorBlackboardAuthoringAsset blackboard in Asset.m_Blackboards)
            {
                m_BlackboardDependencies.TryAdd(blackboard, blackboard.VersionTimestamp);
            }

            foreach (NodeModel node in Asset.Nodes)
            {
                if (node is not SubgraphNodeModel subgraphNode)
                {
                    continue;
                }

                if (subgraphNode.SubgraphAuthoringAsset == null)
                {
                    continue;
                }
                
                m_GraphDependencies.TryAdd(subgraphNode.SubgraphAuthoringAsset, subgraphNode.SubgraphAuthoringAsset.VersionTimestamp);

                if (!subgraphNode.IsDynamic)
                {
                    m_BlackboardDependencies.TryAdd((BehaviorBlackboardAuthoringAsset)subgraphNode.SubgraphAuthoringAsset.Blackboard, subgraphNode.SubgraphAuthoringAsset.Blackboard.VersionTimestamp);
                    // Iterate through all Blackboards on the selected subgraph. 
                    foreach (BehaviorBlackboardAuthoringAsset subgraphBlackboard in subgraphNode.SubgraphAuthoringAsset.m_Blackboards)
                    {
                        m_BlackboardDependencies.TryAdd(subgraphBlackboard, subgraphBlackboard.VersionTimestamp);
                    }
                }

                else if (subgraphNode.RequiredBlackboard != null)
                {
                    m_BlackboardDependencies.TryAdd(subgraphNode.RequiredBlackboard, subgraphNode.RequiredBlackboard.VersionTimestamp);   
                }
            }
        }

        public bool HasBlackboardDependencyChanged()
        {
            foreach (KeyValuePair<BehaviorBlackboardAuthoringAsset, long> blackboardDependency in m_BlackboardDependencies)
            {
                if (blackboardDependency.Key == null)
                {
                    return true;
                }
                // Check if the cached blackboard asset version timestamp is outdated.
                if (blackboardDependency.Key.VersionTimestamp != blackboardDependency.Value)
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasGraphDependencyChanged()
        {
            foreach (KeyValuePair<BehaviorAuthoringGraph, long> graphDependency in m_GraphDependencies)
            {
                if (graphDependency.Key == null)
                {
                    return true;
                }
                // Check if the cached graph asset version timestamp is outdated.
                if (graphDependency.Key.VersionTimestamp != graphDependency.Value)
                {
                    return true;
                }
            }

            return false;
        }


        private void DispatchOutstandingAssetCommands()
        {
            if (!Asset)
            {
                return;
            }

            Asset.CommandBuffer.DispatchCommands(Dispatcher);
        }

        protected override GraphView GetOrCreateGraphView() => this.Q<BehaviorGraphView>();

        protected override BlackboardView CreateBlackboardView()
        {
            BehaviorGraphBlackboardView blackboardView = new BehaviorGraphBlackboardView(CreateBlackboardOptions);
            return blackboardView;
        }

        /// <summary>
        /// Ensures the provided GraphAsset has a 'Self' variable in its Blackboard.
        /// This variable is essential for proper graph functionality, allowing nodes to access the GameObject that owns the graph.
        /// </summary>
        private void EnsureNewAssetHasSelfReferenceVariable(GraphAsset asset)
        {
            // If this is the first time setting up graphs in the project, store the default 
            // name for the Self variable in editor preferences.
            if (panel != null && !GraphPrefsUtility.HasKey(k_PrefsKeyDefaultGraphOwnerName, IsInEditorContext))
            {
                SetDefaultGraphOwnerName(k_SelfDefaultGraphOwnerName);
            }

            if (GraphAssetProcessor.HasGraphOwnerVariable(asset.Blackboard))
            {
                return;
            }

            // For existing assets (non-zero timestamp), this indicates the 'Self' variable was 
            // accidentally deleted, which shouldn't happen anymore (pre 1.0.5).
            if (asset.VersionTimestamp != default)
            {
                Debug.LogWarning($"\"{asset.name}\" embedded blackboard lost its original Self variable. " +
                    $"This should not happen and is required to ensure that each node in the graph can access a valid GameObject property. " +
                    $"Generating a new valid Self variable now.", asset);
            }

            GraphAssetProcessor.EnsureBlackboardGraphOwnerVariable(asset.Blackboard);
        }

        /// <summary>
        /// Set the default graph owner variable name. 
        /// </summary>
        /// <param name="graphOwnerName">The name to be used for the graph owner variable.</param>
        public void SetDefaultGraphOwnerName(string graphOwnerName)
        {
            if (string.IsNullOrEmpty(graphOwnerName))
            {
                Debug.LogError( "Cannot set the graph owner name to null or empty string.");
                return;
            }
            
            GraphPrefsUtility.SetString(k_PrefsKeyDefaultGraphOwnerName, graphOwnerName, IsInEditorContext);
        }

        protected override InspectorView CreateNodeInspector()  
        {
            return new BehaviorInspectorView();
        }

        protected override void RegisterCommandHandlers()
        {
            base.RegisterCommandHandlers();

            Dispatcher.RegisterHandler<SetBlackboardVariableValueCommand, SetBlackboardVariableValueCommandHandler>();
            Dispatcher.RegisterHandler<SetNodeVariableLinkCommand, SetNodeVariableLinkCommandHandler>();
            Dispatcher.RegisterHandler<SetNodeVariableValueCommand, SetNodeVariableValueCommandHandler>();
            Dispatcher.RegisterHandler<SetConditionVariableLinkCommand, SetConditionVariableLinkCommandHandler>();
            Dispatcher.RegisterHandler<SetConditionVariableValueCommand, SetConditionVariableValueCommandHandler>();
            Dispatcher.RegisterHandler<InsertNodeCommand, InsertNodeCommandHandler>();
            Dispatcher.RegisterHandler<CreateVariableFromLinkFieldCommand, CreateVariableFromLinkFieldCommandHandler>();
            Dispatcher.RegisterHandler<CreateNodeFromSerializedTypeCommand, CreateNodeFromSerializedTypeCommandHandler>();
            Dispatcher.RegisterHandler<CreateVariableFromSerializedTypeCommand, CreateVariableFromSerializedTypeCommandHandler>();
            Dispatcher.RegisterHandler<SwapNodeFromSerializedTypeCommand, SwapNodeFromSerializedTypeCommandHandler>();
            Dispatcher.RegisterHandler<AddConditionToNodeCommand, AddConditionToNodeCommandHandler>();
            Dispatcher.RegisterHandler<AddConditionFromSerializedCommand, AddConditionFromSerializedTypeCommandHandler>();
            Dispatcher.RegisterHandler<RemoveConditionFromNodeCommand, RemoveConditionFromNodeCommandHandler>();
            Dispatcher.RegisterHandler<AddBlackboardAssetToGraphCommand, AddBlackboardAssetToGraphCommandHandler>();
            Dispatcher.RegisterHandler<RemoveBlackboardAssetFromGraphCommand, RemoveBlackboardAssetFromGraphCommandHandler>();

            // Replace the default handlers with Unity Behavior versions.
            Dispatcher.UnregisterHandler<ConnectEdgeCommand, GraphFramework.ConnectEdgeCommandHandler>();
            Dispatcher.RegisterHandler<ConnectEdgeCommand, ConnectEdgeCommandHandler>();
            Dispatcher.UnregisterHandler<CreateNodeCommand, GraphFramework.CreateNodeCommandHandler>();
            Dispatcher.RegisterHandler<CreateNodeCommand, CreateNodeCommandHandler>();
            Dispatcher.UnregisterHandler<CopyNodeCommand, GraphFramework.CopyNodeCommandHandler>();
            Dispatcher.RegisterHandler<CopyNodeCommand, CopyNodeCommandHandler>();
        }

        public override SearchMenuBuilder CreateBlackboardOptions()
        {
            SearchMenuBuilder builder = Util.CreateBlackboardOptions(Dispatcher, this, Asset.CommandBuffer);
            
#if UNITY_EDITOR
            BehaviorBlackboardAuthoringAsset[] blackboardAssets = Util.GetNonGraphBlackboardAssets();
            List<SearchView.Item> options = new List<SearchView.Item>();
            if (blackboardAssets.Length > 0)
            {
                options.Add(new SearchView.Item($"Blackboards", icon: BlackboardUtils.GetScriptableObjectIcon(blackboardAssets[0]))); 
                foreach (BehaviorBlackboardAuthoringAsset blackboardAsset in blackboardAssets)
                {
                    bool isAlreadyAdded = false;
                    foreach (BehaviorBlackboardAuthoringAsset blackboard in Asset.m_Blackboards)
                    {
                        if (blackboard.AssetID != blackboardAsset.AssetID)
                        {
                            continue;
                        }
                    
                        options.Add(new SearchView.Item($"Blackboards/{blackboardAsset.name}", enabled: false, icon: BlackboardUtils.GetScriptableObjectIcon(blackboardAsset)));
                        isAlreadyAdded = true;
                        break;
                    }

                    if (!isAlreadyAdded)
                    {
                        options.Add(new SearchView.Item($"Blackboards/{blackboardAsset.name}",  icon: BlackboardUtils.GetScriptableObjectIcon(blackboardAsset), onSelected: () => OnAddBlackboardGroup(blackboardAsset)));   
                    }
                }   
            }
            
            foreach (SearchView.Item option in options)
            {
                builder.Options.Add(option);
            }
#endif
            
            return builder;
        }
        
        private void OnAddBlackboardGroup(BlackboardAsset asset)
        {
            Dispatcher.DispatchImmediate(new AddBlackboardAssetToGraphCommand(Asset, asset as BehaviorBlackboardAuthoringAsset, true));
        }
        
        // todo move to dedicated tutorial manager
        private void OnGraphViewUpdated()
        {
            if (Asset.Nodes.LastOrDefault() is ActionNodeModel)
                CheckSequenceTutorial();

            CheckEdgeConnectionTutorial();
            RefreshPlaceholderNodeToast();
        }

        private void OnDebugButtonClicked()
        {
#if UNITY_2022_2_OR_NEWER
            BehaviorGraphAgentBase[] agents = UnityEngine.Object.FindObjectsByType<BehaviorGraphAgentBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            BehaviorGraphAgentBase[] agents = UnityEngine.Object.FindObjectsOfType<BehaviorGraphAgentBase>(true);
#endif
            if (agents == null)
                return;
            
            List<SearchView.Item> searchItems = new List<SearchView.Item>();
            List<BehaviorGraphAgentBase> matchingAgents = new List<BehaviorGraphAgentBase>();
            foreach (BehaviorGraphAgentBase agent in agents)
            {
                if (!agent.Graph || agent.Graph.m_RootGraph == null)
                {
                    continue;
                }
    
                List<BehaviorGraphModule> matchingModules = agent.Graph.Graphs.Where(module => module.AuthoringAssetID == Asset.AssetID).ToList();
                int id = 1;
                bool isEnabled = agent.isActiveAndEnabled;
                string enabledSuffix = !isEnabled ? "(Disabled)" : string.Empty;
                int priority = agent == m_SelectedAgent ? 10 : (isEnabled ? 0 : -10);
                foreach (BehaviorGraphModule module in matchingModules)
                {
                    string idString = matchingModules.Count == 1 ? string.Empty : $" (Instance {id++})";
                    string outdatedGraph = module.VersionTimestamp == BehaviorGraphView.Asset.VersionTimestamp ? string.Empty : " (Outdated)";
#if UNITY_EDITOR
                    // Some user wrap around the BehaviorGraphAgent component and they also need to debug functionality.
                    isEnabled |= BehaviorProjectSettings.instance.AllowDisabledAgentDebugging;
#endif
                    searchItems.Add(new SearchView.Item(
                        path: $"{agent.name}{idString}{outdatedGraph} {enabledSuffix}", 
                        data: (agent, module),
                        priority: priority, 
                        enabled: isEnabled)
                    );
                    matchingAgents.Add(agent);
                }
            }
            SearchView searchView = SearchWindow.Show("Debug Agent", searchItems, OnDebugTargetSelected, m_BehaviorGraphToolbar.DebugButton, 256, 244, false, false);
            searchView.Q<VisualElement>("ReturnButton").style.display = DisplayStyle.None;
            searchView.Insert(0, m_DebugElement);
            
            if (m_SelectedAgent != null)
            {
                searchView.ListView.SetSelection(0);
            }
            
            if (m_SelectedAgent == null || !matchingAgents.Contains(m_SelectedAgent))
            {
                m_SelectedAgent = null;
                searchView.ListView.SetSelection(-1);
                UnselectDebugTarget();
                m_DebugElement.ResetToggle();
            }
        }

        private void OnDebugToggleValueChanged(ChangeEvent<bool> evt)
        {
            if (evt.newValue == false)
            {
                UnselectDebugTarget();
                return;
            }

            if (m_SelectedAgent != null)
            {
                SetupDebugTarget(m_SelectedAgent);
            }
        }

        private void OnDebugTargetSelected(SearchView.Item obj)
        {
            if (obj.Data is (BehaviorGraphAgentBase agent, BehaviorGraphModule graphModule))
            {
               SetupDebugTarget(agent);
               BehaviorGraphView.ActiveDebugGraph = graphModule;
            }
            else
            {
                UnselectDebugTarget();
            }
        }

#if UNITY_EDITOR
        private void PostRuntimeDeserializationTargetRefresh()
        {
            m_SelectedAgent.OnRuntimeDeserializationEvent -= PostRuntimeDeserializationTargetRefresh;
            SetupDebugTarget(m_SelectedAgent);
        }
#endif

        private void SetupDebugTarget(BehaviorGraphAgentBase agent)
        {
            if (agent == null || !agent.Graph || agent.Graph.m_RootGraph == null)
            {
                BehaviorGraphView.ActiveDebugGraph = null;
                return;
            }

            m_SelectedAgent = agent;
#if UNITY_EDITOR
            m_SelectedAgent.OnRuntimeDeserializationEvent += PostRuntimeDeserializationTargetRefresh;
#endif
            BehaviorGraphView.ResetNodesUI();
            BehaviorGraphView.ActiveDebugGraph = agent.Graph.Graphs.FirstOrDefault(module => module.AuthoringAssetID == Asset.AssetID);

            DebugAgentSelected?.Invoke(m_SelectedAgent.GetInstanceID());
#if UNITY_EDITOR
            if (m_SelectedAgent.gameObject != null)
            {
                Selection.activeGameObject = m_SelectedAgent.gameObject;
            }
#endif
            m_DebugElement.DebugToggle.value = true;
            m_DebugElement.SetAgentToToggle(m_SelectedAgent.name, true);
        }

        private void UnselectDebugTarget()
        {
            BehaviorGraphView.ActiveDebugGraph = null;
            BehaviorGraphView.ResetNodesUI();
            DebugAgentSelected?.Invoke(0);
        }

        private void OnOpenAssetButtonClick()
        {
#if UNITY_EDITOR
            BehaviorAuthoringGraph[] assets = Util.GetBehaviorGraphAssets();
            List<SearchView.Item> searchItems = assets.Select(asset => new SearchView.Item(asset.name, data: asset, icon: BlackboardUtils.GetScriptableObjectIcon(asset))).ToList();
            SearchWindow.Show("Open Graph", searchItems,
                item => BehaviorWindowDelegate.Open(item.Data as BehaviorAuthoringGraph),
                this.Q<ActionButton>("OpenAssetButton"), 200, 300);
#endif
        }

        public override void OnAssetSave()
        {
            if (Asset == null) 
            {
                return;
            }

            var behaviorBB = Asset.Blackboard as BehaviorBlackboardAuthoringAsset;
            if (IsAssetVersionUpToDate() && behaviorBB.IsAssetVersionUpToDate())
            {
                return;
            }
            
            OnSave?.Invoke();
            SaveSubgraphRepresentation();
            base.OnAssetSave();
        }

        private void SaveSubgraphRepresentation()
        {
#if UNITY_EDITOR
            int currentUndoGroup = -1;
            string currentUndoName = string.Empty;

            void MarkUndoAndSaveDataForUndoCollapse()
            {
                Asset.MarkUndo($"Change Subgraph Representation", hasOutstandingChange: false);
                currentUndoGroup = Undo.GetCurrentGroup();
                currentUndoName = Undo.GetCurrentGroupName();
            }

            if (Asset.Story.Story != m_StoryEditor.Sentence.LastSentence)
            {
                MarkUndoAndSaveDataForUndoCollapse();
            }
#endif

            // Assign story info on asset.
            Asset.Story = new StoryInfo
            {
                Story = m_StoryEditor.Sentence.LastSentence ?? string.Empty,
                Variables = m_StoryEditor.Sentence.GetStoryVariables()
                    .Select(pair => new VariableInfo { Name = pair.Key, Type = pair.Value }).ToList()
            };

            // For each variable in the story, create a variable in the asset if it doesn't exist.
            List<VariableInfo> storyVariables = Asset.Story.Variables;
            List<VariableModel> assetVariables = Asset.Blackboard.Variables;
            foreach (VariableInfo storyVariable in storyVariables)
            {
                if (!assetVariables.Any(assetVar =>
                        string.Equals(assetVar.Name, storyVariable.Name, StringComparison.CurrentCultureIgnoreCase)
                        && assetVar.Type == (Type)storyVariable.Type))
                {
#if UNITY_EDITOR
                    // If the story hasn't changed but variable type has, register undo and prepare to collapse.
                    if (currentUndoGroup == -1)
                    {
                        MarkUndoAndSaveDataForUndoCollapse();
                    }
#endif

                    string varName = char.ToUpper(storyVariable.Name.First()) + storyVariable.Name.Substring(1);
                    Type varType = BlackboardUtils.GetVariableModelTypeForType(storyVariable.Type);
                    Dispatcher.DispatchImmediate(new CreateVariableCommand(varName, varType) { ExactName = true }, setHasOutstandingChanges: false);
                }
            }

#if UNITY_EDITOR
            if (currentUndoGroup != -1)
            {
                Undo.SetCurrentGroupName(currentUndoName);
                Undo.CollapseUndoOperations(currentUndoGroup);
            }
#endif
        }

        public override bool IsAssetVersionUpToDate()
        {
#if UNITY_EDITOR
            BehaviorGraph graph = BehaviorAuthoringGraph.GetOrCreateGraph(Asset);
            
            bool assetIsOutOfDate = false;
            if (Asset.HasOutstandingChanges)
            {
                assetIsOutOfDate = true;
            }
            // If the graph is null, the asset has not been saved/built 
            else if (graph == null)
            {
                assetIsOutOfDate = true;
            }
            // If the graph is empty of graph modules, the asset has not been saved/built.
            else if (graph.Graphs.Count == 0)
            {
                assetIsOutOfDate = true;
            }
            // If the root graph is out of date, the asset is out of date.
            // This should probably be move to the BehaviorAuthoringGraph validation layer.
            // We don't check other graphs as they are usually subgraphs that are validated independently.
            else if (graph.RootGraph.VersionTimestamp != Asset.VersionTimestamp)
            {
                assetIsOutOfDate = true;
            }

            if (assetIsOutOfDate)
            {
                if (!AutoSaveIsEnabled)
                {
                    BehaviorWindowDelegate.ShowSaveIndicator(Asset);
                }
                return false;
            }
#endif
            return true;
        }

#if UNITY_EDITOR
        protected override void OnUndoRedoPerformed()
        {
            base.OnUndoRedoPerformed();
            GraphView.Query<BehaviorNodeUI>().ForEach(nodeUI => nodeUI.UpdateLinkFields());
            // Manually refreshes subgraph node UI as their underlying graph asset might have changed.
            GraphView.Query<SubgraphNodeUI>().ForEach(subgraphNodeUI =>  subgraphNodeUI.Refresh(false));
        }
#endif

        private void OnVariableLinkButton(LinkFieldLinkButtonEvent evt)
        {
            Type variableType = evt.FieldType;
            Type variableIconType = variableType;
            if (typeof(BlackboardVariable<>).IsAssignableFrom(variableType))
            {
                variableIconType = variableIconType.GetGenericArguments()[0];
            }
            BaseLinkField field = evt.target as BaseLinkField;
            if (variableType == null)
            {
                using (LinkFieldTypeChangeEvent changeEvent = LinkFieldTypeChangeEvent.GetPooled(field, null))
                {
                    field!.SendEvent(changeEvent);
                }
                m_RecentlyLinkedVariables.Remove(field.FieldName); 
                
                return;
            }

            Texture2D icon = variableIconType.GetIcon();
            SearchMenuBuilder builder = new SearchMenuBuilder();
            if (evt.target is BehaviorLinkField<UnityEngine.Object, RuntimeObjectField> { Model: EventNodeModel })
            {
                icon = typeof(EventChannelBase).GetIcon();
#if UNITY_EDITOR
                foreach (EventChannelUtility.EventChannelInfo channelInfo in EventChannelUtility.GetEventChannelTypes())
                {
                    builder.AddOption($"Create Event Channel.../New {channelInfo.Name}", () =>
                    {
                        OnCreateFromLinkSearch(field, $"{channelInfo.Name}", channelInfo.VariableModelType);
                    }, icon: null, null, true, 1);
                }
#endif
                foreach (VariableModel variable in Asset.Blackboard.Variables.Where(v => variableType.IsAssignableFrom(v.Type)))
                {
                    builder.AddOption($"{variable.Name}",
                        () => { OnLinkFromSearcher(variable, field); }, variable.Type.GetIcon());
                }
            }
            else if (evt.target is BaseLinkField { Model: SubgraphNodeModel subgraphNode } subgraphField &&
                     (subgraphField.LinkVariableType == typeof(BehaviorGraph) || subgraphField.LinkVariableType == typeof(BehaviorBlackboardAuthoringAsset)))
            {
                if (subgraphField.LinkVariableType == typeof(BehaviorGraph))
                {
                    builder.AddOption("New Subgraph...",
                        () =>
                        {
                            OnCreateFromLinkSearch(field, $"Subgraph",
                                BlackboardUtils.GetVariableModelTypeForType(variableType));
                        }, icon: null, null, true, 1);
                    // Populate subgraph link field options from subgraph type Blackboard variables.
                    foreach (VariableModel variable in Asset.Blackboard.Variables.Where(v => typeof(BehaviorGraph).IsAssignableFrom(v.Type)))
                    {
                        builder.AddOption($"{variable.Name}",
                            () =>
                            {
                                OnLinkFromSearcher(variable, field);
                                LinkSubgraph((BehaviorGraph)variable.ObjectValue, variable.Name, subgraphNode, field, variable); 
                            }, icon: variable.Type.GetIcon());
                    }    
                }
                else if (subgraphField.LinkVariableType == typeof(BehaviorBlackboardAuthoringAsset))
                {
#if UNITY_EDITOR
                    foreach (BehaviorBlackboardAuthoringAsset blackboardAsset in Util.GetNonGraphBlackboardAssets())
                    {
                        builder.AddOption(blackboardAsset.name, () => OnBlackboardAssetSelected(blackboardAsset, field),
                            icon: BlackboardUtils.GetScriptableObjectIcon(blackboardAsset), tab: "Assets");
                    }
#endif
                }
            }
            else
            {
#if UNITY_EDITOR
                // this case is for creating a new enum variable without knowing the type upfront 
                if (variableType == typeof(Enum))
                {
                    foreach (var enumVariableType in Util.GetEnumVariableTypes())
                    {
                        builder.AddOption($"Create Enum Variable.../New {enumVariableType.Name}",
                            () =>
                            {
                                OnCreateFromLinkSearch(field, $"{enumVariableType.Name}",
                                    BlackboardUtils.GetVariableModelTypeForType(enumVariableType));
                            }, icon: null, null, true, 1);
                    }
                }
                // this case is for creating a new variable of the given enum type
                else if (variableType.IsEnum)
                {
                    builder.AddOption($"New {BlackboardUtils.GetNameForType(variableType)}...",
                        () =>
                        {
                            OnCreateFromLinkSearch(field, $"{BlackboardUtils.GetNameForType(variableType)}",
                                BlackboardUtils.GetVariableModelTypeForType(variableType));
                        }, icon: null, null, true, 1);
                }
                else if (variableType != typeof(object))
                {
                    builder.AddOption($"New {BlackboardUtils.GetNameForType(variableType)}...",
                        () =>
                        {
                            OnCreateFromLinkSearch(field, $"{BlackboardUtils.GetNameForType(variableType)}",
                                BlackboardUtils.GetVariableModelTypeForType(variableType));
                        }, icon: null, null, true, 1);
                }
#endif
                foreach (VariableModel variableDecl in Asset.Blackboard.Variables)
                {
                    if (field.IsAssignable(variableDecl.Type))
                    {
                        var targetIcon = variableDecl.Type.GetIcon();
                        if (targetIcon != null)
                        {
                            builder.AddOption($"{variableDecl.Name}",
                                () => { OnLinkFromSearcher(variableDecl, field); }, targetIcon);
                        }
                        else
                        {
                            builder.AddOption($"{variableDecl.Name}", onOptionSelected:
                                () => { OnLinkFromSearcher(variableDecl, field); }, iconName: BlackboardUtils.GetIconNameForType(variableDecl.Type));
                        }
                    }
                }
                // Check for variables in added Blackboard groups, and display them with the Blackboard asset name first.
                foreach (BehaviorBlackboardAuthoringAsset blackboard in Asset.m_Blackboards)
                {
                    foreach (VariableModel assetVariable in blackboard.Variables.Where(variableModel => field.IsAssignable(variableModel.Type) 
                        && typeof(EventChannelBase).IsAssignableFrom(variableModel.Type) == false))
                    {
                        var targetIcon = assetVariable.Type.GetIcon();
                        if (targetIcon != null)
                        {
                            builder.AddOption($"{blackboard.name} " + BlackboardUtils.GetArrowUnicode() + $" {assetVariable.Name}",
                                () => { OnLinkFromSearcher(assetVariable, field); }, targetIcon);   
                        }
                        else
                        {
                            builder.AddOption($"{blackboard.name} " + BlackboardUtils.GetArrowUnicode() + $" {assetVariable.Name}",
                                () => { OnLinkFromSearcher(assetVariable, field); }, iconName: BlackboardUtils.GetIconNameForType(assetVariable.Type));
                        }
                    }
                }
            }
            

            builder.Title = "Link Variable";

            if (evt.AllowAssetsEmbeds)
            {
                builder.DefaultTabName = "Variable";
#if UNITY_EDITOR
                if (evt.target is BaseLinkField { Model: SubgraphNodeModel subgraphNode } subgraphField)
                {
                    if (subgraphField.LinkVariableType == typeof(BehaviorGraph))
                    {
                        // Populate subgraph link field options from asset registry.
                        foreach (BehaviorAuthoringGraph subgraphAsset in BehaviorGraphAssetRegistry.GlobalRegistry.Assets)
                        {
                            // Only enable options that don't create cycles, but display the cyclic options as well.
                            bool wouldCreateCycle = subgraphAsset.ContainsCyclicReferenceTo(subgraphField.Model.Asset as BehaviorAuthoringGraph);
                            
                            builder.AddOption(subgraphAsset.name, () => LinkSubgraph(BehaviorAuthoringGraph.GetOrCreateGraph(subgraphAsset), subgraphAsset.name, subgraphNode, field),
                                icon: Util.GetBehaviorGraphIcon(), tab: "Assets", enabled: !wouldCreateCycle);
                        }   
                    }
                }

                builder.AddOption("None", () => { field.SetValue(null); }, icon: null, tab: "Assets", priority: 1);
                
                if (evt.FieldType != typeof(BehaviorBlackboardAuthoringAsset) && evt.FieldType != typeof(BehaviorGraph))
                {
                    string[] assetGUIDSArray = UnityEditor.AssetDatabase.FindAssets($"t:{evt.FieldType.Name}", kSearchFolders);
                    Texture2D thumbnail = null;
                    bool attemptedFetchThumbnail = false;
                    foreach (string assetGUID in assetGUIDSArray)
                    {
                        string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(assetGUID);
                        void OnSelectAsset()
                        {
                            UnityEngine.Object asset = UnityEditor.AssetDatabase.LoadAssetAtPath(assetPath, evt.FieldType);
                            if (asset != null)
                            {
                                field.SetValue(asset);
                            }
                        }

                        if (thumbnail == null && !attemptedFetchThumbnail)
                        {
                            attemptedFetchThumbnail = true;
                            UnityEngine.Object asset = UnityEditor.AssetDatabase.LoadAssetAtPath(assetPath, evt.FieldType);
                            if (asset != null)
                            {
                                thumbnail = UnityEditor.AssetPreview.GetMiniThumbnail(asset);
                            }
                        }
                        string filename = Path.GetFileNameWithoutExtension(assetPath);
                        builder.AddOption(filename, OnSelectAsset, tab: "Assets", icon: thumbnail);
                    }   
                }
#endif

            }
            builder.OnSelection = selection =>
            {
                if (selection.Data is SearchMenuBuilder.OnOptionSelected callback)
                {
                    callback();
                }
            };
            builder.Parent = field;
            builder.ShowIcons = true;
            builder.Width = 220;
            builder.Show();
        }

        private void OnBlackboardAssetSelected(BehaviorBlackboardAuthoringAsset blackboardAsset, BaseLinkField field)
        {
            VariableModel variable = new TypedVariableModel<BehaviorBlackboardAuthoringAsset> { m_Value = blackboardAsset, Name = blackboardAsset.name };
            OnLinkFromSearcher(variable, field);
        }

        private void LinkSubgraph(BehaviorGraph subgraph, string variableName, SubgraphNodeModel subgraphNode, BaseLinkField field, VariableModel variableModel = null)
        {
            VariableModel variable =
                // Link the selected variable.
                variableModel != null ? variableModel :
                // Or create a new variable model for the selected asset.
                new TypedVariableModel<BehaviorGraph> { m_Value = subgraph, Name = variableName};
            OnLinkFromSearcher(variable, field);

#if UNITY_EDITOR
            BehaviorAuthoringGraph subgraphAsset = BehaviorGraphAssetRegistry.TryGetAssetFromGraphPath(subgraph);

            if (subgraphAsset == null)
            {
                return;
            }
                        
            // If a graph owner variable exists in the both assets, link them.
            VariableModel assetGraphOwner = Asset.Blackboard.Variables.FirstOrDefault(variable =>
                variable.ID == BehaviorGraph.k_GraphSelfOwnerID && variable.Type == typeof(GameObject));
            VariableModel subgraphOwner = subgraphAsset.Blackboard.Variables.FirstOrDefault(variable =>
                variable.ID == BehaviorGraph.k_GraphSelfOwnerID && variable.Type == typeof(GameObject));

            if (assetGraphOwner == null || subgraphOwner == null)
            {
                return;
            }

            BehaviorGraphNodeModel.FieldModel ownerField = subgraphNode.Fields.FirstOrDefault(field =>
                field.FieldName == subgraphOwner.Name && (Type)field.Type == typeof(GameObject));
            if (ownerField != null)
            {
                ownerField.LinkedVariable = assetGraphOwner;
            }
#endif
        }

        private void OnCreateFromLinkSearch(BaseLinkField field, string variableName, Type variableDataType, params object[] creationArgs)
        {
            Dispatcher.Dispatch(new CreateVariableFromLinkFieldCommand(field, variableName, variableDataType, creationArgs));
        }

        private void OnLinkFromSearcher(VariableModel variable, BaseLinkField field)
        {
            if (variable != null)
            {
                field.LinkedVariable = variable;
                using (LinkFieldTypeChangeEvent changeEvent = LinkFieldTypeChangeEvent.GetPooled(field, variable.Type))
                {
                    field.SendEvent(changeEvent);
                }
            }

            Asset.SetAssetDirty();
            IsAssetVersionUpToDate();

            if (field.Model is BehaviorGraphNodeModel)
            {
                m_RecentlyLinkedVariables[field.FieldName] = variable;
            }
        }

        private void CheckSequenceTutorial()
        {
            if (panel == null)
            {
                // Panel isn't attached. Delay the execution of this task.
                schedule.Execute(CheckSequenceTutorial);
                return;
            }

            if (GraphPrefsUtility.HasKey(k_PrefsKeySequenceTutorialShown, IsInEditorContext))
            {
                return;
            }

            // Create the sequence tutorial only if exactly two action nodes are present.
            if (BehaviorGraphView.ViewState.Nodes.Count(node => node.Model is ActionNodeModel) != 2)
            {
                return;
            }

            TutorialUtility.CreateAndShowSequencingTutorial(this);
            GraphPrefsUtility.SetBool(k_PrefsKeySequenceTutorialShown, true, IsInEditorContext);
        }

        private void CheckEdgeConnectionTutorial()
        {
            if (panel == null)
            {
                // Panel isn't attached. Delay the execution of this task.
                schedule.Execute(CheckEdgeConnectionTutorial);
                return;
            }

            if (GraphPrefsUtility.HasKey(k_PrefsKeyEdgeTutorialShown, IsInEditorContext))
            {
                return;
            }

            // Check for exactly one node which is neither a sticky note nor a start node.
            if (BehaviorGraphView.ViewState.Nodes.Count(node => node.Model is not StickyNoteModel and not StartNodeModel) != 1) // todo
            {
                return;
            }

            TutorialUtility.CreateAndShowEdgeConnectTutorial(this);
            GraphPrefsUtility.SetBool(k_PrefsKeyEdgeTutorialShown, true, IsInEditorContext);
        }

        internal void SetActiveGraphToDebugAgent(int agentId)
        {
            BehaviorGraphAgentBase agent = GetDebugAgentInScene(agentId);
            SetupDebugTarget(agent);            
        }

        private BehaviorGraphAgentBase GetDebugAgentInScene(int id)
        {
            if (id == 0)
                 return null;

#if UNITY_2022_2_OR_NEWER
            var agents = UnityEngine.Object.FindObjectsByType<BehaviorGraphAgentBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            var agents = UnityEngine.Object.FindObjectsOfType<BehaviorGraphAgentBase>(true);
#endif
            foreach (var agent in agents)
            {
                if (agent.GetInstanceID() == id)
                    return agent;
            }

            return null;
        }

        private int GetPlaceholderNodeCount()
        {
            int count = 0;
            foreach (var node in Asset.Nodes)
            {
                if (node is PlaceholderNodeModel)
                {
                    count++;
                }
            }

            return count;
        }

        private void RefreshPlaceholderNodeToast()
        {
            var placeholderNodeCount = GetPlaceholderNodeCount();
            //prevent respawning the toast whenever the view is updated
            if (placeholderNodeCount == m_PlaceholderNodeCount)
            {
                if (placeholderNodeCount > 0 && m_PlaceholderNodeWarningToast is { isShown: true } ||
                    placeholderNodeCount == 0 && m_PlaceholderNodeWarningToast is { isShown: false })
                {
                    return;
                }
            }

            if (placeholderNodeCount == 0)
            {
                if (m_PlaceholderNodeWarningToast != null)
                {
                    m_PlaceholderNodeWarningToast.Dismiss();
                }
                return;
            }

            VisualElement editorPanel = this.Q<VisualElement>("EditorPanel");
            var text = $"{placeholderNodeCount} Placeholder node{(placeholderNodeCount <= 1 ? "" : "s")} in graph";
            m_PlaceholderNodeWarningToast = Toast.Build(editorPanel, text, Unity.AppUI.Core.NotificationDuration.Indefinite);
            var toast = m_PlaceholderNodeWarningToast;
            toast.SetIcon("info");

            // Move the toast to the top. There currently isn't a way to do it through the API.
            const int kToastTopMargin = 48; // This value includes the height of the toolbar (36px) and the distance from the toolbar to the toast's position (12px).
            toast.view.style.top = kToastTopMargin;
            toast.view.style.bottom = StyleKeyword.Auto;

            // Currently AppUI Toast automatically closes when an action is clicked, so we're hijacking their buttons to avoid that.
            /*toast.SetAction(0, placeholderNodeCount > 1 ? "Next" : "Select", () => { PanToPlaceholderNode(1); });
            if (placeholderNodeCount > 1)
            {
                toast.SetAction(1, "Previous", () => { PanToPlaceholderNode(-1); });
            }*/

            // Remove this section once we can make the Toast not dismiss automatically.
            var actionContainer = toast.view.Q<VisualElement>("appui-toast__actioncontainer");
            actionContainer.EnableInClassList(Styles.hiddenUssClassName, false);
            var divider = toast.view.Q<VisualElement>("appui-toast__divider");
            divider.EnableInClassList(Styles.hiddenUssClassName, false);
            var buttonText = placeholderNodeCount > 1 ? "Next" : "Select";
            var btn = new LocalizedTextElement { focusable = true, text = buttonText };
            btn.AddToClassList("appui-toast__action");
            btn.AddManipulator(new Pressable(() => PanToPlaceholderNode(1)));
            actionContainer.Add(btn);

            if (placeholderNodeCount > 1)
            {
                var previousButton = new LocalizedTextElement { focusable = true, text = "Previous" };
                previousButton.AddToClassList("appui-toast__action");
                previousButton.AddManipulator(new Pressable(() => PanToPlaceholderNode(-1)));
                actionContainer.Add(previousButton);
            }
            //

            toast.SetStyle(NotificationStyle.Negative);
            toast.Show();
            m_PlaceholderNodeCount = placeholderNodeCount;
        }

        public void PanToPlaceholderNode(int offset)
        {
            var allPlaceholderNodeUis = BehaviorGraphView.ViewState.Nodes.Where(nodeUI => nodeUI.Model is PlaceholderNodeModel).ToList();
          
            var nextIndex = (m_PlaceholderNodeIndex + offset) % allPlaceholderNodeUis.Count;
            nextIndex = nextIndex < 0 ? allPlaceholderNodeUis.Count - 1 : nextIndex;
            m_PlaceholderNodeIndex = nextIndex;
            
            var targetNodeUi = allPlaceholderNodeUis[nextIndex];
            BehaviorGraphView.Background.FrameElement(targetNodeUi);
            BehaviorGraphView.ViewState.SetSelected(new [] {targetNodeUi});
        }
        
        internal void LinkVariablesFromBlackboard(BehaviorGraphNodeModel node)
        {
            NodeInfo nodeInfo = NodeRegistry.GetInfoFromTypeID(node.NodeTypeID);
            
            foreach (VariableInfo nodeVariable in nodeInfo.Variables)
            {
                Type type = nodeVariable.GetType().BaseType;
                VariableModel blackboardVariable =
                    Asset.Blackboard.Variables.FirstOrDefault(v => v.Name == nodeVariable.Name && v.Type.BaseType != null && v.Type.BaseType.BaseType == type);

                if (blackboardVariable == null)
                {
                    continue;
                }
                node.SetField(nodeVariable.Name, blackboardVariable, blackboardVariable.Type);
            }
        }
        
        internal void LinkRecentlyLinkedFields(BehaviorGraphNodeModel node)
        {
            if (m_RecentlyLinkedVariables == null || m_RecentlyLinkedVariables.Count == 0)
            {
                return;
            }
            
            NodeInfo nodeInfo = NodeRegistry.GetInfoFromTypeID(node.NodeTypeID);

            foreach (VariableInfo nodeVariable in nodeInfo.Variables)
            {
                if (!m_RecentlyLinkedVariables.TryGetValue(nodeVariable.Name, out VariableModel variable))
                {
                    continue;
                }
                if (Asset.Blackboard.Variables.Contains(variable) && node.HasField(nodeVariable.Name, nodeVariable.Type))
                {
                    node.SetField(nodeVariable.Name, variable, nodeVariable.Type);   
                }
                else
                {
                    m_RecentlyLinkedVariables.Remove(nodeVariable.Name);
                }
            }
        }
    }
}