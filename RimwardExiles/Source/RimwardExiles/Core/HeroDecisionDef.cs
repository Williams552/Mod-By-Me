using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimwardExiles.Core
{
    public class FactionGoodwillDelta
    {
        public FactionDef faction;
        public int amount;
    }

    public class DecisionOption
    {
        public string label;
        public string description;
        public List<AxisDeltaEntry> delta = new List<AxisDeltaEntry>();
        public int silverCost = 0;
        public List<FactionGoodwillDelta> factionGoodwill = new List<FactionGoodwillDelta>();
        public bool createsMemory = true;
        public bool memoryDecayable = true;
        public float memoryHalfLifeDays = 20f;
        public HeroDispositionDef givesDisposition;
        public HeroValueDef tensionBoostAxis;
        public HeroValueDef tensionReduceAxis;
    }

    public class HeroDecisionDef : Def
    {
        public string letterLabel;
        [MustTranslate]
        public string letterText;
        public LetterDef letterDef;

        public float baseWeight = 1.0f;
        public float minRefireDays = 15f;
        public bool formativeDecision = false;

        public List<string> requiredHeroes = new List<string>();
        public bool requiresAllHeroes = false;

        public List<HeroValueDef> escalationAxes = new List<HeroValueDef>();
        public float escalationMultiplier = 2.0f;

        public List<DecisionOption> options = new List<DecisionOption>();
    }
}
