using System;
using System.Collections.Generic;
using Unity.Behavior.GraphFramework;
using UnityEngine;
using UnityEngine.Audio;

#if UNITY_EDITOR
using System.Linq;
#else
using System.Reflection;
#endif

namespace Unity.Behavior
{
    internal class BlackboardRegistry
    {
        public static List<BlackboardOption> GetDefaultBlackboardOptions()
        {
            return new List<BlackboardOption>
            {
                new BlackboardOption(typeof(GameObject), "GameObject", "object", priority: 3),
                new BlackboardOption(typeof(Transform), "Transform", "transform", priority: 2),
                new BlackboardOption(typeof(BehaviorGraph), "Subgraph", "object", priority: 1),

                new BlackboardOption(typeof(string), "Basic Types/String"),
                new BlackboardOption(typeof(float), "Basic Types/Float"),
                new BlackboardOption(typeof(int), "Basic Types/Integer"),
                new BlackboardOption(typeof(double), "Basic Types/Double"),
                new BlackboardOption(typeof(bool), "Basic Types/Boolean"),
                new BlackboardOption(typeof(Vector2), "Vector Types/Vector2"),
                new BlackboardOption(typeof(Vector3), "Vector Types/Vector3"),
                new BlackboardOption(typeof(Vector4), "Vector Types/Vector4"),
                new BlackboardOption(typeof(Vector2Int), "Vector Types/Vector2 Int"),
                new BlackboardOption(typeof(Vector3Int), "Vector Types/Vector3 Int"),
                new BlackboardOption(typeof(Color), "Basic Types/Color"),

                // Resource Types
                new BlackboardOption(typeof(ScriptableObject), "Resources/Scriptable Object"),
                new BlackboardOption(typeof(Texture2D), "Resources/Texture2D"),
                new BlackboardOption(typeof(Sprite), "Resources/Sprite"),
                new BlackboardOption(typeof(Material), "Resources/Material"),
                new BlackboardOption(typeof(AudioClip), "Resources/Audio Clip"),
                new BlackboardOption(typeof(AudioResource), "Resources/Audio Resource"),
                new BlackboardOption(typeof(AnimationClip), "Resources/Animation Clip"),
                new BlackboardOption(typeof(AudioMixer), "Resources/Audio Mixer"),
                new BlackboardOption(typeof(TextAsset), "Resources/Text Asset"),
                new BlackboardOption(typeof(ParticleSystem), "Resources/Particle System"),
              
                // List Types
                new BlackboardOption(typeof(List<GameObject>), "List/Game Object List"),
                new BlackboardOption(typeof(List<string>), "List/String List"),
                new BlackboardOption(typeof(List<float>), "List/Float List"),
                new BlackboardOption(typeof(List<int>), "List/Integer List"),
                new BlackboardOption(typeof(List<double>), "List/Double List"),
                new BlackboardOption(typeof(List<bool>), "List/Boolean List"),
                new BlackboardOption(typeof(List<Vector2>), "List/Vector2 List"),
                new BlackboardOption(typeof(List<Vector3>), "List/Vector3 List"),
                new BlackboardOption(typeof(List<Vector4>), "List/Vector4 List"),
                new BlackboardOption(typeof(List<Vector2Int>), "List/Vector2 Int List"),
                new BlackboardOption(typeof(List<Vector3Int>), "List/Vector3 Int List"),
                new BlackboardOption(typeof(List<Color>), "List/Color List"),
            };
        }

        public static List<BlackboardOption> GetComparsionOperators() {
            return new List<BlackboardOption>
            {
                new BlackboardOption(typeof(ConditionOperator), "Comparsion Operators/Operator (All)"),
                new BlackboardOption(typeof(BooleanOperator), "Comparsion Operators/Operator (Boolean)"),
            };
        }

        public static List<BlackboardOption> GetEnumVariableTypes()
        {
            var enumOptions = new List<BlackboardOption>();
#if UNITY_EDITOR
            var enumTypes = UnityEditor.TypeCache.GetTypesWithAttribute<BlackboardEnumAttribute>()
                .Where(type => type.IsEnum && Enum.GetValues(type).Length > 0);
            foreach (var type in enumTypes)
            {
                enumOptions.Add(new BlackboardOption(type, "Enumeration/" + Util.NicifyVariableName(type.Name), icon: "enum"));
            }
#else
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    // we don't consider empty enum types
                    if (!type.IsEnum || type.GetCustomAttribute<BlackboardEnumAttribute>() == null || Enum.GetValues(type).Length == 0)
                    {
                        continue;
                    }
                    enumOptions.Add(new BlackboardOption(type, "Enumeration/" + Util.NicifyVariableName(type.Name)));
                }
            }
#endif
            return enumOptions;
        }

        public static List<BlackboardOption> GetStructVariableTypes()
        {
            bool IsStruct(Type type)
            {
                return type.IsValueType && !type.IsPrimitive && !type.IsEnum;
            }

            var structOptions = new List<BlackboardOption>();
#if UNITY_EDITOR
            var structTypes = UnityEditor.TypeCache.GetTypesWithAttribute<BlackboardStructAttribute>()
                .Where(type => IsStruct(type));
            foreach (var type in structTypes)
            {
                structOptions.Add(new BlackboardOption(type, "Structs/" + Util.NicifyVariableName(type.Name), icon: "enum"));
                Type listType = typeof(List<>).MakeGenericType(type);
                structOptions.Add(new BlackboardOption(listType, "List/" + Util.NicifyVariableName(type.Name) + " List", icon: "enum"));
            }
#else
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    // we don't consider non struct derrived types
                    if (!IsStruct(type) || type.GetCustomAttribute<BlackboardStructAttribute>() == null)
                    {
                        continue;
                    }
                    structOptions.Add(new BlackboardOption(type, "Structs/" + Util.NicifyVariableName(type.Name)));
                    Type listType = typeof(List<>).MakeGenericType(type);
                    structOptions.Add(new BlackboardOption(listType, "List/" + Util.NicifyVariableName(type.Name) + " List", icon: "enum"));
                }
            }
#endif
            return structOptions;
        }

        public static List<BlackboardOption> GetStoryVariableTypes()
        {
            var options = GetDefaultBlackboardOptions();
            var enums = GetEnumVariableTypes();
            var structs = GetStructVariableTypes();
            options.Insert(0, new BlackboardOption { Type = typeof(RegularText) , Path = "Regular Text" });

            AddCustomTypes<Behaviour>(options, "Other/MonoBehaviours");
            AddCustomTypes<ScriptableObject>(options, "Other/ScriptableObjects");

            options.AddRange(enums);
            options.AddRange(structs);

            return options;
        }

        public static List<BlackboardOption> GetStoryVariableTypesWithOperators()
        {
            var options = GetStoryVariableTypes();
            var operators = GetComparsionOperators();
            options.AddRange(operators);

            return options;
        }

        private static List<BlackboardOption> m_CustomVariableOptions = null;
        public static List<BlackboardOption> GetCustomTypes()
        {
            if (m_CustomVariableOptions == null)
            {
                m_CustomVariableOptions = new List<BlackboardOption>();
                AddCustomTypes<Component>(m_CustomVariableOptions, "MonoBehaviours");
                AddCustomTypes<ScriptableObject>(m_CustomVariableOptions, "ScriptableObjects");
            }
            return m_CustomVariableOptions;
        }


        public static List<BlackboardOption> AddCustomTypes<TypeName>(List<BlackboardOption> options, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = "";
            }
            else
            {
                path += "/";
            }
#if UNITY_EDITOR
            var monobehaviourTypes = UnityEditor.TypeCache.GetTypesDerivedFrom<TypeName>();
            foreach (var type in monobehaviourTypes)
            {
#else
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
#endif
                if (type.IsNotPublic || !type.IsVisible || IsExcludedNamespaceOrType(type))
                {
                    continue;
                }
                string namespacePath = type.Namespace?.Replace('.', '/');
                if (!string.IsNullOrEmpty(namespacePath))
                {
                    namespacePath += "/";
                }

                var icon = type.GetIcon();
                options.Add(new BlackboardOption(type, $"{ path }{namespacePath}{ Util.NicifyVariableName(type.Name, detectAbbreviation: true) }", iconImage: icon));
            }
#if !UNITY_EDITOR
            }
#endif
                return options;
        }

        private static readonly string[] kExcludedNamespaces =
        {
            "Editor",
            "Muse",
            "AppUI"
        };

        private static readonly string[] kExcludedNamespacesExact =
        {
            "Unity.Behavior",
            "Unity.Behavior.GraphFramework"
        };

        internal static bool IsExcludedNamespaceOrType(Type type)
        {
            if (typeof(EventChannelBase).IsAssignableFrom(type))
            {
                return true;
            }
            if (string.IsNullOrEmpty(type.Namespace))
            {
                return false;
            }

            foreach (string excludedNamespace in kExcludedNamespaces)
            {
                if (type.Namespace.Contains(excludedNamespace))
                {
                    return true;
                }
            }

            foreach (string excludedNamespace in kExcludedNamespacesExact)
            {
                if (type.Namespace.Equals(excludedNamespace))
                {
                    return true;
                }
            }
            return false;
        }
    }    

    internal struct BlackboardOption
    {
        public string Name => System.IO.Path.GetFileName(Path);
        public string Path;
        public SerializableType Type;
        public string Icon;
        public Texture2D IconImage;
        public int Priority;

        public BlackboardOption(SerializableType type, string path = null, string icon = null, Texture2D iconImage = null, int priority = 0)
        {
            Path = path;
            Type = type;
            Icon = icon;
            Priority = priority;
            IconImage = iconImage;

            if (string.IsNullOrEmpty(path))
            {
                Path = Util.NicifyVariableName(type.Type.Name);
            }
            
            if (string.IsNullOrEmpty(icon))
            {
                // If no icon name is given, try to get one defined for the type from the utility.
                Icon = BlackboardUtils.GetIconNameForType(type.Type);
            }

            if (iconImage == null)
            {
                IconImage = type.Type.GetIcon();
            }
        }
    }
}