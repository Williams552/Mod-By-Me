using FireDiscipline.Core;
using FireDiscipline.Encumbrance;
using FireDiscipline.ShotgunAoE;
using FireDiscipline.AimStance;
using FireDiscipline.Suppression;
using FireDiscipline.Graze;
using FireDiscipline.Shock;
using RimWorld;
using UnityEngine;
using Verse;

namespace FireDiscipline
{
    public class FireDisciplineMod : Mod
    {
        public static FireDisciplineSettings Settings { get; private set; }
        private static Vector2 scrollPosition = Vector2.zero;

        public FireDisciplineMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<FireDisciplineSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Rect outerRect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height);
            // Grown alongside the weapon-classification and Wave B toggle sections. If content is
            // added below and this is not raised, the tail of the settings list is silently clipped.
            Rect viewRect = new Rect(0f, 0f, inRect.width - 30f, 2150f);

            Widgets.BeginScrollView(outerRect, ref scrollPosition, viewRect);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(viewRect);

            listing.Label("<b><size=16>Fire Discipline - Mod Settings</size></b>");
            listing.Gap(10f);

            // =========================================================================
            // MODULE TOGGLES
            // =========================================================================
            listing.Label("<b><color=#66CCFF>Enabled Modules</color></b>");
            foreach (var module in PatchRegistry.Modules)
            {
                bool currentVal = Settings.IsModuleEnabled(module);
                bool prevVal = currentVal;
                listing.CheckboxLabeled($"{module.DisplayName}: {module.Description}", ref currentVal);
                if (currentVal != prevVal)
                {
                    Settings.SetModuleEnabled(module, currentVal);

                    // Keep the live flag in sync so runtime guards react on the next stat recalc.
                    module.IsEnabled = currentVal;

                    // Turning a module OFF is immediate: every guard reads IsEnabled. Turning one ON
                    // is not - Harmony patches are registered once at startup and cannot be added
                    // retroactively, so a module that was disabled when the game loaded stays inert.
                    if (currentVal && !PatchRegistry.WasPatchedAtStartup(module.ModuleId))
                    {
                        string warning = $"[Fire Discipline] Module [{module.DisplayName}] was enabled mid-session. "
                            + "Its Harmony patches were not registered at startup, so it stays inactive until the game is restarted.";
                        Log.Warning(warning);
                        Messages.Message(
                            $"{module.DisplayName}: restart required before this module becomes active.",
                            MessageTypeDefOf.CautionInput, false);
                    }
                }
            }

            listing.Gap(15f);

            // =========================================================================
            // SECTION 0b: SUPPRESSION ENGINE + EXTERNAL MOD TRADE-OFF
            // =========================================================================
            listing.Label("<b><color=#FFAA55>Suppression Engine</color></b>");
            listing.CheckboxLabeled("Enable Fire Discipline's own suppression", ref Settings.enableSuppressionEngine,
                "Incoming fire pins pawns down and drives the stance matrix. Requires a restart to take effect when turning on.");
            DrawSuppressionTradeOff(listing);

            listing.Gap(15f);

            // =========================================================================
            // SECTION 0c: COVER SUPPRESSION (Wave B3)
            // =========================================================================
            listing.Label("<b><color=#AAAAFF>Cover Suppression</color></b> <i>(Wave B3)</i>");

            listing.CheckboxLabeled("Enable Cover Suppression", ref Settings.enableCoverSuppression,
                "Cover reduces incoming suppression severity.");

            if (Settings.enableCoverSuppression)
            {
                listing.Label($"Cover Suppression Factor: <b>{Settings.coverSuppressionFactor:F2}</b> (Default: 0.85)");
                Settings.coverSuppressionFactor = listing.Slider(Settings.coverSuppressionFactor, 0.10f, 1.00f);

                listing.Label($"Cover Suppression Floor: <b>{Settings.coverSuppressionFloor:F2}</b> (Default: 0.25 - minimum suppression received)");
                Settings.coverSuppressionFloor = listing.Slider(Settings.coverSuppressionFloor, 0.10f, 0.80f);
            }

            listing.Gap(15f);

            // =========================================================================
            // SECTION 1: SHARPSHOT STANCE
            // =========================================================================
            listing.Label("<b><color=#FFD700>Sharpshot Stance (Sniper)</color></b>");
            
            listing.Label($"Warmup Time Multiplier: <b>x{Settings.sharpshotWarmupMultiplier:F2}</b> (Default: x1.40)");
            Settings.sharpshotWarmupMultiplier = listing.Slider(Settings.sharpshotWarmupMultiplier, 1.00f, 2.50f);

            listing.Label($"Distance Exponent Factor: <b>d * {Settings.sharpshotDistanceExponentFactor:F2}</b> (Default: d * 0.80)");
            Settings.sharpshotDistanceExponentFactor = listing.Slider(Settings.sharpshotDistanceExponentFactor, 0.50f, 1.00f);

            listing.Label($"Close Range (<5c) Accuracy Penalty: <b>x{Settings.sharpshotCloseRangePenalty:F2}</b> (Default: x0.70)");
            Settings.sharpshotCloseRangePenalty = listing.Slider(Settings.sharpshotCloseRangePenalty, 0.30f, 1.00f);

            listing.Label($"Received Suppression Vulnerability: <b>x{Settings.sharpshotSuppressionVulnerability:F2}</b> (Default: x2.00)");
            Settings.sharpshotSuppressionVulnerability = listing.Slider(Settings.sharpshotSuppressionVulnerability, 1.00f, 3.00f);

            listing.Gap(15f);

            // =========================================================================
            // SECTION 2: RAPID FIRE STANCE
            // =========================================================================
            listing.Label("<b><color=#FF5555>Rapid Fire Stance (Close-Quarters)</color></b>");

            listing.Label($"Min Warmup Ratio Clamp: <b>x{Settings.rapidMinWarmupRatio:F2}</b> (Default: x0.30)");
            Settings.rapidMinWarmupRatio = listing.Slider(Settings.rapidMinWarmupRatio, 0.10f, 0.50f);

            listing.Label($"Max Warmup Ratio Clamp: <b>x{Settings.rapidMaxWarmupRatio:F2}</b> (Default: x0.75)");
            Settings.rapidMaxWarmupRatio = listing.Slider(Settings.rapidMaxWarmupRatio, 0.50f, 0.95f);

            listing.Label($"Inflicted Suppression Multiplier: <b>x{Settings.rapidSuppressionMultiplier:F2}</b> (Default: x1.50)");
            Settings.rapidSuppressionMultiplier = listing.Slider(Settings.rapidSuppressionMultiplier, 1.00f, 2.50f);

            listing.Gap(15f);

            // =========================================================================
            // SECTION 3: PRONE STANCE
            // =========================================================================
            listing.Label("<b><color=#55FF55>Prone Stance (Cover/Dug-in)</color></b>");

            listing.Label($"Target Size Reduction Factor: <b>x{Settings.proneTargetSizeFactor:F2}</b> (Default: x0.65)");
            Settings.proneTargetSizeFactor = listing.Slider(Settings.proneTargetSizeFactor, 0.30f, 0.95f);

            listing.Label($"Shooter Accuracy Multiplier: <b>x{Settings.proneAccuracyMultiplier:F2}</b> (Default: x0.85)");
            Settings.proneAccuracyMultiplier = listing.Slider(Settings.proneAccuracyMultiplier, 0.50f, 1.00f);

            listing.Label($"Received Suppression Resistance: <b>x{Settings.proneSuppressionResistance:F2}</b> (Default: x0.50)");
            Settings.proneSuppressionResistance = listing.Slider(Settings.proneSuppressionResistance, 0.10f, 0.90f);

            listing.Gap(15f);

            // =========================================================================
            // SECTION 3b: WEAPON CLASSIFICATION (architecture rule 2)
            // =========================================================================
            listing.Label("<b><color=#99FF99>Weapon Classification</color></b> <i>(derived from vanilla stats - no weapon lists)</i>");
            listing.Label("<i>Use the \"Print Weapon Classification\" debug action to audit these against your modlist.</i>");

            listing.Label($"Shotgun: Min Range: <b>{Settings.shotgunMinRange:F0}c</b> (Default: 8)");
            Settings.shotgunMinRange = listing.Slider(Settings.shotgunMinRange, 0f, 20f);

            listing.Label($"Shotgun: Max Range: <b>{Settings.shotgunMaxRange:F0}c</b> (Default: 17)");
            Settings.shotgunMaxRange = listing.Slider(Settings.shotgunMaxRange, 10f, 35f);

            listing.Label($"Shotgun: Min Peak Accuracy: <b>{(int)(Settings.shotgunMinPeakAccuracy * 100f)}%</b> (Default: 55%)");
            Settings.shotgunMinPeakAccuracy = listing.Slider(Settings.shotgunMinPeakAccuracy, 0.20f, 0.90f);

            listing.Label($"Shotgun: Min Long/Short Flatness Ratio: <b>{Settings.shotgunMinLongShortRatio:F2}</b> (Default: 0.50)");
            Settings.shotgunMinLongShortRatio = listing.Slider(Settings.shotgunMinLongShortRatio, 0.20f, 0.90f);

            listing.Label($"Rapid: d0 Base (long-range weapons): <b>{Settings.d0Base:F0}c</b> (Default: 4)");
            Settings.d0Base = listing.Slider(Settings.d0Base, 1f, 12f);

            listing.Label($"Rapid: d0 Span (added for close-range weapons): <b>{Settings.d0Span:F0}c</b> (Default: 12, giving {Settings.d0Base:F0}-{Settings.d0Base + Settings.d0Span:F0}c)");
            Settings.d0Span = listing.Slider(Settings.d0Span, 0f, 24f);

            listing.Gap(15f);

            // =========================================================================
            // SECTION 4: GRAZE SYSTEM (Module 5.4)
            // =========================================================================
            listing.Label("<b><color=#00FFFF>Graze System (Anti-One-Shot)</color></b>");

            listing.Label($"Never Graze Above Hit Chance: <b>{(int)(Settings.grazeHitChanceCeiling * 100f)}%</b> (Default: 65%)");
            Settings.grazeHitChanceCeiling = listing.Slider(Settings.grazeHitChanceCeiling, 0.30f, 0.95f);

            listing.Label($"Graze Ramp Width: <b>{(int)(Settings.grazeChanceSpan * 100f)}%</b> "
                + $"(always grazes at or below {(int)((Settings.grazeHitChanceCeiling - Settings.grazeChanceSpan) * 100f)}% hit chance)");
            Settings.grazeChanceSpan = listing.Slider(Settings.grazeChanceSpan, 0.10f, 0.80f);

            listing.Label($"Graze Damage Retained: <b>{(int)(Settings.grazeDamageMultiplier * 100f)}%</b> (-{(int)((1f - Settings.grazeDamageMultiplier) * 100f)}% damage reduction)");
            Settings.grazeDamageMultiplier = listing.Slider(Settings.grazeDamageMultiplier, 0.10f, 0.50f);

            listing.CheckboxLabeled("Reroute Vital Organ Shots (Brain/Heart) to Limbs on Graze", ref Settings.protectVitalOrgans);

            listing.Gap(15f);

            // =========================================================================
            // SECTION 5: SHOCK & SHELL SHOCK (Module 5.5)
            // =========================================================================
            listing.Label("<b><color=#FF9933>Shock & Shell Shock System</color></b>");

            listing.Label($"Ally Downed Combat Shock Radius: <b>{Settings.allyShockRadius:F1} cells</b> (Default: 6.0c)");
            Settings.allyShockRadius = listing.Slider(Settings.allyShockRadius, 3.0f, 12.0f);

            float mortarShock = Mathf.Min(Settings.shellShockRadiusCap,
                4.9f + Settings.shellShockRadiusCoefficient * Mathf.Sqrt(4.9f));
            listing.Label($"Shell Shock Radius Coefficient: <b>{Settings.shellShockRadiusCoefficient:F1}</b> "
                + $"(radius = r + {Settings.shellShockRadiusCoefficient:F1} x sqrt(r); mortar 4.9c -> {mortarShock:F1}c)");
            Settings.shellShockRadiusCoefficient = listing.Slider(Settings.shellShockRadiusCoefficient, 0.5f, 4.0f);

            listing.Label($"Shell Shock Radius Cap: <b>{Settings.shellShockRadiusCap:F0}c</b> (Default: 20c - always leaves somewhere safe to stand)");
            Settings.shellShockRadiusCap = listing.Slider(Settings.shellShockRadiusCap, 8f, 40f);

            listing.Gap(15f);

            // =========================================================================
            // SECTION 6: EMBRASURE INTERACTION (Module 5.7)
            // =========================================================================
            listing.Label("<b><color=#00FFaa>Embrasure Interaction</color></b> <i>(experimental - off by default)</i>");

            listing.CheckboxLabeled("Enable Embrasure Interaction", ref Settings.enableEmbrasureInteraction,
                "Pawns leaning past an embrasure resist suppression but fire less accurately. Experimental: "
                + "embrasure detection is not yet verified against the game's cover calculation.");

            if (Settings.enableEmbrasureInteraction)
            {
                listing.Label($"Embrasure Suppression Resistance: <b>{(int)(Settings.embrasureSuppressionMultiplier * 100f)}%</b> (-{(int)((1f - Settings.embrasureSuppressionMultiplier) * 100f)}% suppression taken)");
                Settings.embrasureSuppressionMultiplier = listing.Slider(Settings.embrasureSuppressionMultiplier, 0.10f, 0.70f);

                listing.Label($"Embrasure Firing Accuracy Multiplier: <b>{(int)(Settings.embrasureAccuracyMultiplier * 100f)}%</b> (-{(int)((1f - Settings.embrasureAccuracyMultiplier) * 100f)}% accuracy penalty)");
                Settings.embrasureAccuracyMultiplier = listing.Slider(Settings.embrasureAccuracyMultiplier, 0.50f, 1.00f);
            }

            listing.Gap(15f);

            // =========================================================================
            // SECTION 5c: SHOTGUN SPREAD (Wave B2)
            // =========================================================================
            listing.Label("<b><color=#FFAA88>Shotgun Spread</color></b> <i>(module off by default - no danger-zone overlay yet)</i>");

            listing.Label($"Spread Width At Far End: <b>{Settings.shotgunSpreadWidthEnd:F1}c</b> (Default: 3.0 - the muzzle end is always 1 cell wide)");
            Settings.shotgunSpreadWidthEnd = listing.Slider(Settings.shotgunSpreadWidthEnd, 1.0f, 8.0f);

            listing.Label($"Primary Hit Damage: <b>{(int)(Settings.shotgunPrimaryDamageMultiplier * 100f)}%</b> (Default: 70% - the direct hit is reduced to pay for the splash)");
            Settings.shotgunPrimaryDamageMultiplier = listing.Slider(Settings.shotgunPrimaryDamageMultiplier, 0.30f, 1.00f);

            listing.Label($"Edge Damage at Shooting 0 / 20: <b>{(int)(Settings.shotgunEdgeDamageMin * 100f)}% / {(int)(Settings.shotgunEdgeDamageMax * 100f)}%</b> (skill controls the EDGE, never the radius)");
            Settings.shotgunEdgeDamageMin = listing.Slider(Settings.shotgunEdgeDamageMin, 0.00f, 0.50f);
            Settings.shotgunEdgeDamageMax = listing.Slider(Settings.shotgunEdgeDamageMax, 0.20f, 1.00f);

            listing.CheckboxLabeled("Splash can hit your own pawns", ref Settings.shotgunFriendlyFire,
                "The shooter is never hit by their own spread. This controls everyone else. With no danger-zone overlay yet, leaving this on will surprise you.");

            listing.Gap(15f);

            // =========================================================================
            // SECTION 5b: SUPPRESSION MOVE SPEED PENALTY
            // =========================================================================
            listing.Label("<b><color=#FF66AA>Suppression Move Speed Penalties</color></b>");

            listing.Label($"Stage 1 (Shaken) Speed Multiplier: <b>x{Settings.suppressionMoveSpeedFactorStage1:F2}</b> (Default: x0.95)");
            Settings.suppressionMoveSpeedFactorStage1 = listing.Slider(Settings.suppressionMoveSpeedFactorStage1, 0.10f, 1.00f);

            listing.Label($"Stage 2 (Wavering) Speed Multiplier: <b>x{Settings.suppressionMoveSpeedFactorStage2:F2}</b> (Default: x0.80)");
            Settings.suppressionMoveSpeedFactorStage2 = listing.Slider(Settings.suppressionMoveSpeedFactorStage2, 0.10f, 1.00f);

            listing.Label($"Stage 3 (Ducking) Speed Multiplier: <b>x{Settings.suppressionMoveSpeedFactorStage3:F2}</b> (Default: x0.50)");
            Settings.suppressionMoveSpeedFactorStage3 = listing.Slider(Settings.suppressionMoveSpeedFactorStage3, 0.10f, 1.00f);

            listing.Label($"Stage 4 (Cowering) Speed Multiplier: <b>x{Settings.suppressionMoveSpeedFactorStage4:F2}</b> (Default: x0.15)");
            Settings.suppressionMoveSpeedFactorStage4 = listing.Slider(Settings.suppressionMoveSpeedFactorStage4, 0.05f, 1.00f);

            listing.Label($"Suppressed Move Speed Floor: <b>{Settings.suppressedMoveSpeedFloor:F2} c/s</b> (Default: 0.7 - won't speed up slow pawns)");
            Settings.suppressedMoveSpeedFloor = listing.Slider(Settings.suppressedMoveSpeedFloor, 0.10f, 2.00f);

            listing.Gap(15f);

            // =========================================================================
            // SECTION 6: TRANSITION & ENCUMBRANCE
            // =========================================================================
            listing.Label("<b><color=#AA88FF>Transition & Encumbrance</color></b>");

            listing.Label($"Stance Transition Delay Ticks: <b>{Settings.stanceTransitionTicks} ticks</b> ({Settings.stanceTransitionTicks / 60f:F2}s, Default: 45)");
            Settings.stanceTransitionTicks = (int)listing.Slider(Settings.stanceTransitionTicks, 0f, 120f);

            listing.Label($"Encumbrance Threshold: <b>{(int)(Settings.encumbranceThreshold * 100f)}%</b> (Default: 15%)");
            Settings.encumbranceThreshold = listing.Slider(Settings.encumbranceThreshold, 0.05f, 0.50f);

            listing.Label($"Encumbrance Max Speed Penalty: <b>-{(int)(Settings.encumbranceMaxPenalty * 100f)}%</b> (Default: -35%)");
            Settings.encumbranceMaxPenalty = listing.Slider(Settings.encumbranceMaxPenalty, 0.10f, 0.70f);

            listing.Gap(10f);
            listing.CheckboxLabeled("Enable High-Precision ShotReport Harmony Patch", ref Settings.enableHighPrecisionShotReportPatch);
            listing.CheckboxLabeled("Enable Verbose Combat Logging", ref Settings.verboseCombatLogging, "Logs detailed events for graze hits, stance changes, and suppression resets to Player.log.");

            listing.Gap(15f);
            listing.Label("<b>What needs a restart:</b>");
            listing.Label("<i>Sliders and tuning values take effect immediately. Turning a module or feature OFF also "
                + "takes effect immediately. Turning one ON needs a restart - Harmony patches are registered once when "
                + "the game loads and cannot be added to a running session. The mod says so in the log and on screen "
                + "when it happens.</i>");

            listing.End();
            Widgets.EndScrollView();

            base.DoSettingsWindowContents(inRect);
        }

        /// <summary>
        /// States both sides of the suppression choice in concrete terms.
        ///
        /// Deliberately never says anything like "incomplete experience". A player turning the
        /// engine off needs to know exactly which mechanics stop existing, and a player turning it
        /// on alongside another suppression mod needs to know exactly what stacks. Vague warnings
        /// make people guess, and guessing produces bug reports.
        /// </summary>
        private static void DrawSuppressionTradeOff(Listing_Standard listing)
        {
            bool external = ExternalSuppressionDetection.IsAnyExternalSuppressionActive();
            bool combatExtended = ExternalSuppressionDetection.IsCombatExtendedActive();

            if (external)
            {
                string detected = string.Join(", ", ExternalSuppressionDetection.DetectedPackageIds().ToArray());
                listing.Label($"<color=#FFCC00>Detected another suppression source: {detected}</color>");
            }

            if (Settings.enableSuppressionEngine)
            {
                if (combatExtended)
                {
                    listing.Label("<color=#FF6666><b>Combat Extended is active.</b> CE replaces the combat model rather than "
                        + "layering on it. Running both suppression systems means a pawn accumulates suppression from two "
                        + "independent sources, and CE's own pinning logic reacts to its own values, not to Fire Discipline's. "
                        + "Expect pawns to be pinned roughly twice as often as either mod intends.</color>");
                }
                else if (external)
                {
                    listing.Label("<color=#FFCC00><b>Both systems are running.</b> The same pawn will receive suppression from "
                        + "Fire Discipline and from the other mod at the same time. Severity builds about twice as fast, and "
                        + "each mod applies its own debuffs on top of the other's.</color>");
                }
                else
                {
                    listing.Label("<color=#99FF99>Engine active. Nothing else on this modlist applies suppression.</color>");
                }
            }
            else
            {
                listing.Label("<color=#FFCC00><b>Engine off. These mechanics do not exist while it is off:</b></color>");
                listing.Label("  - Rapid stance inflicting extra suppression (x"
                    + $"{Settings.rapidSuppressionMultiplier:F2})");
                listing.Label("  - Sharpshot taking extra suppression (x"
                    + $"{Settings.sharpshotSuppressionVulnerability:F2}) and losing its aim to incoming fire");
                listing.Label("  - Prone resisting suppression (x"
                    + $"{Settings.proneSuppressionResistance:F2})");
                listing.Label("  - Embrasure suppression resistance");
                listing.Label("  - The Pinned state");
                listing.Label("<color=#99FF99><b>These still work normally:</b></color>");
                listing.Label("  - All four aim stances, their accuracy curves and warmup times");
                listing.Label("  - Gear encumbrance");
                listing.Label("  - Graze");
                listing.Label("  - Combat shock and shell shock");
            }
        }

        public override string SettingsCategory()
        {
            return "Fire Discipline";
        }
    }

    [StaticConstructorOnStartup]
    public static class FireDisciplineStartup
    {
        static FireDisciplineStartup()
        {
            // Must run before any ShouldEnable() call reads enableSuppressionEngine.
            FireDisciplineMod.Settings?.ApplyFirstRunDefaults();

            PatchRegistry.RegisterModule(new EncumbranceModule());
            PatchRegistry.RegisterModule(new AimStanceModule());
            PatchRegistry.RegisterModule(new SuppressionCoreModule());
            PatchRegistry.RegisterModule(new ShotgunAoEModule());
            PatchRegistry.RegisterModule(new GrazeModule());
            PatchRegistry.RegisterModule(new ShockModule());

            PatchRegistry.InitializeAll();
        }
    }
}
