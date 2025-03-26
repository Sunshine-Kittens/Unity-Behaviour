using Unity.Behavior;
using UnityEngine;

public class FlowController : MonoBehaviour
{
    public DebugGraphAgent Agent;
    public BehaviorGraph Graph;

    async void Start()
    {
        Debug.Log(Graph.IsRunning);

        Agent.Graph = Graph;
        Agent.StartGraph();
        await Agent.AwaitGraphEnd();

        Agent.Graph.BlackboardReference.GetVariableValue("Test", out string result);
        Debug.Log(result);
        
    }

    void OnDestroy()
    {
        Agent.EndGraph();
    }
}
