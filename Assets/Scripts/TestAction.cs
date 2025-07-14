using System;
using System.Collections.Generic;
using Unity.AppUI.UI;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Test", story: "[AgentSomething]", category: "Action", id: "3cec54c1b7e847f74052729bbf970da5")]
public partial class TestAction : Action
{
    [SerializeReference] public BlackboardVariable<List<Dialog>> AgentSomething;

    protected override Status OnStart()
    {
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

