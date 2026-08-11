using HarmonyLib;
using RimWorld;
using Verse;

namespace FireDiscipline.AimStance
{
    /// <summary>
    /// Harmony Postfix on VerbProperties.AdjustedCooldownTicks (Wave B6).
    /// Applies cooldown penalty multiplier (default x1.6) for heavy automatic weapons (burstShotCount >= fullAutoMinBurstCount)
    /// when the attacker is in the Rapid stance.
    /// </summary>
    public static class Patch_Verb_AdjustedCooldownTicks
    {
        public static void Postfix(VerbProperties __instance, Verb ownerVerb, Pawn attacker, ref int __result)
        {
            if (FireDisciplineMod.Settings == null || !FireDisciplineMod.Settings.enableRapidFullAuto)
                return;

            if (!Core.PatchRegistry.IsModuleEnabled(AimStanceModule.Id))
                return;

            if (attacker == null || ownerVerb == null || __instance == null) return;

            int minBurst = FireDisciplineMod.Settings.fullAutoMinBurstCount;
            if (__instance.burstShotCount >= minBurst)
            {
                AimStanceMode stance = AimStanceTracker.GetStance(attacker);
                if (stance == AimStanceMode.Rapid)
                {
                    float mult = FireDisciplineMod.Settings.fullAutoCooldownMultiplier;
                    __result = (int)(__result * mult);
                }
            }
        }
    }
}
