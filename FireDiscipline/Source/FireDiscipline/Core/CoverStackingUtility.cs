using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace FireDiscipline.Core
{
    /// <summary>
    /// Line Cover Stacking Utility.
    /// Accumulates cover from obstacles along the ShootLine between shooter and target
    /// that are NOT adjacent to the target (which Vanilla already counts).
    /// </summary>
    public static class CoverStackingUtility
    {
        // P4: Pre-cleared static buffer for cells to avoid allocations in hot path.
        private static readonly List<IntVec3> cellBuffer = new List<IntVec3>();

        public static float LineCoverBlockChance(IntVec3 shooterLoc, LocalTargetInfo target, Map map)
        {
            // P5: Early-out before map access or calculations if settings or map are invalid/disabled.
            FireDisciplineSettings settings = FireDisciplineMod.Settings;
            if (settings == null || !settings.enableCoverStacking || map == null || !target.IsValid)
            {
                return 0f;
            }

            // P3: Read settings into locals BEFORE the loop.
            float lineFactor = settings.lineCoverFactor;
            int minDistFromShooter = settings.lineCoverMinDistanceFromShooter;
            int minDistSq = minDistFromShooter * minDistFromShooter;

            IntVec3 targetCell = target.Cell;
            IntVec3 shooterPos = shooterLoc;

            // Clear cell buffer at the TOP of execution (P4 / victimBuffer pattern).
            cellBuffer.Clear();

            // Collect line cells using ShootLine
            ShootLine shootLine = new ShootLine(shooterPos, targetCell);
            foreach (IntVec3 pt in shootLine.Points())
            {
                cellBuffer.Add(pt);
            }

            float lineBlock = 0f;

            for (int i = 0; i < cellBuffer.Count; i++)
            {
                IntVec3 cell = cellBuffer[i];

                // R1: SKIP target cell and any of the 8 cells ADJACENT to target.
                // Vanilla's CoverUtility.CalculateOverallBlockChance already counts the 8 target-adjacent cells;
                // double-counting them would cause incorrect stack calculations.
                if (cell == targetCell || cell.AdjacentTo8Way(targetCell))
                {
                    continue;
                }

                // R2: SKIP any cell whose distance to shooterLoc is < lineCoverMinDistanceFromShooter.
                // Prevents a shooter's own adjacent sandbag/cover from reducing their own accuracy.
                if ((cell - shooterPos).LengthHorizontalSquared < minDistSq)
                {
                    continue;
                }

                if (!cell.InBounds(map))
                {
                    continue;
                }

                // R4: Use cell.GetCover(map) - same accessor vanilla uses. Skip if null.
                Thing coverThing = cell.GetCover(map);
                if (coverThing == null)
                {
                    continue;
                }

                // R3: Cover value = BaseBlockChance() * lineCoverFactor
                float baseBlock = coverThing.BaseBlockChance();
                if (baseBlock <= 0f)
                {
                    continue;
                }

                float b = baseBlock * lineFactor;
                // Vanilla accumulation formula: num += (1 - num) * b
                lineBlock += (1f - lineBlock) * b;
            }

            return lineBlock;
        }

        public static void ClearCache()
        {
            cellBuffer.Clear();
        }
    }
}
