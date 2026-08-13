using System.Collections.Generic;
using Verse;
using RimWorld;

namespace EchoResonance.Perks
{
    public enum PerkBranch
    {
        Flesh,      // Nhục Thân
        Mind,       // Tâm Trí
        Livelihood, // Sinh Kế
        Combat      // Chiến Trận
    }

    public class PerkDef : Def
    {
        public int tier = 1; // 1, 2, 3
        public PerkBranch branch;
        public float baseCost = 20f;
        public bool isTradeOff = false;
        public HediffDef hediffDef;
        public ThingDef catalystItemDef;
        public List<WorkTypeDef> disabledWorkTypes;
        public string tradeOffDescription;
    }
}
