using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Behavior
{
    public abstract class StructFieldBase : VisualElement
    {
        public StructFieldBase()
        {
            style.width = Length.Percent(100.0F);
        }

        public VisualElement CreateLabelledField(string label, float labelMinSize, VisualElement fieldElement)
        {
            VisualElement element = new VisualElement();
            element.style.alignItems = Align.FlexStart;
            element.style.flexDirection = FlexDirection.Row;

            Label labelElement = new Label(label);
            labelElement.style.alignSelf = Align.Center;
            labelElement.style.minWidth = new StyleLength(labelMinSize);
            element.hierarchy.Add(labelElement);

            fieldElement.style.flexGrow = 1;
            element.hierarchy.Add(fieldElement);
            return element;
        }
    }
}
