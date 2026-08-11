using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace FireDiscipline.Core
{
    /// <summary>
    /// Utility for detecting active personal energy shields (CompShield) on Pawns.
    ///
    /// Architecture Rule 2: Derives shield status from RimWorld's CompShield API without hardcoding
    /// defNames or string labels. Compatible with vanilla Shield Belts, modded apparel shields,
    /// Biotech gene shields, and Mechanoid shields.
    /// </summary>
    public static class ShieldUtility
    {
        /// <summary>
        /// Returns the active shield energy fraction (0.0 to 1.0) of a Pawn.
        /// Returns 0.0 if the pawn has no shield, or if all shields are in Resetting/Disabled state.
        /// </summary>
        public static float GetActiveShieldEnergyFraction(Pawn pawn)
        {
            if (pawn == null) return 0f;

            float maxFraction = 0f;

            // 1. Inspect worn apparel for CompShield (Humanlike pawns)
            if (pawn.apparel != null && pawn.apparel.WornApparel != null)
            {
                List<Apparel> worn = pawn.apparel.WornApparel;
                int count = worn.Count;
                for (int i = 0; i < count; i++)
                {
                    Apparel app = worn[i];
                    if (app == null) continue;

                    CompShield shieldComp = app.GetComp<CompShield>();
                    if (shieldComp != null && shieldComp.ShieldState == ShieldState.Active)
                    {
                        float energyMax = shieldComp.parent != null ? shieldComp.parent.GetStatValue(StatDefOf.EnergyShieldEnergyMax) : 100f;
                        if (energyMax <= 0f) energyMax = 100f;
                        float fraction = Mathf.Clamp01(shieldComp.Energy / energyMax);
                        if (fraction > maxFraction)
                        {
                            maxFraction = fraction;
                        }
                    }
                }
            }

            // 2. Inspect pawn-level comps for direct shield comps (Genes, Mechs, or special PawnComps)
            if (pawn.AllComps != null)
            {
                List<ThingComp> comps = pawn.AllComps;
                int count = comps.Count;
                for (int i = 0; i < count; i++)
                {
                    if (comps[i] is CompShield shieldComp && shieldComp.ShieldState == ShieldState.Active)
                    {
                        float energyMax = shieldComp.parent != null ? shieldComp.parent.GetStatValue(StatDefOf.EnergyShieldEnergyMax) : 100f;
                        if (energyMax <= 0f) energyMax = 100f;
                        float fraction = Mathf.Clamp01(shieldComp.Energy / energyMax);
                        if (fraction > maxFraction)
                        {
                            maxFraction = fraction;
                        }
                    }
                }
            }

            return maxFraction;
        }
    }
}
