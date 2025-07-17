using System;

namespace Unity.Behavior
{
    /// <summary>
    ///  The attribute specified when creating Blackboard structs.
    ///  Apply this attribute above newly created structs to ensure they can be recognized and parsed by the Blackboard.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct)]
    public class BlackboardStructAttribute : Attribute
    {
    }
}