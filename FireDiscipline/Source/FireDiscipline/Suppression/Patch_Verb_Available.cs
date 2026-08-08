using HarmonyLib;
using RimWorld;
using Verse;

namespace FireDiscipline.Suppression
{
    /// <summary>
    /// Harmony Postfix on Verb.Available (v3 Execution Spec Section 3).
    /// Enforces the Pinned state: when a pawn's suppression severity > 0.8,
    /// weapons are rendered unavailable (cannot fire), while movement remains unrestricted.
    /// </summary>
    // No [HarmonyPatch] attribute by design. This patch is registered manually by
    // SuppressionCoreModule only when the Pinned feature is enabled. An attribute here would
    // survive as a trap: the day anyone adds Harmony.PatchAll() it would register itself and
    // bypass the toggle entirely, and it would also double-patch on top of the manual call.
    public static class Patch_Verb_Available
    {
        public static void Postfix(Verb __instance, ref bool __result)
        {
            if (!__result || __instance?.CasterPawn == null) return;

            // Runtime guard: lets the player switch Pinned off mid-session even though the patch
            // itself stays registered for the rest of the session.
            if (!(FireDisciplineMod.Settings?.enableSuppressionPinned ?? false)) return;

            Pawn pawn = __instance.CasterPawn;
            HediffDef suppressionDef = DefDatabase<HediffDef>.GetNamedSilentFail("FD_Suppressed");
            if (suppressionDef == null) return;

            float threshold = FireDisciplineMod.Settings?.pinnedSeverityThreshold ?? 0.80f;
            Hediff hediff = pawn.health?.hediffSet?.GetFirstHediffOfDef(suppressionDef);
            if (hediff != null && hediff.Severity >= threshold)
            {
                // PINNED! Cannot fire weapons while pinned under heavy suppression!
                __result = false;
            }
        }
    }
}
