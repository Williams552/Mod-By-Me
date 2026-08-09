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

            if (___pawn != null && ___pawn.Drafted)
            {
                // If pawn is in Prone stance, moving automatically exits Prone and incurs transition delay
                if (AimStanceTracker.GetStance(___pawn) == AimStanceMode.Prone)
                {
                    AimStanceTracker.SetStance(___pawn, AimStanceMode.Standard);
                    Messages.Message($"{___pawn.LabelShort} is packing up from Prone stance.", ___pawn, MessageTypeDefOf.NeutralEvent, false);
                }
            }
        }
    }
}
