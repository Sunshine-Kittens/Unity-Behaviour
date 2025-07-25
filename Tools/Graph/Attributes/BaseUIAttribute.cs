using System;

namespace Unity.Behavior.GraphFramework
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class BaseUIAttribute : System.Attribute
    {
        public Type Type { get; }
        public BaseUIAttribute(Type modelType)
        {
            Type = modelType;
        }
    }
}
