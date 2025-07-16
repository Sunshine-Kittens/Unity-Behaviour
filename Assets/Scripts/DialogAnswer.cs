using System;
using System.Collections.Generic;
using Unity.Behavior.GraphFramework;
using UnityEngine.UIElements;
using TextField = Unity.AppUI.UI.TextField;
using System.Collections.Generic;
using Unity.Behavior.GraphFramework;
using UnityEditor;


[Serializable]
public struct DialogueAnswer
{
    public string Id;
    public string Message;
}


[VariableUI(typeof(TypedVariableModel<List<DialogueAnswer>>))]
internal sealed class DialogueAnswerListVariableElement
    : TypedListVariableElement<DialogueAnswer, DialogueAnswer>   // <TValue , TBase>
{
    public DialogueAnswerListVariableElement(
            BlackboardView view,
            VariableModel  variableModel,
            bool           isEditable)
        : base(view, variableModel, isEditable, typeof(DialogueAnswerField))
    { }
}


#if UNITY_EDITOR

[InitializeOnLoad]
internal static class DialogueAnswerVariableUIBootstrap
{
    static DialogueAnswerVariableUIBootstrap()
    {
        NodeRegistry.RegisterVariableModelUI(
            typeof(TypedVariableModel<List<DialogueAnswer>>),
            typeof(DialogueAnswerListVariableElement));
    }
}
#endif

internal sealed class DialogueAnswerField : VisualElement, INotifyValueChanged<DialogueAnswer>
{
    private readonly TextField _idField;
    private readonly TextField _messageField;
    private DialogueAnswer _value;

    public DialogueAnswerField()
    {
        _idField     = new TextField("Id");
        _messageField = new TextField("Message");

        Add(_idField);
        Add(_messageField);

        _idField.RegisterValueChangedCallback(OnIdChanged);
        _messageField.RegisterValueChangedCallback(OnMessageChanged);
    }

    public DialogueAnswer value
    {
        get => _value;
        set => SetValueWithoutNotify(value);
    }

    public void SetValueWithoutNotify(DialogueAnswer newValue)
    {
        _value = newValue;
        _idField.SetValueWithoutNotify(newValue.Id);
        _messageField.SetValueWithoutNotify(newValue.Message);
    }

    private void OnIdChanged(ChangeEvent<string> evt)
    {
        DialogueAnswer tmp = _value;
        tmp.Id = evt.newValue;
        value = tmp;
        using ChangeEvent<DialogueAnswer> change = ChangeEvent<DialogueAnswer>.GetPooled(_value, tmp);
        change.target = this;
        SendEvent(change);
    }

    private void OnMessageChanged(ChangeEvent<string> evt)
    {
        DialogueAnswer tmp = _value;
        tmp.Message = evt.newValue;
        value = tmp;
        using ChangeEvent<DialogueAnswer> change = ChangeEvent<DialogueAnswer>.GetPooled(_value, tmp);
        change.target = this;
        SendEvent(change);
    }
}
