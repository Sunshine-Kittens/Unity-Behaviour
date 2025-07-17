#if UNITY_EDITOR
using System.IO;

using UnityEditor;

namespace Unity.Behavior
{
    internal static class StructGeneratorUtility
    {
        internal static bool CreateStructAsset(string name)
        {
            string suggestedSavePath = Util.GetAbsolutePathToProjectAssets(BehaviorProjectSettings.instance.SaveFolderEnum);
            var path = EditorUtility.SaveFilePanel($"Create Struct \"{name}\"", suggestedSavePath, name, "cs");

            if (path.Length == 0)
            {
                return false;
            }
            if (BehaviorProjectSettings.instance.AutoSaveLastSaveLocation)
            {
                BehaviorProjectSettings.instance.SaveFolderEnum = Path.GetDirectoryName(path);
            }

            using (var outfile = new StreamWriter(path))
            {
                // Using defines
                outfile.WriteLine("using System;");
                outfile.WriteLine("using System.Collections.Generic;");
                outfile.WriteLine("");
                outfile.WriteLine("using Unity.Behavior;");
                outfile.WriteLine("using Unity.Behavior.GraphFramework;");
                outfile.WriteLine("");
                outfile.WriteLine("using UnityEngine.UIElements;");
                outfile.WriteLine("");
                outfile.WriteLine("using TextField = Unity.AppUI.UI.TextField;");
                outfile.WriteLine("");

                // Struct def
                outfile.WriteLine("[Serializable]");
                outfile.WriteLine("[BlackboardStruct]");
                outfile.WriteLine($"public struct {name}");
                outfile.WriteLine("{");
                outfile.WriteLine($"    // Populate with Fields and Properties.");
                outfile.WriteLine($"    public string ReplaceMe;");
                outfile.WriteLine("");
                outfile.WriteLine($"    public {name}(string replaceMe)");
                outfile.WriteLine("    {");
                outfile.WriteLine("        ReplaceMe = replaceMe;");
                outfile.WriteLine("    }");
                outfile.WriteLine("}");

                // Empty line
                outfile.WriteLine("");

                // VisualElement def

                outfile.WriteLine($"public sealed class {name}BlackboardField : StructFieldBase, INotifyValueChanged<{name}>");
                outfile.WriteLine("{");
                outfile.WriteLine("    private readonly TextField _replaceMeTextField;");
                outfile.WriteLine("");
                outfile.WriteLine($"    public {name} value");
                outfile.WriteLine("    {");
                outfile.WriteLine("        get => _value;");
                outfile.WriteLine("        set => SetValueWithoutNotify(value);");
                outfile.WriteLine("    }");
                outfile.WriteLine("");
                outfile.WriteLine($"    private {name} _value;");
                outfile.WriteLine("");
                outfile.WriteLine($"    public {name}BlackboardField() : base()");
                outfile.WriteLine("    {");
                outfile.WriteLine("        // Build VisualElement content.");                
                outfile.WriteLine("        _replaceMeTextField = new TextField();");
                outfile.WriteLine("        _replaceMeTextField.placeholder = \"Replace me...\";");
                outfile.WriteLine("        VisualElement replaceMeElement = CreateLabelledField(\"Replace Me\", 60.0F, _replaceMeTextField);");
                outfile.WriteLine("        hierarchy.Add(replaceMeElement);");
                outfile.WriteLine("");
                outfile.WriteLine("        _replaceMeTextField.RegisterValueChangedCallback(OnReplaceMeChanged);");
                outfile.WriteLine("    }");
                outfile.WriteLine("");
                outfile.WriteLine($"    public void SetValueWithoutNotify({name} newValue)");
                outfile.WriteLine("    {");
                outfile.WriteLine("        _value = newValue;");
                outfile.WriteLine("        _replaceMeTextField.SetValueWithoutNotify(newValue.ReplaceMe);");
                outfile.WriteLine("    }");
                outfile.WriteLine("");
                outfile.WriteLine("    private void OnReplaceMeChanged(ChangeEvent<string> evt)");
                outfile.WriteLine("    {");
                outfile.WriteLine($"        {name} temp = _value;");
                outfile.WriteLine("        temp.ReplaceMe = evt.newValue;");
                outfile.WriteLine("        value = temp;");
                outfile.WriteLine($"        using ChangeEvent<{name}> change = ChangeEvent<{name}>.GetPooled(_value, temp);");
                outfile.WriteLine("        change.target = this;");
                outfile.WriteLine("        SendEvent(change);");
                outfile.WriteLine("    }");
                outfile.WriteLine("}");

                // Empty line
                outfile.WriteLine("");

                // VariableElement def
                outfile.WriteLine($"[VariableUI(typeof(TypedVariableModel<{name}>))]");
                outfile.WriteLine($"public class {name}VariableElement : TypedVariableElement<{name}, {name}BlackboardField>");
                outfile.WriteLine("{");
                outfile.WriteLine($"    public {name}VariableElement(BlackboardView view, VariableModel variableModel, bool isEditable) : base(view, variableModel, isEditable) {{ }}");
                outfile.WriteLine("}");

                // Empty line
                outfile.WriteLine("");

                // ListVariableElement def
                outfile.WriteLine($"[VariableUI(typeof(TypedVariableModel<List<{name}>>))]");
                outfile.WriteLine($"public class {name}ListVariableElement : TypedListVariableElement<{name}, {name}>");
                outfile.WriteLine("{");
                outfile.WriteLine($"    public {name}ListVariableElement(BlackboardView view, VariableModel variableModel, bool isEditable) : base(view, variableModel, isEditable, typeof({name}BlackboardField)) {{ }}");
                outfile.WriteLine("}");
            }
            AssetDatabase.Refresh();
            return true;
        }
    }
}
#endif