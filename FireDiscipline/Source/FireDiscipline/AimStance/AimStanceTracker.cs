using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FireDiscipline.AimStance
{
    /// <summary>
    /// Bộ theo dõi và quản lý tư thế tác chiến thời gian thực cho Pawn.
    /// 
    /// [TÍNH NĂNG / FEATURE]: Module Tư thế Tác chiến (AimStanceModule) - Quản lý việc chuyển đổi và áp dụng tư thế cho Pawn.
    /// [TẠI SAO LÀM THẾ / RATIONALE]: Giúp người chơi điều khiển tiểu đội linh hoạt giữa bắn nhanh cự ly gần và bắn tỉa cự ly xa; 
    ///     tự động gán tư thế tối ưu cho NPC/Quân địch mà không làm giảm hiệu năng (dùng cache phân chia theo tick).
    /// [ĐIỀU CHỈNH MẶC ĐỊNH / DEFAULTS]: Tư thế mặc định là Standard. Độ trễ chuyển tư thế là 45 ticks (0.75 giây). 
    ///     Tự động đánh giá tư thế NPC mỗi 45 ticks.
    /// [Ý NGHĨA & CƠ CHẾ / MECHANICS]: Lưu trữ tư thế hiện tại của từng Pawn ID (`pawnStances`), xử lý phạt độ trễ khi chuyển đổi tư thế 
    ///     (`transitionEndTicks`), và tự động làm mới StatCache để giao diện Character Sheet hiển thị chỉ số chính xác ngay lập tức.
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

        private static readonly HarmonyLib.AccessTools.FieldRef<Verb, System.Nullable<int>> cachedBurstShotCountRef =
            HarmonyLib.AccessTools.FieldRefAccess<Verb, System.Nullable<int>>("cachedBurstShotCount");

        private static void ClearVerbBurstCache(Pawn pawn)
        {
            if (pawn?.equipment?.PrimaryEq?.PrimaryVerb is Verb verb && cachedBurstShotCountRef != null)
            {
                cachedBurstShotCountRef(verb) = null;
            }
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

            // Reset Verb burst shot count cache so RimWorld immediately re-evaluates ShotsPerBurst for new stance
            ClearVerbBurstCache(pawn);

            // Cancel active warmup if mid-shot
            if (pawn.stances?.curStance is Stance_Warmup)
            {
                pawn.stances.SetStance(new Stance_Mobile());
            }
        }

        public static bool IsDugIn(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.health == null) return false;
            // Update live dug-in state for pawn
            PronePassiveTracker.UpdatePawnDugInState(pawn);

            HediffDef def = PronePassiveTracker.DugInDef;
            if (def != null && pawn.health.hediffSet?.HasHediff(def) == true)
            {
                return true;
            }

            return pawn.pather != null && !pawn.pather.MovingNow && pawn.Drafted;
        }

        public static void CycleStance(Pawn pawn)
        {
            if (pawn == null) return;
            var current = GetStance(pawn);
            var next = (AimStanceMode)(((int)current + 1) % 3);
            SetStance(pawn, next);
            if (FireDisciplineMod.Settings != null && FireDisciplineMod.Settings.verboseCombatLogging)
            {
                Log.Message($"[Fire Discipline] Stance for {pawn.LabelShort} (ID:{pawn.thingIDNumber}) changed to {next}");
            }
        }

        public static void Notify_Suppressed(Pawn pawn)
        {
            // Warmup reset on suppression removed completely per design request.
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
