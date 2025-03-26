using UnityEngine;

namespace Unity.Behavior
{
    /// <summary>
    /// <para>Manages a behavior graph's lifecycle on a GameObject and handles data through blackboard variables.</para>
    /// <para>The BehaviorGraphAgent maintains the following lifecycle states:</para>
    /// <para>- <b>Uninitialized</b> - The graph has been assigned but not instantiated yet</para>
    /// <para>- <b>Initialized</b> - The graph has been instantiated with a unique copy for this agent</para>
    /// <para>- <b>Started</b> - The graph has started running</para>
    /// <para>- <b>Running</b> - The graph is being updated each frame via Tick()</para>
    /// <para>- <b>Ended</b> - The graph has been stopped and is no longer running</para>
    ///
    /// <para><b>Initialization Sequence:</b></para>
    /// <para>- When a graph is assigned in the Inspector, it's automatically initialized during Awake()</para>
    /// <para>- When assigning a graph via the Graph property at runtime, it's automatically initialized during the next Update()</para>
    /// <para>- You can also explicitly control initialization by calling Init() manually</para>
    ///
    /// <para><b>Blackboard Variable Handling:</b></para>
    /// <para>- Before initialization: SetVariableValue() sets agent-level overrides (visible in the Inspector)</para>
    /// <para>- After initialization: SetVariableValue() sets values in the instanced graph's blackboard</para>
    /// </summary>
    /// <example>
    /// <para><b>Common Usage Patterns:</b></para>
    /// <code>
    /// // Basic usage - assign graph and configure at runtime
    /// agent.Graph = myBehaviorGraph;  // Graph will auto-initialize next Update
    /// agent.SetVariableValue("Destination", targetPosition);
    /// 
    /// // Template pattern - configure, then instantiate multiple agents
    /// templateAgent.Graph = sharedGraph;
    /// templateAgent.SetVariableValue("Speed", defaultSpeed);  // Sets override
    /// 
    /// var newAgent = Instantiate(templateAgent);
    /// newAgent.Init();  // Explicitly initialize
    /// newAgent.SetVariableValue("PatrolPoints", uniquePatrolPoints);  // Per-instance value
    /// </code>
    /// </example>
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