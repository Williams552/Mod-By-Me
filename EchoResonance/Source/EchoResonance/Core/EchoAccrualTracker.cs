using HarmonyLib;
using RimWorld;
using Verse;

namespace EchoResonance.Core
{
    public static class EchoAccrualTracker
    {
        // Harmony Patch: Skill Record Level Up
        [HarmonyPatch(typeof(SkillRecord), nameof(SkillRecord.Learn))]
        public static class SkillRecord_Learn_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(SkillRecord __instance, float xp, bool direct)
            {
                // Simple level change tracking could be hooked via level property set or check
            }
        }

        // Harmony Patch: Item Quality Generation
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
    }
}
