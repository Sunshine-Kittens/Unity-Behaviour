using Unity.Behavior.GraphFramework;

using UnityEngine.UIElements;

namespace Unity.Behavior
{
    internal class RuntimeStructField<T> : BaseField<T>
    {
        public RuntimeStructField() : this(null) { }

        public RuntimeStructField(string label) : base(label, null)
        {
            AddToClassList("Runtime-Object-Field");
            styleSheets.Add(ResourceLoadAPI.Load<StyleSheet>("Packages/com.unity.behavior/Elements/Assets/RuntimeObjectFieldStyles.uss"));

            labelElement.focusable = false;

            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
        }
    }

    internal class StructLinkField<TValueType> : LinkField<TValueType, RuntimeStructField<TValueType>> { }
}