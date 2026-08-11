using RimWorld;
using UnityEngine;
using Verse;

namespace FireDiscipline.AimStance
{
    /// <summary>
    /// Automatic Passive Tracker for Dug-In (Prone) condition.
    /// Whenever a combat/drafted pawn stands still (stationary), this system automatically injects
    /// the FD_DugIn Hediff into their Health Tab. When the pawn moves, it automatically removes it.
    /// </summary>
    public static class PronePassiveTracker
    {
        private static HediffDef dugInDefCache;

        public static HediffDef DugInDef
        {
            get
            {
                if (dugInDefCache == null)
                {
                    dugInDefCache = DefDatabase<HediffDef>.GetNamedSilentFail("FD_DugIn");
                }
                return dugInDefCache;
            }
        }

        public static void UpdatePawnDugInState(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.health == null) return;

            HediffDef def = DugInDef;
            if (def == null) return;

            // Pawn is stationary when drafted or targeting and not currently moving
            bool isStationary = pawn.pather != null && !pawn.pather.MovingNow && (pawn.Drafted || pawn.stances?.curStance is Stance_Busy);

            Hediff existing = pawn.health.hediffSet?.GetFirstHediffOfDef(def);

            if (isStationary)
            {
                if (existing == null)
                {
                    Hediff newHediff = HediffMaker.MakeHediff(def, pawn);
                    pawn.health.AddHediff(newHediff);
                }
            }
            else
            {
                if (existing != null)
                {
                    pawn.health.RemoveHediff(existing);
                }
            }
        }
    }
}
