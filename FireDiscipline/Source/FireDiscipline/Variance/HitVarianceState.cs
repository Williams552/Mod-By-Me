using System;
using System.Collections.Generic;
using Verse;

namespace FireDiscipline.Variance
{
    /// <summary>
    /// Centralized state manager for the Hit Variance Mitigation Module (Wave B8).
    /// Manages ThreadStatic execution context, Quota carry values, Pity offsets, and live statistical metrics.
    /// Includes map-validation cleanup to prevent memory leaks (AC7).
    /// </summary>
    public static class HitVarianceState
    {
        /// <summary>
        /// Flag set to true during Verb_LaunchProjectile.TryCastShot execution.
        /// Prevents tooltip hover calculation from consuming quota or mutating pity offsets (AC2 / AC4).
        /// </summary>
        [ThreadStatic]
        public static bool InRealShot;

        private static readonly Dictionary<int, float> carryDict = new Dictionary<int, float>();
        private static readonly Dictionary<int, float> offsetDict = new Dictionary<int, float>();

        // Live statistical metrics for deviation measurement (VIỆC 5 / AC8 / AC9)
        public static long totalShotsQuota;
        public static long totalHitsQuota;
        public static double sumPQuota;

        public static long totalShotsPity;
        public static long totalHitsPity;
        public static double sumPPity;

        public static float GetQuotaCarry(Pawn pawn)
        {
            if (pawn == null) return 0f;
            int id = pawn.thingIDNumber;
            if (!carryDict.TryGetValue(id, out float carry))
            {
                // AC6: Initialize carry with random phase Rand.Value, NOT 0f.
                carry = Rand.Value;
                carryDict[id] = carry;
            }
            return carry;
        }

        public static void SetQuotaCarry(Pawn pawn, float val)
        {
            if (pawn == null) return;
            carryDict[pawn.thingIDNumber] = val;
        }

        public static float GetPityOffset(Pawn pawn)
        {
            if (pawn == null) return 0f;
            if (offsetDict.TryGetValue(pawn.thingIDNumber, out float offset))
            {
                return offset;
            }
            return 0f;
        }

        public static void SetPityOffset(Pawn pawn, float val)
        {
            if (pawn == null) return;
            offsetDict[pawn.thingIDNumber] = val;
        }

        public static void RecordQuotaShot(float p, bool hit)
        {
            totalShotsQuota++;
            if (hit) totalHitsQuota++;
            sumPQuota += p;
        }

        public static void RecordPityShot(float p, bool hit)
        {
            totalShotsPity++;
            if (hit) totalHitsPity++;
            sumPPity += p;
        }

        public static void ResetStats()
        {
            totalShotsQuota = 0;
            totalHitsQuota = 0;
            sumPQuota = 0.0;

            totalShotsPity = 0;
            totalHitsPity = 0;
            sumPPity = 0.0;
        }

        public static void ClearCache()
        {
            carryDict.Clear();
            offsetDict.Clear();
        }

        public static void CleanupStaleEntries()
        {
            if (carryDict.Count == 0 && offsetDict.Count == 0)
                return;

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

            List<int> staleKeys = null;
            foreach (var id in carryDict.Keys)
            {
                if (!validPawnIDs.Contains(id))
                {
                    if (staleKeys == null) staleKeys = new List<int>();
                    staleKeys.Add(id);
                }
            }
            if (staleKeys != null)
            {
                for (int i = 0; i < staleKeys.Count; i++)
                {
                    carryDict.Remove(staleKeys[i]);
                }
            }

            staleKeys = null;
            foreach (var id in offsetDict.Keys)
            {
                if (!validPawnIDs.Contains(id))
                {
                    if (staleKeys == null) staleKeys = new List<int>();
                    staleKeys.Add(id);
                }
            }
            if (staleKeys != null)
            {
                for (int i = 0; i < staleKeys.Count; i++)
                {
                    offsetDict.Remove(staleKeys[i]);
                }
            }
        }
    }
}
