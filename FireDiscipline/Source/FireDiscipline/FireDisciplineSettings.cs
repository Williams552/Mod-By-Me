using System.Collections.Generic;
using FireDiscipline.Core;
using Verse;

namespace FireDiscipline
{
    /// <summary>
    /// Centralized configuration file for all Fire Discipline modules.
    /// Single source of truth for all formulas, multipliers, thresholds, and explanation strings.
    /// </summary>
    public class FireDisciplineSettings : ModSettings
    {
        public Dictionary<string, bool> moduleEnabledStates = new Dictionary<string, bool>();

        // =========================================================================
        // SECTION 1: ENCUMBRANCE MODULE CONFIGURATION (Module 5.3)
        // =========================================================================
        public float encumbranceThreshold = 0.15f;           // 15% capacity threshold before speed penalty begins
        public float encumbranceMaxPenalty = 0.35f;           // 35% max move speed penalty from mass

        // =========================================================================
        // SECTION 2: AIM STANCE v2 CONFIGURATION (Module 5.2)
        // =========================================================================
        // 2.1 Rapid Fire Stance
        public float rapidMinWarmupRatio = 0.30f;              // Min warmup ratio clamp (x0.30)
        public float rapidMaxWarmupRatio = 0.75f;              // Max warmup ratio clamp (x0.75)
        public float rapidSuppressionMultiplier = 1.50f;       // Inflicted suppression multiplier (x1.5)

        // 2.1b Rapid Full-Auto (Wave B6 - default OFF)
        public bool enableRapidFullAuto = false;               // Full-auto burst expansion for heavy automatics
        public int fullAutoMinBurstCount = 5;                  // Min burst shot count gate (default 5, excluding AR/SMG)
        public float fullAutoBurstMultiplier = 1.50f;          // Multiplier for burst shot count in Rapid stance (x1.5)
        public float fullAutoCooldownMultiplier = 1.60f;       // Cooldown penalty multiplier for Rapid Full-Auto (x1.6)

        // 2.3 Hit Variance Mitigation (Wave B8 - default OFF)
        public bool enableHitVariance = false;                 // Variance mitigation module toggle
        public bool varianceQuotaForSingleShot = true;        // Quota-carry model for single-shot weapons
        public bool variancePityForBurst = true;              // Pity-symmetric model for burst weapons
        public float variancePityStep = 0.08f;                // Pity step offset per shot (default 0.08)
        public float variancePityClamp = 0.32f;               // Max pity offset clamp (default 0.32)

        // 2.2 Sharpshot Stance
        public float sharpshotWarmupMultiplier = 1.40f;        // Warmup time multiplier (x1.4)
        public float sharpshotDistanceExponentFactor = 0.80f;   // Distance exponent factor (d * 0.80)
        public float sharpshotCloseRangePenalty = 0.70f;        // Under 5 cells accuracy penalty (x0.70)
        public float sharpshotSuppressionVulnerability = 2.00f; // Received suppression multiplier (x2.0)
        public float sharpshotCoverBypassFactor = 0.50f;        // Bypasses 50% of target cover block chance in Sharpshot stance
        public float suppressionCoverDegradationMax = 0.40f;    // Max reduction (40%) of target cover block chance when fully suppressed

        // 2.3 Prone Stance
        public float proneTargetSizeFactor = 0.65f;            // Target size reduction (x0.65)
        public float proneMoveSpeedMultiplier = 0.60f;          // Move speed multiplier (-40% speed / x0.60)
        public float proneAccuracyMultiplier = 0.85f;           // Accuracy multiplier (x0.85)
        public float proneSuppressionResistance = 0.50f;       // Received suppression multiplier (x0.50)

        // 2.4 Transition Costs
        public int stanceTransitionTicks = 45;                 // 45 ticks (0.75s) delay when switching into non-Standard stance

        // =========================================================================
        // SECTION 2b: WEAPON CLASSIFICATION (architecture rule 2 - derive, never declare)
        // =========================================================================
        // Every weapon-shape judgement is derived from vanilla accuracy stats so weapons from any
        // mod classify without a hardcoded list. Rewritten after the debug action E audit showed the
        // previous single predicate (AccuracyTouch >= AccuracyMedium) misclassified ~68% of the
        // weapons it flagged as shotguns, and put 64% of all ranged weapons on the wide d0 branch.
        //
        // The root cause was one predicate answering two opposite questions: a shotgun has a FLAT
        // curve that decays gently, while a wide d0 belongs to a weapon whose curve falls off
        // STEEPLY with range. Those are now separate calculations.

        // Shotgun profile gates. Previous logic: (AccuracyTouch >= AccuracyMedium) && range <= 25.
        // Gate 1 (flat line) is an exact equality test with no tunable threshold, so it has no setting.
        public float shotgunMinRange = 8f;               // Below this it is a thrown weapon or a flamer, not a shotgun
        // 25 -> 20 -> 17. Every real shotgun in the audited modlist tops out at range 15.9, while a
        // gauss magnum pistol sits at 19.9 and passed every other gate. Range is what actually
        // separates pistols from shotguns here; the long/short ratio does not.
        public float shotgunMaxRange = 17f;
        public float shotgunMinPeakAccuracy = 0.55f;     // A shotgun is accurate somewhere; junk weapons are not
        public float shotgunMinLongShortRatio = 0.50f;   // Long/Short: the flatness test. SMGs collapse well below this
        // Note: the Touch >= Medium test is retained as the final gate, not as the whole test.

        // Continuous d0. Previous logic: binary 12 if AccuracyTouch >= AccuracyMedium, else 5.
        public float d0Base = 4f;                        // d0 for a pure long-range weapon (closeBias = 0)
        public float d0Span = 12f;                       // Added at closeBias = 1, giving a 4..16 range

        // Shared weapon filter: excludes turret and artillery armaments, which are not pawn tactics.
        public float weaponFilterMaxRange = 100f;        // Mortars sit at 500

        // =========================================================================
        // SECTION 3: GRAZE MODULE CONFIGURATION (Module 5.4)
        // =========================================================================
        // grazeChance = clamp(0, 1, (hitChanceCeiling - p) / chanceSpan)
        // Replaces the old grazeBaseChance, which had a slider but no code path: the v3 formula
        // derives graze probability from the shot's hit chance, so a flat base chance meant nothing.
        public float grazeHitChanceCeiling = 0.65f;            // At or above this hit chance, never graze
        public float grazeChanceSpan = 0.45f;                  // Width of the ramp down to always-graze
        public float grazeDamageMultiplier = 0.35f;            // 35% damage retained (65% damage reduction)
        public bool protectVitalOrgans = true;                 // Reroute vital organ shots to limbs

        // =========================================================================
        // SECTION 4: SHOCK & SHELL SHOCK MODULE CONFIGURATION (Module 5.5)
        // =========================================================================
        public float allyShockRadius = 6.0f;                   // Radius around downed/killed ally to trigger Combat Shock
        // shockRadius = min(cap, r + coefficient * sqrt(r))
        // Replaces shellShockRadiusMultiplier, which had a slider but no code path: the v3 formula
        // is non-linear, so a flat multiplier was never read. Mortar r=4.9 -> 9.3c, Doomsday -> cap.
        public float shellShockRadiusCoefficient = 2.0f;       // Multiplier on sqrt(r)
        public float shellShockRadiusCap = 20f;                // Hard ceiling, about half of max weapon range

        // =========================================================================
        // SECTION 4a: EMBRASURE INTERACTION (Wave B4)
        // =========================================================================
        // Feature is OFF by default pending release decision. Applies an accuracy penalty
        // when firing through narrow embrasure slits. Cover suppression resistance is handled
        // automatically by standard cover calculation.
        public bool enableEmbrasureInteraction = false;
        public float embrasureAccuracyMultiplier = 0.85f;     // x0.85 accuracy multiplier when firing from behind embrasures

        // =========================================================================
        // SECTION 4b: SUPPRESSION ENGINE (Module 5.0)
        // =========================================================================
        // The engine is always present; this is the player's switch, not a compatibility gate.
        // The first-run value is chosen from what else is installed (see ApplyFirstRunDefaults),
        // after which the player owns it and detection never overrides them again.
        public bool enableSuppressionEngine = true;
        public bool suppressionEngineDefaultApplied = false;

        public float suppressionBaseAmount = 0.25f;           // Severity added by one round landing nearby
        public float suppressionRadius = 3.5f;                // Cells around an impact that feel it
        public float suppressionDecayPerSecond = 0.10f;       // Recovery rate once the shooting stops
        public int suppressionDecayDelayTicks = 120;          // Grace period after the last round lands
        public bool enablePinnedState = true;
        public float pinnedSeverityThreshold = 7.0f;

        // =========================================================================
        // SECTION 4d: COVER SUPPRESSION (Wave B3)
        // =========================================================================
        public bool enableCoverSuppression = true;
        // The 5.8 design document originally stated 0.40 and floor 0.35,
        // but those values were never implemented or tuned until Wave B3.
        public float coverSuppressionFactor = 0.85f;
        public float coverSuppressionFloor = 0.25f;

        // Line Cover Stacking (Intermediate cover along ShootLine)
        public bool enableCoverStacking = false;
        public float lineCoverFactor = 0.50f;
        public float coverStackingCap = 0.85f;
        public int lineCoverMinDistanceFromShooter = 3;

        // =========================================================================
        // SECTION 4c: SHOTGUN SPREAD (Wave B2)
        // =========================================================================
        // Spread is a WEDGE from the muzzle toward the target, not a shape centred on the impact.
        //
        // Shapes tried and discarded:
        //   full disc at impact  - reached backwards past the muzzle; the shooter was caught in it
        //   half-disc at impact  - fixed that, but playtesting found the real problem: when the
        //                          target is not the nearest enemy, the whole footprint lands out
        //                          at the target and the enemies closing in are untouched. The
        //                          weapon lost the exact job it exists for.
        //
        // A wedge covers the lane from the muzzle outward, so anything between the shooter and the
        // target is inside it. Geometry copied in shape from vanilla's Biotech Fire Spew
        // (CompProperties_AbilityFireSpew: range 7.9, lineWidthEnd 3), which is defined by WIDTH AT
        // THE END rather than by an angle - the reason a true cone was rejected earlier, since an
        // angle-based cone opens to 40 degrees at point-blank range.
        public float shotgunSpreadWidthEnd = 3.0f;            // Full width at the far end; the muzzle end is 1 cell
        public float shotgunEdgeDamageMin = 0.15f;            // Edge damage fraction at shooting skill 0
        public float shotgunEdgeDamageMax = 0.55f;            // Edge damage fraction at shooting skill 20
        public float shotgunPrimaryDamageMultiplier = 0.70f;  // Splash Damage Base: 70% of the primary hit's damage
        public float shotgunSplashSuppressionMultiplier = 0.40f; // Splash suppresses at a reduced rate
        public bool shotgunFriendlyFire = true;               // Design 5.5(a): ship both, default ON, read feedback

        // =========================================================================
        // SECTION 5b: SUPPRESSION MOVE SPEED PENALTY
        // =========================================================================
        public float suppressionMoveSpeedFactorStage1 = 0.95f;
        public float suppressionMoveSpeedFactorStage2 = 0.80f;
        public float suppressionMoveSpeedFactorStage3 = 0.50f;
        public float suppressionMoveSpeedFactorStage4 = 0.15f;
        public float suppressedMoveSpeedFloor = 0.7f;

        // =========================================================================
        // SECTION 6: HARMONY PATCH TOGGLES & LOGGING
        // =========================================================================
        public bool enableHighPrecisionShotReportPatch = true; // Toggle for ShotReport Harmony Postfix
        public bool verboseCombatLogging = false;              // Toggle for hotpath combat event logging (Player.log)

        /// <summary>
        /// Picks the suppression engine's value the first time this mod runs, based on what else is
        /// installed. Runs exactly once: after that the player owns the setting and detection never
        /// overrides them, even if they later add or remove a suppression mod.
        /// </summary>
        public void ApplyFirstRunDefaults()
        {
            if (suppressionEngineDefaultApplied) return;

            enableSuppressionEngine = Suppression.ExternalSuppressionDetection.RecommendedDefault();
            suppressionEngineDefaultApplied = true;
            Write();

            Log.Message($"[Fire Discipline] First run: suppression engine defaulted to {(enableSuppressionEngine ? "ON" : "OFF")}"
                + (enableSuppressionEngine ? "." : " because another suppression mod was detected. You can turn it on in mod settings."));
        }

        public bool IsModuleEnabled(IModule module)
        {
            if (moduleEnabledStates != null && moduleEnabledStates.TryGetValue(module.ModuleId, out bool state))
            {
                return state;
            }
            return module.DefaultEnabled;
        }

        public void SetModuleEnabled(IModule module, bool enabled)
        {
            if (moduleEnabledStates == null)
            {
                moduleEnabledStates = new Dictionary<string, bool>();
            }
            moduleEnabledStates[module.ModuleId] = enabled;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref moduleEnabledStates, "moduleEnabledStates", LookMode.Value, LookMode.Value);

            // Encumbrance
            Scribe_Values.Look(ref encumbranceThreshold, "encumbranceThreshold", 0.15f);
            Scribe_Values.Look(ref encumbranceMaxPenalty, "encumbranceMaxPenalty", 0.35f);

            // Rapid
            Scribe_Values.Look(ref rapidMinWarmupRatio, "rapidMinWarmupRatio", 0.30f);
            Scribe_Values.Look(ref rapidMaxWarmupRatio, "rapidMaxWarmupRatio", 0.75f);
            Scribe_Values.Look(ref rapidSuppressionMultiplier, "rapidSuppressionMultiplier", 1.50f);
            Scribe_Values.Look(ref enableRapidFullAuto, "enableRapidFullAuto", false);
            Scribe_Values.Look(ref fullAutoMinBurstCount, "fullAutoMinBurstCount", 5);
            Scribe_Values.Look(ref fullAutoBurstMultiplier, "fullAutoBurstMultiplier", 1.50f);
            Scribe_Values.Look(ref fullAutoCooldownMultiplier, "fullAutoCooldownMultiplier", 1.60f);
            Scribe_Values.Look(ref enableHitVariance, "enableHitVariance", false);
            Scribe_Values.Look(ref varianceQuotaForSingleShot, "varianceQuotaForSingleShot", true);
            Scribe_Values.Look(ref variancePityForBurst, "variancePityForBurst", true);
            Scribe_Values.Look(ref variancePityStep, "variancePityStep", 0.08f);
            Scribe_Values.Look(ref variancePityClamp, "variancePityClamp", 0.32f);

            // Sharpshot
            Scribe_Values.Look(ref sharpshotWarmupMultiplier, "sharpshotWarmupMultiplier", 1.40f);
            Scribe_Values.Look(ref sharpshotDistanceExponentFactor, "sharpshotDistanceExponentFactor", 0.80f);
            Scribe_Values.Look(ref sharpshotCloseRangePenalty, "sharpshotCloseRangePenalty", 0.70f);
            Scribe_Values.Look(ref sharpshotSuppressionVulnerability, "sharpshotSuppressionVulnerability", 2.00f);
            Scribe_Values.Look(ref sharpshotCoverBypassFactor, "sharpshotCoverBypassFactor", 0.50f);
            Scribe_Values.Look(ref suppressionCoverDegradationMax, "suppressionCoverDegradationMax", 0.40f);

            // Prone
            Scribe_Values.Look(ref proneTargetSizeFactor, "proneTargetSizeFactor", 0.65f);
            Scribe_Values.Look(ref proneMoveSpeedMultiplier, "proneMoveSpeedMultiplier", 0.60f);
            Scribe_Values.Look(ref proneAccuracyMultiplier, "proneAccuracyMultiplier", 0.85f);
            Scribe_Values.Look(ref proneSuppressionResistance, "proneSuppressionResistance", 0.50f);

            // Transitions & Patches
            Scribe_Values.Look(ref stanceTransitionTicks, "stanceTransitionTicks", 45);
            Scribe_Values.Look(ref enableHighPrecisionShotReportPatch, "enableHighPrecisionShotReportPatch", true);
            Scribe_Values.Look(ref verboseCombatLogging, "verboseCombatLogging", false);

            // Weapon classification (architecture rule 2)
            Scribe_Values.Look(ref shotgunMinRange, "shotgunMinRange", 8f);
            Scribe_Values.Look(ref shotgunMaxRange, "shotgunMaxRange", 17f);
            Scribe_Values.Look(ref shotgunMinPeakAccuracy, "shotgunMinPeakAccuracy", 0.55f);
            Scribe_Values.Look(ref shotgunMinLongShortRatio, "shotgunMinLongShortRatio", 0.50f);
            Scribe_Values.Look(ref d0Base, "d0Base", 4f);
            Scribe_Values.Look(ref d0Span, "d0Span", 12f);
            Scribe_Values.Look(ref weaponFilterMaxRange, "weaponFilterMaxRange", 100f);

            // Graze
            Scribe_Values.Look(ref grazeHitChanceCeiling, "grazeHitChanceCeiling", 0.65f);
            Scribe_Values.Look(ref grazeChanceSpan, "grazeChanceSpan", 0.45f);
            Scribe_Values.Look(ref grazeDamageMultiplier, "grazeDamageMultiplier", 0.35f);
            Scribe_Values.Look(ref protectVitalOrgans, "protectVitalOrgans", true);

            // Shock
            Scribe_Values.Look(ref allyShockRadius, "allyShockRadius", 6.0f);
            Scribe_Values.Look(ref shellShockRadiusCoefficient, "shellShockRadiusCoefficient", 2.0f);
            Scribe_Values.Look(ref shellShockRadiusCap, "shellShockRadiusCap", 20f);

            // Embrasure Interaction (Wave B4 - default OFF)
            Scribe_Values.Look(ref enableEmbrasureInteraction, "enableEmbrasureInteraction", false);
            Scribe_Values.Look(ref embrasureAccuracyMultiplier, "embrasureAccuracyMultiplier", 0.85f);

            // Suppression engine
            Scribe_Values.Look(ref enableSuppressionEngine, "enableSuppressionEngine", true);
            Scribe_Values.Look(ref suppressionEngineDefaultApplied, "suppressionEngineDefaultApplied", false);
            Scribe_Values.Look(ref suppressionBaseAmount, "suppressionBaseAmount", 0.25f);
            Scribe_Values.Look(ref suppressionRadius, "suppressionRadius", 3.5f);
            Scribe_Values.Look(ref suppressionDecayPerSecond, "suppressionDecayPerSecond", 0.10f);
            Scribe_Values.Look(ref suppressionDecayDelayTicks, "suppressionDecayDelayTicks", 120);
            Scribe_Values.Look(ref enablePinnedState, "enablePinnedState", true);
            Scribe_Values.Look(ref pinnedSeverityThreshold, "pinnedSeverityThreshold", 7.0f);

            // Cover Suppression (Wave B3)
            Scribe_Values.Look(ref enableCoverSuppression, "enableCoverSuppression", true);
            Scribe_Values.Look(ref coverSuppressionFactor, "coverSuppressionFactor", 0.85f);
            Scribe_Values.Look(ref coverSuppressionFloor, "coverSuppressionFloor", 0.25f);
            Scribe_Values.Look(ref enableCoverStacking, "enableCoverStacking", false);
            Scribe_Values.Look(ref lineCoverFactor, "lineCoverFactor", 0.50f);
            Scribe_Values.Look(ref coverStackingCap, "coverStackingCap", 0.85f);
            Scribe_Values.Look(ref lineCoverMinDistanceFromShooter, "lineCoverMinDistanceFromShooter", 3);

            // Shotgun spread (Wave B2 - module default OFF)
            Scribe_Values.Look(ref shotgunSpreadWidthEnd, "shotgunSpreadWidthEnd", 3.0f);
            Scribe_Values.Look(ref shotgunEdgeDamageMin, "shotgunEdgeDamageMin", 0.15f);
            Scribe_Values.Look(ref shotgunEdgeDamageMax, "shotgunEdgeDamageMax", 0.55f);
            Scribe_Values.Look(ref shotgunPrimaryDamageMultiplier, "shotgunPrimaryDamageMultiplier", 0.70f);
            Scribe_Values.Look(ref shotgunSplashSuppressionMultiplier, "shotgunSplashSuppressionMultiplier", 0.40f);
            Scribe_Values.Look(ref shotgunFriendlyFire, "shotgunFriendlyFire", true);

            // Suppression Move Speed Penalties
            Scribe_Values.Look(ref suppressionMoveSpeedFactorStage1, "suppressionMoveSpeedFactorStage1", 0.95f);
            Scribe_Values.Look(ref suppressionMoveSpeedFactorStage2, "suppressionMoveSpeedFactorStage2", 0.80f);
            Scribe_Values.Look(ref suppressionMoveSpeedFactorStage3, "suppressionMoveSpeedFactorStage3", 0.50f);
            Scribe_Values.Look(ref suppressionMoveSpeedFactorStage4, "suppressionMoveSpeedFactorStage4", 0.15f);
            Scribe_Values.Look(ref suppressedMoveSpeedFloor, "suppressedMoveSpeedFloor", 0.7f);

            if (moduleEnabledStates == null)
            {
                moduleEnabledStates = new Dictionary<string, bool>();
            }
        }
    }
}
