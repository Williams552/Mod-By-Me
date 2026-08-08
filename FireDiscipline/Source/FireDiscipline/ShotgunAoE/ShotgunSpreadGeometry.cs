using System.Collections.Generic;
using FireDiscipline.Core;
using UnityEngine;
using Verse;

namespace FireDiscipline.ShotgunAoE
{
    /// <summary>
    /// The shape of a shotgun's spread, in one place.
    ///
    /// Both the damage path and the on-screen danger overlay read from here. If they each computed
    /// the wedge themselves the overlay would eventually stop matching what actually gets hit, and
    /// an overlay that lies is worse than no overlay - the player would position around a shape the
    /// game is not using.
    ///
    /// The wedge starts one cell wide at the muzzle and widens to shotgunSpreadWidthEnd at its far
    /// end, mirroring how vanilla's Biotech Fire Spew is parameterised
    /// (CompProperties_AbilityFireSpew: range, lineWidthEnd). Defining it by width-at-the-end rather
    /// than by an angle is what keeps point-blank shots from spraying sideways: an angle-based cone
    /// opens to roughly 40 degrees at three cells.
    /// </summary>
    public static class ShotgunSpreadGeometry
    {
        /// <summary>Muzzle end is always one cell wide, so half of it.</summary>
        private const float HalfWidthAtMuzzle = 0.5f;

        /// <summary>
        /// Resolves the wedge for a shot from origin toward target. False when there is no usable
        /// direction - the shooter standing on the target cell, for instance.
        /// </summary>
        public static bool TryResolve(IntVec3 origin, IntVec3 target, out Vector3 direction,
            out float length, out float halfWidthEnd)
        {
            direction = Vector3.zero;
            length = 0f;
            halfWidthEnd = 0f;

            Vector3 toTarget = (target - origin).ToVector3();
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.01f) return false;

            FireDisciplineSettings settings = FireDisciplineMod.Settings;

            direction = toTarget.normalized;

            // Never reaches past the target: at long range the pellets have dispersed well before
            // they get there, and the target itself is already covered by the primary hit.
            length = Mathf.Min(settings?.shotgunSpreadLength ?? 8.0f, toTarget.magnitude);
            halfWidthEnd = (settings?.shotgunSpreadWidthEnd ?? 3.0f) * 0.5f;

            return length > 0.1f;
        }

        /// <summary>
        /// Whether a cell falls inside the wedge, and how far off the centre line it sits.
        /// <paramref name="edgeFraction"/> is 0 on the centre line and 1 at the rim, which is what
        /// the damage falloff interpolates along.
        /// </summary>
        public static bool Contains(IntVec3 origin, IntVec3 cell, Vector3 direction, float length,
            float halfWidthEnd, out float edgeFraction)
        {
            edgeFraction = 0f;

            Vector3 offset = (cell - origin).ToVector3();
            offset.y = 0f;

            float along = Vector3.Dot(offset, direction);
            if (along < 0f || along > length) return false;

            float lateral = (offset - direction * along).magnitude;
            float halfWidth = Mathf.Lerp(HalfWidthAtMuzzle, halfWidthEnd, along / length);
            if (lateral > halfWidth) return false;

            edgeFraction = Mathf.Clamp01(lateral / Mathf.Max(halfWidth, 0.01f));
            return true;
        }

        /// <summary>
        /// Every cell the spread would touch. Used by the danger overlay; the damage path tests
        /// pawns directly rather than building a list on every impact.
        /// </summary>
        public static List<IntVec3> AffectedCells(IntVec3 origin, IntVec3 target, Map map)
        {
            var cells = new List<IntVec3>();
            if (map == null) return cells;

            if (!TryResolve(origin, target, out Vector3 direction, out float length, out float halfWidthEnd))
            {
                return cells;
            }

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(origin, length + 1f, true))
            {
                if (!cell.InBounds(map)) continue;
                if (Contains(origin, cell, direction, length, halfWidthEnd, out _))
                {
                    cells.Add(cell);
                }
            }

            return cells;
        }

        /// <summary>
        /// True when this pawn's equipped weapon would produce a spread at all - module on, weapon
        /// classified as a shotgun, and the shot is not an explosive one.
        /// </summary>
        public static bool WouldSpread(Pawn pawn)
        {
            if (pawn == null) return false;
            if (!PatchRegistry.IsModuleEnabled(ShotgunAoEModule.Id)) return false;

            ThingDef weaponDef = pawn.equipment?.Primary?.def;
            if (weaponDef == null) return false;
            if (!WeaponClassification.HasShotgunProfile(weaponDef)) return false;

            ThingDef projectile = weaponDef.Verbs != null && weaponDef.Verbs.Count > 0
                ? weaponDef.Verbs[0].defaultProjectile
                : null;

            return projectile?.projectile == null || projectile.projectile.explosionRadius <= 0.5f;
        }
    }
}
