using System;

namespace Unity.Behavior.GraphFramework
{
    public class BaseModel
    {
        public GraphAsset Asset { get; set; }
        public virtual IVariableLink GetVariableLink(string variableName, Type type) => null;
    }
}
