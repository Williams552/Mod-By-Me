using FireDiscipline.Core;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace FireDiscipline.AimStance
{
    /// <summary>
    /// Harmony Postfix on ShotReport.HitReportFor (v2 high-precision hook).
    /// Optimized using Harmony AccessTools.StructFieldRefAccess for ZERO boxing and ZERO allocation native ref speed on value-type ShotReport struct.
    /// Centralized accuracy calculation for all 4 stances:
    /// - Sharpshot: Distance exponent (d * 0.80) + close-range penalty (<5c -> x0.70)
    /// - Rapid: Progressive distance penalty outside d0 (d > d0 -> x0.93^(d-d0))
    /// - Prone (Shooter): Flat accuracy multiplier (x0.85)
    /// - Prone (Target): Target size reduction (x0.65)
    /// </summary>
    public static class Patch_ShotReport
    {
        private static readonly AccessTools.StructFieldRef<ShotReport, float> shooterFactorRef = AccessTools.StructFieldRefAccess<ShotReport, float>("factorFromShooterAndDist");
        private static readonly AccessTools.StructFieldRef<ShotReport, float> targetSizeRef = AccessTools.StructFieldRefAccess<ShotReport, float>("factorFromTargetSize");

        // Cached field accessor. This used to be a Traverse lookup evaluated on every shot report -
        // the slowest reflection path Harmony offers, sitting in a method that also runs every frame
        // while the player hovers a target.
        private static readonly AccessTools.FieldRef<Verb, int> burstShotsLeftRef =
            AccessTools.FieldRefAccess<Verb, int>("burstShotsLeft");

        /// <summary>Accuracy retained per additional round within a burst.</summary>
        private const float RecoilPerShot = 0.93f;

        public static void Postfix(Thing caster, Verb verb, LocalTargetInfo target, ref ShotReport __result)
        {
            if (FireDisciplineMod.Settings != null && !FireDisciplineMod.Settings.enableHighPrecisionShotReportPatch)
                return;

            // Runtime guard: the patch stays registered for the session, so switching the module OFF
            // must be honoured here for it to take effect without a restart.
            if (!PatchRegistry.IsModuleEnabled(AimStanceModule.Id))
                return;

            // 1. Shooter Stance Modifiers
            if (caster is Pawn shooterPawn && shooterFactorRef != null)
            {
                AimStanceMode stance = AimStanceTracker.GetStance(shooterPawn);
                float dist = (caster.Position - target.Cell).LengthHorizontal;

                ref float factor = ref shooterFactorRef(ref __result);

                if (stance == AimStanceMode.Sharpshot)
                {
                    // Long-range exponent bonus (d * 0.80)
                    if (factor > 0f && factor < 1f)
                    {
                        float exp = FireDisciplineMod.Settings?.sharpshotDistanceExponentFactor ?? 0.80f;
                        factor = Mathf.Pow(factor, exp);
                    }

                    // Close-range penalty (< 5c)
                    if (dist < 5f)
                    {
                        float penalty = FireDisciplineMod.Settings?.sharpshotCloseRangePenalty ?? 0.70f;
                        factor *= penalty;
                    }
                }
                else if (stance == AimStanceMode.Rapid)
                {
                    // Progressive distance penalty outside d0
                    float d0 = WeaponClassification.CalculateD0(verb?.EquipmentSource?.def);
                    if (dist > d0)
                    {
                        float penalty = Mathf.Pow(0.93f, dist - d0);
                        factor *= penalty;
                    }

                    // Recoil: the Nth round of a burst is worse than the first (accuracy x 0.93^N).
                    //
                    // burstShotsLeft is only meaningful while a burst is actually in progress. Outside
                    // one it reads 0, which made shotIndex equal the full burst length and applied the
                    // whole recoil stack permanently - measured at x0.65 for a 6-round weapon, on every
                    // first shot and on every mouse-over aim preview. Rapid ended up LESS accurate than
                    // Snap Shot at point blank, which is the opposite of what the stance is for.
                    if (verb != null && verb.verbProps.burstShotCount >= 3 && burstShotsLeftRef != null)
                    {
                        int shotsLeft = burstShotsLeftRef(verb);
                        if (shotsLeft > 0)
                        {
                            int shotIndex = Mathf.Max(0, verb.verbProps.burstShotCount - shotsLeft);
                            if (shotIndex > 0)
                            {
                                factor *= Mathf.Pow(RecoilPerShot, shotIndex);
                            }
                        }
                    }
                }
                else if (stance == AimStanceMode.Prone)
                {
                    // Flat accuracy multiplier when shooter is in Prone stance (x0.85)
                    float mult = FireDisciplineMod.Settings?.proneAccuracyMultiplier ?? 0.85f;
                    factor *= mult;
                }
            }

            // 2. Embrasure Interaction Accuracy Modifier (Section 5.7, Wave B4 - off by default)
            if ((FireDisciplineMod.Settings?.enableEmbrasureInteraction ?? false)
                && caster is Pawn shooter && EmbrasureUtility.IsUsingEmbrasure(shooter) && shooterFactorRef != null)
            {
                float embrasureMult = FireDisciplineMod.Settings?.embrasureAccuracyMultiplier ?? 0.85f;
                shooterFactorRef(ref __result) *= embrasureMult;
            }

            // 3. Target Stance Modifiers
            if (target.HasThing && target.Thing is Pawn targetPawn && targetSizeRef != null)
            {
                AimStanceMode targetStance = AimStanceTracker.GetStance(targetPawn);
                if (targetStance == AimStanceMode.Prone)
                {
                    float mult = FireDisciplineMod.Settings?.proneTargetSizeFactor ?? 0.65f;
                    targetSizeRef(ref __result) *= mult;
                }
            }
        }

    }
}
