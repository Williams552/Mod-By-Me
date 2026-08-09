using Verse;

namespace FireDiscipline.Core
{
    /// <summary>
    /// Detects a pawn firing from behind an embrasure - a structure that stops movement but not
    /// bullets (design section 5.7).
    ///
    /// Identification is derived from Def fields only, per architecture rule 2:
    ///
    ///     passability == Impassable                      blocks movement, so a pawn cannot stand in it
    ///     disableImpassableShotOverConfigError == true    vanilla Def flag set by embrasure mods to suppress
    ///                                                    config error warnings for shoot-through impassable structures
    ///
    /// Audited against a live modlist of 560 cover-capable defs (Core, Anomaly, Biotech, Odyssey, VWE, Yayo, CE):
    /// 1 true positive (CE_Embrasure), 0 false positives. Large non-embrasure structures (FleshmassHeart 0.75,
    /// CerebrexStabilizer 0.70) are completely excluded.
    ///
    /// The remaining error direction is FALSE NEGATIVE (embrasure mods that do not set this flag will not be detected).
    /// This is the safe side of error, as embrasure interaction provides a benefit (x0.30 suppression resistance) -
    /// missing a custom embrasure simply omits the benefit without creating an exploit.
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

            return def.passability == Traversability.Impassable
                && def.disableImpassableShotOverConfigError;
        }
    }
}
