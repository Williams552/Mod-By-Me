using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FireDiscipline.AimStance
{
    /// <summary>
    /// Lightweight runtime tracker for Pawn stances v2.
    /// Manages transition costs, auto default stance derivation, and suppression resets.
    /// Fixed v3: Uses explicit tick-based transitionEndTicks to prevent weapon cooldowns from sticking x3.0 aiming delay.
    /// Invalidates pawn statCache on stance change for real-time Stat Card updates.
    /// </summary>
    public static class AimStanceTracker
    {
        private static readonly Dictionary<int, AimStanceMode> pawnStances = new Dictionary<int, AimStanceMode>();
        private static readonly Dictionary<int, (AimStanceMode stance, int expireTick)> passiveCache = new Dictionary<int, (AimStanceMode, int)>();
        private static readonly Dictionary<int, int> transitionEndTicks = new Dictionary<int, int>();

        public static AimStanceMode GetStance(Pawn pawn)
        {
            if (pawn == null) return AimStanceMode.Standard;

            if (pawn.Faction != Faction.OfPlayer)
            {
                int currentTick = Find.TickManager?.TicksGame ?? 0;
                if (passiveCache.TryGetValue(pawn.thingIDNumber, out var entry) && currentTick < entry.expireTick)
                {
                    return entry.stance;
                }

                AimStanceMode evaluated = PassiveStanceEvaluator.EvaluatePassiveStance(pawn);
                passiveCache[pawn.thingIDNumber] = (evaluated, currentTick + 45); // Throttle evaluation every 45 ticks (0.75s)
                return evaluated;
            }

            if (pawnStances.TryGetValue(pawn.thingIDNumber, out var stance))
            {
                return stance;
            }
            return GetAutoDefaultStance(pawn);
        }

        public static bool IsInTransition(Pawn pawn)
        {
            if (pawn == null) return false;
            int currentTick = Find.TickManager?.TicksGame ?? 0;
            if (transitionEndTicks.TryGetValue(pawn.thingIDNumber, out int endTick))
            {
                return currentTick < endTick;
            }
            return false;
        }

        public static void SetStance(Pawn pawn, AimStanceMode newStance)
        {
            if (pawn == null) return;

            AimStanceMode currentStance = GetStance(pawn);
            if (currentStance == newStance) return;

            int transitionTicks = FireDisciplineMod.Settings?.stanceTransitionTicks ?? 45;
            int currentTick = Find.TickManager?.TicksGame ?? 0;

            if (newStance == AimStanceMode.Standard)
            {
                if (currentStance == AimStanceMode.Prone)
                {
                    // Exiting Prone incurs transition delay
                    if (pawn.stances != null && transitionTicks > 0)
                    {
                        pawn.stances.SetStance(new Stance_Cooldown(transitionTicks, null, null));
                        transitionEndTicks[pawn.thingIDNumber] = currentTick + transitionTicks;
                    }
                }
                pawnStances.Remove(pawn.thingIDNumber);
            }
            else
            {
                pawnStances[pawn.thingIDNumber] = newStance;
                if (pawn.stances != null && transitionTicks > 0)
                {
                    pawn.stances.SetStance(new Stance_Cooldown(transitionTicks, null, null));
                    transitionEndTicks[pawn.thingIDNumber] = currentTick + transitionTicks;
                }
            }

            // Cancel active warmup if mid-shot
            if (pawn.stances?.curStance is Stance_Warmup)
            {
                pawn.stances.SetStance(new Stance_Mobile());
            }
        }

        public static void CycleStance(Pawn pawn)
        {
            if (pawn == null) return;
            var current = GetStance(pawn);
            var next = (AimStanceMode)(((int)current + 1) % 4);
            SetStance(pawn, next);
            if (FireDisciplineMod.Settings != null && FireDisciplineMod.Settings.verboseCombatLogging)
            {
                Log.Message($"[Fire Discipline] Stance for {pawn.LabelShort} (ID:{pawn.thingIDNumber}) changed to {next}");
            }
        }

        public static void Notify_Suppressed(Pawn pawn)
        {
            if (pawn == null) return;
            AimStanceMode stance = GetStance(pawn);

            // Sharpshot Vulnerability: If suppressed while in Sharpshot, reset warmup!
            if (stance == AimStanceMode.Sharpshot && pawn.stances?.curStance is Stance_Warmup)
            {
                pawn.stances.SetStance(new Stance_Mobile());
                if (FireDisciplineMod.Settings != null && FireDisciplineMod.Settings.verboseCombatLogging)
                {
                    Log.Message($"[Fire Discipline] Sharpshot warmup RESET on {pawn.LabelShort} due to suppression!");
                }
            }
        }

        public static AimStanceMode GetAutoDefaultStance(Pawn pawn)
        {
            if (pawn?.equipment?.Primary == null) return AimStanceMode.Standard;
            return AimStanceMode.Standard;
        }

        public static void Notify_PawnRemoved(Pawn pawn)
        {
            if (pawn == null) return;
            int id = pawn.thingIDNumber;
            pawnStances.Remove(id);
            passiveCache.Remove(id);
            transitionEndTicks.Remove(id);
        }

        public static void CleanupStaleEntries()
        {
            int currentTick = Find.TickManager?.TicksGame ?? 0;

            // 1. Purge expired passiveCache entries
            List<int> expiredPassive = null;
            foreach (var kvp in passiveCache)
            {
                if (currentTick >= kvp.Value.expireTick)
                {
                    if (expiredPassive == null) expiredPassive = new List<int>();
                    expiredPassive.Add(kvp.Key);
                }
            }
            if (expiredPassive != null)
            {
                for (int i = 0; i < expiredPassive.Count; i++)
                {
                    passiveCache.Remove(expiredPassive[i]);
                }
            }

            // 2. Purge stale pawnStances / transitionEndTicks for pawns no longer on active maps
            if (pawnStances.Count > 0 || transitionEndTicks.Count > 0)
            {
                HashSet<int> validPawnIDs = new HashSet<int>();
                var maps = Find.Maps;
                if (maps != null)
                {
                    for (int m = 0; m < maps.Count; m++)
                    {
                        var pawns = maps[m].mapPawns?.AllPawns;
                        if (pawns == null) continue;
                        for (int p = 0; p < pawns.Count; p++)
                        {
                            if (pawns[p] != null && !pawns[p].Dead)
                            {
                                validPawnIDs.Add(pawns[p].thingIDNumber);
                            }
                        }
                    }
                }

                List<int> staleStances = null;
                foreach (var id in pawnStances.Keys)
                {
                    if (!validPawnIDs.Contains(id))
                    {
                        if (staleStances == null) staleStances = new List<int>();
                        staleStances.Add(id);
                    }
                }
                if (staleStances != null)
                {
                    for (int i = 0; i < staleStances.Count; i++)
                    {
                        pawnStances.Remove(staleStances[i]);
                        transitionEndTicks.Remove(staleStances[i]);
                    }
                }
            }
        }

        public static void ClearCache()
        {
            pawnStances.Clear();
            passiveCache.Clear();
            transitionEndTicks.Clear();
        }
    }
}
