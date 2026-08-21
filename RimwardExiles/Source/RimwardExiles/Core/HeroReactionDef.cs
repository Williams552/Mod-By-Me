using System.Collections.Generic;
using Verse;

namespace RimwardExiles.Core
{
    public class ReactionTriggerEntry
    {
        public string incident;
        public string action;
        public string condition;
    }

    public class AxisDeltaEntry
    {
        public HeroValueDef axis;
        public float amount;
    }

    public class HeroReactionDef : Def
    {
        public List<ReactionTriggerEntry> triggers = new List<ReactionTriggerEntry>();
        public List<AxisDeltaEntry> delta = new List<AxisDeltaEntry>();
        public float memoryHalfLifeDays = 20f;
        public bool memoryDecayable = true;
        public string memoryLabel;

        public Dictionary<HeroValueDef, float> GetDeltaDictionary()
        {
            var dict = new Dictionary<HeroValueDef, float>();
            if (delta == null) return dict;

            for (int i = 0; i < delta.Count; i++)
            {
                var entry = delta[i];
                if (entry.axis != null)
                {
                    dict[entry.axis] = entry.amount;
                }
            }
            return dict;
        }
    }
}
