using System.Collections.Generic;
using FireDiscipline.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace FireDiscipline.NoFireZone
{
    /// <summary>
    /// Trạng thái "khẩu này có tuân thủ vùng cấm bắn không", gắn trên từng turret.
    ///
    /// [TÍNH NĂNG / FEATURE]: Gizmo bật/tắt trên turret, cộng với việc tự hiện vùng cấm khi chọn turret.
    /// [TẠI SAO LÀM THẾ / RATIONALE]: Làm bằng ThingComp chứ không phải Harmony patch, vì ThingComp đã có
    ///     sẵn đúng hai hook cần dùng: PostDrawExtraSelectionOverlays và CompGetGizmosExtra. Kế hoạch
    ///     tính patch Building_TurretGun.DrawExtraSelectionOverlays - không cần, và một patch không tồn
    ///     tại là một patch không xung đột với mod khác.
    ///     Có cờ per-turret để người chơi cố ý để lại một khẩu "phá rào" mà không phải xoá cả vùng cấm.
    /// [Ý NGHĨA & CƠ CHẾ / MECHANICS]: Comp được tiêm vào ThingDef lúc startup (xem NoFireZoneModule),
    ///     nên turret đã tồn tại trong save cũ cũng có - ThingWithComps.ExposeData gọi InitializeComps
    ///     khi load. Mặc định tuân thủ.
    /// </summary>
    public class CompNoFireZoneObedience : ThingComp
    {
        private bool obeys = true;

        public bool Obeys => obeys;

        private bool ShouldShowControls()
        {
            if (!PatchRegistry.IsModuleEnabled(NoFireZoneModule.Id)) return false;
            if (!(parent is Building_TurretGun turret)) return false;
            if (turret.Faction != Faction.OfPlayer) return false;

            // Khẩu không thuộc diện áp dụng thì gizmo chỉ gây hiểu nhầm: bật/tắt nó không đổi gì cả.
            FireDisciplineSettings settings = FireDisciplineMod.Settings;
            bool allTurrets = settings != null && settings.noFireZoneAllTurrets;
            return NoFireZoneUtility.ScanRadiusFor(turret.AttackVerb, allTurrets) >= 0f;
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();

            if (!PatchRegistry.IsModuleEnabled(NoFireZoneModule.Id)) return;
            if (parent?.Map == null) return;

            // Hiện cả khi khẩu này đang phá rào: người chơi vẫn cần thấy vùng cấm nằm ở đâu để hiểu
            // tại sao khẩu bên cạnh im lặng.
            MapComponent_NoFireZone.GetFor(parent.Map)?.MarkForDraw();
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (!ShouldShowControls()) yield break;

            yield return new Command_Toggle
            {
                defaultLabel = "FD_NoFireZone_ObeyGizmo".Translate(),
                defaultDesc = "FD_NoFireZone_ObeyGizmoDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Designators/FireDiscipline/NoFireZoneOn"),
                isActive = () => obeys,
                toggleAction = () => obeys = !obeys
            };
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref obeys, "fdNoFireZoneObeys", true);
        }
    }
}
