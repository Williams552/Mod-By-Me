using HarmonyLib;
using RimWorld;
using Verse;

namespace FireDiscipline.AimStance
{
    /// <summary>
    /// NOT REGISTERED. Belongs to Wave B6 (full-auto for the Rapid stance), which is not implemented
    /// and is still blocked on ILSpy questions 6.1-6.4. Nothing calls this and no module patches it.
    ///
    /// Kept rather than deleted because it encodes a decision from design 5.4 - the cooldown penalty
    /// that pays for the extra burst shots - and because AdjustedCooldownTicks is the correct hook:
    /// it receives the attacker, so cooldown can be adjusted per pawn and per stance without ever
    /// mutating the shared Def-level VerbProperties (architecture rule 8).
    ///
    /// The [HarmonyPatch] attribute was removed. Leaving it on an unregistered class is a trap: the
    /// day anyone adds Harmony.PatchAll() this would silently go live, ungated by any toggle.
    /// Run the "Print Patch Registration Audit" debug action to see the current registration state.
    /// </summary>
    public static class Patch_Verb_AdjustedCooldownTicks
    {
        public static void Postfix(VerbProperties __instance, Verb ownerVerb, Pawn attacker, ref int __result)
        {
            if (attacker == null || ownerVerb == null) return;

            if (__instance.burstShotCount >= 3)
            {
                AimStanceMode stance = AimStanceTracker.GetStance(attacker);
                if (stance == AimStanceMode.Rapid)
                {
                    // Rapid Full-Auto Cooldown Penalty (x1.6)
                    __result = (int)(__result * 1.6f);
                }
            }
        }
    }
}
