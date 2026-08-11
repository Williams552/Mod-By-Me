using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace FireDiscipline.AimStance
{
    /// <summary>
    /// Harmony Postfix on Verb.ShotsPerBurst (Wave B6).
    /// Expands the burst shot count for heavy automatic weapons (burstShotCount >= fullAutoMinBurstCount, default 5)
    /// when the shooter is in the Rapid stance.
    /// </summary>
    public static class Patch_Verb_ShotsPerBurst
    {
        private static readonly AccessTools.FieldRef<Verb, System.Nullable<int>> cachedBurstShotCountRef =
            AccessTools.FieldRefAccess<Verb, System.Nullable<int>>("cachedBurstShotCount");

        public static Pawn GetShooterPawn(Verb verb)
        {
            if (verb == null) return null;
            if (verb.caster is Pawn pawn) return pawn;
            if (verb.CasterPawn != null) return verb.CasterPawn;
            if (verb.EquipmentSource?.ParentHolder is Pawn equipmentPawn) return equipmentPawn;
            if (verb.DirectOwner is CompEquippable compEq && compEq.parent?.ParentHolder is Pawn compPawn) return compPawn;
            if (verb.DirectOwner is Pawn_EquipmentTracker eqTracker && eqTracker.pawn != null) return eqTracker.pawn;
            return null;
        }

        public static void Postfix(Verb __instance, ref int __result)
        {
            if (FireDisciplineMod.Settings == null || !FireDisciplineMod.Settings.enableRapidFullAuto)
                return;

            if (!Core.PatchRegistry.IsModuleEnabled(AimStanceModule.Id))
                return;

            if (__instance?.verbProps == null) return;

            Pawn shooter = GetShooterPawn(__instance);
            if (shooter == null) return;

            int minBurst = FireDisciplineMod.Settings.fullAutoMinBurstCount;
            int defBurst = __instance.verbProps.burstShotCount;

            if (defBurst >= minBurst)
            {
                AimStanceMode stance = AimStanceTracker.GetStance(shooter);
                if (stance == AimStanceMode.Rapid)
                {
                    float mult = FireDisciplineMod.Settings.fullAutoBurstMultiplier;
                    int newBurst = Mathf.Max(__result, Mathf.RoundToInt(defBurst * mult));

                    if (FireDisciplineMod.Settings.verboseCombatLogging) Log.Message($"[Fire Discipline B6 DIAG] ShotsPerBurst Postfix FIRING for {shooter.LabelShort}: __result={__result} → newBurst={newBurst} (defBurst={defBurst}, mult={mult:F2}, minGate={minBurst}, stance={stance})");

                    __result = newBurst;

                    if (cachedBurstShotCountRef != null)
                    {
                        cachedBurstShotCountRef(__instance) = __result;
                    }
                }
            }
        }
    }
}
