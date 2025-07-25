using UnityEngine.UIElements;

namespace Unity.Behavior.GraphFramework 
{
    public class NodeInspectorUI : VisualElement
    {
        public NodeModel InspectedNode { get; internal set; }
        public NodeInspectorUI(NodeModel nodeModel)
        {
            InspectedNode = nodeModel;

            AddToClassList("NodeInspectorUI");
        }
        
        public virtual void Refresh()
        {

        }
    }
}
