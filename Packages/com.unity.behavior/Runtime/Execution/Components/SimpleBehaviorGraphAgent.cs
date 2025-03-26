using UnityEngine;

namespace Unity.Behavior
{
    /// <summary>
    /// Behavior agent component.
    /// </summary>
    [AddComponentMenu("Behavior Graph/Simple Behavior Agent")]
    public class SimpleBehaviorGraphAgent : BehaviorGraphAgentBase
    {
        protected override BehaviorGraph GetGraphInstance()
        {
            return Graph;
        }
    }
}