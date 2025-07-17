namespace Unity.Behavior.GraphFramework
{
    public interface IVariableLink
    {
        public object Value { get; set; }
        public VariableModel BlackboardVariable { get; set; }
    }
}
