using System.Threading.Tasks;
using Unity.Behavior;
using UnityEngine;
using Status = Unity.Behavior.Node.Status;

public class DebugGraphAgent : BehaviorGraphAgentBase
{
    public async Awaitable AwaitGraphEnd() {
        Debug.Log(Graph.RootGraph.Root.CurrentStatus);
        while(IsRunning && Graph.RootGraph.Root is { CurrentStatus: Status.Uninitialized or Status.Running or Status.Waiting }) {
            UpdateGraph();
            await Task.Yield();
        }
    }
}
