using System.Collections.Generic;
using HarmonyLib;
using FireDiscipline.AimStance;
using RimWorld;
using UnityEngine;
using Verse;

namespace FireDiscipline.Shock
{
    /// <summary>
    /// Harmony Postfix on Explosion.StartExplosion (v3 Execution Spec).
    /// Calculates non-linear Shell Shock radius: shockRadius = min(20, r + 2 * sqrt(r)).
    /// Enforces 5 strict filter gates (Physical dmg only, floor cut < 0.15, non-drafted x0.3, LOS check, 40-pawn cap).
    /// </summary>
    public static class Patch_Explosion
    {
        public static void Postfix(Explosion __instance)
        {
            if (!Core.PatchRegistry.IsModuleEnabled(ShockModule.Id))
                return;

            if (__instance == null || __instance.Map == null) return;

            // Gate 2.2: Damage Type Filter (Physical/Explosive only, damAmount >= 10)
            if (__instance.damAmount < 10) return;
            DamageDef damDef = __instance.damType;
            if (damDef == null || (!damDef.isExplosive && damDef != DamageDefOf.Bullet && damDef != DamageDefOf.Bomb && damDef != DamageDefOf.Cut && damDef != DamageDefOf.Blunt))
            {
                return; // Skip firefoam, smoke, EMP, extinguish, foam
            }

            Map map = __instance.Map;
            IntVec3 center = __instance.Position;
            float dmgRadius = __instance.radius;

            if (dmgRadius <= 0.5f) return;

            // shockRadius = min(cap, r + coefficient * sqrt(r))
            float cap = FireDisciplineMod.Settings?.shellShockRadiusCap ?? 20f;
            float coefficient = FireDisciplineMod.Settings?.shellShockRadiusCoefficient ?? 2f;
            float shockRadius = Mathf.Min(cap, dmgRadius + coefficient * Mathf.Sqrt(dmgRadius));
            float powerFactor = Mathf.Clamp(__instance.damAmount / 50f, 0.4f, 1.0f);

            HediffDef shellShockDef = DefDatabase<HediffDef>.GetNamedSilentFail("FD_ShellShock");
            if (shellShockDef == null) return;

            int processedPawns = 0;

            foreach (Thing thing in GenRadial.RadialDistinctThingsAround(center, map, shockRadius, true))
            {
                // Gate 2.5: Cap at 40 pawns per explosion
                if (processedPawns >= 40) break;

                if (thing is Pawn victim && !victim.Dead && victim.RaceProps.Humanlike)
                {
                    // Gate 2.4: Line of Sight (LOS) Check
                    if (!GenSight.LineOfSight(center, victim.Position, map, true))
                        continue;

                    float dist = (victim.Position - center).LengthHorizontal;
                    float baseSeverity = 0.25f;

                    if (dist <= dmgRadius)
                    {
                        // Direct blast zone
                        baseSeverity = 0.85f * powerFactor;
                    }
                    else
                    {
                        // Proportional shockwave falloff zone
                        float frac = (dist - dmgRadius) / (shockRadius - dmgRadius);
                        baseSeverity = Mathf.Clamp(1.0f - frac, 0.10f, 0.85f) * powerFactor;
                    }

                    // Gate 2.3: Non-drafted pawns multiplier (x0.3)
                    if (!victim.Drafted)
                    {
                        baseSeverity *= 0.30f;
                    }

                    // Gate 2.1: Floor cut: Ignore if severity < 0.15
                    if (baseSeverity < 0.15f)
                        continue;

                    processedPawns++;

                    // Gate 2.5: Refresh severity instead of stacking
                    Hediff hediff = victim.health.hediffSet.GetFirstHediffOfDef(shellShockDef);
                    if (hediff != null)
                    {
                        hediff.Severity = Mathf.Max(hediff.Severity, baseSeverity);
                    }
                    else
                    {
                        hediff = HediffMaker.MakeHediff(shellShockDef, victim);
                        hediff.Severity = baseSeverity;
                        victim.health.AddHediff(hediff);
                    }

                    // Sharpshot warmup reset from concussive shockwave
                    AimStanceTracker.Notify_Suppressed(victim);

                    if (map != null && Find.CameraDriver != null && dist <= dmgRadius)
                    {
                        MoteMaker.ThrowText(victim.DrawPos, map, "Shell Shocked!", Color.yellow);
                    }
                }
            }
        }
    }
}
