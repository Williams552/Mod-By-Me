using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace FireDiscipline.ShotgunAoE
{
    /// <summary>
    /// Danger-zone overlay for shotgun spread (design 5.5b, Wave B8).
    ///
    /// The design document calls this mandatory, in those words: without it a player watches their
    /// own colonist wound a neighbour and concludes the mod is broken. Splash that arrives with no
    /// warning is indistinguishable from a bug.
    ///
    /// Postfix on Verb.DrawHighlight, which vanilla already calls while the player is aiming at a
    /// target. Draws the exact cells ShotgunSpreadGeometry will use, in two colours: the full wedge,
    /// and inside it the cells currently holding a pawn of the shooter's own faction.
    ///
    /// It draws only. It never decides anything, so if it ever breaks the weapon still fires.
    /// </summary>
    public static class Patch_Verb_DrawHighlight
    {
        private static readonly Color SpreadColor = new Color(1.0f, 0.55f, 0.25f, 0.35f);
        private static readonly Color FriendlyAtRiskColor = new Color(1.0f, 0.15f, 0.15f, 0.55f);

        public static void Postfix(Verb __instance, LocalTargetInfo target)
        {
            Pawn shooter = __instance?.CasterPawn;
            if (shooter == null || !shooter.Spawned || shooter.Map == null) return;
            if (!target.IsValid) return;

            if (!ShotgunSpreadGeometry.WouldSpread(shooter)) return;

            ThingDef weaponDef = shooter.equipment?.Primary?.def;
            List<IntVec3> cells = ShotgunSpreadGeometry.AffectedCells(shooter.Position, target.Cell, shooter.Map, weaponDef);
            if (cells.Count == 0) return;

            GenDraw.DrawFieldEdges(cells, SpreadColor);

            // Highlight friendlies standing in the spread separately. The whole point of the overlay
            // is the moment before a mistake, and "there is an ally in there" is the part that
            // matters - a wedge outline alone still needs the player to spot who is standing in it.
            var friendliesAtRisk = new List<IntVec3>();
            foreach (IntVec3 cell in cells)
            {
                foreach (Thing thing in cell.GetThingList(shooter.Map))
                {
                    if (!(thing is Pawn pawn) || pawn == shooter || pawn.Dead) continue;
                    if (pawn.Faction != shooter.Faction) continue;
                    if (target.Thing == pawn) continue;

                    friendliesAtRisk.Add(cell);
                    break;
                }
            }

            if (friendliesAtRisk.Count > 0)
            {
                GenDraw.DrawFieldEdges(friendliesAtRisk, FriendlyAtRiskColor);
            }
        }
    }
}
