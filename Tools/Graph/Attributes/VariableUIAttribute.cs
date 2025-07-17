using System;

namespace Unity.Behavior.GraphFramework
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class VariableUIAttribute : BaseUIAttribute
    {
        public VariableUIAttribute(Type variableModelType) : base(variableModelType)
        {
        }
    }
}
