using FireDiscipline.Core;
using RimWorld;
using Verse;

namespace FireDiscipline.NoFireZone
{
    /// <summary>
    /// Báo cho người chơi biết lần đầu vùng cấm thực sự chặn một khẩu pháo.
    ///
    /// [TÍNH NĂNG / FEATURE]: Đúng một message, lần đầu tiên trong mỗi ván.
    /// [TẠI SAO LÀM THẾ / RATIONALE]: Turret không bắn là một trạng thái câm - không có animation, không
    ///     có dòng inspect nào nói tại sao. Người chơi vẽ vùng cấm xong, thấy pháo đứng im, và kết luận
    ///     là mod hỏng. Một dòng chữ ở lần đầu tiên xoá hẳn hiểu nhầm đó.
    ///     Cờ được lưu vào save chứ không chỉ giữ trong bộ nhớ: nhắc lại mỗi lần load game là phiền chứ
    ///     không phải hữu ích.
    /// [Ý NGHĨA & CƠ CHẾ / MECHANICS]: Đường nóng chỉ đọc một biến static bool. Việc chạm tới GameComponent
    ///     chỉ xảy ra đúng một lần, ngay trước khi hiện message.
    /// </summary>
    public static class NoFireZoneNotice
    {
        private static bool armed;

        /// <summary>Gọi khi vào game. alreadyShown đọc từ save.</summary>
        public static void Reset(bool alreadyShown)
        {
            armed = !alreadyShown;
        }

        public static void NotifyBlocked(Thing turret)
        {
            if (!armed) return;
            armed = false;

            FireDisciplineGameComponent gameComp = Current.Game?.GetComponent<FireDisciplineGameComponent>();
            if (gameComp != null)
            {
                gameComp.noFireZoneNoticeShown = true;
            }

            Messages.Message("FD_NoFireZone_FirstBlockMessage".Translate(), turret,
                MessageTypeDefOf.NeutralEvent, false);
        }
    }
}
