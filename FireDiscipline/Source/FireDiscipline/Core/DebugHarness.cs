using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using FireDiscipline.AimStance;
using FireDiscipline.Graze;
using FireDiscipline.Encumbrance;
using FireDiscipline.Suppression;
using HarmonyLib;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;

namespace FireDiscipline.Core
{
    /// <summary>
    /// Debug harness v3 for evaluating exact HitReport, DPS matrix, and Target Hit matrix across
    /// Skills (4, 10, 16, 20) x Distances (Touch, Short, Medium, Long) x 4 Stances.
    /// Provides empirical measurement foundation for tuning.
    /// </summary>
    public static class DebugHarness
    {
        // Monitoring band around the shotgun flatness gate - reporting only, never a classification input.
        private const float BorderlineRatioLow = 0.45f;
        private const float BorderlineRatioHigh = 0.58f;

        private static readonly int[] skills = new int[] { 4, 10, 16, 20 };
        private static readonly int[] distances = new int[] { 3, 12, 25, 40 };
        private static readonly string[] distLabels = new string[] { "Touch (3c)", "Short (12c)", "Medium (25c)", "Long (40c)" };

        [DebugAction("Fire Discipline", "Print HitReport & DPS Matrix", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void PrintHitReportMatrix()
        {
            Pawn selectedPawn = Find.Selector.SingleSelectedThing as Pawn;
            if (selectedPawn == null)
            {
                Messages.Message("Please select a Pawn first to run Debug Harness matrix.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=========================================================================================");
            sb.AppendLine($"[Fire Discipline Debug Harness v3] HitReport & DPS Matrix for Pawn: {selectedPawn.LabelShort}");
            sb.AppendLine($"Primary Weapon: {selectedPawn.equipment?.Primary?.def?.defName ?? "None"}");
            sb.AppendLine("=========================================================================================");

            int originalSkill = selectedPawn.skills?.GetSkill(SkillDefOf.Shooting)?.Level ?? 10;
            AimStanceMode originalStance = AimStanceTracker.GetStance(selectedPawn);
            Verb verb = selectedPawn.equipment?.PrimaryEq?.PrimaryVerb;

            if (verb == null)
            {
                Messages.Message("Pawn must have a ranged weapon equipped.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            float warmupBase = verb.verbProps.warmupTime;

            // Cooldown lives on the RangedWeapon_Cooldown stat, not on verbProps. Reading verbProps
            // returned 0 for most weapons and inflated every DPS figure this action has ever printed.
            // Taken from the equipment instance so weapon quality is included.
            float cooldownBase = verb.EquipmentSource?.GetStatValue(StatDefOf.RangedWeapon_Cooldown)
                ?? verb.verbProps.defaultCooldownTime;
            if (cooldownBase <= 0f) cooldownBase = verb.verbProps.defaultCooldownTime;
            int burstCount = verb.verbProps.burstShotCount;
            float baseDamage = verb.verbProps.defaultProjectile?.projectile?.GetDamageAmount(null) ?? 10;

            // The harness temporarily rewrites live pawn state (stance, shooting skill). If anything
            // throws mid-sweep the pawn would be left permanently holding a fabricated skill level,
            // so restoration is guaranteed by finally rather than by reaching the end of the loop.
            try
            {
                foreach (AimStanceMode stance in System.Enum.GetValues(typeof(AimStanceMode)))
                {
                    AimStanceTracker.SetStance(selectedPawn, stance);
                    sb.AppendLine($"\n--- STANCE: {stance} ---");
                    sb.AppendLine("Dist \\ Skill |       Skill 4       |       Skill 10      |       Skill 16      |       Skill 20      |");
                    sb.AppendLine("-------------|---------------------|---------------------|---------------------|---------------------|");

                    for (int d = 0; d < distances.Length; d++)
                    {
                        int dist = distances[d];
                        sb.Append($"{distLabels[d],-12} |");

                        for (int s = 0; s < skills.Length; s++)
                        {
                            int skill = skills[s];
                            if (selectedPawn.skills != null)
                            {
                                selectedPawn.skills.GetSkill(SkillDefOf.Shooting).Level = skill;
                            }

                            IntVec3 targetCell = selectedPawn.Position + new IntVec3(dist, 0, 0);
                            LocalTargetInfo target = new LocalTargetInfo(targetCell);

                            ShotReport report = ShotReport.HitReportFor(selectedPawn, verb, target);
                            float hitChancePct = report.TotalEstimatedHitChance * 100f;

                            // Calculate DPS: (burstCount * damage * hitChance) / (warmup + cooldown)
                            float warmup = warmupBase;
                            if (stance == AimStanceMode.Sharpshot) warmup *= 1.4f;
                            else if (stance == AimStanceMode.Rapid) warmup *= StatPart_AimingDelay.CalculateRapidWarmupRatio(selectedPawn);

                            float cycleTime = Mathf.Max(0.5f, warmup + cooldownBase);
                            float dps = (burstCount * baseDamage * report.TotalEstimatedHitChance) / cycleTime;

                            sb.Append($" {hitChancePct,5:F1}% ({dps,4:F1}dps) |");
                        }
                        sb.AppendLine();
                    }
                }
            }
            finally
            {
                // Restore original states
                AimStanceTracker.SetStance(selectedPawn, originalStance);
                if (selectedPawn.skills != null)
                {
                    selectedPawn.skills.GetSkill(SkillDefOf.Shooting).Level = originalSkill;
                }
            }

            sb.AppendLine("=========================================================================================");
            Log.Message(sb.ToString());
            Messages.Message("HitReport & DPS matrix printed to dev console.", MessageTypeDefOf.PositiveEvent, false);
        }

        [DebugAction("Fire Discipline", "Print Incoming Target Hit Matrix (Prone Verification)", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void PrintIncomingHitMatrix()
        {
            Pawn selectedPawn = Find.Selector.SingleSelectedThing as Pawn;
            if (selectedPawn == null)
            {
                Messages.Message("Please select a Pawn first.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=========================================================================================");
            sb.AppendLine($"[Fire Discipline Debug Harness v3] Incoming Hit Matrix (Enemy Shooting AT {selectedPawn.LabelShort})");
            sb.AppendLine($"Verifies Prone Target Size reduction (x0.65) empirically across Distances & Shooter Skills.");
            sb.AppendLine("=========================================================================================");

            IntVec3 originalPos = selectedPawn.Position;
            int originalSkill = selectedPawn.skills?.GetSkill(SkillDefOf.Shooting)?.Level ?? 10;
            AimStanceMode originalStance = AimStanceTracker.GetStance(selectedPawn);
            Verb verb = selectedPawn.equipment?.PrimaryEq?.PrimaryVerb;

            if (verb == null)
            {
                Messages.Message("Pawn must have a ranged weapon equipped.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            // This sweep displaces the pawn cell by cell to sample hit chance at range. An exception
            // mid-sweep would strand the pawn at a fabricated position with a fabricated skill level,
            // so both are restored in finally regardless of how the loop exits.
            try
            {
                foreach (AimStanceMode targetStance in System.Enum.GetValues(typeof(AimStanceMode)))
                {
                    AimStanceTracker.SetStance(selectedPawn, targetStance);
                    sb.AppendLine($"\n--- TARGET STANCE: {targetStance} ---");
                    sb.AppendLine("Dist \\ Shooter|  Shooter Skill 4  |  Shooter Skill 10 |  Shooter Skill 16 |  Shooter Skill 20 |");
                    sb.AppendLine("-------------|-------------------|-------------------|-------------------|-------------------|");

                    for (int d = 0; d < distances.Length; d++)
                    {
                        int dist = distances[d];
                        sb.Append($"{distLabels[d],-12} |");

                        for (int s = 0; s < skills.Length; s++)
                        {
                            int skill = skills[s];
                            if (selectedPawn.skills != null)
                            {
                                selectedPawn.skills.GetSkill(SkillDefOf.Shooting).Level = skill;
                            }

                            // Temporarily position shooter at distance 'dist' targeting selectedPawn at originalPos
                            selectedPawn.Position = originalPos + new IntVec3(dist, 0, 0);

                            ShotReport report = ShotReport.HitReportFor(selectedPawn, verb, new LocalTargetInfo(originalPos));
                            float hitChancePct = report.TotalEstimatedHitChance * 100f;

                            sb.Append($"       {hitChancePct,5:F1}%       |");
                        }
                        sb.AppendLine();
                    }
                }
            }
            finally
            {
                // Restore original position, skill & stance
                selectedPawn.Position = originalPos;
                AimStanceTracker.SetStance(selectedPawn, originalStance);
                if (selectedPawn.skills != null)
                {
                    selectedPawn.skills.GetSkill(SkillDefOf.Shooting).Level = originalSkill;
                }
            }

            sb.AppendLine("=========================================================================================");
            Log.Message(sb.ToString());
            Messages.Message("Incoming Target Hit Matrix printed to dev console.", MessageTypeDefOf.PositiveEvent, false);
        }

        /// <summary>
        /// Applies suppression to the selected pawn and reports WHICH ENGINE did it.
        ///
        /// Written because a modlist running both Fire Discipline and an external suppression mod
        /// gives no visible signal about which one is authoritative. It is entirely possible to tune
        /// against an engine that is not running - the old version of this action fabricated an
        /// FD_Suppressed hediff with its own private copy of the stance maths, so it produced a
        /// convincing result even when Fire Discipline's engine was completely inert.
        ///
        /// This version calls SuppressionEngine directly. If the engine is off, it says so and
        /// applies nothing rather than inventing a number.
        /// </summary>
        [DebugAction("Fire Discipline", "Apply Suppression + Show Engine Routing", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void TestSuppressionImpact()
        {
            Pawn selectedPawn = Find.Selector.SingleSelectedThing as Pawn;
            if (selectedPawn == null)
            {
                Messages.Message("Please select a Pawn first to test Suppression.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=========================================================================================");
            sb.AppendLine($"[Fire Discipline] Suppression routing for {selectedPawn.LabelShort}");
            sb.AppendLine("=========================================================================================");

            // ---------------------------------------------------------------- routing
            bool engineSettingOn = FireDisciplineMod.Settings?.enableSuppressionEngine ?? false;
            bool enginePatched = PatchRegistry.WasPatchedAtStartup(SuppressionCoreModule.Id);
            bool engineLive = engineSettingOn && enginePatched
                && PatchRegistry.IsModuleEnabled(SuppressionCoreModule.Id);

            bool externalActive = ExternalSuppressionDetection.IsAnyExternalSuppressionActive();
            HediffDef externalDef = ExternalSuppressionDetection.FindExternalSuppressedHediff();

            sb.AppendLine("ENGINE ROUTING");
            sb.AppendLine($"  Fire Discipline setting 'enableSuppressionEngine' : {engineSettingOn}");
            sb.AppendLine($"  Fire Discipline patches registered at startup     : {enginePatched}"
                + (engineSettingOn && !enginePatched ? "   <-- turned on mid-session, RESTART REQUIRED" : ""));
            sb.AppendLine($"  Fire Discipline engine actually running           : {engineLive}");
            sb.AppendLine($"  External suppression mod detected                 : {externalActive}"
                + (externalActive ? $" ({string.Join(", ", ExternalSuppressionDetection.DetectedPackageIds().ToArray())})" : ""));
            sb.AppendLine($"  External suppression hediff def present           : {(externalDef != null ? externalDef.defName : "none")}");
            sb.AppendLine();

            string authority;
            if (engineLive && externalDef != null) authority = "BOTH - suppression stacks from two independent sources";
            else if (engineLive) authority = "Fire Discipline";
            else if (externalDef != null) authority = "External mod only - Fire Discipline contributes nothing";
            else authority = "NONE - nothing in this modlist applies suppression";
            sb.AppendLine($"  => AUTHORITATIVE: {authority}");
            sb.AppendLine();

            // ---------------------------------------------------------------- current state
            sb.AppendLine("CURRENT SEVERITY ON THIS PAWN");
            sb.AppendLine($"  FD_Suppressed (scale 0.0 - {SuppressionEngine.MaxSeverity(SuppressionEngine.SuppressedDef):F1}) : {SuppressionEngine.GetSeverity(selectedPawn):F3}");
            if (externalDef != null)
            {
                Hediff ext = selectedPawn.health?.hediffSet?.GetFirstHediffOfDef(externalDef);
                sb.AppendLine($"  {externalDef.defName} (scale 0.0 - {externalDef.maxSeverity:F1}) : "
                    + (ext != null ? $"{ext.Severity:F3}  stage '{ext.CurStage?.label ?? "?"}'" : "not present"));
            }
            sb.AppendLine();

            // ---------------------------------------------------------------- apply
            AimStanceMode stance = AimStanceTracker.GetStance(selectedPawn);
            sb.AppendLine("STANCE MATRIX");
            sb.AppendLine($"  Stance                     : {stance}");

            if (engineLive)
            {
                float baseAmount = FireDisciplineMod.Settings?.suppressionBaseAmount ?? 0.25f;
                float amount = SuppressionEngine.CalculateSuppressionAmount(null, selectedPawn);
                sb.AppendLine($"  Base per round             : {baseAmount:F3}");
                sb.AppendLine($"  After received multipliers : {amount:F3}  (x{amount / Mathf.Max(baseAmount, 0.0001f):F2})");

                bool wasAiming = selectedPawn.stances?.curStance is Stance_Warmup;
                float before = SuppressionEngine.GetSeverity(selectedPawn);
                SuppressionEngine.SuppressPawn(null, selectedPawn);
                float after = SuppressionEngine.GetSeverity(selectedPawn);

                sb.AppendLine();
                sb.AppendLine("APPLIED via Fire Discipline engine");
                sb.AppendLine($"  FD_Suppressed severity     : {before:F3} -> {after:F3}");
                if (wasAiming && stance == AimStanceMode.Sharpshot)
                {
                    sb.AppendLine("  Sharpshot warmup was RESET by this suppression.");
                }
            }
            else
            {
                sb.AppendLine("  (multipliers are inert - the Fire Discipline engine is not running)");
                sb.AppendLine();
                sb.AppendLine("APPLIED: nothing.");
                if (externalDef != null)
                {
                    sb.AppendLine($"  Suppression on this modlist belongs to '{externalDef.defName}'. This action does not");
                    sb.AppendLine("  write to another mod's hediff - that mod's own tick logic governs its severity, and");
                    sb.AppendLine("  injecting a value from outside would produce a state it never creates itself.");
                    sb.AppendLine("  To test it, have a pawn actually shoot at the target.");
                }
                else
                {
                    sb.AppendLine("  No suppression system is active at all. Enable the Fire Discipline engine in mod");
                    sb.AppendLine("  settings (requires a restart) or install a suppression mod.");
                }
            }

            sb.AppendLine("=========================================================================================");
            Log.Message(sb.ToString());
            Messages.Message($"Suppression routing: {authority}. See dev console.", selectedPawn, MessageTypeDefOf.NeutralEvent, false);
        }

        /// <summary>
        /// Turns on a per-event trace of the suppression engine. Intended to be switched on right
        /// before a fight and read out of the log afterwards, so observation does not require
        /// pausing and opening health tabs mid-firefight.
        /// </summary>
        [DebugAction("Fire Discipline", "Toggle Suppression Event Logging", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void ToggleSuppressionLogging()
        {
            SuppressionEngine.LogEvents = !SuppressionEngine.LogEvents;

            bool engineLive = (FireDisciplineMod.Settings?.enableSuppressionEngine ?? false)
                && PatchRegistry.WasPatchedAtStartup(SuppressionCoreModule.Id);

            string state = SuppressionEngine.LogEvents ? "ON" : "OFF";
            string caveat = SuppressionEngine.LogEvents && !engineLive
                ? " - but the engine is not running, so nothing will be logged."
                : "";

            Log.Message($"[Fire Discipline] Suppression event logging {state}{caveat}");
            Messages.Message($"Suppression logging {state}{caveat}", MessageTypeDefOf.NeutralEvent, false);
        }

        [DebugAction("Fire Discipline", "Clear Suppression & Stances", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void ClearSuppressionAndStances()
        {
            Pawn selectedPawn = Find.Selector.SingleSelectedThing as Pawn;
            if (selectedPawn == null)
            {
                Messages.Message("Please select a Pawn first.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            AimStanceTracker.SetStance(selectedPawn, AimStanceMode.SnapShot);
            
            HediffDef suppressionDef = DefDatabase<HediffDef>.GetNamedSilentFail("FD_Suppressed");
            if (suppressionDef != null)
            {
                Hediff hediff = selectedPawn.health.hediffSet.GetFirstHediffOfDef(suppressionDef);
                if (hediff != null)
                {
                    selectedPawn.health.RemoveHediff(hediff);
                }
            }

            Messages.Message($"Cleared all suppression and reset stance to SnapShot for {selectedPawn.LabelShort}.", MessageTypeDefOf.PositiveEvent, false);
        }

        [DebugAction("Fire Discipline", "Test Graze Shot on Selected Pawn", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void TestGrazeShot()
        {
            Pawn selectedPawn = Find.Selector.SingleSelectedThing as Pawn;
            if (selectedPawn == null)
            {
                Messages.Message("Please select a Pawn first to test Graze.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            BodyPartRecord brain = null;
            foreach (var part in selectedPawn.health.hediffSet.GetNotMissingParts())
            {
                if (part.def.defName.ToLower().Contains("brain") || part.def.defName.ToLower().Contains("head"))
                {
                    brain = part;
                    break;
                }
            }

            if (brain == null) brain = selectedPawn.RaceProps.body.corePart;

            ThingDef weaponDef = selectedPawn.equipment?.Primary?.def ?? DefDatabase<ThingDef>.GetNamedSilentFail("Gun_BoltActionRifle");
            DamageInfo dinfo = new DamageInfo(DamageDefOf.Bullet, 30f, 0f, -1f, selectedPawn, brain, weaponDef);
            
            float origDmg = dinfo.Amount;
            float mult = FireDisciplineMod.Settings?.grazeDamageMultiplier ?? 0.35f;

            dinfo.SetAmount(origDmg * mult);

            BodyPartRecord outerLimb = Patch_DamageWorker_AddInjury.FindOuterLimb(selectedPawn);
            if (outerLimb != null)
            {
                dinfo.SetHitPart(outerLimb);
            }

            if (selectedPawn.Map != null && Find.CameraDriver != null)
            {
                MoteMaker.ThrowText(selectedPawn.DrawPos, selectedPawn.Map, $"Graze (-{(int)((1f - mult) * 100f)}%)", Color.cyan);
            }

            string msg = $"[Graze Test] {selectedPawn.LabelShort} | Brain Shot REDIRECTED to {dinfo.HitPart.def.defName}! Dmg reduced from {origDmg:F1} to {dinfo.Amount:F1}";
            Log.Message(msg);
            Messages.Message(msg, selectedPawn, MessageTypeDefOf.NeutralEvent, false);
        }

        [DebugAction("Fire Discipline", "Test Proportional Shell Shock Wave", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void TestProportionalShellShock()
        {
            Pawn selectedPawn = Find.Selector.SingleSelectedThing as Pawn;
            if (selectedPawn == null)
            {
                Messages.Message("Please select a Pawn first to test Shell Shock.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            float mortarDmgRadius = 4.9f;
            float cap = FireDisciplineMod.Settings?.shellShockRadiusCap ?? 20f;
            float coefficient = FireDisciplineMod.Settings?.shellShockRadiusCoefficient ?? 2f;
            float shockRadius = Mathf.Min(cap, mortarDmgRadius + coefficient * Mathf.Sqrt(mortarDmgRadius));

            float dist = 6.0f;
            float frac = (dist - mortarDmgRadius) / (shockRadius - mortarDmgRadius);
            float severity = Mathf.Clamp(1.0f - frac, 0.10f, 0.85f);

            HediffDef shellShockDef = DefDatabase<HediffDef>.GetNamedSilentFail("FD_ShellShock");
            if (shellShockDef != null)
            {
                Hediff hediff = selectedPawn.health.hediffSet.GetFirstHediffOfDef(shellShockDef);
                if (hediff != null)
                {
                    hediff.Severity = Mathf.Clamp(hediff.Severity + severity, 0.1f, 1.0f);
                }
                else
                {
                    hediff = HediffMaker.MakeHediff(shellShockDef, selectedPawn);
                    hediff.Severity = severity;
                    selectedPawn.health.AddHediff(hediff);
                }
            }

            if (selectedPawn.Map != null && Find.CameraDriver != null)
            {
                MoteMaker.ThrowText(selectedPawn.DrawPos, selectedPawn.Map, "Shell Shock Wave!", Color.yellow);
            }

            string msg = $"[Shell Shock Test v3] {selectedPawn.LabelShort} | Mortar Radius: {mortarDmgRadius}c | Shock Radius: {shockRadius:F1}c | Dist: {dist}c | Severity: {severity:F2}";
            Log.Message(msg);
            Messages.Message(msg, selectedPawn, MessageTypeDefOf.NeutralEvent, false);
        }

        [DebugAction("Fire Discipline", "Test Embrasure Interaction on Selected Pawn", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void TestEmbrasureInteraction()
        {
            Pawn selectedPawn = Find.Selector.SingleSelectedThing as Pawn;
            if (selectedPawn == null)
            {
                Messages.Message("Please select a Pawn first to test Embrasure Interaction.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            bool featureOn = FireDisciplineMod.Settings?.enableEmbrasureInteraction ?? false;
            bool isBehindEmbrasure = EmbrasureUtility.IsUsingEmbrasure(selectedPawn);

            // Detection is reported even when the feature is off, but the multipliers must reflect
            // what is actually being applied - otherwise the readout claims an effect that is not live.
            bool applied = featureOn && isBehindEmbrasure;
            float accMult = applied ? (FireDisciplineMod.Settings?.embrasureAccuracyMultiplier ?? 0.85f) : 1.0f;
            float suppMult = applied ? (FireDisciplineMod.Settings?.embrasureSuppressionMultiplier ?? 0.30f) : 1.0f;

            string status = isBehindEmbrasure ? "<color=#00FF00>BEHIND EMBRASURE</color>" : "<color=#FF5555>OPEN GROUND / COVER (NO EMBRASURE)</color>";
            if (!featureOn) status += " <color=#FFAA00>[FEATURE DISABLED - no modifiers applied]</color>";
            string msg = $"[Embrasure Test] {selectedPawn.LabelShort} | Status: {status} | Accuracy Mult: x{accMult:F2} | Suppression Resistance: x{suppMult:F2}";

            if (selectedPawn.Map != null && Find.CameraDriver != null && applied)
            {
                MoteMaker.ThrowText(selectedPawn.DrawPos, selectedPawn.Map,
                    $"Embrasure (Supp x{suppMult:F2}, Acc x{accMult:F2})", Color.green);
            }

            Log.Message(msg);
            Messages.Message(msg, selectedPawn, MessageTypeDefOf.NeutralEvent, false);
        }

        /// <summary>
        /// Debug action E. Audits architecture rule 2 ("derive, never declare") across the live
        /// modlist: every ranged weapon currently loaded is classified using the SAME production
        /// methods the combat patches call, never a copy of them. If the AccuracyTouch >= AccuracyMedium
        /// heuristic misclassifies a modded weapon, it shows up here as a sniper rifle marked
        /// shotgun-like, or an obviously close-range weapon landing on d0 = 5.
        /// </summary>
        [DebugAction("Fire Discipline", "Print Weapon Classification", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void PrintWeaponClassification()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=========================================================================================");
            sb.AppendLine("[Fire Discipline Debug Harness] Weapon Classification (architecture rule 2 audit)");
            sb.AppendLine("Classification uses the live production methods:");
            sb.AppendLine("  d0            = WeaponClassification.CalculateD0(ThingDef)");
            sb.AppendLine("  shotgun-like  = WeaponClassification.HasShotgunProfile(ThingDef) && !explosive");
            sb.AppendLine("Extra shots is a PROJECTION of design 5.6 (Suppression stance) - not yet implemented.");
            sb.AppendLine("=========================================================================================");
            sb.AppendLine($"{"defName",-32}|{"Src",-20}|{"Touch",6}|{"Short",6}|{"Med",6}|{"Long",6}|{"Range",6}|{"Burst",6}|{"l/s",6}|{"bias",6}|{"d0",6}|{"Shotgun",8}|{"Extra",6}|");
            sb.AppendLine(new string('-', 140));

            var allRanged = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(d => d.IsRangedWeapon && d.Verbs != null && d.Verbs.Count > 0)
                .ToList();

            // Shared filter: turret and artillery armaments are not pawn tactics.
            var weapons = allRanged
                .Where(WeaponClassification.IsPawnRangedWeapon)
                .OrderBy(d => d.modContentPack?.Name ?? "Core")
                .ThenBy(d => d.defName)
                .ToList();

            int shotgunCount = 0;
            var borderline = new List<string>();

            foreach (ThingDef def in weapons)
            {
                VerbProperties verb = def.Verbs[0];

                float accTouch = def.GetStatValueAbstract(StatDefOf.AccuracyTouch);
                float accShort = def.GetStatValueAbstract(StatDefOf.AccuracyShort);
                float accMedium = def.GetStatValueAbstract(StatDefOf.AccuracyMedium);
                float accLong = def.GetStatValueAbstract(StatDefOf.AccuracyLong);

                float d0 = WeaponClassification.CalculateD0(def);
                float closeBias = Mathf.Clamp01((accTouch - accLong) / Mathf.Max(accTouch, 0.01f));
                float longShort = accShort > 0f ? accLong / accShort : 0f;

                // Mirrors the explosive exclusion inside IsShotgun, evaluated from the default
                // projectile because a def scan has no live Projectile instance to inspect.
                ThingDef projectileDef = verb.defaultProjectile;
                bool isExplosive = projectileDef?.projectile != null
                    && projectileDef.projectile.explosionRadius > 0.5f;
                bool shotgunLike = WeaponClassification.HasShotgunProfile(def) && !isExplosive;

                int burst = verb.burstShotCount;
                int extraShots = SuppressionStanceExtraShots(burst);

                if (shotgunLike) shotgunCount++;

                // Gate 4 is the decisive one and its threshold sits at 0.50. Anything landing near
                // that line could flip on a rebalance patch, so it is surfaced for monitoring.
                if (longShort >= BorderlineRatioLow && longShort <= BorderlineRatioHigh)
                {
                    borderline.Add($"    {def.defName,-34} l/s={longShort:F3}  range={verb.range,5:F1}  "
                        + $"peak={Mathf.Max(accTouch, accShort):P0}  "
                        + $"-> {(shotgunLike ? "SHOTGUN" : "not shotgun")}");
                }

                string source = def.modContentPack?.Name ?? "Core";
                if (source.Length > 19) source = source.Substring(0, 19);
                string defName = def.defName.Length > 31 ? def.defName.Substring(0, 31) : def.defName;

                sb.AppendLine($"{defName,-32}|{source,-20}|{accTouch,6:P0}|{accShort,6:P0}|{accMedium,6:P0}|{accLong,6:P0}"
                    + $"|{verb.range,6:F1}|{burst,6}|{longShort,6:F2}|{closeBias,6:F2}|{d0,6:F1}|{(shotgunLike ? "YES" : "-"),8}|{extraShots,6}|");
            }

            sb.AppendLine(new string('-', 140));
            sb.AppendLine($"Pawn ranged weapons: {weapons.Count}");
            sb.AppendLine($"Classified shotgun-like: {shotgunCount}");
            sb.AppendLine();

            // A weapon vanishing from the table with no explanation hid two genuine shotguns once.
            // Every exclusion is now listed with its cause.
            var filtered = allRanged
                .Where(d => !WeaponClassification.IsPawnRangedWeapon(d))
                .OrderBy(d => d.defName)
                .ToList();

            sb.AppendLine($"FILTERED OUT ({filtered.Count}) - excluded before classification:");
            if (filtered.Count == 0)
            {
                sb.AppendLine("    (none)");
            }
            else
            {
                foreach (ThingDef def in filtered)
                {
                    sb.AppendLine($"    {def.defName,-34} {WeaponClassification.GetFilterReason(def)}");
                }
            }
            sb.AppendLine();
            sb.AppendLine($"BORDERLINE WATCH - long/short ratio within [{BorderlineRatioLow:F2}, {BorderlineRatioHigh:F2}] "
                + $"around the {FireDisciplineMod.Settings?.shotgunMinLongShortRatio ?? 0.50f:F2} gate:");
            if (borderline.Count == 0)
            {
                sb.AppendLine("    (none - every weapon sits clear of the flatness threshold)");
            }
            else
            {
                foreach (string line in borderline) sb.AppendLine(line);
            }
            sb.AppendLine();
            sb.AppendLine("REVIEW: any weapon marked shotgun-like that is not a shotgun, or any shotgun not marked,");
            sb.AppendLine("is a counter-example to architecture rule 2 and must be reported, not patched around.");
            sb.AppendLine("=========================================================================================");

            Log.Message(sb.ToString());
            Messages.Message($"Weapon classification printed ({weapons.Count} ranged weapons).", MessageTypeDefOf.PositiveEvent, false);
        }

        /// <summary>
        /// Projection of the Suppression stance extra-shot curve from design section 5.6.
        /// Kept in the harness on purpose: the stance itself is Wave B1 and does not exist in
        /// production yet, so this must not be mistaken for a live formula.
        /// </summary>
        private static int SuppressionStanceExtraShots(int burstShotCount)
        {
            if (burstShotCount <= 0) return 0;
            return burstShotCount <= 5
                ? Mathf.RoundToInt(10f * burstShotCount / 5f)
                : Mathf.RoundToInt(10f * 5f / burstShotCount);
        }

        /// <summary>
        /// Prints the packageId of every active mod. Fire Discipline detects other suppression mods
        /// by packageId, and the previous list contained at least one id that does not appear to be
        /// real ("suppression.mod") and one that was missing its author prefix ("CombatExtended").
        /// Detection failing silently is indistinguishable from no mod being installed, so the ids
        /// have to be read off a live modlist rather than remembered.
        /// </summary>
        // Single game state only: RimWorld requires every flag in the combination to match the
        // current state, so "Entry | PlayingOnMap" can never be satisfied and the action silently
        // never appears in the debug menu.
        [DebugAction("Fire Discipline", "Print Active Mod PackageIds", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void PrintActiveModPackageIds()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=========================================================================================");
            sb.AppendLine("[Fire Discipline Debug Harness] Active mod packageIds");
            sb.AppendLine("=========================================================================================");
            sb.AppendLine($"{"packageId",-45}| Name");
            sb.AppendLine(new string('-', 100));

            foreach (ModContentPack pack in LoadedModManager.RunningModsListForReading)
            {
                sb.AppendLine($"{pack.PackageId,-45}| {pack.Name}");
            }

            sb.AppendLine(new string('-', 100));
            sb.AppendLine("Fire Discipline is watching for these ids:");
            foreach (string id in ExternalSuppressionDetection.CombatExtendedPackageIds)
            {
                sb.AppendLine($"    [Combat Extended] {id,-38} {(ModsConfig.IsActive(id) ? "ACTIVE" : "not active")}");
            }
            foreach (string id in ExternalSuppressionDetection.SuppressionModPackageIds)
            {
                sb.AppendLine($"    [Suppression]     {id,-38} {(ModsConfig.IsActive(id) ? "ACTIVE" : "not active")}");
            }
            sb.AppendLine();
            sb.AppendLine($"External suppression detected: {ExternalSuppressionDetection.IsAnyExternalSuppressionActive()}");
            sb.AppendLine($"Suppression engine enabled:    {FireDisciplineMod.Settings?.enableSuppressionEngine}");
            sb.AppendLine("If a suppression mod is in the list above but shows 'not active', the id in");
            sb.AppendLine("ExternalSuppressionDetection is wrong and must be corrected.");
            sb.AppendLine("=========================================================================================");

            Log.Message(sb.ToString());
            Messages.Message("Active mod packageIds printed to dev console.", MessageTypeDefOf.PositiveEvent, false);
        }

        /// <summary>
        /// Measures suppression output per weapon, which is the axis high rate-of-fire weapons win
        /// on and the DPS matrix does not show at all.
        ///
        /// Suppression is a flat amount per round landing near a target. That makes suppression
        /// output directly proportional to rounds per second, with no cost attached anywhere - an
        /// LMG suppresses roughly as many times faster than a bolt-action as it fires faster. If
        /// that is a problem, this is the table that proves it, rather than a feeling during a
        /// firefight.
        ///
        /// The "sustain" column is the important one: while a pawn keeps taking rounds the decay
        /// timer never starts, so weapons that can fire again inside the decay delay hold a target
        /// down indefinitely while weapons that cannot let it recover between bursts.
        /// </summary>
        /// <summary>
        /// Debug action C. Monte-Carlo measurement of how SWINGY damage output is, not how large it
        /// is on average. Every other harness action reports an expectation; this one reports the
        /// spread around it, which is the number that decides whether combat feels deterministic.
        ///
        /// Each cell is simulated twice - with Graze and without - so the contribution of the mod's
        /// own anti-one-shot rule is isolated rather than assumed.
        ///
        /// WHAT IS MODELLED: hit/miss against the live ShotReport hit chance, which body part is
        /// struck (weighted by real body coverage), and the Graze downgrade using the production
        /// formula.
        ///
        /// WHAT IS NOT: armour deflection, damage falloff, pain and capacity effects, downing rules.
        /// Those are vanilla randomness that exists with or without this mod. The output answers
        /// "how much spread does Fire Discipline add or remove", not "what is the total spread of a
        /// firefight".
        /// </summary>
        [DebugAction("Fire Discipline", "Print Damage Distribution (variance)", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void PrintDamageDistribution()
        {
            Pawn selectedPawn = Find.Selector.SingleSelectedThing as Pawn;
            if (selectedPawn == null)
            {
                Messages.Message("Please select a Pawn first.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            Verb verb = selectedPawn.equipment?.PrimaryEq?.PrimaryVerb;
            if (verb?.verbProps == null)
            {
                Messages.Message("Pawn must have a ranged weapon equipped.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            const int Trials = 4000;
            const int Seed = 20260806; // Fixed so repeated runs are comparable between tuning passes.

            FireDisciplineSettings settings = FireDisciplineMod.Settings;
            float grazeMult = settings?.grazeDamageMultiplier ?? 0.35f;
            int burst = Mathf.Max(1, verb.verbProps.burstShotCount);
            float roundDamage = verb.verbProps.defaultProjectile?.projectile?.GetDamageAmount(null) ?? 10f;

            float vitalFraction = VitalCoverageFraction(selectedPawn);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=========================================================================================");
            sb.AppendLine($"[Fire Discipline Debug Harness] Damage Distribution for {selectedPawn.LabelShort}");
            sb.AppendLine($"Weapon: {selectedPawn.equipment?.Primary?.def?.defName ?? "none"} | "
                + $"{burst} rounds/burst x {roundDamage:F0} dmg | {Trials} bursts simulated per cell");
            sb.AppendLine($"Vital-part coverage (from this pawn's body): {vitalFraction:P1} of hits land somewhere vital");
            sb.AppendLine($"Graze: p>=0.65 never, p<=0.20 always, damage retained {grazeMult:P0}");
            sb.AppendLine("=========================================================================================");
            sb.AppendLine("CV = stdev/mean. Lower means more predictable. p90/p10 is the luck spread:");
            sb.AppendLine("a value of 3.0 means a good burst delivers three times a bad one.");
            sb.AppendLine();
            sb.AppendLine($"{"stance",-10}|{"dist",5}|{"hit%",6}|{"graze",6}|{"mean",7}|{"stdev",7}|{"CV",6}|{"p10",6}|{"p50",6}|{"p90",6}|{"p90/p10",8}|");
            sb.AppendLine(new string('-', 90));

            AimStanceMode originalStance = AimStanceTracker.GetStance(selectedPawn);
            int originalSkill = selectedPawn.skills?.GetSkill(SkillDefOf.Shooting)?.Level ?? 10;

            try
            {
                foreach (AimStanceMode stance in System.Enum.GetValues(typeof(AimStanceMode)))
                {
                    AimStanceTracker.SetStance(selectedPawn, stance);

                    for (int d = 0; d < distances.Length; d++)
                    {
                        IntVec3 targetCell = selectedPawn.Position + new IntVec3(distances[d], 0, 0);
                        ShotReport report = ShotReport.HitReportFor(selectedPawn, verb, new LocalTargetInfo(targetCell));
                        float hitChance = report.TotalEstimatedHitChance;
                        float grazeChance = Patch_DamageWorker_AddInjury.CalculateGrazeChance(hitChance);

                        AppendDistributionRow(sb, $"{stance}", distances[d], hitChance, grazeChance,
                            SimulateBursts(Trials, Seed, burst, roundDamage, hitChance, vitalFraction, grazeChance, grazeMult), true);

                        AppendDistributionRow(sb, "  (no graze)", distances[d], hitChance, 0f,
                            SimulateBursts(Trials, Seed, burst, roundDamage, hitChance, vitalFraction, 0f, grazeMult), false);
                    }
                    sb.AppendLine(new string('-', 90));
                }
            }
            finally
            {
                AimStanceTracker.SetStance(selectedPawn, originalStance);
                if (selectedPawn.skills != null)
                {
                    selectedPawn.skills.GetSkill(SkillDefOf.Shooting).Level = originalSkill;
                }
            }

            sb.AppendLine("Read the paired rows: the difference between a stance row and its (no graze) row is");
            sb.AppendLine("exactly what Graze contributes. Expect Graze to lower the mean slightly and lower p90");
            sb.AppendLine("a lot - it trims the lucky tail, which is the point.");
            sb.AppendLine("=========================================================================================");

            Log.Message(sb.ToString());
            Messages.Message("Damage distribution printed to dev console.", MessageTypeDefOf.PositiveEvent, false);
        }

        private static void AppendDistributionRow(StringBuilder sb, string label, int dist, float hitChance,
            float grazeChance, float[] samples, bool showGraze)
        {
            System.Array.Sort(samples);

            float mean = samples.Average();
            float variance = samples.Select(x => (x - mean) * (x - mean)).Sum() / samples.Length;
            float stdev = Mathf.Sqrt(variance);
            float p10 = samples[(int)(samples.Length * 0.10f)];
            float p50 = samples[(int)(samples.Length * 0.50f)];
            float p90 = samples[(int)(samples.Length * 0.90f)];

            string cv = mean > 0.001f ? $"{stdev / mean:F2}" : "-";
            string ratio = p10 > 0.001f ? $"{p90 / p10:F1}" : "inf";
            string grazeCol = showGraze ? $"{grazeChance:P0}" : "-";

            sb.AppendLine($"{label,-10}|{dist,5}|{hitChance,6:P0}|{grazeCol,6}|{mean,7:F1}|{stdev,7:F1}"
                + $"|{cv,6}|{p10,6:F0}|{p50,6:F0}|{p90,6:F0}|{ratio,8}|");
        }

        /// <summary>
        /// One sample = the damage delivered by one full burst. Uses a fixed RNG seed and restores
        /// the game's RNG state afterwards, so running this never perturbs the live simulation.
        /// </summary>
        private static float[] SimulateBursts(int trials, int seed, int burst, float roundDamage,
            float hitChance, float vitalFraction, float grazeChance, float grazeMult)
        {
            float[] results = new float[trials];

            Rand.PushState(seed);
            try
            {
                for (int i = 0; i < trials; i++)
                {
                    float total = 0f;
                    for (int shot = 0; shot < burst; shot++)
                    {
                        if (!Rand.Chance(hitChance)) continue;

                        float damage = roundDamage;
                        bool vital = Rand.Chance(vitalFraction);
                        if (vital && grazeChance > 0f && Rand.Chance(grazeChance))
                        {
                            damage *= grazeMult;
                        }
                        total += damage;
                    }
                    results[i] = total;
                }
            }
            finally
            {
                Rand.PopState();
            }

            return results;
        }

        /// <summary>
        /// Fraction of incoming hits that land on something Graze considers vital, weighted by the
        /// body's real coverage values rather than assumed. Derived from the pawn's own BodyDef so
        /// it stays correct for modded races.
        /// </summary>
        private static float VitalCoverageFraction(Pawn pawn)
        {
            if (pawn?.RaceProps?.body?.AllParts == null) return 0.2f;

            float total = 0f;
            float vital = 0f;
            foreach (BodyPartRecord part in pawn.RaceProps.body.AllParts)
            {
                if (part.coverageAbs <= 0f) continue;
                total += part.coverageAbs;
                if (Patch_DamageWorker_AddInjury.IsVitalOrganOrHead(part)) vital += part.coverageAbs;
            }

            return total > 0f ? vital / total : 0.2f;
        }

        // ---------------------------------------------------------------------------------------
        // Variance model comparison. SIMULATION ONLY - none of these are implemented in the mod.
        // The numbers below decide whether any of them is worth implementing at all.
        // ---------------------------------------------------------------------------------------

        private enum HitModel
        {
            /// <summary>What the game does today: every shot is an independent coin flip.</summary>
            Independent,

            /// <summary>
            /// Hit quota with carry-over. Each shot adds p to an accumulator; every time it passes
            /// 1.0 a hit is spent. Fully deterministic - the theoretical floor for variance, and the
            /// only model here that would require intercepting the vanilla hit roll.
            /// </summary>
            QuotaCarry,

            /// <summary>Each miss raises the next shot's chance; a hit clears the bonus. Raises the mean.</summary>
            PityOneWay,

            /// <summary>
            /// Miss raises the next chance, hit lowers it. Roughly mean-preserving, and because the
            /// bonus applies to the very next round it damps variance inside a burst as well as
            /// across bursts. Implementable entirely through the hooks the mod already owns.
            /// </summary>
            PitySymmetric
        }

        private const float PityStep = 0.08f;      // Accuracy shift per consecutive miss / hit
        private const float PityMaxBonus = 0.32f;  // Four steps
        private const float WindowSeconds = 10f;           // One engagement, as a player experiences it
        private const float SingleShotCycleSeconds = 3.2f; // Bolt-action rhythm, for the reference block
        private const float PityMinP = 0.02f;
        private const float PityMaxP = 0.95f;

        /// <summary>
        /// Compares four ways of deciding hit/miss over a TEN SECOND window of sustained fire, on
        /// the selected pawn's real weapon and real hit chances.
        ///
        /// The window is the point. An earlier version measured damage per burst, which cannot see
        /// what a quota does for a single-shot weapon: with one round per burst every burst is
        /// either zero or full damage no matter how the hits are scheduled, so per-burst spread
        /// stayed flat while the actual experience changed completely. Players do not feel a burst.
        /// They feel "how much did those ten seconds accomplish".
        ///
        /// Nothing here touches the game. The mod still uses the Independent model.
        /// </summary>
        [DebugAction("Fire Discipline", "Compare Variance Models (simulation only)", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void CompareVarianceModels()
        {
            Pawn pawn = Find.Selector.SingleSelectedThing as Pawn;
            if (pawn == null)
            {
                Messages.Message("Please select a Pawn first.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            Verb verb = pawn.equipment?.PrimaryEq?.PrimaryVerb;
            ThingDef weaponDef = pawn.equipment?.Primary?.def;
            if (verb?.verbProps == null || weaponDef == null)
            {
                Messages.Message("Pawn must have a ranged weapon equipped.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            const int Trials = 20000;
            const int Seed = 20260806;

            FireDisciplineSettings settings = FireDisciplineMod.Settings;
            float grazeMult = settings?.grazeDamageMultiplier ?? 0.35f;
            int burst = Mathf.Max(1, verb.verbProps.burstShotCount);
            float roundDamage = verb.verbProps.defaultProjectile?.projectile?.GetDamageAmount(null) ?? 10f;
            float vitalFraction = VitalCoverageFraction(pawn);

            float cycleSeconds = WeaponClassification.GetCycleSeconds(weaponDef);
            int burstsPerWindow = Mathf.Max(1, Mathf.RoundToInt(WindowSeconds / cycleSeconds));

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=========================================================================================");
            sb.AppendLine($"[Fire Discipline Debug Harness] Variance Model Comparison - {pawn.LabelShort}");
            sb.AppendLine($"Weapon: {weaponDef.defName} | stance: {AimStanceTracker.GetStance(pawn)}");
            sb.AppendLine($"Window: {WindowSeconds:F0}s of sustained fire | {Trials} windows per cell");
            sb.AppendLine("Streak and quota state carry WITHIN a window and reset between windows,");
            sb.AppendLine("because each window represents one separate engagement.");
            sb.AppendLine($"Pity parameters: {PityStep:P0} per step, cap {PityMaxBonus:P0}");
            sb.AppendLine("=========================================================================================");
            sb.AppendLine("SIMULATION ONLY. None of these models are implemented. The mod uses 'independent'.");
            sb.AppendLine();
            sb.AppendLine("hit%  = measured hit rate. Drift from baseP means the model is not mean-preserving.");
            sb.AppendLine("CV    = stdev/mean of damage per window. Target for an RTS feel is roughly 0.20.");
            sb.AppendLine();

            AppendModelBlock(sb, $"AS EQUIPPED - {burst} rounds/burst, {cycleSeconds:F2}s cycle, "
                + $"{burstsPerWindow} bursts ({burst * burstsPerWindow} rounds) per window",
                pawn, verb, burst, burstsPerWindow, roundDamage, vitalFraction, grazeMult, Trials, Seed);

            sb.AppendLine();

            // Synthetic single-shot reference at a bolt-action rhythm. Over half the weapons in a
            // typical modlist fire one round per cycle, and that is the shape a quota was suspected
            // of being useless for.
            int refBursts = Mathf.Max(1, Mathf.RoundToInt(WindowSeconds / SingleShotCycleSeconds));
            AppendModelBlock(sb, $"SINGLE-SHOT REFERENCE - 1 round/burst, {SingleShotCycleSeconds:F1}s cycle, "
                + $"{refBursts} rounds per window (synthetic, same hit chance and damage)",
                pawn, verb, 1, refBursts, roundDamage, vitalFraction, grazeMult, Trials, Seed);

            sb.AppendLine();
            sb.AppendLine("HOW TO READ:");
            sb.AppendLine(" - quota-carry is the lower bound on variance and the upper bound on architectural cost:");
            sb.AppendLine("   it is the only model that requires intercepting the vanilla hit roll.");
            sb.AppendLine(" - any model whose hit% drifts above baseP is buying its consistency with a stealth");
            sb.AppendLine("   accuracy buff, and the buff is largest exactly where the odds were worst.");
            sb.AppendLine(" - compare the two blocks. If quota helps the single-shot reference too, then the");
            sb.AppendLine("   earlier per-burst measurement was hiding the effect rather than disproving it.");
            sb.AppendLine("=========================================================================================");

            Log.Message(sb.ToString());
            Messages.Message("Variance model comparison printed to dev console.", MessageTypeDefOf.PositiveEvent, false);
        }

        private static void AppendModelBlock(StringBuilder sb, string title, Pawn pawn, Verb verb, int burst,
            int burstsPerWindow, float roundDamage, float vitalFraction, float grazeMult, int trials, int seed)
        {
            sb.AppendLine($"--- {title} ---");
            sb.AppendLine($"{"model",-16}|{"dist",5}|{"baseP",6}|{"hit%",6}|{"dmg/window",11}|{"CV",6}|{"p10",6}|{"p50",6}|{"p90",6}|{"p90/p10",8}|");
            sb.AppendLine(new string('-', 87));

            for (int d = 0; d < distances.Length; d++)
            {
                IntVec3 targetCell = pawn.Position + new IntVec3(distances[d], 0, 0);
                ShotReport report = ShotReport.HitReportFor(pawn, verb, new LocalTargetInfo(targetCell));
                float baseP = report.TotalEstimatedHitChance;

                foreach (HitModel model in System.Enum.GetValues(typeof(HitModel)))
                {
                    float measuredHitRate;
                    float[] samples = SimulateWindows(model, trials, seed, burst, burstsPerWindow, roundDamage,
                        baseP, vitalFraction, grazeMult, out measuredHitRate);

                    System.Array.Sort(samples);
                    float mean = samples.Average();
                    float stdev = Mathf.Sqrt(samples.Select(x => (x - mean) * (x - mean)).Sum() / samples.Length);
                    float p10 = samples[(int)(samples.Length * 0.10f)];
                    float p50 = samples[(int)(samples.Length * 0.50f)];
                    float p90 = samples[(int)(samples.Length * 0.90f)];

                    string cv = mean > 0.001f ? $"{stdev / mean:F2}" : "-";
                    string ratio = p10 > 0.001f ? $"{p90 / p10:F1}" : "inf";

                    sb.AppendLine($"{ModelLabel(model),-16}|{distances[d],5}|{baseP,6:P0}|{measuredHitRate,6:P0}"
                        + $"|{mean,11:F1}|{cv,6}|{p10,6:F0}|{p50,6:F0}|{p90,6:F0}|{ratio,8}|");
                }
                sb.AppendLine(new string('-', 87));
            }
        }

        private static string ModelLabel(HitModel model)
        {
            switch (model)
            {
                case HitModel.Independent: return "independent";
                case HitModel.QuotaCarry: return "quota-carry";
                case HitModel.PityOneWay: return "pity-oneway";
                case HitModel.PitySymmetric: return "pity-symmetric";
                default: return model.ToString();
            }
        }

        /// <summary>
        /// One sample = total damage delivered across one window of sustained fire.
        /// Graze is evaluated against the EFFECTIVE hit chance, so a model that raises accuracy also
        /// correctly grazes less often.
        /// </summary>
        private static float[] SimulateWindows(HitModel model, int trials, int seed, int burst, int burstsPerWindow,
            float roundDamage, float baseP, float vitalFraction, float grazeMult, out float measuredHitRate)
        {
            float[] results = new float[trials];
            long hits = 0;
            long shots = 0;

            Rand.PushState(seed);
            try
            {
                for (int w = 0; w < trials; w++)
                {
                    // One engagement: state builds through the window and starts fresh on the next.
                    //
                    // The quota accumulator starts at a RANDOM phase, not at zero. Starting at zero
                    // throws away the fractional remainder every window, which biases the model
                    // downward - measured at 33% hits against a 37% base - and at low hit chances
                    // breaks it outright: 18 rounds at 3% accumulate 0.54 and never cross the
                    // threshold, so the model scored a flat 0% instead of 3%. A random starting
                    // phase makes the expected hit count exactly rounds x p at every hit chance.
                    float carry = model == HitModel.QuotaCarry ? Rand.Value : 0f;
                    float bonus = 0f;
                    float total = 0f;

                    for (int b = 0; b < burstsPerWindow; b++)
                    {
                        for (int shot = 0; shot < burst; shot++)
                        {
                            float effectiveP = baseP;
                            bool hit;

                            switch (model)
                            {
                                case HitModel.QuotaCarry:
                                    carry += baseP;
                                    hit = carry >= 1f;
                                    if (hit) carry -= 1f;
                                    break;

                                case HitModel.PityOneWay:
                                    effectiveP = Mathf.Min(baseP + bonus, PityMaxP);
                                    hit = Rand.Chance(effectiveP);
                                    bonus = hit ? 0f : Mathf.Min(bonus + PityStep, PityMaxBonus);
                                    break;

                                case HitModel.PitySymmetric:
                                    effectiveP = Mathf.Clamp(baseP + bonus, PityMinP, PityMaxP);
                                    hit = Rand.Chance(effectiveP);
                                    bonus = Mathf.Clamp(bonus + (hit ? -PityStep : PityStep), -PityMaxBonus, PityMaxBonus);
                                    break;

                                default:
                                    hit = Rand.Chance(baseP);
                                    break;
                            }

                            shots++;
                            if (!hit) continue;
                            hits++;

                            float damage = roundDamage;
                            float grazeChance = Patch_DamageWorker_AddInjury.CalculateGrazeChance(effectiveP);
                            if (Rand.Chance(vitalFraction) && grazeChance > 0f && Rand.Chance(grazeChance))
                            {
                                damage *= grazeMult;
                            }
                            total += damage;
                        }
                    }

                    results[w] = total;
                }
            }
            finally
            {
                Rand.PopState();
            }

            measuredHitRate = shots > 0 ? (float)hits / shots : 0f;
            return results;
        }

        [DebugAction("Fire Discipline", "Print Suppression Output Matrix", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void PrintSuppressionOutputMatrix()
        {
            FireDisciplineSettings settings = FireDisciplineMod.Settings;
            float perRound = settings?.suppressionBaseAmount ?? 0.25f;
            float rapidMult = settings?.rapidSuppressionMultiplier ?? 1.50f;
            float decayPerSec = settings?.suppressionDecayPerSecond ?? 0.20f;
            int decayDelay = settings?.suppressionDecayDelayTicks ?? 60;

            // Stage thresholds are read from the Def so this table cannot drift away from the XML.
            HediffDef def = SuppressionEngine.SuppressedDef;
            float duckingAt = 2.0f;
            float coweringAt = 5.5f;
            if (def?.stages != null && def.stages.Count >= 5)
            {
                duckingAt = def.stages[3].minSeverity;
                coweringAt = def.stages[4].minSeverity;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=========================================================================================");
            sb.AppendLine("[Fire Discipline Debug Harness] Suppression Output Matrix");
            sb.AppendLine("=========================================================================================");
            sb.AppendLine($"Suppression per round : {perRound:F3}   (Rapid stance: {perRound * rapidMult:F3})");
            sb.AppendLine($"Recovery              : {decayPerSec:F2}/s starting {decayDelay} ticks ({decayDelay / 60f:F1}s) after the last round");
            sb.AppendLine($"Stage thresholds      : ducking {duckingAt:F1}, cowering {coweringAt:F1}");
            sb.AppendLine();
            sb.AppendLine("sec/duck = seconds of sustained fire from one shooter to reach 'ducking'.");
            sb.AppendLine("sustain  = can this weapon fire again before recovery begins? If NO, the target");
            sb.AppendLine("           recovers between bursts and a single shooter cannot hold it down.");
            sb.AppendLine("enc      = move speed multiplier from this weapon's mass alone on a 35 kg-capacity pawn.");
            sb.AppendLine();
            sb.AppendLine($"{"defName",-30}|{"kg",6}|{"burst",6}|{"cycle s",8}|{"rnd/s",7}|{"supp/s",7}|{"sec/duck",9}|{"sec/cower",10}|{"sustain",8}|{"enc",6}|{"dps",7}|");
            sb.AppendLine(new string('-', 118));

            const float BaselineCarryCapacity = 35f;

            var rows = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(d => d.IsRangedWeapon && d.Verbs != null && d.Verbs.Count > 0)
                .Where(WeaponClassification.IsPawnRangedWeapon)
                .Select(d =>
                {
                    VerbProperties v = d.Verbs[0];

                    int burst = Mathf.Max(1, v.burstShotCount);
                    float cycleSeconds = WeaponClassification.GetCycleSeconds(d);

                    float roundsPerSec = burst / cycleSeconds;
                    float suppPerSec = roundsPerSec * perRound;

                    bool sustains = WeaponClassification.GetBurstGapSeconds(d) < decayDelay / 60f;

                    float damage = v.defaultProjectile?.projectile?.GetDamageAmount(null) ?? 0f;
                    float mass = d.GetStatValueAbstract(StatDefOf.Mass);

                    return new
                    {
                        Def = d,
                        Mass = mass,
                        Burst = burst,
                        Cycle = cycleSeconds,
                        RoundsPerSec = roundsPerSec,
                        SuppPerSec = suppPerSec,
                        SecToDuck = suppPerSec > 0f ? duckingAt / suppPerSec : 999f,
                        SecToCower = suppPerSec > 0f ? coweringAt / suppPerSec : 999f,
                        Sustains = sustains,
                        Enc = StatPart_Encumbrance.MultiplierForLoadRatio(mass / BaselineCarryCapacity),
                        Dps = damage * roundsPerSec
                    };
                })
                .OrderByDescending(r => r.SuppPerSec)
                .ToList();

            foreach (var r in rows)
            {
                string name = r.Def.defName.Length > 29 ? r.Def.defName.Substring(0, 29) : r.Def.defName;
                sb.AppendLine($"{name,-30}|{r.Mass,6:F1}|{r.Burst,6}|{r.Cycle,8:F2}|{r.RoundsPerSec,7:F2}"
                    + $"|{r.SuppPerSec,7:F2}|{r.SecToDuck,9:F1}|{r.SecToCower,10:F1}"
                    + $"|{(r.Sustains ? "yes" : "NO"),8}|{r.Enc,6:F2}|{r.Dps,7:F1}|");
            }

            sb.AppendLine(new string('-', 118));
            if (rows.Count > 0)
            {
                var top = rows[0];
                var median = rows[rows.Count / 2];
                sb.AppendLine($"Highest suppression output : {top.Def.defName} at {top.SuppPerSec:F2}/s");
                sb.AppendLine($"Median                     : {median.Def.defName} at {median.SuppPerSec:F2}/s");
                sb.AppendLine($"Ratio top:median           : {(median.SuppPerSec > 0f ? top.SuppPerSec / median.SuppPerSec : 0f):F1}x");
                sb.AppendLine($"Weapons that can sustain   : {rows.Count(r => r.Sustains)} of {rows.Count}");
            }
            sb.AppendLine("=========================================================================================");

            Log.Message(sb.ToString());
            Messages.Message($"Suppression output matrix printed ({rows.Count} weapons).", MessageTypeDefOf.PositiveEvent, false);
        }

        /// <summary>
        /// Debug action G. Lists every ThingDef the embrasure rule accepts, so the detection can be
        /// checked against a real modlist instead of trusted.
        ///
        /// The previous detection matched hundreds of defs - walls, ore, doors - because of a
        /// defName substring test and an isStuffableAirtight fallback. A list this short is the
        /// evidence that it no longer does.
        /// </summary>
        /// <summary>
        /// Lists every Patch_* class in the mod and whether Harmony actually has it registered.
        ///
        /// This exists because of a specific failure that cost the project weeks: Patch_Projectile_Impact
        /// was 149 lines carrying the entire suppression engine, and no module ever registered it.
        /// Nothing complained. The compiler cannot see it - the class is public and well-formed - and
        /// the design document happily listed the feature as finished. It took measuring in-game to
        /// notice the engine contributed nothing.
        ///
        /// A patch class not being registered is legitimate when its module is off. What is never
        /// legitimate is not knowing which case you are in.
        /// </summary>
        // ---------------------------------------------------------------------------------------
        // Regression: "all features off must equal vanilla, and Snap Shot must equal vanilla".
        //
        // Design 7.3 puts this before every other test, and it is the one check that catches a whole
        // class of bug at once - a patch or a StatPart running when it should not. It was unreachable
        // until A1, because OnStartup() injected StatParts even for disabled modules.
        //
        // Doing it by eye means comparing 16 numbers across two game sessions. That is exactly where
        // a one-digit drift survives, and a one-digit drift is the entire signal. So it is captured
        // to disk and diffed by the machine.
        // ---------------------------------------------------------------------------------------

        private static string RegressionFilePath =>
            Path.Combine(UnityEngine.Application.persistentDataPath, "FireDiscipline_regression_baseline.txt");

        /// <summary>
        /// Records what the selected pawn's numbers look like RIGHT NOW.
        ///
        /// Run this with every Fire Discipline module switched off and the game restarted: that is
        /// the vanilla baseline. Stance is forced to Snap Shot during capture and restored after,
        /// because Snap Shot is defined as the vanilla-equivalent stance.
        ///
        /// Captures more than hit chance. AimingDelayFactor, MoveSpeed and ShootingAccuracyPawn are
        /// recorded too, because a StatPart leaking while its module is off would never show up in
        /// a hit-chance grid - that was the shape of the original bug.
        /// </summary>
        [DebugAction("Fire Discipline", "Regression: Capture Vanilla Baseline", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void CaptureRegressionBaseline()
        {
            Pawn pawn = Find.Selector.SingleSelectedThing as Pawn;
            if (pawn == null)
            {
                Messages.Message("Please select a Pawn first.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            List<string> lines = SampleRegression(pawn, out string error);
            if (error != null)
            {
                Messages.Message(error, MessageTypeDefOf.RejectInput, false);
                return;
            }

            try
            {
                File.WriteAllLines(RegressionFilePath, lines.ToArray());
            }
            catch (Exception ex)
            {
                Log.Error($"[Fire Discipline] Could not write regression baseline: {ex.Message}");
                Messages.Message("Failed to write baseline - see log.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            int enabledModules = PatchRegistry.Modules.Count(m => m.IsEnabled);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=========================================================================================");
            sb.AppendLine("[Fire Discipline] Regression baseline captured");
            sb.AppendLine("=========================================================================================");
            sb.AppendLine($"File   : {RegressionFilePath}");
            sb.AppendLine($"Pawn   : {pawn.LabelShort} | weapon: {pawn.equipment?.Primary?.def?.defName ?? "none"}");
            sb.AppendLine($"Samples: {lines.Count(l => l.StartsWith("hit."))} hit-chance cells + 3 stat values");
            sb.AppendLine();

            if (enabledModules > 0)
            {
                sb.AppendLine($"WARNING: {enabledModules} module(s) are still ENABLED. This is NOT a vanilla baseline.");
                sb.AppendLine("Switch every module off, restart the game, then capture again.");
                foreach (IModule m in PatchRegistry.Modules.Where(m => m.IsEnabled))
                {
                    sb.AppendLine($"    still on: {m.ModuleId}");
                }
            }
            else
            {
                sb.AppendLine("All modules are off. This is a valid vanilla baseline.");
            }

            sb.AppendLine();
            sb.AppendLine("NEXT: enable the modules you want to test, RESTART the game, select the same pawn with the");
            sb.AppendLine("same weapon, and run \"Regression: Compare To Baseline\".");
            sb.AppendLine("=========================================================================================");

            Log.Message(sb.ToString());
            Messages.Message("Regression baseline captured. See dev console.", MessageTypeDefOf.PositiveEvent, false);
        }

        /// <summary>
        /// Re-samples and diffs against the stored baseline. Any non-zero delta on Snap Shot means a
        /// patch or StatPart is running when it should not be - design 7.3 treats a single digit of
        /// drift as a failure, and so does this.
        /// </summary>
        [DebugAction("Fire Discipline", "Regression: Compare To Baseline", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void CompareRegressionBaseline()
        {
            Pawn pawn = Find.Selector.SingleSelectedThing as Pawn;
            if (pawn == null)
            {
                Messages.Message("Please select a Pawn first.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (!File.Exists(RegressionFilePath))
            {
                Messages.Message("No baseline found. Run \"Regression: Capture Vanilla Baseline\" first.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            var baseline = new Dictionary<string, string>();
            foreach (string line in File.ReadAllLines(RegressionFilePath))
            {
                int eq = line.IndexOf('=');
                if (eq > 0) baseline[line.Substring(0, eq)] = line.Substring(eq + 1);
            }

            List<string> current = SampleRegression(pawn, out string error);
            if (error != null)
            {
                Messages.Message(error, MessageTypeDefOf.RejectInput, false);
                return;
            }

            const float Tolerance = 0.00005f;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=========================================================================================");
            sb.AppendLine("[Fire Discipline] Regression comparison");
            sb.AppendLine("=========================================================================================");

            var mismatches = new List<string>();   // must be identical, but drifted  -> FAIL
            var expected = new List<string>();     // designed to differ, and did      -> informational
            var silent = new List<string>();       // designed to differ, but did not  -> worth a look
            var contextNotes = new List<string>();

            foreach (string line in current)
            {
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                string key = line.Substring(0, eq);
                string nowRaw = line.Substring(eq + 1);

                if (!baseline.TryGetValue(key, out string wasRaw))
                {
                    mismatches.Add($"    {key,-28} MISSING FROM BASELINE");
                    continue;
                }

                if (!float.TryParse(nowRaw, out float now) || !float.TryParse(wasRaw, out float was))
                {
                    if (nowRaw != wasRaw) contextNotes.Add($"    {key,-28} baseline='{wasRaw}'  now='{nowRaw}'");
                    continue;
                }

                bool drifted = Mathf.Abs(now - was) > Tolerance;
                string owner = ExpectedToDifferBecauseOf(key);
                string detail = $"    {key,-28} baseline={was,10:F5}  now={now,10:F5}  delta={now - was,+10:F5}";

                if (owner == null)
                {
                    if (drifted) mismatches.Add(detail);
                }
                else if (drifted)
                {
                    expected.Add(detail + $"   <- {owner}");
                }
                else
                {
                    silent.Add($"    {key,-28} unchanged, but {owner} is enabled");
                }
            }

            sb.AppendLine($"Pawn   : {pawn.LabelShort} | weapon: {pawn.equipment?.Primary?.def?.defName ?? "none"}");
            sb.AppendLine($"Modules enabled now: {string.Join(", ", PatchRegistry.Modules.Where(m => m.IsEnabled).Select(m => m.ModuleId).ToArray())}");
            sb.AppendLine();
            sb.AppendLine("Values are graded by whether a module is DESIGNED to change them at Snap Shot.");
            sb.AppendLine("Hit chances and AimingDelayFactor must never move: Snap Shot is defined as vanilla.");
            sb.AppendLine("MoveSpeed may move, because Gear Encumbrance exists to change exactly that.");
            sb.AppendLine();

            if (contextNotes.Count > 0)
            {
                sb.AppendLine("CONTEXT CHANGED SINCE BASELINE (the comparison may be meaningless):");
                foreach (string n in contextNotes) sb.AppendLine(n);
                sb.AppendLine();
            }

            if (mismatches.Count == 0)
            {
                sb.AppendLine("RESULT: PASS - nothing drifted that was required to stay still.");
            }
            else
            {
                sb.AppendLine($"RESULT: FAIL - {mismatches.Count} value(s) drifted that must not have.");
                sb.AppendLine();
                foreach (string m in mismatches) sb.AppendLine(m);
                sb.AppendLine();
                sb.AppendLine("Usual suspects:");
                sb.AppendLine("  - a StatPart injected for a module that is switched off");
                sb.AppendLine("  - a patch body missing its PatchRegistry.IsModuleEnabled guard");
                sb.AppendLine("  - the pawn is not in the same position, lighting or weather as at capture time");
            }

            if (expected.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("DESIGNED EFFECTS OBSERVED (not failures - this is the mod working):");
                foreach (string e in expected) sb.AppendLine(e);
            }

            if (silent.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("NO EFFECT WHERE ONE WAS POSSIBLE (may be fine - e.g. the pawn carries almost nothing):");
                foreach (string q in silent) sb.AppendLine(q);
            }

            sb.AppendLine("=========================================================================================");
            Log.Message(sb.ToString());
            Messages.Message(mismatches.Count == 0 ? "Regression PASS." : $"Regression FAIL ({mismatches.Count} drifted).",
                mismatches.Count == 0 ? MessageTypeDefOf.PositiveEvent : MessageTypeDefOf.NegativeEvent, false);
        }

        /// <summary>
        /// Which module, if any, is legitimately allowed to move this value while the pawn is in
        /// Snap Shot. Returns null when the value must be bit-identical to vanilla.
        ///
        /// Without this split the test is useless: it flagged Gear Encumbrance slowing a pawn who
        /// was carrying 22 kg as a regression failure, which is the module doing precisely its job.
        /// A test that cannot tell a designed effect from a leak gets ignored, and an ignored test
        /// is worse than no test.
        /// </summary>
        private static string ExpectedToDifferBecauseOf(string key)
        {
            // Snap Shot is defined as vanilla-equivalent, so nothing stance-related may move.
            if (key.StartsWith("hit.")) return null;
            if (key == "stat.AimingDelayFactor") return null;
            if (key == "stat.ShootingAccuracyPawn") return null;

            // Encumbrance exists to change move speed and does so regardless of stance.
            if (key == "stat.MoveSpeed" && PatchRegistry.IsModuleEnabled(EncumbranceModule.Id))
            {
                return "Gear Encumbrance (by design)";
            }

            return null;
        }

        /// <summary>
        /// Samples the values the regression cares about. Stance is forced to Snap Shot and every
        /// mutation is undone in finally, so a throw mid-sweep cannot leave the pawn holding a
        /// fabricated skill level or stance.
        /// </summary>
        private static List<string> SampleRegression(Pawn pawn, out string error)
        {
            error = null;
            var lines = new List<string>();

            Verb verb = pawn.equipment?.PrimaryEq?.PrimaryVerb;
            if (verb?.verbProps == null)
            {
                error = "Pawn must have a ranged weapon equipped.";
                return lines;
            }

            lines.Add($"context.pawn={pawn.LabelShort}");
            lines.Add($"context.weapon={pawn.equipment?.Primary?.def?.defName ?? "none"}");
            lines.Add($"context.position={pawn.Position}");

            int originalSkill = pawn.skills?.GetSkill(SkillDefOf.Shooting)?.Level ?? 10;
            AimStanceMode originalStance = AimStanceTracker.GetStance(pawn);

            try
            {
                AimStanceTracker.SetStance(pawn, AimStanceMode.SnapShot);

                for (int d = 0; d < distances.Length; d++)
                {
                    for (int s = 0; s < skills.Length; s++)
                    {
                        if (pawn.skills != null)
                        {
                            pawn.skills.GetSkill(SkillDefOf.Shooting).Level = skills[s];
                        }

                        IntVec3 cell = pawn.Position + new IntVec3(distances[d], 0, 0);
                        ShotReport report = ShotReport.HitReportFor(pawn, verb, new LocalTargetInfo(cell));
                        lines.Add($"hit.d{distances[d]}.s{skills[s]}={report.TotalEstimatedHitChance:F5}");
                    }
                }

                if (pawn.skills != null)
                {
                    pawn.skills.GetSkill(SkillDefOf.Shooting).Level = originalSkill;
                }

                // Stat values catch a leaking StatPart, which a hit-chance grid never would.
                lines.Add($"stat.AimingDelayFactor={pawn.GetStatValue(StatDefOf.AimingDelayFactor):F5}");
                lines.Add($"stat.MoveSpeed={pawn.GetStatValue(StatDefOf.MoveSpeed):F5}");
                lines.Add($"stat.ShootingAccuracyPawn={pawn.GetStatValue(StatDefOf.ShootingAccuracyPawn):F5}");
            }
            finally
            {
                AimStanceTracker.SetStance(pawn, originalStance);
                if (pawn.skills != null)
                {
                    pawn.skills.GetSkill(SkillDefOf.Shooting).Level = originalSkill;
                }
            }

            return lines;
        }

        [DebugAction("Fire Discipline", "Print Patch Registration Audit", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void PrintPatchRegistrationAudit()
        {
            // Every patch method Harmony currently holds that belongs to this mod.
            var registeredTypes = new HashSet<string>();
            foreach (MethodBase method in Harmony.GetAllPatchedMethods())
            {
                Patches info = Harmony.GetPatchInfo(method);
                if (info == null) continue;

                foreach (Patch patch in info.Prefixes.Concat(info.Postfixes).Concat(info.Transpilers).Concat(info.Finalizers))
                {
                    if (patch.owner != PatchRegistry.HarmonyId) continue;
                    registeredTypes.Add(patch.PatchMethod.DeclaringType.Name);
                }
            }

            var patchClasses = typeof(DebugHarness).Assembly.GetTypes()
                .Where(t => t.Name.StartsWith("Patch_"))
                .OrderBy(t => t.Name)
                .ToList();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=========================================================================================");
            sb.AppendLine("[Fire Discipline Debug Harness] Patch Registration Audit");
            sb.AppendLine("=========================================================================================");
            sb.AppendLine($"{"patch class",-38}|{"registered",12}|{"namespace",-30}|");
            sb.AppendLine(new string('-', 84));

            int live = 0;
            foreach (Type t in patchClasses)
            {
                bool isRegistered = registeredTypes.Contains(t.Name);
                if (isRegistered) live++;
                sb.AppendLine($"{t.Name,-38}|{(isRegistered ? "YES" : "no"),12}|{t.Namespace,-30}|");
            }

            sb.AppendLine(new string('-', 84));
            sb.AppendLine($"{live} of {patchClasses.Count} patch classes are live.");
            sb.AppendLine();

            sb.AppendLine("MODULE STATE (a patch may be unregistered simply because its module is off):");
            foreach (IModule module in PatchRegistry.Modules)
            {
                sb.AppendLine($"    {module.ModuleId,-22} enabled={module.IsEnabled,-6} patchedAtStartup={PatchRegistry.WasPatchedAtStartup(module.ModuleId)}");
            }

            sb.AppendLine();
            sb.AppendLine("REVIEW: for every 'no' above, confirm it is explained by a disabled module or by an");
            sb.AppendLine("explicit note in the class comment. An unexplained 'no' is a feature that does not exist.");
            sb.AppendLine("=========================================================================================");

            Log.Message(sb.ToString());
            Messages.Message($"Patch audit: {live}/{patchClasses.Count} registered. See dev console.", MessageTypeDefOf.PositiveEvent, false);
        }

        [DebugAction("Fire Discipline", "Print Embrasure Detection", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void PrintEmbrasureDetection()
        {
            float minFill = FireDisciplineMod.Settings?.embrasureMinFillPercent ?? 0.65f;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=========================================================================================");
            sb.AppendLine("[Fire Discipline Debug Harness] Embrasure Detection");
            sb.AppendLine("=========================================================================================");
            sb.AppendLine($"Rule: passability == Impassable AND fillPercent >= {minFill:F2} AND fillPercent < 1.00");
            sb.AppendLine($"Feature enabled: {FireDisciplineMod.Settings?.enableEmbrasureInteraction}");
            sb.AppendLine();
            sb.AppendLine($"{"ThingDef",-38}|{"fill",7}|{"blockLight",11}|{"source",-24}|");
            sb.AppendLine(new string('-', 84));

            var matched = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(d => d.passability == Traversability.Impassable
                            && d.fillPercent >= minFill && d.fillPercent < 1.0f)
                .OrderByDescending(d => d.fillPercent)
                .ThenBy(d => d.defName)
                .ToList();

            foreach (ThingDef def in matched)
            {
                string name = def.defName.Length > 37 ? def.defName.Substring(0, 37) : def.defName;
                string source = def.modContentPack?.Name ?? "Core";
                if (source.Length > 23) source = source.Substring(0, 23);
                sb.AppendLine($"{name,-38}|{def.fillPercent,7:P0}|{def.blockLight,11}|{source,-24}|");
            }

            sb.AppendLine(new string('-', 84));
            sb.AppendLine($"Total: {matched.Count} defs treated as embrasures.");
            sb.AppendLine();
            sb.AppendLine("REVIEW: anything in this list that is not a firing slit is a false positive.");
            sb.AppendLine("Large Impassable structures in the same fill band are expected to slip through until");
            sb.AppendLine("ILSpy question 6.7 (how vanilla decides what can be shot through) is answered.");
            sb.AppendLine("The blockLight column is printed as a candidate discriminator - it is NOT used by the");
            sb.AppendLine("rule, because it defaults to false and only a handful of vanilla defs set it at all.");
            sb.AppendLine("=========================================================================================");

            Log.Message(sb.ToString());
            Messages.Message($"Embrasure detection printed ({matched.Count} defs).", MessageTypeDefOf.PositiveEvent, false);
        }

        [DebugAction("Fire Discipline", "Print Cover Values", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void PrintCoverValues()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=========================================================================================");
            sb.AppendLine("[Fire Discipline Debug Harness] Cover Values");
            sb.AppendLine("=========================================================================================");
            sb.AppendLine("WHAT IS VERIFIED AND WHAT IS NOT:");
            sb.AppendLine("  fillPercent   VERIFIED   - read directly from ThingDef.fillPercent.");
            sb.AppendLine("  passability   VERIFIED   - read directly from ThingDef.passability.");
            sb.AppendLine("  coverPercent  UNVERIFIED - the real cover value is produced by a game method whose");
            sb.AppendLine("                             name and return shape are not yet confirmed (ILSpy Q6.8).");
            sb.AppendLine("                             Until then this column shows NO NUMBER. It is NOT fillPercent;");
            sb.AppendLine("                             the previous version of this action printed fillPercent under");
            sb.AppendLine("                             the coverPercent label, which made the question look answered.");
            sb.AppendLine("  suppressMult  BLOCKED    - design 5.8 defines it as clamp(1 - coverPercent*0.40, 0.35, 1.0).");
            sb.AppendLine("                             It consumes coverPercent, so it cannot be computed yet either.");
            sb.AppendLine("                             The fillPercent-based projection below is a PLACEHOLDER ONLY");
            sb.AppendLine("                             and must not be used to tune B3.");
            sb.AppendLine("=========================================================================================");

            AppendCoverApiProbe(sb);

            sb.AppendLine();
            sb.AppendLine($"{"ThingDef Name",-35}|{"fillPercent",13}|{"passability",-14}|{"coverPercent",13}|{"suppMult (placeholder)",23}|");
            sb.AppendLine(new string('-', 102));

            const float PlaceholderCoverCoefficient = 0.40f;   // design 5.8 'k', pending B3
            const float PlaceholderSuppressionFloor = 0.35f;   // design 5.8 hard floor, pending B3

            // Blueprints and construction frames inherit fillPercent from the finished building but
            // are transient scaffolding, not cover a player positions around. They were over half of
            // this table's rows and drowned the real entries.
            int rows = 0;
            int skippedScaffolding = 0;
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading
                .Where(d => d.Fillage != FillCategory.None && d.fillPercent > 0.05f)
                .Where(d =>
                {
                    if (!d.IsFrame && !d.IsBlueprint) return true;
                    skippedScaffolding++;
                    return false;
                })
                .OrderByDescending(d => d.fillPercent)
                .ThenBy(d => d.defName))
            {
                float fill = def.fillPercent;
                float placeholderMult = Mathf.Clamp(1.0f - (fill * PlaceholderCoverCoefficient), PlaceholderSuppressionFloor, 1.0f);

                string defName = def.defName.Length > 34 ? def.defName.Substring(0, 34) : def.defName;
                sb.AppendLine($"{defName,-35}|{fill,13:P0}|{def.passability,-14}|{"UNVERIFIED",13}|{placeholderMult,23:F2}|");
                rows++;
            }

            sb.AppendLine(new string('-', 102));
            sb.AppendLine($"Total cover-capable defs: {rows} (excluded {skippedScaffolding} blueprints/frames)");
            sb.AppendLine("The 30/40/55/75% coverPercent figures in design table 5.8 remain ESTIMATES. This action");
            sb.AppendLine("cannot confirm them. B3 and B4 stay blocked until ILSpy Q6.8 is answered.");
            sb.AppendLine("=========================================================================================");

            Log.Message(sb.ToString());
            Messages.Message($"Cover values printed ({rows} defs). coverPercent still unverified - see log.", MessageTypeDefOf.PositiveEvent, false);
        }

        /// <summary>
        /// Reports what the loaded assemblies actually contain for cover calculation, as evidence
        /// for ILSpy Q6.8. This answers the NAMING half of that question only - it cannot reveal
        /// what a return value means, whether directional weighting is already folded in, or whether
        /// smoke / light / shield belts contribute. Those still require reading the method bodies.
        ///
        /// An earlier version of this probe looked up the single fully qualified name
        /// "RimWorld.CoverUtility" and reported NOT FOUND, which could not distinguish a genuinely
        /// missing type from one sitting in a different namespace. It now scans every loaded
        /// assembly by short name so a null result actually means something.
        /// </summary>
        private static void AppendCoverApiProbe(StringBuilder sb)
        {
            sb.AppendLine("COVER API PROBE (evidence for ILSpy Q6.8 - names and signatures only, not semantics):");

            List<Type> coverTypes = FindTypesNamedLike("Cover");
            if (coverTypes.Count == 0)
            {
                sb.AppendLine("  No type with 'Cover' in its name found in ANY loaded assembly.");
                sb.AppendLine("  That would be a genuine absence, not a namespace miss - treat it as a real result.");
            }
            else
            {
                sb.AppendLine($"  Types with 'Cover' in the name ({coverTypes.Count} found):");
                foreach (Type t in coverTypes)
                {
                    sb.AppendLine($"    {t.FullName}   [{t.Assembly.GetName().Name}]");
                }
            }

            // Dump the full static surface of anything named exactly CoverUtility, in any namespace.
            foreach (Type t in coverTypes.Where(t => t.Name == "CoverUtility"))
            {
                sb.AppendLine($"  Static members of {t.FullName}:");
                AppendStaticMethods(sb, t);
            }

            // ShotReport is the struct Fire Discipline already patches, so whatever cover value the
            // game feeds into a shot is almost certainly a field on it. Listing those fields points
            // straight at the real name without guessing.
            Type shotReport = typeof(ShotReport);
            sb.AppendLine($"  Fields on {shotReport.FullName} (the struct this mod already postfixes):");
            foreach (FieldInfo f in shotReport.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                sb.AppendLine($"    {f.FieldType.Name} {f.Name}");
            }
            sb.AppendLine($"  Properties on {shotReport.FullName}:");
            foreach (PropertyInfo p in shotReport.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                sb.AppendLine($"    {p.PropertyType.Name} {p.Name}");
            }

            // Named candidates: one from the design document, one from the reality report, two guesses.
            sb.AppendLine("  Named candidate lookup (searched across every loaded assembly):");
            foreach (string candidate in new[] { "CalculateOverallCover", "CalculateOverallBlockChance", "BaseBlockChance", "CalculateCoverGiverSet", "CalculateCoverGiver" })
            {
                List<string> hits = FindMethodsNamed(candidate);
                sb.AppendLine(hits.Count == 0
                    ? $"    '{candidate}': NOT FOUND anywhere"
                    : $"    '{candidate}': {string.Join(" | ", hits.ToArray())}");
            }
        }

        private static void AppendStaticMethods(StringBuilder sb, Type type)
        {
            foreach (MethodInfo m in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (m.DeclaringType != type) continue;
                string args = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}").ToArray());
                sb.AppendLine($"    {m.ReturnType.Name} {m.Name}({args})");
            }
        }

        private static List<Type> FindTypesNamedLike(string fragment)
        {
            var found = new List<Type>();
            foreach (Type t in AllLoadedTypes())
            {
                if (t.Name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    found.Add(t);
                }
            }
            return found.OrderBy(t => t.FullName).ToList();
        }

        private static List<string> FindMethodsNamed(string methodName)
        {
            var hits = new List<string>();
            foreach (Type t in AllLoadedTypes())
            {
                MethodInfo[] methods;
                try
                {
                    methods = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                }
                catch
                {
                    continue;
                }

                foreach (MethodInfo m in methods)
                {
                    if (m.DeclaringType != t || m.Name != methodName) continue;
                    string args = string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name).ToArray());
                    hits.Add($"{t.FullName}.{m.Name}({args}) -> {m.ReturnType.Name}");
                }
            }
            return hits;
        }

        /// <summary>
        /// Enumerates every type in every loaded assembly, tolerating assemblies that fail to fully
        /// load their type list - a modlist with broken or partially loaded mods is normal, and a
        /// debug probe must not throw because of one of them.
        /// </summary>
        private static IEnumerable<Type> AllLoadedTypes()
        {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = asm.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(t => t != null).ToArray();
                }
                catch
                {
                    continue;
                }

                foreach (Type t in types)
                {
                    if (t != null) yield return t;
                }
            }
        }

        // =========================================================================
        // SECTION: ACTIONS C, D, F, H (TEST HARNESS §7.1 AUDIT PROBES)
        // =========================================================================

        [DebugAction("Fire Discipline", "Print Graze Distribution", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void PrintGrazeDistribution()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=========================================================================================");
            sb.AppendLine("[Fire Discipline Debug Harness] Action C: Print Graze Distribution");
            bool enabled = PatchRegistry.IsModuleEnabled(GrazeModule.Id);
            sb.AppendLine($"Module Graze Status: {(enabled ? "ENABLED" : "DISABLED (off by default or in settings)")}");
            sb.AppendLine("=========================================================================================");

            Pawn shooter = Find.Selector.SingleSelectedThing as Pawn;
            if (shooter == null)
            {
                Messages.Message("Please select a Pawn with a ranged weapon equipped first.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            Verb verb = shooter.equipment?.PrimaryEq?.PrimaryVerb;
            if (verb == null)
            {
                Messages.Message("Selected pawn must have a ranged weapon equipped.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            sb.AppendLine($"Shooter: {shooter.LabelShort} (Shooting Skill: {shooter.skills?.GetSkill(SkillDefOf.Shooting)?.Level ?? 0})");
            sb.AppendLine($"Weapon: {shooter.equipment.Primary.def.defName}");
            sb.AppendLine();
            sb.AppendLine($"{"Distance",-12}|{"Hit Chance (p)",-16}|{"Graze Chance",-16}|{"% Fatal Reduced",-18}|");
            sb.AppendLine(new string('-', 68));

            int[] testDistances = new int[] { 3, 5, 8, 12, 16, 20, 25, 32, 40 };
            IntVec3 origin = shooter.Position;
            Map map = shooter.Map;

            foreach (int dist in testDistances)
            {
                IntVec3 targetCell = origin + new IntVec3(dist, 0, 0);
                float p = 0.50f;
                if (targetCell.InBounds(map))
                {
                    ShotReport report = ShotReport.HitReportFor(shooter, verb, targetCell);
                    p = report.TotalEstimatedHitChance;
                }

                // Rule 8: Call production CalculateGrazeChance directly
                float grazeChance = Patch_DamageWorker_AddInjury.CalculateGrazeChance(p);
                float pctReduced = (1f - 0.35f) * 100f; // Graze reduces damage by 65%

                sb.AppendLine($"{dist + " cells",-12}|{p,-16:P1}|{grazeChance,-16:P1}|{"-" + (int)pctReduced + "%",-18}|");
            }

            Log.Message(sb.ToString());
        }

        [DebugAction("Fire Discipline", "Simulate Explosion Table", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SimulateExplosionTable()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=========================================================================================");
            sb.AppendLine("[Fire Discipline Debug Harness] Action D: Simulate Explosion Table");
            bool enabled = PatchRegistry.IsModuleEnabled(Shock.ShockModule.Id);
            sb.AppendLine($"Module Shock Status: {(enabled ? "ENABLED" : "DISABLED (off by default or in settings)")}");
            float cap = FireDisciplineMod.Settings?.shellShockRadiusCap ?? 20f;
            float coef = FireDisciplineMod.Settings?.shellShockRadiusCoefficient ?? 2f;
            sb.AppendLine($"Current Settings: shellShockRadiusCoefficient = {coef:F1}, shellShockRadiusCap = {cap:F1}c");
            sb.AppendLine("=========================================================================================");
            sb.AppendLine($"{"Source Radius (r)",-20}|{"Shell Shock Radius",-20}|{"Cells Affected",-18}|{"Power Factor (50dmg)",-22}|");
            sb.AppendLine(new string('-', 82));

            float[] testRadii = new float[] { 1.0f, 1.9f, 2.9f, 4.9f, 7.9f, 9.0f, 12.9f, 15.0f, 20.0f, 30.0f };

            foreach (float r in testRadii)
            {
                // Rule 8: Call production helper methods directly
                float shockRadius = Shock.Patch_Explosion.CalculateShockRadius(r);
                float powerFactor = Shock.Patch_Explosion.CalculatePowerFactor(50);
                int numCells = GenRadial.NumCellsInRadius(shockRadius);

                sb.AppendLine($"{r + "c",-20}|{shockRadius,-20:F1}c|{numCells,-18}|{powerFactor,-22:P0}|");
            }

            Log.Message(sb.ToString());
        }

        [DebugAction("Fire Discipline", "Test Pinned Cycle", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void TestPinnedCycle()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=========================================================================================");
            sb.AppendLine("[Fire Discipline Debug Harness] Action F: Test Pinned Cycle");
            bool pinnedEnabled = FireDisciplineMod.Settings?.enableSuppressionPinned ?? false;
            sb.AppendLine($"Pinned Feature Status: {(pinnedEnabled ? "ENABLED" : "DISABLED (OFF by default in settings)")}");
            sb.AppendLine("=========================================================================================");

            Pawn selectedPawn = Find.Selector.SingleSelectedThing as Pawn;
            if (selectedPawn == null)
            {
                Messages.Message("Please select a Pawn first to test Pinned cycle.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            HediffDef def = SuppressionEngine.SuppressedDef;
            if (def == null)
            {
                Messages.Message("FD_Suppressed hediff def not found.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            Hediff hediff = selectedPawn.health?.hediffSet?.GetFirstHediffOfDef(def);
            float origSeverity = hediff?.Severity ?? 0f;

            try
            {
                if (hediff == null)
                {
                    hediff = HediffMaker.MakeHediff(def, selectedPawn);
                    hediff.Severity = 0f;
                    selectedPawn.health.AddHediff(hediff);
                }

                Verb verb = selectedPawn.equipment?.PrimaryEq?.PrimaryVerb;
                sb.AppendLine($"Pawn: {selectedPawn.LabelShort}");
                sb.AppendLine($"Equipped Ranged Verb: {verb?.EquipmentSource?.def?.defName ?? "None"}");
                sb.AppendLine();
                sb.AppendLine($"{"Step Severity",-16}|{"Stage Name",-24}|{"Verb.Available()",-18}|");
                sb.AppendLine(new string('-', 60));

                float[] testSteps = new float[] { 0.0f, 0.25f, 0.50f, 0.75f, 0.85f, 0.95f, 1.00f };

                foreach (float step in testSteps)
                {
                    hediff.Severity = step;
                    string stageLabel = hediff.CurStage?.label ?? "None";
                    bool available = verb != null && verb.Available();

                    sb.AppendLine($"{step,-16:F2}|{stageLabel,-24}|{(available ? "True (Can Fire)" : "False (Blocked)"),-18}|");
                }
            }
            finally
            {
                // Mandatory Rule: Restore original pawn severity state
                if (hediff != null)
                {
                    if (origSeverity <= 0f)
                    {
                        selectedPawn.health.RemoveHediff(hediff);
                    }
                    else
                    {
                        hediff.Severity = origSeverity;
                    }
                }
            }

            Log.Message(sb.ToString());
        }

        [DebugAction("Fire Discipline", "Print Shotgun Spread Damage", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void PrintShotgunSpreadDamage()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=========================================================================================");
            sb.AppendLine("[Fire Discipline Debug Harness] Action H: Print Shotgun Spread Damage");
            bool enabled = PatchRegistry.IsModuleEnabled(ShotgunAoE.ShotgunAoEModule.Id);
            sb.AppendLine($"Module ShotgunAoE Status: {(enabled ? "ENABLED" : "DISABLED (experimental, off by default)")}");
            sb.AppendLine("=========================================================================================");

            Pawn shooter = Find.Selector.SingleSelectedThing as Pawn;
            if (shooter == null)
            {
                Messages.Message("Please select a Pawn with a shotgun equipped first.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            ThingDef weaponDef = shooter.equipment?.Primary?.def;
            if (weaponDef == null)
            {
                Messages.Message("Selected pawn must have a weapon equipped.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            // Rule 8: Call production WeaponClassification.HasShotgunProfile
            bool isShotgun = WeaponClassification.HasShotgunProfile(weaponDef);
            sb.AppendLine($"Shooter: {shooter.LabelShort}");
            sb.AppendLine($"Equipped Weapon: {weaponDef.defName}");
            sb.AppendLine($"Shotgun Profile Classification: {(isShotgun ? "YES (Shotgun Profile)" : "NO (Standard Ranged Weapon)")}");

            if (!isShotgun)
            {
                sb.AppendLine("Aborting: Equipped weapon does not fit Shotgun profile criteria (accuracy flatness / range).");
                Log.Message(sb.ToString());
                return;
            }

            Map map = shooter.Map;
            IntVec3 origin = shooter.Position;
            IntVec3 targetCell = origin + new IntVec3(10, 0, 0); // Simulated target cell at 10c

            // Rule 8: Call production ShotgunSpreadGeometry.AffectedCells
            List<IntVec3> cells = ShotgunAoE.ShotgunSpreadGeometry.AffectedCells(origin, targetCell, map);
            sb.AppendLine($"Simulated Target Cell: {targetCell} (Distance: 10c)");
            sb.AppendLine($"Total Affected AoE Cells: {cells.Count}");
            sb.AppendLine();
            sb.AppendLine($"{"Cell Position",-18}|{"Distance from Origin",-24}|{"Contains Ally?",-16}|");
            sb.AppendLine(new string('-', 60));

            foreach (IntVec3 cell in cells)
            {
                float d = (cell - origin).LengthHorizontal;
                Pawn pawnInCell = cell.GetFirstPawn(map);
                bool isAlly = pawnInCell != null && pawnInCell.Faction == shooter.Faction && pawnInCell != shooter;

                string allyNote = isAlly ? $"YES ({pawnInCell.LabelShort})" : "No";
                sb.AppendLine($"{cell,-18}|{d,-24:F1}c|{allyNote,-16}|");
            }

            Log.Message(sb.ToString());
        }
    }
}
