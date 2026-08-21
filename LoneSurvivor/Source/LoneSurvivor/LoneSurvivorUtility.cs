using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace LoneSurvivor
{
    public static class LoneSurvivorUtility
    {
        public static List<Pawn> GetAllAliveFreeColonists()
        {
            var result = new List<Pawn>();
            if (Current.Game == null) return result;

            var pawns = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive;
            if (pawns != null)
            {
                foreach (var p in pawns)
                {
                    if (p != null && !p.Dead && p.IsFreeColonist)
                    {
                        result.Add(p);
                    }
                }
            }
            return result;
        }

        public static int GetColonistCount(Pawn pawn)
        {
            if (Current.Game == null) return 1;

            if (LoneSurvivorMod.Settings.countPerMapOnly && pawn?.Map != null)
            {
                return Mathf.Max(1, pawn.Map.mapPawns.FreeColonistsSpawnedCount);
            }

            var colonists = GetAllAliveFreeColonists();
            return Mathf.Max(1, colonists.Count);
        }

        public static float CalculateBuffRatio(int colonistCount, int threshold)
        {
            if (threshold <= 1) return colonistCount <= 1 ? 1.0f : 0.0f;
            if (colonistCount >= threshold) return 0.0f;
            if (colonistCount <= 1) return 1.0f;

            return Mathf.Clamp01((float)(threshold - colonistCount) / (threshold - 1));
        }
    }
}
