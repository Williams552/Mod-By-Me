using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace FireDiscipline.AimStance
{
    /// <summary>
    /// Automatic Passive Tracker for Dug-In (Prone) condition.
    /// Whenever a combat/drafted pawn stands still (stationary) for the required entry delay (default 90 ticks / 1.5s),
    /// this system automatically injects the FD_DugIn Hediff into their Health Tab.
    /// When the pawn moves, it immediately removes it.
    /// </summary>
    public static class PronePassiveTracker
    {
        private static HediffDef dugInDefCache;
        private static readonly Dictionary<int, int> stationaryStartTicks = new Dictionary<int, int>();

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
            int pId = pawn.thingIDNumber;
            int currentTick = Find.TickManager?.TicksGame ?? 0;

            if (isStationary)
            {
                if (!stationaryStartTicks.TryGetValue(pId, out int startTick))
                {
                    stationaryStartTicks[pId] = currentTick;
                    startTick = currentTick;
                }

                int requiredTicks = FireDisciplineMod.Settings?.dugInEntryTicks ?? 90;
                if (currentTick - startTick >= requiredTicks)
                {
                    if (existing == null)
                    {
                        Hediff newHediff = HediffMaker.MakeHediff(def, pawn);
                        pawn.health.AddHediff(newHediff);
                    }
                }
            }
            else
            {
                stationaryStartTicks.Remove(pId);
                if (existing != null)
                {
                    pawn.health.RemoveHediff(existing);
                }
            }
        }

        public static void ClearCache()
        {
            stationaryStartTicks.Clear();
        }
    }
}

