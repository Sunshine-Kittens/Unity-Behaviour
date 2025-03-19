using System.Collections.Generic;

namespace Unity.Behavior
{
    public interface IConditional
    {
        public List<Condition> Conditions { get; set; }
        public bool RequiresAllConditions { get; set; }
    }
}