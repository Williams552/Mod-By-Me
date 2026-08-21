using System.Collections.Generic;
using Verse;

namespace EchoResonance.Perks
{
    [StaticConstructorOnStartup]
    public static class PerkGraph
    {
        static PerkGraph()
        {
            ValidateGraph();
        }

        public static void ValidateGraph()
        {
            var allPerks = DefDatabase<PerkDef>.AllDefsListForReading;
            if (allPerks.NullOrEmpty()) return;

            int validCount = 0;
            foreach (var perk in allPerks)
            {
                // Check self-replacement cycle
                if (perk.replaces == perk || (!perk.replacesList.NullOrEmpty() && perk.replacesList.Contains(perk)))
                {
                    Log.Error($"[Echo Resonance] Perk '{perk.defName}' cannot replace itself!");
                }

                // Check tier ordering
                if (perk.replaces != null && perk.replaces.tier > perk.tier)
                {
                    Log.Warning($"[Echo Resonance] Perk '{perk.defName}' (Tier {perk.tier}) replaces a higher tier perk '{perk.replaces.defName}' (Tier {perk.replaces.tier}).");
                }

                if (!perk.replacesList.NullOrEmpty())
                {
                    foreach (var rep in perk.replacesList)
                    {
                        if (rep.tier > perk.tier)
                        {
                            Log.Warning($"[Echo Resonance] Perk '{perk.defName}' (Tier {perk.tier}) replaces a higher tier perk '{rep.defName}' (Tier {rep.tier}).");
                        }
                    }
                }

                if (!perk.requires.NullOrEmpty())
                {
                    foreach (var req in perk.requires)
                    {
                        if (req.tier > perk.tier)
                        {
                            Log.Warning($"[Echo Resonance] Perk '{perk.defName}' (Tier {perk.tier}) requires a higher tier perk '{req.defName}' (Tier {req.tier}).");
                        }
                    }
                }

                validCount++;
            }

            Log.Message($"[Echo Resonance] Validated {validCount} PerkDefs in PerkGraph.");
        }
    }
}
