using UnityEngine;

namespace Unity.Behavior
{
    /// <summary>
    /// Behavior agent component.
    /// </summary>
    [AddComponentMenu("AI/Behavior Agent")]
    public class BehaviorGraphAgent : BehaviorGraphAgentBase
    {
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