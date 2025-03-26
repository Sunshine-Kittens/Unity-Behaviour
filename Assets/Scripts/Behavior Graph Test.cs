using Unity.Behavior;

using UnityEngine;

public class BehaviorGraphTest : MonoBehaviour
{
    [SerializeField] private SimpleBehaviorGraphAgent _agent = null;

    [SerializeField] private BehaviorGraph _graph1 = null;
    [SerializeField] private BehaviorGraph _graph2 = null;

    private BlackboardVariable _variable = null;

    private async void Start()
    {
        await Test(_graph1, _agent, "Graph 1", "Test");
        await Test(_graph2, _agent, "Graph 2", "Test");
    }

    private async Awaitable Test(BehaviorGraph graph, SimpleBehaviorGraphAgent agent, string graphName, string variableName)
    {
        graph.BlackboardReference.GetVariable(variableName, out _variable);
        Debug.Log($"{graphName} {variableName} initial value: {_variable.ObjectValue.ToString()}");

        graph.BlackboardReference.SetVariableValue(variableName, "xyz");
        graph.BlackboardReference.GetVariable(variableName, out _variable);
        Debug.Log($"{graphName} {variableName} updated value before setting graph: {_variable.ObjectValue.ToString()}");

        agent.Graph = graph;

        graph.BlackboardReference.GetVariable(variableName, out _variable);
        Debug.Log($"{graphName} {variableName} agent value before initializing graph: {_variable.ObjectValue.ToString()}");

        agent.InitGraph();

        agent.GetVariable(variableName, out _variable);
        Debug.Log($"{graphName} {variableName} agent value after setting & initializing graph: {_variable.ObjectValue.ToString()}");

        agent.StartGraph();
        while (agent.Graph.IsRunning)
        {
            agent.UpdateGraph();
            await Awaitable.NextFrameAsync();
        }

        agent.GetVariable(variableName, out _variable);
        Debug.Log($"{graphName} {variableName} agent value after running graph: {_variable.ObjectValue.ToString()}");

        graph.BlackboardReference.GetVariable(variableName, out _variable);
        Debug.Log($"{graphName} {variableName} original graph value after running graph: {_variable.ObjectValue.ToString()}");
    }
}
