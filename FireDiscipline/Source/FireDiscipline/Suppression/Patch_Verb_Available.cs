using HarmonyLib;
using Verse;
using FireDiscipline.Core;

namespace FireDiscipline.Suppression
{
    /// <summary>
    /// Harmony Postfix on Verse.Verb.Available.
    /// Blocks ranged verbs when the caster pawn's suppression severity reaches or exceeds the Pinned threshold.
    /// Melee attacks and non-pawn casters (turrets) are explicitly excluded.
    /// </summary>
    public static class Patch_Verb_Available
    {
        public static void Postfix(Verb __instance, ref bool __result)
        {
            if (!__result) return;
            if (!PatchRegistry.IsModuleEnabled(SuppressionCoreModule.Id)) return;

            FireDisciplineSettings settings = FireDisciplineMod.Settings;
            if (settings == null || !settings.enablePinnedState) return;

            if (__instance == null || __instance.IsMeleeAttack) return;

            Pawn pawn = __instance.CasterPawn;
            if (pawn == null) return;

            float threshold = settings.pinnedSeverityThreshold;
            if (SuppressionEngine.GetSeverity(pawn) >= threshold)
            {
                __result = false;
            }
        }
    }
}
