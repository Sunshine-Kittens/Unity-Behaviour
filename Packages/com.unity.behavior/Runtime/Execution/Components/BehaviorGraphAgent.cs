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

        /// <summary>
        /// Begins execution of the agent's behavior graph.
        /// </summary>
        public void StartGraph()
        {
            _ = StartGraphInternal();
        }

        /// <summary>
        /// Restarts the execution of the agent's behavior graph.
        /// </summary>
        public void RestartGraph()
        {
            _ = RestartGraphInternal();
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