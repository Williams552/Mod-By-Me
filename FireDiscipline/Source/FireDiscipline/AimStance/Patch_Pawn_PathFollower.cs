using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace FireDiscipline.AimStance
{
    /// <summary>
    /// Harmony Prefix on Pawn_PathFollower.StartPath.
    /// Automatically cancels Prone stance (invoking exit delay) when a pawn is ordered to move.
    /// Actual movement delay is handled natively by vanilla Stance_Cooldown and FullBodyBusy set in SetStance.
    /// </summary>
    public static class Patch_Pawn_PathFollower
    {
        public static void Prefix(Pawn_PathFollower __instance, Pawn ___pawn)
        {
            if (!FireDiscipline.Core.PatchRegistry.IsModuleEnabled(AimStanceModule.Id))
                return;

            if (___pawn != null)
            {
                PronePassiveTracker.UpdatePawnDugInState(___pawn);
            }
        }
    }
}
