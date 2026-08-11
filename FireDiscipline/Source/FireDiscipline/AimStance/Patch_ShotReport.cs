using System.Collections.Generic;
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
        private static readonly AccessTools.StructFieldRef<ShotReport, float> coverBlockRef = AccessTools.StructFieldRefAccess<ShotReport, float>("coversOverallBlockChance");
        private static readonly AccessTools.StructFieldRef<ShotReport, List<CoverInfo>> coversListRef = AccessTools.StructFieldRefAccess<ShotReport, List<CoverInfo>>("covers");

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

                    // Recoil in Sharpshot: Burst recoil is DOUBLED (2x penalty power -> 0.93^(2*N) per shot)
                    if (verb != null && verb.verbProps.burstShotCount >= 2 && burstShotsLeftRef != null)
                    {
                        int shotsLeft = burstShotsLeftRef(verb);
                        if (shotsLeft > 0)
                        {
                            int shotIndex = Mathf.Max(0, verb.verbProps.burstShotCount - shotsLeft);
                            if (shotIndex > 0)
                            {
                                factor *= Mathf.Pow(RecoilPerShot, shotIndex * 2f);
                            }
                        }
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
                    // Standard Shot at point blank, which is the opposite of what the stance is for.
                    if (verb != null && verb.verbProps.burstShotCount >= 2 && burstShotsLeftRef != null)
                    {
                        int shotsLeft = burstShotsLeftRef(verb);
                        if (shotsLeft > 0)
                        {
                            int shotIndex = Mathf.Max(0, verb.verbProps.burstShotCount - shotsLeft);
                            if (shotIndex > 0)
                            {
                                float recoilPower = 1.0f;
                                ThingDef wDef = verb.EquipmentSource?.def;
                                if (wDef != null && WeaponClassification.HasShotgunProfile(wDef) && verb.verbProps.burstShotCount > 1)
                                {
                                    recoilPower = FireDisciplineMod.Settings?.shotgunRapidRecoilMultiplier ?? 2.50f;
                                }
                                factor *= Mathf.Pow(RecoilPerShot, shotIndex * recoilPower);
                            }
                        }
                    }
                }
                // Automatic Passive Dug-In (Prone) Shooter Accuracy Modifier
                if (AimStanceTracker.IsDugIn(shooterPawn))
                {
                    float mult = FireDisciplineMod.Settings?.proneAccuracyMultiplier ?? 0.85f;
                    factor *= mult;
                }
            }


            // 3. Target Dug-In (Prone) Modifiers
            if (target.HasThing && target.Thing is Pawn targetPawn && targetSizeRef != null)
            {
                if (AimStanceTracker.IsDugIn(targetPawn))
                {
                    float mult = FireDisciplineMod.Settings?.proneTargetSizeFactor ?? 0.65f;
                    targetSizeRef(ref __result) *= mult;
                }
            }

            // 4. Cover Bypass, Cover Degradation & Line Cover Stacking
            if (coverBlockRef != null)
            {
                ref float coverBlock = ref coverBlockRef(ref __result);

                // Option 0: Line Cover Stacking (Intermediate cover along ShootLine)
                FireDisciplineSettings settings = FireDisciplineMod.Settings;
                if (settings != null && settings.enableCoverStacking && caster != null && caster.Map != null)
                {
                    List<CoverInfo> lineCoverBuffer = CoverStackingUtility.GetLineCoverInfosBuffer();
                    float lineBlock = CoverStackingUtility.LineCoverBlockChance(caster.Position, target, caster.Map, lineCoverBuffer);
                    if (lineBlock > 0f)
                    {
                        float combined = coverBlock + (1f - coverBlock) * lineBlock;
                        float cap = settings.coverStackingCap;
                        coverBlock = Mathf.Min(combined, cap);

                        // Itemize line cover items in __result.covers for player-facing tooltip readout UI
                        if (lineCoverBuffer != null && lineCoverBuffer.Count > 0 && coversListRef != null)
                        {
                            ref List<CoverInfo> covers = ref coversListRef(ref __result);
                            if (covers == null)
                            {
                                covers = new List<CoverInfo>();
                            }
                            for (int i = 0; i < lineCoverBuffer.Count; i++)
                            {
                                covers.Add(lineCoverBuffer[i]);
                            }
                        }
                    }
                }

                if (coverBlock > 0f)
                {
                    // Option 1: Sharpshot Cover Bypass (bypasses 50% of target cover block chance)
                    if (caster is Pawn sPawn && AimStanceTracker.GetStance(sPawn) == AimStanceMode.Sharpshot)
                    {
                        float bypassFactor = FireDisciplineMod.Settings?.sharpshotCoverBypassFactor ?? 0.50f;
                        coverBlock *= (1f - bypassFactor);
                    }

                    // Option 2: Suppression Cover Degradation (reduces target cover block chance when suppressed)
                    if ((FireDisciplineMod.Settings?.enableSuppressionCoverDegradation ?? true)
                        && target.HasThing && target.Thing is Pawn tPawn)
                    {
                        HediffDef suppDef = Suppression.SuppressionEngine.SuppressedDef;
                        if (suppDef != null && tPawn.health?.hediffSet != null)
                        {
                            var hediff = tPawn.health.hediffSet.GetFirstHediffOfDef(suppDef);
                            if (hediff != null && hediff.Severity > 0f)
                            {
                                float maxPenalty = FireDisciplineMod.Settings?.suppressionCoverDegradationMax ?? 0.40f;
                                float penalty = Mathf.Clamp01(maxPenalty * hediff.Severity);
                                coverBlock *= (1f - penalty);
                            }
                        }
                    }
                }
            }

            // 5. Hit Variance Mitigation Module (Wave B8)
            Variance.HitVarianceEngine.ProcessHitReport(caster, verb, ref __result);
        }

    }
}
