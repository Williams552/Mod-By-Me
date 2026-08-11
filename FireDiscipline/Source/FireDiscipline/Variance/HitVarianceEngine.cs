using HarmonyLib;
using UnityEngine;
using Verse;

namespace FireDiscipline.Variance
{
    /// <summary>
    /// Bộ máy tính toán và điều tiết tính ngẫu nhiên (RNG) của đường đạn súng bắn.
    /// 
    /// [TÍNH NĂNG / FEATURE]: Module Giảm Nhẹ Ngẫu Nhiên Đường Đạn (VarianceModule - Hit Variance Mitigation Engine).
    /// [TẠI SAO LÀM THẾ / RATIONALE]: Giảm thiểu hiện tượng hên xui phi lý (chuỗi trượt đạn liên tiếp dù tỷ lệ trúng cao, hoặc chuỗi trúng liên tiếp khi tỷ lệ trúng cực thấp); 
    ///     mang lại độ ổn định thực tế cho xạ thủ dựa trên chỉ số kỹ năng.
    /// [ĐIỀU CHỈNH MẶC ĐỊNH / DEFAULTS]: Module mặc định TẮT (`enableHitVariance = false`). Áp dụng mô hình Quota cho súng 1 phát và Pity (+8%/phát trượt, tối đa +32%) cho súng burst.
    /// [Ý NGHĨA & CƠ CHẾ / MECHANICS]: Can thiệp vào cấu trúc `ShotReport` của đợt bắn. Nếu súng 1 phát, tích lũy Quota theo thời gian; 
    ///     nếu súng bắn loạt, cộng điểm bảo hiểm (Pity step) tăng tỷ lệ trúng cho các viên đạn sau nếu viên trước đó bị trượt. Bỏ qua các vũ khí có `ForcedMissRadius` (như lựu đạn/súng cối).
    /// </summary>
    public static class HitVarianceEngine
    {
        private static readonly AccessTools.StructFieldRef<ShotReport, float> shooterFactorRef =
            AccessTools.StructFieldRefAccess<ShotReport, float>("factorFromShooterAndDist");

        private static readonly AccessTools.StructFieldRef<ShotReport, float> coverBlockRef =
            AccessTools.StructFieldRefAccess<ShotReport, float>("coversOverallBlockChance");

        private const float ForceHitFactor = 9999f;

        public static void ProcessHitReport(Thing caster, Verb verb, ref ShotReport report)
        {
            // AC4: If not in real shot execution (e.g. UI tooltip hover), return immediately without touching state or report.
            if (!HitVarianceState.InRealShot)
                return;

            if (FireDisciplineMod.Settings == null || !FireDisciplineMod.Settings.enableHitVariance)
                return;

            if (caster is not Pawn shooterPawn || verb?.verbProps == null)
                return;

            // AC17 / Rule 8: Bypasses forced-miss weapons (e.g. Mortars, Grenades) to avoid Def mutation.
            if (verb.verbProps.ForcedMissRadius > 0f)
                return;

            // BS-1 / AC14: p is the true probability of passing both aim and cover gates.
            float coverBlock = coverBlockRef != null ? coverBlockRef(ref report) : 0f;
            float passCover = Mathf.Clamp01(1f - coverBlock);
            float p = Mathf.Clamp01(report.AimOnTargetChance_IgnoringPosture * passCover);

            // AC5: Select model by Def-level verbProps.burstShotCount (NOT runtime ShotsPerBurst).
            int baseBurstCount = verb.verbProps.burstShotCount;
            bool hit = false;
            bool decisionMade = false;

            // UNIFIED BURST-SCOPED QUOTA MODEL (Option B: Universal deterministic expectation preservation for all weapons)
            float carry = HitVarianceState.GetQuotaCarry(shooterPawn);
            carry += p;

            if (carry >= 1.0f - 1e-4f)
            {
                carry -= 1.0f;
                hit = true;
            }
            else
            {
                hit = false;
            }

            HitVarianceState.SetQuotaCarry(shooterPawn, carry);
            HitVarianceState.RecordQuotaShot(p, hit);
            decisionMade = true;

            if (!decisionMade)
                return;

            // BS-2 / AC15 / AC16: Force outcome on ShotReport fields.
            if (shooterFactorRef != null)
            {
                if (hit)
                {
                    // AC15: Force hit by setting shooter factor large and clearing cover block.
                    shooterFactorRef(ref report) = ForceHitFactor;
                    if (coverBlockRef != null)
                    {
                        coverBlockRef(ref report) = 0f;
                    }
                }
                else
                {
                    // AC16: Force miss by zeroing shooter factor. Do NOT modify cover block on miss.
                    shooterFactorRef(ref report) = 0f;
                }
            }
        }
    }
}
