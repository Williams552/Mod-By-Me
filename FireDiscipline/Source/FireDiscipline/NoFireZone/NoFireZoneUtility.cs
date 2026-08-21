using UnityEngine;
using Verse;

namespace FireDiscipline.NoFireZone
{
    /// <summary>
    /// Quyết định khẩu nào bị vùng cấm ràng buộc, và quét vùng nguy hiểm quanh một mục tiêu.
    ///
    /// [TÍNH NĂNG / FEATURE]: Hai câu hỏi của giai đoạn 2 - "khẩu này có thuộc diện không?" và
    ///     "bắn vào ô này thì có ô cấm nào dính đạn không?".
    /// [TẠI SAO LÀM THẾ / RATIONALE]: Kiểm tra đúng ô mục tiêu là chưa đủ. Địch đứng sát mép vùng cấm
    ///     vẫn khiến đạn cối rơi vào trong, vì thứ quyết định điểm rơi là forced miss radius chứ không
    ///     phải ô người chơi thấy. Bán kính quét vì thế là forced miss radius CỘNG bán kính nổ của đạn:
    ///     đó mới là toàn bộ vùng có thể ăn damage. Cách này tự đúng với đạn mod, vì đọc thẳng từ def.
    ///     Không memo hoá gì cả: mọi thứ ở đây là vài lần đọc field, trong khi cache theo VerbProperties
    ///     sẽ sai với cối - cối đổi loại đạn theo quả đang nạp, nên bán kính nổ không phải hằng số của
    ///     khẩu pháo. VerbProperties.ForcedMissRadius (property, không phải field cùng tên) cũng đổi
    ///     theo tuỳ chọn Classic Mortars của storyteller, tức đổi theo từng save.
    /// [Ý NGHĨA & CƠ CHẾ / MECHANICS]: Vòng quét chạy trong đường nóng tìm mục tiêu nên không cấp phát
    ///     gì: dùng mảng offset tĩnh GenRadial.RadialPattern thay cho iterator RadialCellsAround, và
    ///     thoát ngay ở ô cấm đầu tiên gặp được.
    /// </summary>
    public static class NoFireZoneUtility
    {
        // Ngưỡng vanilla dùng để coi một verb là "bắn lệch có chủ đích" (Verb_LaunchProjectile).
        private const float BlastThreshold = 0.5f;

        // Chặn trên cho vòng quét. Không vũ khí vanilla nào tới gần mức này; nó tồn tại để một def mod
        // có forced miss radius vô lý không biến mỗi lần tìm mục tiêu thành vòng lặp hàng nghìn ô.
        private const float MaxScanRadius = 12f;

        /// <summary>
        /// Bán kính cần quét quanh mục tiêu, hoặc số âm nếu khẩu này không thuộc diện áp dụng.
        /// Trả về 0 là hợp lệ và có nghĩa "chỉ kiểm tra đúng ô mục tiêu".
        /// </summary>
        public static float ScanRadiusFor(Verb verb, bool applyToAllTurrets)
        {
            if (verb?.verbProps == null) return -1f;

            float forcedMiss = Mathf.Max(0f, verb.verbProps.ForcedMissRadius);
            float explosion = Mathf.Max(0f, VerbUtility.GetProjectile(verb)?.projectile?.explosionRadius ?? 0f);

            // ProjectileFliesOverhead là cách vanilla tự nhận diện cối/pháo bắn cầu vồng (chính
            // Building_TurretGun.IsValidTarget dùng nó), và nó đúng cả với def mod không đặt isMortar.
            bool subject = forcedMiss > BlastThreshold
                || explosion > BlastThreshold
                || VerbUtility.ProjectileFliesOverhead(verb);

            if (!subject && !applyToAllTurrets) return -1f;
            return forcedMiss + explosion;
        }

        /// <summary>
        /// True nếu có ít nhất một ô cấm nằm trong bán kính quanh tâm. Thoát ngay ở ô đầu tiên tìm thấy.
        /// </summary>
        public static bool AnyNoFireCellWithin(MapComponent_NoFireZone zone, IntVec3 center, float radius)
        {
            if (zone == null) return false;

            int cellCount = NumCellsFor(radius);
            IntVec3[] pattern = GenRadial.RadialPattern;

            for (int i = 0; i < cellCount; i++)
            {
                // Indexer của zone tự kiểm tra biên, nên offset chạy ra ngoài map chỉ trả về false.
                if (zone[center + pattern[i]]) return true;
            }

            return false;
        }

        private static int NumCellsFor(float radius)
        {
            if (radius <= 0f) return 1;

            float clamped = Mathf.Min(radius, MaxScanRadius);
            return GenRadial.NumCellsInRadius(clamped);
        }
    }
}
