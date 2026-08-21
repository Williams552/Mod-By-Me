using FireDiscipline.Core;
using RimWorld;
using Verse;

namespace FireDiscipline.NoFireZone
{
    /// <summary>
    /// Loại mục tiêu nằm trong (hoặc đủ gần) vùng cấm bắn khỏi việc tự động ngắm của turret người chơi.
    ///
    /// [TÍNH NĂNG / FEATURE]: Pháo tự động sẽ không chọn mục tiêu mà đạn có thể rơi vào vùng cấm.
    /// [TẠI SAO LÀM THẾ / RATIONALE]: Điểm patch là IsValidTarget chứ không phải TryFindNewTarget.
    ///     TryFindNewTarget trả về mục tiêu đã chọn xong, nên chặn ở đó là all-or-nothing: mục tiêu tốt
    ///     nhất nằm trong vùng cấm thì khẩu pháo bỏ luôn lượt tìm và đứng im, kể cả khi ngoài vùng cấm
    ///     còn ba mục tiêu khác bắn được - người chơi sẽ đọc đó là "pháo hỏng". IsValidTarget là predicate
    ///     mà AttackTargetFinder.BestShootTargetFromCurrentPosition dùng để lọc từng ứng viên, nên loại ở
    ///     đây khiến finder tự chọn mục tiêu hợp lệ kế tiếp.
    ///     Không patch OrderAttack: chỉ điểm thủ công luôn được tôn trọng, vùng cấm chỉ ràng buộc phần
    ///     máy tự quyết. Đây là điểm phân biệt module này với một khoá cứng.
    /// [Ý NGHĨA & CƠ CHẾ / MECHANICS]: Chỉ áp dụng cho turret phe người chơi, và mặc định chỉ cho vũ khí
    ///     nổ (cối, pháo, đạn nổ). Turret súng máy không cần và người chơi không muốn nó ngừng bắn oan -
    ///     có setting để mở rộng ra mọi turret.
    ///     Postfix này chạy mỗi ứng viên mục tiêu chứ không phải mỗi lượt tìm, nên mọi nhánh thoát sớm
    ///     được xếp theo giá: cờ module, phe, rồi mới đến map component và vòng quét.
    /// </summary>
    public static class Patch_TurretAutoTarget
    {
        public static void Postfix_IsValidTarget(Building_TurretGun __instance, Thing t, ref bool __result)
        {
            // Mục tiêu đã bị vanilla loại rồi thì không có gì để làm.
            if (!__result) return;

            if (!PatchRegistry.IsModuleEnabled(NoFireZoneModule.Id)) return;

            // Chỉ turret của người chơi. Bỏ qua kiểm tra này là vô tình buff turret của địch: pháo địch
            // sẽ tránh bắn vào chính khu vực người chơi đánh dấu là cần bảo vệ.
            if (__instance == null || __instance.Faction != Faction.OfPlayer) return;

            Map map = __instance.Map;
            if (map == null || t == null) return;

            MapComponent_NoFireZone zone = MapComponent_NoFireZone.GetFor(map);
            if (zone == null || !zone.AnyCellMarked) return;

            // Khẩu được người chơi cố ý cho phá rào. Kiểm tra trước vòng quét vì nó rẻ hơn nhiều.
            CompNoFireZoneObedience obedience = __instance.GetComp<CompNoFireZoneObedience>();
            if (obedience != null && !obedience.Obeys) return;

            FireDisciplineSettings settings = FireDisciplineMod.Settings;
            bool allTurrets = settings != null && settings.noFireZoneAllTurrets;

            float radius = NoFireZoneUtility.ScanRadiusFor(__instance.AttackVerb, allTurrets);
            if (radius < 0f) return;

            if (NoFireZoneUtility.AnyNoFireCellWithin(zone, t.Position, radius))
            {
                __result = false;
                NoFireZoneNotice.NotifyBlocked(__instance);
            }
        }
    }
}
