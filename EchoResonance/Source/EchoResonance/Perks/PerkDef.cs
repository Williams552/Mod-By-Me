using System.Collections.Generic;
using Verse;
using RimWorld;

namespace EchoResonance.Perks
{
    public enum PerkBranch
    {
        Flesh,      // 🫀 Nhục Thân
        Mind,       // 🧠 Tâm Trí
        Livelihood, // 🔨 Sinh Kế
        Combat      // ⚔️ Chiến Trận
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

        // Tree Relationships (Section 7.1)
        public PerkDef replaces;                   // ⬆️ Upgrades & removes 1 old perk (N does not increase)
        public List<PerkDef> replacesList;         // ⬆️ Upgrades & removes MULTIPLE old perks (absorbs all, N shrinks!)
        public List<PerkDef> requires;             // 🔗 Prerequisites (old perk stays active)
        public List<string> exclusionTags;         // ⛔ Mutual exclusion conflict tags

        public List<PerkDef> GetAllReplacedPerks()
        {
            var list = new List<PerkDef>();
            if (replaces != null) list.Add(replaces);
            if (!replacesList.NullOrEmpty())
            {
                foreach (var p in replacesList)
                {
                    if (p != null && !list.Contains(p)) list.Add(p);
                }
            }
            return list;
        }
    }
}
