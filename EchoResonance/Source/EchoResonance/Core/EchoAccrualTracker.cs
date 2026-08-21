using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace EchoResonance.Core
{
    public static class EchoAccrualTracker
    {
        // 1. Harmony Patch: Skill Level Up Event
        [HarmonyPatch(typeof(SkillRecord), nameof(SkillRecord.Level), MethodType.Setter)]
        public static class SkillRecord_Level_Setter_Patch
        {
            [HarmonyPrefix]
            public static void Prefix(SkillRecord __instance, out int __state)
            {
                __state = __instance.Level;
            }

            [HarmonyPostfix]
            public static void Postfix(SkillRecord __instance, int __state)
            {
                int oldLevel = __state;
                int newLevel = __instance.Level;

                if (newLevel > oldLevel && __instance.Pawn != null && __instance.Pawn.IsColonist)
                {
                    float reward = 0f;
                    if (newLevel >= 1 && newLevel <= 10) reward = EchoTuning.EchoSkillLevel1_10;
                    else if (newLevel >= 11 && newLevel <= 15) reward = EchoTuning.EchoSkillLevel11_15;
                    else if (newLevel >= 16 && newLevel <= 20) reward = EchoTuning.EchoSkillLevel16_20;

                    if (newLevel == 20 && !(EchoWorldComponent.Instance?.HasFirstLevel20Occurred ?? true))
                    {
                        EchoWorldComponent.Instance?.SetFirstLevel20Reached();
                        reward += EchoTuning.EchoFirstLevel20Bonus;
                        Messages.Message($"[Echo Resonance] FIRST Level 20 Skill achieved by {__instance.Pawn.LabelShort}! Bonus +10 Echo!", MessageTypeDefOf.PositiveEvent, true);
                    }

                    if (reward > 0f)
                    {
                        EchoWorldComponent.Instance?.AddEcho(reward, $"{__instance.Pawn.LabelShort} {__instance.def.label} Lvl {newLevel}");
                    }
                }
            }
        }

        // 2. Harmony Patch: Crafting Quality Event
        [HarmonyPatch(typeof(QualityUtility), nameof(QualityUtility.GenerateQualityCreatedByPawn))]
        public static class QualityUtility_GenerateQuality_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(QualityCategory __result, Pawn pawn)
            {
                if (pawn != null && pawn.IsColonist)
                {
                    if (__result == QualityCategory.Masterwork)
                    {
                        EchoWorldComponent.Instance?.AddEcho(EchoTuning.EchoCraftMasterwork, $"Masterwork created by {pawn.LabelShort}");
                    }
                    else if (__result == QualityCategory.Legendary)
                    {
                        EchoWorldComponent.Instance?.AddEcho(EchoTuning.EchoCraftLegendary, $"Legendary created by {pawn.LabelShort}");
                    }
                }
            }
        }

        // 3. Harmony Patch: Research Project Completion Event
        [HarmonyPatch(typeof(ResearchManager), nameof(ResearchManager.FinishProject))]
        public static class ResearchManager_FinishProject_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(ResearchProjectDef proj)
            {
                if (proj != null)
                {
                    float reward = 1.0f;
                    if (proj.techLevel == TechLevel.Industrial) reward = 2.0f;
                    else if (proj.techLevel >= TechLevel.Spacer) reward = 3.0f;

                    EchoWorldComponent.Instance?.AddEcho(reward, $"Research Completed: {proj.label}");
                }
            }
        }

        // 4. Harmony Patch: Pawn Joined Colony Event
        [HarmonyPatch(typeof(Pawn), nameof(Pawn.SetFaction))]
        public static class Pawn_SetFaction_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(Pawn __instance, Faction newFaction, Faction previousFaction)
            {
                if (newFaction == Faction.OfPlayer && previousFaction != Faction.OfPlayer && __instance.RaceProps.Humanlike)
                {
                    EchoWorldComponent.Instance?.AddEcho(EchoTuning.EchoPawnJoin, $"New Colonist Joined: {__instance.LabelShort}");
                }
            }
        }

        // 5. Harmony Patch: Animal Tamed Event
        [HarmonyPatch(typeof(InteractionWorker_RecruitAttempt), nameof(InteractionWorker_RecruitAttempt.DoRecruit))]
        public static class InteractionWorker_RecruitAttempt_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(Pawn recruiter, Pawn recruitee)
            {
                if (recruitee != null && recruitee.RaceProps.Animal && recruitee.GetStatValue(StatDefOf.Wildness) >= 0.75f)
                {
                    EchoWorldComponent.Instance?.AddEcho(EchoTuning.EchoHighWildnessTame, $"High Wildness Animal Tamed ({recruitee.LabelShort})");
                }
            }
        }
    }
}
