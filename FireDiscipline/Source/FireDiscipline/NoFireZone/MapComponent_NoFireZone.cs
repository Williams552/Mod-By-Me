using UnityEngine;
using Verse;

namespace FireDiscipline.NoFireZone
{
    /// <summary>
    /// Vùng cấm bắn của một map - một vùng duy nhất, lưu trong MapComponent.
    ///
    /// [TÍNH NĂNG / FEATURE]: Giữ lưới ô "không được tự động khai hỏa vào đây" và vẽ nó lên map.
    /// [TẠI SAO LÀM THẾ / RATIONALE]: Cách làm hiển nhiên hơn là kế thừa Verse.Area, nhưng Area được
    ///     lưu deep bên trong AreaManager, mà AreaManager.ExposeData là sealed - không chèn được logic
    ///     dọn dẹp. Gỡ mod giữa save sẽ để lại một phần tử null trong danh sách areas, và danh sách đó
    ///     bị duyệt mỗi frame để vẽ, tức NRE lặp chứ không phải một dòng log rồi chạy tiếp. MapComponent
    ///     thiếu class thì bị Scribe bỏ qua: mất dữ liệu vùng cấm, không crash. Kèm theo đó là không cần
    ///     reflection vào AreaManager.areas (private) để chèn area.
    ///     Đánh đổi: mất mục "hiện/ẩn" trong danh sách area của vanilla - không đáng kể khi mỗi map chỉ
    ///     có đúng một vùng.
    /// [Ý NGHĨA & CƠ CHẾ / MECHANICS]: BoolGrid lưu ô, CellBoolDrawer vẽ (cùng render queue và độ mờ
    ///     vanilla dùng cho area, nên vùng cấm trông đồng bộ với zone khác). Drawer chỉ được tạo khi
    ///     thực sự cần vẽ, và chỉ vẽ khi có thứ gì đó gọi MarkForDraw trong frame đó.
    /// </summary>
    public class MapComponent_NoFireZone : MapComponent, ICellBoolGiver
    {
        // Cùng render queue và opacity mà Verse.Area dùng cho drawer của nó, để vùng cấm bắn xếp lớp
        // giống hệt zone vanilla thay vì đè lên hoặc chui xuống dưới chúng.
        private const int AreaRenderQueue = 3650;
        private const float AreaOpacity = 0.33f;

        // Đỏ bão hòa. Zone trồng trọt là xanh lá, home area là xanh dương nhạt, stockpile là vàng nhạt -
        // không có zone vanilla nào là đỏ, nên không nhầm được ngay cả khi hai vùng chồng lên nhau.
        private static readonly Color ZoneColor = new Color(0.90f, 0.15f, 0.15f);

        private BoolGrid grid;
        private CellBoolDrawer drawerInt;

        public MapComponent_NoFireZone(Map map) : base(map)
        {
            grid = new BoolGrid(map);
        }

        public Color Color => ZoneColor;

        public int CellCount => grid?.TrueCount ?? 0;

        public bool AnyCellMarked => CellCount > 0;

        private CellBoolDrawer Drawer
        {
            get
            {
                if (drawerInt == null)
                {
                    drawerInt = new CellBoolDrawer(this, map.Size.x, map.Size.z, AreaRenderQueue, AreaOpacity);
                }
                return drawerInt;
            }
        }

        public bool this[IntVec3 c]
        {
            get => grid != null && c.InBounds(map) && grid[c];
            set => Set(c, value);
        }

        /// <summary>
        /// Đường tra cứu cho code gọi từ ngoài (giai đoạn 2 dùng cái này trong vòng lặp tìm mục tiêu).
        /// Trả về false cho map null, ô ngoài biên, hoặc map chưa từng có vùng cấm nào - nghĩa là
        /// "không cấm" luôn là mặc định an toàn.
        /// </summary>
        public static bool IsNoFireCell(Map map, IntVec3 c)
        {
            MapComponent_NoFireZone comp = GetFor(map);
            return comp != null && comp[c];
        }

        public static MapComponent_NoFireZone GetFor(Map map)
        {
            return map?.GetComponent<MapComponent_NoFireZone>();
        }

        public void Set(IntVec3 c, bool value)
        {
            if (grid == null || !c.InBounds(map)) return;
            if (grid[c] == value) return;

            grid[c] = value;
            drawerInt?.SetDirty();
        }

        public void Clear()
        {
            if (grid == null || grid.TrueCount == 0) return;

            grid.Clear();
            drawerInt?.SetDirty();
        }

        /// <summary>
        /// Phải được gọi mỗi frame mà vùng cấm cần hiện. Không gọi thì không vẽ - giống hệt cách
        /// Verse.Area hoạt động, nên vùng cấm không nằm chình ình trên map lúc người chơi không quan tâm.
        /// </summary>
        public void MarkForDraw()
        {
            if (map != Find.CurrentMap) return;
            if (Find.ScreenshotModeHandler.Active) return;

            Drawer.MarkForDraw();
        }

        public bool GetCellBool(int index)
        {
            return grid != null && grid[index] && !map.fogGrid.IsFogged(index);
        }

        public Color GetCellExtraColor(int index)
        {
            return Color.white;
        }

        public override void MapComponentUpdate()
        {
            // Không chạm vào Drawer qua property ở đây: property tạo mesh cho cả map, và MapComponentUpdate
            // chạy cho map không hiển thị nữa. Chưa ai gọi MarkForDraw thì chưa có gì để cập nhật.
            drawerInt?.CellBoolDrawerUpdate();
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();

            // Save cũ (trước module này) không có node nào, và Scribe_Deep để lại null. Save từ một map
            // có kích thước khác - qua dev mode hoặc mod đổi map size - để lại lưới sai chiều, mà BoolGrid
            // đánh chỉ số theo width nên lưới sai chiều là dữ liệu rác chứ không phải lỗi đọc.
            if (grid == null || !grid.MapSizeMatches(map))
            {
                grid = new BoolGrid(map);
                drawerInt?.SetDirty();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref grid, "noFireGrid");
        }
    }
}
