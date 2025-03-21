using UnityEngine.AI;

namespace Unity.Behavior
{
    public static class NavMeshAgentExtensions
    {
        public static bool IsNavigationComplete(this NavMeshAgent agent)
        {
            return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance;
        }
    }
}