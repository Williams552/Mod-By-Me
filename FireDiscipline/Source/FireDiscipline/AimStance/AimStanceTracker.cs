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
            if (pawn == null) return AimStanceMode.SnapShot;

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

            if (newStance == AimStanceMode.SnapShot)
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
            Log.Message($"[Fire Discipline] Stance for {pawn.LabelShort} (ID:{pawn.thingIDNumber}) changed to {next}");
        }

        public static void Notify_Suppressed(Pawn pawn)
        {
            if (pawn == null) return;
            AimStanceMode stance = GetStance(pawn);

            // Sharpshot Vulnerability: If suppressed while in Sharpshot, reset warmup!
            if (stance == AimStanceMode.Sharpshot && pawn.stances?.curStance is Stance_Warmup)
            {
                pawn.stances.SetStance(new Stance_Mobile());
                Log.Message($"[Fire Discipline] Sharpshot warmup RESET on {pawn.LabelShort} due to suppression!");
            }
        }

        public static AimStanceMode GetAutoDefaultStance(Pawn pawn)
        {
            if (pawn?.equipment?.Primary == null) return AimStanceMode.SnapShot;
            return AimStanceMode.SnapShot;
        }

        public static void ClearCache()
        {
            pawnStances.Clear();
            passiveCache.Clear();
            transitionEndTicks.Clear();
        }
    }
}
