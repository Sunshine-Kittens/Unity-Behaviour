using System;
using UnityEngine.UIElements;
using TextField = Unity.AppUI.UI.TextField;

[Serializable]
public struct DialogueAnswer
{
    public string Id;
    public string Message;
}


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
