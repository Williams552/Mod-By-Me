using System.Collections.Generic;
using Verse;

namespace RimwardExiles.Core
{
    public class ValueEffectEntry
    {
        public string source;
        public float perUnit;
        public float cap = float.NaN;

        public bool HasCap => !float.IsNaN(cap);
    }

    public class HeroValueEffectDef : Def
    {
        public HeroValueDef axis;
        public List<ValueEffectEntry> effects = new List<ValueEffectEntry>();
    }
}
