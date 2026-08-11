using HarmonyLib;
using Verse;
using FireDiscipline.Core;

namespace FireDiscipline.Suppression
{
    /// <summary>
    /// Harmony Postfix Can thiệp vào Verse.Verb.Available để thực thi trạng thái Bị Ghim (Pinned State).
    /// 
    /// [TÍNH NĂNG / FEATURE]: Module Áp chế & Vật nấp (SuppressionCoreModule - Pinned State Mechanism).
    /// [TẠI SAO LÀM THẾ / RATIONALE]: Giả lập thực tế khi bị hỏa lực áp chế quá nặng nề (mức áp chế >= 7.0 trên thang 0-9), Pawn bị hoảng loạn/dập đầu xuống đất và hoàn toàn KHÔNG THỂ ngắm bắn trả (khóa đòn tấn công tầm xa).
    /// [ĐIỀU CHỈNH MẶC ĐỊNH / DEFAULTS]: Bật mặc định (`enablePinnedState = true`). Ngưỡng ghim `pinnedSeverityThreshold = 7.0f`.
    /// [Ý NGHĨA & CƠ CHẾ / MECHANICS]: Kiểm tra mức độ áp chế của Pawn, nếu severity >= 7.0 thì trả về `__result = false` cho đòn tấn công tầm xa (`Verb.Available`), 
    ///     khóa hoàn toàn khả năng bắn súng của Pawn đó. Đòn tấn công cận chiến (Melee) và công trình tự động (Turret) không bị ảnh hưởng.
    /// </summary>
    public static class Patch_Verb_Available
    {
        public static void Postfix(Verb __instance, ref bool __result)
        {
            if (!__result) return;
            if (!PatchRegistry.IsModuleEnabled(SuppressionCoreModule.Id)) return;

            FireDisciplineSettings settings = FireDisciplineMod.Settings;
            if (settings == null || !settings.enablePinnedState) return;

            if (__instance == null || __instance.IsMeleeAttack) return;

            Pawn pawn = __instance.CasterPawn;
            if (pawn == null) return;

            float threshold = settings.pinnedSeverityThreshold;
            if (SuppressionEngine.GetSeverity(pawn) >= threshold)
            {
                __result = false;
            }
        }
    }
}
