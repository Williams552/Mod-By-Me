using Verse;

namespace FireDiscipline.Core
{
    /// <summary>
    /// Detects a pawn firing from behind an embrasure - a structure that stops movement but not
    /// bullets (design section 5.7).
    ///
    /// Identification is derived from Def fields only, per architecture rule 2:
    ///
    ///     passability == Impassable      blocks movement, so a pawn cannot stand in it
    ///     fillPercent >= 0.65            substantial cover, not a knee-high rail
    ///     fillPercent &lt; 1.0              not a solid wall - a wall stops the bullet too
    ///
    /// The previous implementation added two fallbacks that made this match almost everything:
    /// a defName/label substring test for "embrasure", which rule 2 forbids outright and which
    /// silently fails on any non-English client, and a check on !isStuffableAirtight. That flag is
    /// false by default on most buildings, so every Impassable structure qualified - the audit of a
    /// live modlist found hundreds of matches including plain walls, granite, every ore, and doors.
    /// A pawn standing next to any wall in the colony was quietly taking the embrasure accuracy
    /// penalty with nothing on screen to explain it.
    ///
    /// KNOWN LIMITATION: the fillPercent band still admits a handful of large Impassable structures
    /// that are not embrasures - on the audited modlist, the Anomaly fleshmass pieces at 0.75 and
    /// the Cerebrex stabiliser at 0.70. Separating those needs the game's own shoot-through rule,
    /// which is ILSpy question 6.7 and still unanswered. Being slightly over-inclusive is the safe
    /// side of that error: the effect is a small accuracy penalty, not a benefit.
    /// </summary>
    public static class EmbrasureUtility
    {
        /// <summary>
        /// True if the pawn is adjacent to an embrasure, i.e. leaning out through a firing slit.
        /// Evaluated live rather than cached so placing or destroying a structure takes effect at once.
        /// </summary>
        public static bool IsUsingEmbrasure(Pawn pawn)
        {
            if (pawn == null || pawn.Map == null || !pawn.Spawned) return false;

            IntVec3 pos = pawn.Position;
            Map map = pawn.Map;

            foreach (IntVec3 offset in GenAdj.AdjacentCells)
            {
                IntVec3 adj = pos + offset;
                if (!adj.InBounds(map)) continue;

                if (IsEmbrasure(adj.GetEdifice(map))) return true;
            }

            return false;
        }

        public static bool IsEmbrasure(Building building)
        {
            ThingDef def = building?.def;
            if (def == null) return false;

            if (def.passability != Traversability.Impassable) return false;

            float minFill = FireDisciplineMod.Settings?.embrasureMinFillPercent ?? 0.65f;
            return def.fillPercent >= minFill && def.fillPercent < 1.0f;
        }
    }
}
