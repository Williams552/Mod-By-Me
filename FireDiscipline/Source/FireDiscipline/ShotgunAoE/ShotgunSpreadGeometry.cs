using System.Collections.Generic;
using FireDiscipline.Core;
using UnityEngine;
using Verse;

namespace FireDiscipline.ShotgunAoE
{
    /// <summary>
    /// Hình học và thuật toán tính toán vùng lan đạn chùm (Shotgun Cone Spread) duy nhất trong dự án.
    /// 
    /// [TÍNH NĂNG / FEATURE]: Module Tỏa Đạn Shotgun (ShotgunAoEModule).
    /// [TẠI SAO LÀM THẾ / RATIONALE]: Giả lập thực tế độ tỏa chùm pellet của súng shotgun theo hình nêm từ nòng súng tới tầm tối đa; 
    ///     đảm bảo cả thuật toán tính sát thương lan và thuật toán vẽ giao diện Danger Zone màu đỏ trên màn hình sử dụng chung 1 Nguồn sự thật duy nhất (Single Source of Truth).
    /// [ĐIỀU CHỈNH MẶC ĐỊNH / DEFAULTS]: Module mặc định TẮT. Độ rộng nêm ở đích = 3.0 ô (`shotgunSpreadWidthEnd`). Mức sát thương vùng lan = 70% sát thương đạn chính.
    /// [Ý NGHĨA & CƠ CHẾ / MECHANICS]: Tính toán hình nêm (Wedge) bắt đầu từ nòng súng (rộng 1 ô) mở rộng tới tầm bắn tối đa (rộng 3 ô). 
    ///     Xác định danh sách các ô nằm trong vùng lan (`AffectedCells`), mật độ đạn tỏa và hệ số sát thương rìa dựa trên Shooting Skill của người bắn.
    /// </summary>
    public static class ShotgunSpreadGeometry
    {
        /// <summary>Muzzle end is always one cell wide, so half of it.</summary>
        private const float HalfWidthAtMuzzle = 0.5f;

        private const float FallbackSpreadLength = 8f;   // Fallback range if weaponDef is null
        private const float SpreadReferenceRange = 8f;   // Distance at which widthEnd is measured

        /// <summary>
        /// Resolves the wedge for a shot from origin toward target. False when there is no usable
        /// direction - the shooter standing on the target cell, for instance.
        /// </summary>
        public static bool TryResolve(IntVec3 origin, IntVec3 target, ThingDef weaponDef, out Vector3 direction,
            out float length, out float spreadPerCell)
        {
            direction = Vector3.zero;
            length = 0f;
            spreadPerCell = 0f;

            Vector3 toTarget = (target - origin).ToVector3();
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.01f) return false;

            FireDisciplineSettings settings = FireDisciplineMod.Settings;

            direction = toTarget.normalized;

            // Use the actual weapon range for the spread, so pellets can hit targets behind the primary target.
            float actualRange = weaponDef != null ? WeaponClassification.GetWeaponRange(weaponDef) : FallbackSpreadLength;
            length = actualRange;
            
            float widthEndAtRef = (settings?.shotgunSpreadWidthEnd ?? 3.0f) * 0.5f;
            spreadPerCell = (widthEndAtRef - HalfWidthAtMuzzle) / SpreadReferenceRange;

            return length > 0.1f;
        }

        /// <summary>
        /// Whether a cell falls inside the wedge, and how far off the centre line it sits.
        /// <paramref name="edgeFraction"/> is 0 on the centre line and 1 at the rim, which is what
        /// the damage falloff interpolates along.
        /// </summary>
        public static bool Contains(IntVec3 origin, IntVec3 cell, Vector3 direction, float length,
            float spreadPerCell, out float edgeFraction, out float densityFactor)
        {
            edgeFraction = 0f;
            densityFactor = 1f;

            Vector3 offset = (cell - origin).ToVector3();
            offset.y = 0f;

            float along = Vector3.Dot(offset, direction);
            if (along < 0f || along > length) return false;

            float lateral = (offset - direction * along).magnitude;
            float halfWidth = HalfWidthAtMuzzle + along * spreadPerCell;
            if (lateral > halfWidth) return false;

            edgeFraction = Mathf.Clamp01(lateral / Mathf.Max(halfWidth, 0.01f));
            densityFactor = HalfWidthAtMuzzle / Mathf.Max(halfWidth, 0.01f);
            return true;
        }

        /// <summary>
        /// Checks line of sight from origin to cell, skipping the shooter's own cell.
        /// LineOfSight checks map obstacles (walls, rock, edifices) but not pawns,
        /// ensuring shotgun pellets pass through pawns to hit targets behind them as designed,
        /// while preventing pellets from passing through solid walls.
        /// </summary>
        public static bool HasLineOfFire(IntVec3 origin, IntVec3 cell, Map map)
        {
            if (map == null) return false;
            return GenSight.LineOfSight(origin, cell, map, true);
        }

        /// <summary>
        /// Every cell the spread would touch. Used by the danger overlay; the damage path tests
        /// pawns directly rather than building a list on every impact.
        /// </summary>
        public static List<IntVec3> AffectedCells(IntVec3 origin, IntVec3 target, Map map, ThingDef weaponDef)
        {
            var cells = new List<IntVec3>();
            if (map == null) return cells;

            if (!TryResolve(origin, target, weaponDef, out Vector3 direction, out float length, out float spreadPerCell))
            {
                return cells;
            }

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(origin, length + 1f, true))
            {
                if (!cell.InBounds(map)) continue;
                if (Contains(origin, cell, direction, length, spreadPerCell, out _, out _) && HasLineOfFire(origin, cell, map))
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
