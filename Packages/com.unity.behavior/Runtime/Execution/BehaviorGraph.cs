using System;
using System.Collections.Generic;
using Unity.Behavior.GraphFramework;
using Unity.Properties;
using UnityEngine;
using Status = Unity.Behavior.Node.Status;

namespace Unity.Behavior
{
    /// <summary>
    /// BehaviorGraph holds all the runtime graph instances linked together into a complete behaviour
    /// defined within a BehaviorAuthoringGraph.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    public partial class BehaviorGraph : ScriptableObject, ISerializationCallbackReceiver
    {
        internal static readonly SerializableGUID k_GraphSelfOwnerID = new SerializableGUID(1, 0);

        /// <summary>
        /// The blackboard reference used for accessing variables.
        /// </summary>
        public BlackboardReference BlackboardReference => m_RootGraph?.BlackboardReference;
        
        /// <summary>
        /// True if the graph is running, false otherwise.
        /// </summary>
        public bool IsRunning => m_RootGraph?.Root is { CurrentStatus: Status.Running or Status.Waiting };

        /// <summary>
        /// Root node of the RootGraph BehaviorGraphModule.
        /// </summary>
        public Node RootNode
        {
            get
            {
                if(m_RootGraph != null)
                {
                    return m_RootGraph.Root;
                }
                return null;
            }
        }

        /// <summary>
        /// The set of linked graphs that make up the behaviour.
        /// </summary>
        [SerializeReference]
        internal List<BehaviorGraphModule> Graphs = new();

        /// <summary>
        /// The primary entry point for the behaviour defined by the BehaviorAuthoringGraph.
        /// </summary>
        [SerializeReference]
        internal BehaviorGraphModule m_RootGraph;
        public BehaviorGraphModule RootGraph => m_RootGraph;

        [SerializeReference, DontCreateProperty]
        internal BehaviorGraphDebugInfo m_DebugInfo;

        /// <summary>
        /// Begins execution of the behavior graph.
        /// </summary>
        public void Start()
        {
            if (m_RootGraph?.Root == null)
            {
                return;
            }
            m_RootGraph.StartNode(m_RootGraph.Root);
        }

        /// <summary>
        /// Executes one step of the graph.
        /// </summary>
        public void Tick()
        {
            m_RootGraph?.Tick();
        }

        /// <summary>
        /// Ends the execution of the behavior graph.
        /// </summary>
        public void End()
        {
            if (m_RootGraph?.Root == null)
            {
                return;
            }
            m_RootGraph.EndNode(m_RootGraph.Root);
            foreach (BehaviorGraphModule graphModule in Graphs)
            {
                graphModule.Reset();
            }
        }

        /// <summary>
        /// Resets the execution state and restarts the graph.
        /// </summary>
        public void Restart()
        {
            End();
            Start();
        }

        internal void AssignGameObjectToGraphModules(GameObject gameObject)
        {
            if (m_RootGraph == null)
            {
                return;
            }

            m_RootGraph.GameObject = gameObject;
            foreach (var graphModule in Graphs)
            {
                graphModule.GameObject = gameObject;
            }
        }
        
        /// <summary>
        /// Raise OnRuntimeSerialize in each BehaviorGraphModule to notify nodes.
        /// </summary>
        internal void SerializeGraphModules()
        {
            for (int i = Graphs.Count - 1; i >= 0; i--)
            {
                Graphs[i].Serialize();
            }
        }

        /// <summary>
        /// Raise OnRuntimeDeserialize in each BehaviorGraphModule to notify nodes.
        /// </summary>
        internal void DeserializeGraphModules()
        {
            for (int i = Graphs.Count - 1; i >= 0; i--)
            {
                Graphs[i].Deserialize();
            }
        }

        /// <inheritdoc cref="OnBeforeSerialize"/>
        public void OnBeforeSerialize()
        {
        }

        /// <inheritdoc cref="OnAfterDeserialize"/>
        public void OnAfterDeserialize()
        {
#if DEBUG && UNITY_EDITOR
            foreach (BehaviorGraphModule graph in Graphs)
            {
                graph.DebugInfo = m_DebugInfo;
            }
#endif
        }
    }
}