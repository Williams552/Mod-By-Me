using RimWorld;
using Verse;

namespace FireDiscipline.AimStance
{
    /// <summary>
    /// Evaluates passive tactical stances for NPC/Raider pawns based on current weapon + target distance.
    /// Pure stat modifier layer - ZERO touch on ThinkTree or JobGiver.
    /// Prevents one-sided player power creep.
    /// </summary>
    public static class PassiveStanceEvaluator
    {
        public static AimStanceMode EvaluatePassiveStance(Pawn pawn)
        {
            if (pawn == null) return AimStanceMode.SnapShot;

            // Non-player pawn passive stance evaluation:
            LocalTargetInfo currentTarget = pawn.mindState?.enemyTarget;
            if (currentTarget.IsValid)
            {
                float dist = pawn.Position.DistanceTo(currentTarget.Cell);
                if (dist <= 6f)
                {
                    return AimStanceMode.Rapid;
                }
                else if (dist >= 30f)
                {
                    return AimStanceMode.Sharpshot;
                }
            }

            return AimStanceMode.SnapShot;
        }
    }
}
