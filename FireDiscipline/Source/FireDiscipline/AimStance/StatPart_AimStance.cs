using System.Linq;
using FireDiscipline.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace FireDiscipline.AimStance
{
    /// <summary>
    /// StatPart v2 for StatDefOf.AimingDelayFactor.
    /// Derived warmup formulas per stance:
    /// - Rapid: r = clamp(rapidMinWarmupRatio, rapidMaxWarmupRatio, cooldown / (warmup + cooldown))
    /// - Sharpshot: sharpshotWarmupMultiplier (x1.4)
    /// </summary>
    public class StatPart_AimingDelay : StatPart
    {
        /// <summary>
        /// Warmup multiplier applied while a pawn is actively in a stance transition window.
        /// Retained at 3.0f to discourage snapshotting mid-transition.
        /// </summary>
        public const float TransitionWarmupMultiplier = 3.0f;

        public override void TransformValue(StatRequest req, ref float val)
        {
            if (!req.HasThing || !(req.Thing is Pawn pawn) || pawn.Dead)
                return;

            // Injected StatParts cannot be un-injected, so a disabled module must neutralise itself
            // here. Without this the "all features off == vanilla" baseline is unreachable.
            if (!PatchRegistry.IsModuleEnabled(AimStanceModule.Id))
                return;

            if (AimStanceTracker.IsInTransition(pawn))
            {
                val *= TransitionWarmupMultiplier;
                return;
            }

            AimStanceMode stance = AimStanceTracker.GetStance(pawn);
            if (stance == AimStanceMode.Rapid)
            {
                float r = CalculateRapidWarmupRatio(pawn);
                val *= r;
            }
            else if (stance == AimStanceMode.Sharpshot)
            {
                float mult = FireDisciplineMod.Settings?.sharpshotWarmupMultiplier ?? 1.4f;
                val *= mult;
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (!PatchRegistry.IsModuleEnabled(AimStanceModule.Id))
                return null;

            if (req.HasThing && req.Thing is Pawn pawn && !pawn.Dead)
            {
                if (AimStanceTracker.IsInTransition(pawn))
                {
                    return $"Fire Discipline Stance Transition: x{TransitionWarmupMultiplier:F1}";
                }

                AimStanceMode stance = AimStanceTracker.GetStance(pawn);
                if (stance == AimStanceMode.Rapid)
                {
                    float r = CalculateRapidWarmupRatio(pawn);
                    return $"Fire Discipline Rapid Stance: x{r:F2}";
                }
                else if (stance == AimStanceMode.Sharpshot)
                {
                    float mult = FireDisciplineMod.Settings?.sharpshotWarmupMultiplier ?? 1.4f;
                    return $"Fire Discipline Sharpshot Stance: x{mult:F2}";
                }
            }
            return null;
        }

        public static float CalculateRapidWarmupRatio(Pawn pawn)
        {
            Verb primaryVerb = pawn.equipment?.PrimaryEq?.PrimaryVerb;
            if (primaryVerb?.verbProps == null) return 0.50f;

            float warmup = primaryVerb.verbProps.warmupTime;

            // Cooldown comes from the RangedWeapon_Cooldown stat. Reading verbProps.defaultCooldownTime
            // returned 0 on most weapons, so rawRatio was 0 and every weapon clamped to the minimum -
            // Rapid gave a flat x0.30 warmup to a bolt-action and an SMG alike, when the whole point
            // of the formula is that the reduction is derived from the weapon's own rhythm.
            // Read from the equipment instance so weapon quality counts.
            float cooldown = primaryVerb.EquipmentSource?.GetStatValue(StatDefOf.RangedWeapon_Cooldown) ?? 0f;
            if (cooldown <= 0f) cooldown = primaryVerb.verbProps.defaultCooldownTime;

            if (warmup + cooldown <= 0f) return 0.50f;

            float rawRatio = cooldown / (warmup + cooldown);
            float minRatio = FireDisciplineMod.Settings?.rapidMinWarmupRatio ?? 0.30f;
            float maxRatio = FireDisciplineMod.Settings?.rapidMaxWarmupRatio ?? 0.75f;
            return Mathf.Clamp(rawRatio, minRatio, maxRatio);
        }
    }


}
