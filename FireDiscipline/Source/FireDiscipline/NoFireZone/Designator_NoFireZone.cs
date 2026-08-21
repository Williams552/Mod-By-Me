using FireDiscipline.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace FireDiscipline.NoFireZone
{
    /// <summary>
    /// Vẽ và xoá vùng cấm bắn của map hiện tại.
    ///
    /// [TÍNH NĂNG / FEATURE]: Hai nút trong tab Zone - một để thêm ô vào vùng cấm, một để xoá.
    /// [TẠI SAO LÀM THẾ / RATIONALE]: Chép cấu trúc của Designator_AreaHomeExpand/Clear chứ không phải
    ///     Designator_AreaAllowed. Designator_AreaAllowed mang theo static selectedArea, dropdown chọn
    ///     area trong ProcessInput và ClearSelectedArea - toàn bộ cơ chế nhiều area. Mỗi map chỉ có đúng
    ///     một vùng cấm bắn, nên cặp Home là mẫu đúng: ctor không tham số, một đích cố định, không dropdown.
    /// [Ý NGHĨA & CƠ CHẾ / MECHANICS]: Ghi thẳng vào MapComponent_NoFireZone. SelectedUpdate gọi MarkForDraw
    ///     mỗi frame nên vùng cấm chỉ hiện khi người chơi đang cầm một trong hai công cụ này.
    /// </summary>
    public abstract class Designator_NoFireZone : Designator_Cells
    {
        protected DesignateMode mode;

        protected Designator_NoFireZone(DesignateMode mode)
        {
            this.mode = mode;
            soundDragSustain = SoundDefOf.Designate_DragStandard;
            soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
            useMouseIcon = true;
        }

        public override bool DragDrawMeasurements => true;

        public override DrawStyleCategoryDef DrawStyleCategory => DrawStyleCategoryDefOf.Areas;

        /// <summary>
        /// Module tắt thì hai nút này biến mất khỏi tab Zone thay vì trơ ra đó không làm gì. Dữ liệu vùng
        /// cấm vẫn nằm trong save - bật lại module là thấy lại đúng vùng cũ.
        /// </summary>
        public override bool Visible => PatchRegistry.IsModuleEnabled(NoFireZoneModule.Id);

        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            if (!c.InBounds(Map)) return false;

            MapComponent_NoFireZone zone = MapComponent_NoFireZone.GetFor(Map);
            if (zone == null) return false;

            bool marked = zone[c];
            return mode == DesignateMode.Add ? !marked : marked;
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            MapComponent_NoFireZone.GetFor(Map)?.Set(c, mode == DesignateMode.Add);
        }

        public override void SelectedUpdate()
        {
            GenUI.RenderMouseoverBracket();
            MapComponent_NoFireZone.GetFor(Map)?.MarkForDraw();
        }
    }

    public class Designator_NoFireZoneExpand : Designator_NoFireZone
    {
        public Designator_NoFireZoneExpand() : base(DesignateMode.Add)
        {
            defaultLabel = "FD_Designator_NoFireZoneExpand".Translate();
            defaultDesc = "FD_Designator_NoFireZoneExpandDesc".Translate();
            icon = ContentFinder<Texture2D>.Get("UI/Designators/FireDiscipline/NoFireZoneOn");
            soundDragSustain = SoundDefOf.Designate_DragAreaAdd;
            soundDragChanged = SoundDefOf.Designate_DragZone_Changed;
            soundSucceeded = SoundDefOf.Designate_ZoneAdd;
        }
    }

    public class Designator_NoFireZoneClear : Designator_NoFireZone
    {
        public Designator_NoFireZoneClear() : base(DesignateMode.Remove)
        {
            defaultLabel = "FD_Designator_NoFireZoneClear".Translate();
            defaultDesc = "FD_Designator_NoFireZoneClearDesc".Translate();
            icon = ContentFinder<Texture2D>.Get("UI/Designators/FireDiscipline/NoFireZoneOff");
            soundDragSustain = SoundDefOf.Designate_DragAreaDelete;
            soundDragChanged = null;
            soundSucceeded = SoundDefOf.Designate_ZoneDelete;
        }
    }
}
