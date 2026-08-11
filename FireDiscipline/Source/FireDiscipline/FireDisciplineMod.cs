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
            Rect viewRect = new Rect(0f, 0f, inRect.width - 30f, 2450f);

            Widgets.BeginScrollView(outerRect, ref scrollPosition, viewRect);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(viewRect);

            listing.Label("<b><size=18><color=#66CCFF>Fire Discipline</color></size> — Combat Engine & Tactical Stances</b>");
            listing.Label("<i>Tailor tactical stances, suppression dynamics, cover mechanics, and RNG variance mitigation.</i>");
            listing.GapLine(12f);

            // =========================================================================
            // CATEGORY 1: MODULE TOGGLES & CORE ENGINES
            // =========================================================================
            listing.Label("<b><size=14><color=#66CCFF>1. Core Active Modules</color></size></b>");
            foreach (var module in PatchRegistry.Modules)
            {
                bool currentVal = Settings.IsModuleEnabled(module);
                bool prevVal = currentVal;
                listing.CheckboxLabeled($"<b>{module.DisplayName}</b>: <i>{module.Description}</i>", ref currentVal);
                if (currentVal != prevVal)
                {
                    Settings.SetModuleEnabled(module, currentVal);
                    module.IsEnabled = currentVal;

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

            listing.GapLine(15f);

            // =========================================================================
            // CATEGORY 2: TACTICAL STANCES & POSTURES
            // =========================================================================
            listing.Label("<b><size=14><color=#FFD700>2. Tactical Stances & Dug-In Posture</color></size></b>");
            
            // 2a. Sharpshot Stance
            listing.Label("  <b><color=#FFD700>[Sharpshot Stance — Sniper Precision]</color></b>");
            listing.Label($"  Warmup Time Multiplier: <b>x{Settings.sharpshotWarmupMultiplier:F2}</b> (Default: x1.40)");
            Settings.sharpshotWarmupMultiplier = listing.Slider(Settings.sharpshotWarmupMultiplier, 1.00f, 2.50f);

            listing.Label($"  Distance Exponent Factor: <b>d * {Settings.sharpshotDistanceExponentFactor:F2}</b> (Default: d * 0.80)");
            Settings.sharpshotDistanceExponentFactor = listing.Slider(Settings.sharpshotDistanceExponentFactor, 0.50f, 1.00f);

            listing.Label($"  Close Range (<5c) Accuracy Penalty: <b>x{Settings.sharpshotCloseRangePenalty:F2}</b> (Default: x0.70)");
            Settings.sharpshotCloseRangePenalty = listing.Slider(Settings.sharpshotCloseRangePenalty, 0.30f, 1.00f);

            listing.Label($"  Received Suppression Vulnerability: <b>x{Settings.sharpshotSuppressionVulnerability:F2}</b> (Default: x2.00)");
            Settings.sharpshotSuppressionVulnerability = listing.Slider(Settings.sharpshotSuppressionVulnerability, 1.00f, 3.00f);

            listing.Label($"  Target Cover Block Bypass: <b>-{(Settings.sharpshotCoverBypassFactor * 100f):F0}%</b> (Default: -50%)");
            Settings.sharpshotCoverBypassFactor = listing.Slider(Settings.sharpshotCoverBypassFactor, 0.10f, 0.80f);

            listing.Gap(10f);

            // 2b. Rapid Fire Stance
            listing.Label("  <b><color=#FF5555>[Rapid Fire Stance — Close-Quarters Hipfire]</color></b>");
            listing.Label($"  Min Warmup Ratio Clamp: <b>x{Settings.rapidMinWarmupRatio:F2}</b> (Default: x0.30)");
            Settings.rapidMinWarmupRatio = listing.Slider(Settings.rapidMinWarmupRatio, 0.10f, 0.50f);

            listing.Label($"  Max Warmup Ratio Clamp: <b>x{Settings.rapidMaxWarmupRatio:F2}</b> (Default: x0.75)");
            Settings.rapidMaxWarmupRatio = listing.Slider(Settings.rapidMaxWarmupRatio, 0.50f, 0.95f);

            listing.Label($"  Inflicted Suppression Multiplier: <b>x{Settings.rapidSuppressionMultiplier:F2}</b> (Default: x1.50)");
            Settings.rapidSuppressionMultiplier = listing.Slider(Settings.rapidSuppressionMultiplier, 1.00f, 2.50f);

            listing.Label($"  Max Suppressed Target Cover Degradation: <b>-{(Settings.suppressionCoverDegradationMax * 100f):F0}%</b> (Default: -40%)");
            Settings.suppressionCoverDegradationMax = listing.Slider(Settings.suppressionCoverDegradationMax, 0.10f, 0.70f);

            listing.CheckboxLabeled("  Enable Rapid Full-Auto Burst Expansion", ref Settings.enableRapidFullAuto,
                "Expands burst shot count for heavy automatic weapons (burst >= min gate) in exchange for a cooldown penalty.");
            if (Settings.enableRapidFullAuto)
            {
                listing.Label($"    Full-Auto Min Burst Gate: <b>{Settings.fullAutoMinBurstCount} rounds/burst</b> (Default: 5)");
                Settings.fullAutoMinBurstCount = Mathf.RoundToInt(listing.Slider(Settings.fullAutoMinBurstCount, 3f, 10f));

                listing.Label($"    Full-Auto Burst Multiplier: <b>x{Settings.fullAutoBurstMultiplier:F2}</b> (Default: x1.50)");
                Settings.fullAutoBurstMultiplier = listing.Slider(Settings.fullAutoBurstMultiplier, 1.10f, 2.50f);

                listing.Label($"    Full-Auto Cooldown Penalty: <b>x{Settings.fullAutoCooldownMultiplier:F2}</b> (Default: x1.60)");
                Settings.fullAutoCooldownMultiplier = listing.Slider(Settings.fullAutoCooldownMultiplier, 1.00f, 2.50f);
            }

            listing.Gap(10f);

            // 2c. Passive Dug-In (Prone)
            listing.Label("  <b><color=#55FF55>[Passive Dug-In (Prone) Condition]</color></b> <i>(Auto-activates when stationary & stacks with stances)</i>");
            listing.Label($"  Target Size Reduction Factor: <b>x{Settings.proneTargetSizeFactor:F2}</b> (Default: x0.65)");
            Settings.proneTargetSizeFactor = listing.Slider(Settings.proneTargetSizeFactor, 0.30f, 0.95f);

            listing.Label($"  Shooter Accuracy Multiplier: <b>x{Settings.proneAccuracyMultiplier:F2}</b> (Default: x0.85)");
            Settings.proneAccuracyMultiplier = listing.Slider(Settings.proneAccuracyMultiplier, 0.50f, 1.00f);

            listing.Label($"  Received Suppression Resistance: <b>x{Settings.proneSuppressionResistance:F2}</b> (Default: x0.50)");
            Settings.proneSuppressionResistance = listing.Slider(Settings.proneSuppressionResistance, 0.10f, 0.90f);

            listing.GapLine(15f);

            // =========================================================================
            // CATEGORY 3: SUPPRESSION & COVER DYNAMICS
            // =========================================================================
            listing.Label("<b><size=14><color=#FFAA55>3. Suppression & Cover Dynamics</color></size></b>");
            listing.CheckboxLabeled("Enable Fire Discipline's Suppression Engine", ref Settings.enableSuppressionEngine,
                "Incoming fire pins pawns down and drives the stance matrix.");
            DrawSuppressionTradeOff(listing);

            listing.Gap(5f);
            listing.CheckboxLabeled("Enable Cover Suppression Reduction", ref Settings.enableCoverSuppression,
                "Cover reduces incoming suppression severity.");

            if (Settings.enableCoverSuppression)
            {
                listing.Label($"  Cover Suppression Factor: <b>{Settings.coverSuppressionFactor:F2}</b> (Default: 0.85)");
                Settings.coverSuppressionFactor = listing.Slider(Settings.coverSuppressionFactor, 0.10f, 1.00f);

                listing.Label($"  Cover Suppression Floor: <b>{Settings.coverSuppressionFloor:F2}</b> (Default: 0.25 - minimum suppression received)");
                Settings.coverSuppressionFloor = listing.Slider(Settings.coverSuppressionFloor, 0.10f, 0.80f);
            }

            listing.Gap(5f);
            listing.CheckboxLabeled("Enable Intermediate Line Cover Stacking", ref Settings.enableCoverStacking,
                "Accumulates cover from non-adjacent obstacles along the line of fire (ShootLine).");

            if (Settings.enableCoverStacking)
            {
                listing.Label($"  Line Cover Effectiveness Factor: <b>{Settings.lineCoverFactor:F2}</b> (Default: 0.50)");
                Settings.lineCoverFactor = listing.Slider(Settings.lineCoverFactor, 0.10f, 1.00f);

                listing.Label($"  Max Total Cover Stacking Cap: <b>{Settings.coverStackingCap:F2}</b> (Default: 0.85)");
                Settings.coverStackingCap = listing.Slider(Settings.coverStackingCap, 0.50f, 0.95f);

                listing.Label($"  Min Exclusion Distance from Shooter: <b>{Settings.lineCoverMinDistanceFromShooter} cells</b> (Default: 3)");
                Settings.lineCoverMinDistanceFromShooter = Mathf.RoundToInt(listing.Slider(Settings.lineCoverMinDistanceFromShooter, 1f, 6f));
            }

            listing.Gap(5f);
            listing.CheckboxLabeled("Enable Pinned State at High Suppression", ref Settings.enablePinnedState,
                "Pawns under high suppression cannot fire ranged weapons at all until severity decays.");

            if (Settings.enablePinnedState)
            {
                listing.Label($"  Pinned Severity Threshold: <b>{Settings.pinnedSeverityThreshold:F1}</b> (of 9.0 max; cowering starts 5.5)");
                Settings.pinnedSeverityThreshold = listing.Slider(Settings.pinnedSeverityThreshold, 2.0f, 9.0f);
            }

            listing.Gap(5f);
            listing.Label($"Suppression Move Speed Penalties (Stage 1-4):");
            listing.Label($"  Shaken: <b>x{Settings.suppressionMoveSpeedFactorStage1:F2}</b> | Wavering: <b>x{Settings.suppressionMoveSpeedFactorStage2:F2}</b> | Ducking: <b>x{Settings.suppressionMoveSpeedFactorStage3:F2}</b> | Cowering: <b>x{Settings.suppressionMoveSpeedFactorStage4:F2}</b>");
            Settings.suppressionMoveSpeedFactorStage1 = listing.Slider(Settings.suppressionMoveSpeedFactorStage1, 0.10f, 1.00f);
            Settings.suppressionMoveSpeedFactorStage2 = listing.Slider(Settings.suppressionMoveSpeedFactorStage2, 0.10f, 1.00f);
            Settings.suppressionMoveSpeedFactorStage3 = listing.Slider(Settings.suppressionMoveSpeedFactorStage3, 0.10f, 1.00f);
            Settings.suppressionMoveSpeedFactorStage4 = listing.Slider(Settings.suppressionMoveSpeedFactorStage4, 0.05f, 1.00f);

            listing.GapLine(15f);

            // =========================================================================
            // CATEGORY 4: HIT VARIANCE MITIGATION (QUOTA ENGINE)
            // =========================================================================
            listing.Label("<b><size=14><color=#FFCC00>4. Hit Variance Mitigation (RNG Control)</color></size></b>");
            listing.CheckboxLabeled("Enable Hit Variance Mitigation (Unified Quota Model)", ref Settings.enableHitVariance,
                "Applies Universal Expectation Preservation (Quota-Carry) across ALL ranged weapons (both single-shot & burst weapons), guaranteeing exact DPS expectations and eliminating RNG miss streaks.");

            listing.GapLine(15f);

            // =========================================================================
            // CATEGORY 5: GRAZE, SHOCK & SPECIAL COMBAT
            // =========================================================================
            listing.Label("<b><size=14><color=#00FFFF>5. Graze, Shock & Special Combat Systems</color></size></b>");

            listing.Label("  <b><color=#00FFFF>[Graze System — Anti-One-Shot Protection]</color></b>");
            listing.Label($"  Never Graze Above Hit Chance: <b>{(int)(Settings.grazeHitChanceCeiling * 100f)}%</b> (Default: 65%)");
            Settings.grazeHitChanceCeiling = listing.Slider(Settings.grazeHitChanceCeiling, 0.30f, 0.95f);

            listing.Label($"  Graze Ramp Width: <b>{(int)(Settings.grazeChanceSpan * 100f)}%</b>");
            Settings.grazeChanceSpan = listing.Slider(Settings.grazeChanceSpan, 0.10f, 0.80f);

            listing.Label($"  Graze Damage Retained: <b>{(int)(Settings.grazeDamageMultiplier * 100f)}%</b> (-{(int)((1f - Settings.grazeDamageMultiplier) * 100f)}% damage reduction)");
            Settings.grazeDamageMultiplier = listing.Slider(Settings.grazeDamageMultiplier, 0.10f, 0.50f);

            listing.CheckboxLabeled("  Reroute Vital Organ Shots (Brain/Heart) to Limbs on Graze", ref Settings.protectVitalOrgans);

            listing.Gap(10f);
            listing.Label("  <b><color=#FF9933>[Combat Shock & Shell Shock]</color></b>");
            listing.Label($"  Ally Downed Shock Radius: <b>{Settings.allyShockRadius:F1} cells</b> (Default: 6.0c)");
            Settings.allyShockRadius = listing.Slider(Settings.allyShockRadius, 3.0f, 12.0f);

            listing.Label($"  Shell Shock Radius Cap: <b>{Settings.shellShockRadiusCap:F0}c</b> (Default: 20c)");
            Settings.shellShockRadiusCap = listing.Slider(Settings.shellShockRadiusCap, 8f, 40f);

            listing.Gap(10f);
            listing.Label("  <b><color=#00FFaa>[Embrasure Interaction]</color></b>");
            listing.CheckboxLabeled("  Enable Embrasure Interaction Accuracy Penalty", ref Settings.enableEmbrasureInteraction);
            if (Settings.enableEmbrasureInteraction)
            {
                listing.Label($"    Embrasure Firing Accuracy Multiplier: <b>{(int)(Settings.embrasureAccuracyMultiplier * 100f)}%</b>");
                Settings.embrasureAccuracyMultiplier = listing.Slider(Settings.embrasureAccuracyMultiplier, 0.50f, 1.00f);
            }

            listing.GapLine(15f);

            // =========================================================================
            // CATEGORY 6: WEAPON CLASSIFICATION & DIAGNOSTICS
            // =========================================================================
            listing.Label("<b><size=14><color=#99FF99>6. Weapon Classification & Diagnostics</color></size></b>");
            listing.Label("<i>Derived dynamically from vanilla accuracy stats — works with all modded weapons out-of-the-box.</i>");

            listing.Label($"Shotgun Min Range: <b>{Settings.shotgunMinRange:F0}c</b> | Max Range: <b>{Settings.shotgunMaxRange:F0}c</b>");
            Settings.shotgunMinRange = listing.Slider(Settings.shotgunMinRange, 0f, 20f);
            Settings.shotgunMaxRange = listing.Slider(Settings.shotgunMaxRange, 10f, 35f);

            listing.Label($"Rapid d0 Base: <b>{Settings.d0Base:F0}c</b> | d0 Span: <b>{Settings.d0Span:F0}c</b>");
            Settings.d0Base = listing.Slider(Settings.d0Base, 1f, 12f);
            Settings.d0Span = listing.Slider(Settings.d0Span, 0f, 24f);

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
