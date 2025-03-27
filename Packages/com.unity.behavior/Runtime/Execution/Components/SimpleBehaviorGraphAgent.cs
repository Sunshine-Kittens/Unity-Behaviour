using System.Threading;

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

        /// <summary>
        /// Begins execution of the agent's behavior graph.
        /// Awaits the execution of the graph ending
        /// </summary>
        public async Awaitable StartGraphAsync(bool performUpdate = true)
        {
            if (StartGraphInternal())
            {
                CancellationToken cancellationToken = GraphCancellationToken;
                while (Graph.IsRunning && !cancellationToken.IsCancellationRequested)
                {
                    await Awaitable.NextFrameAsync();
                    if (performUpdate)
                    {
                        UpdateGraph();
                    }
                }
            }
        }

        /// <summary>
        /// Restarts the execution of the agent's behavior graph.
        /// Awaits the execution of the graph ending
        /// </summary>
        public async Awaitable RetartGraphAsync(bool performUpdate = true)
        {
            if (RestartGraphInternal())
            {
                CancellationToken cancellationToken = GraphCancellationToken;
                while (Graph.IsRunning && !cancellationToken.IsCancellationRequested)
                {
                    await Awaitable.NextFrameAsync();
                    if (performUpdate)
                    {
                        UpdateGraph();
                    }
                }
            }
        }
    }
}