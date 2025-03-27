using UnityEngine;

namespace Unity.Behavior
{
    /// <summary>
    /// Behavior agent component.
    /// </summary>
    [AddComponentMenu("AI/Behavior Agent")]
    public class BehaviorGraphAgent : BehaviorGraphAgentBase
    {
        protected override BehaviorGraph GetGraphInstance()
        {
            return ScriptableObject.Instantiate(Graph);
        }

        private void Start()
        {
            StartGraph();
        }

        private void Update()
        {
            UpdateGraph();
        }
    }
}