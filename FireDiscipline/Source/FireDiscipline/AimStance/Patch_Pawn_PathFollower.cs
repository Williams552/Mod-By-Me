using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace FireDiscipline.AimStance
{
    /// <summary>
    /// Harmony Prefix on Pawn_PathFollower.StartPath.
    /// Automatically cancels Prone stance (invoking exit delay) if the pawn is ordered to move.
    /// Prevents movement entirely while transitioning.
    /// </summary>
    public static class Patch_Pawn_PathFollower
    {
        public static bool Prefix(Pawn_PathFollower __instance, Pawn ___pawn)
        {
            if (!FireDiscipline.Core.PatchRegistry.IsModuleEnabled(AimStanceModule.Id))
                return true;

            if (___pawn != null && ___pawn.Drafted)
            {
                // If pawn is in Prone stance, moving automatically exits Prone and incurs transition delay
                if (AimStanceTracker.GetStance(___pawn) == AimStanceMode.Prone)
                {
                    AimStanceTracker.SetStance(___pawn, AimStanceMode.SnapShot);
                    Messages.Message($"{___pawn.LabelShort} is packing up from Prone stance.", ___pawn, MessageTypeDefOf.NeutralEvent, false);
                }

                // If pawn is currently in a transition delay, DO NOT allow them to start moving!
                if (AimStanceTracker.IsInTransition(___pawn))
                {
                    return false; // Suppress movement StartPath
                }
            }
            return true;
        }
    }
}
