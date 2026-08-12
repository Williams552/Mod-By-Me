using FireDiscipline.AimStance;
using FireDiscipline.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace FireDiscipline.Suppression
{
    /// <summary>
    /// Bộ máy tính toán và xử lý cơ chế Áp chế (Suppression Engine) cốt lõi của mod.
    /// 
    /// [TÍNH NĂNG / FEATURE]: Module Áp chế & Vật nấp (SuppressionCoreModule) - Quản lý việc tích lũy, tính toán và gắn Hediff FD_Suppressed.
    /// [TẠI SAO LÀM THẾ / RATIONALE]: Xây dựng vòng lặp chiến thuật thực sự (Tư thế Rapid gây áp chế -> Kẻ địch bị gìm chân/giảm ngắm -> Đồng đội bọc lót/Sharpshot tiêu diệt).
    ///     Phạt tốc độ di chuyển thay vì khóa cứng ngắm bắn để người chơi vẫn phản công được khi đứng trong Cover.
    /// [ĐIỀU CHỈNH MẶC ĐỊNH / DEFAULTS]: Tích lũy cơ bản +0.25 severity/viên đạn sượt. Bán kính áp chế 3.5 ô. 
    ///     Tự động tắt ở lần đầu chạy nếu phát hiện mod áp chế khác (Combat Extended / Suppression Continued).
    /// [Ý NGHĨA & CƠ CHẾ / MECHANICS]: Tính toán tổng lượng áp chế dựa trên: Tư thế người bắn (Rapid x1.5), Tư thế nạn nhân (Sharpshot x2.0, Prone x0.5),
    ///     và Vật nấp của nạn nhân (Cover reduction clamp 25%-100%). Gán Hediff FD_Suppressed trực tiếp lên Pawn.
    /// </summary>
    public static class SuppressionEngine
    {
        private static HediffDef suppressedDefCache;

        public static HediffDef SuppressedDef
        {
            get
            {
                if (suppressedDefCache == null)
                {
                    suppressedDefCache = DefDatabase<HediffDef>.GetNamedSilentFail("FD_Suppressed");
                }
                return suppressedDefCache;
            }
        }

        /// <summary>
        /// Suppression inflicted by one shot landing near a pawn, with the full stance matrix
        /// applied:
        ///
        ///   amount = base
        ///          * inflicted(shooter stance)   Rapid x1.50 - laying down volume
        ///          * received(victim stance)     Sharpshot x2.00 - exposed and concentrating
        ///                                        Prone x0.50 - dug in
        ///          * cover(victim position)      reduced by CoverUtility.CalculateOverallBlockChance
        ///
        /// Every multiplier is a mod setting, none are magic numbers.
        /// </summary>
        public static float CalculateSuppressionAmount(Pawn shooter, Pawn victim)
        {
            FireDisciplineSettings settings = FireDisciplineMod.Settings;
            float amount = settings?.suppressionBaseAmount ?? 0.25f;

            if (shooter != null)
            {
                if (AimStanceTracker.GetStance(shooter) == AimStanceMode.Rapid)
                {
                    amount *= settings?.rapidSuppressionMultiplier ?? 1.50f;
                }

                // Heavy Weapon Suppression Bonus (+100% bonus suppression for heavy automatics with burst >= 5)
                Verb verb = shooter.equipment?.PrimaryEq?.PrimaryVerb;
                if (verb != null && verb.verbProps != null && verb.verbProps.burstShotCount >= 5)
                {
                    amount *= settings?.heavyWeaponSuppressionMultiplier ?? 2.00f;
                }
            }

            if (victim != null)
            {
                AimStanceMode victimStance = AimStanceTracker.GetStance(victim);
                if (victimStance == AimStanceMode.Sharpshot)
                {
                    amount *= settings?.sharpshotSuppressionVulnerability ?? 2.00f;
                }
                if (AimStanceTracker.IsDugIn(victim))
                {
                    amount *= settings?.proneSuppressionResistance ?? 0.50f;
                }

                if ((settings?.enableCoverSuppression ?? true) && shooter != null && victim.Map != null)
                {
                    float vanillaBlock = CoverUtility.CalculateOverallBlockChance(victim, shooter.Position, victim.Map);
                    float block = vanillaBlock;
                    if (settings?.enableCoverStacking ?? false)
                    {
                        float lineBlock = CoverStackingUtility.LineCoverBlockChance(shooter.Position, victim, victim.Map);
                        if (lineBlock > 0f)
                        {
                            float combined = vanillaBlock + (1f - vanillaBlock) * lineBlock;
                            block = Mathf.Min(combined, settings.coverStackingCap);
                        }
                    }
                    float factor = settings?.coverSuppressionFactor ?? 0.85f;
                    float floor = settings?.coverSuppressionFloor ?? 0.25f;
                    amount *= Mathf.Clamp(1f - block * factor, floor, 1f);
                }
            }

            return amount;
        }

        /// <summary>
        /// Adds suppression severity to a pawn, creating the hediff if needed.
        /// Returns the resulting severity, or -1 if nothing was applied.
        /// </summary>
        public static float ApplySuppression(Pawn victim, float amount)
        {
            if (victim == null || victim.Dead || victim.health == null) return -1f;
            if (amount <= 0f) return -1f;

            HediffDef def = SuppressedDef;
            if (def == null) return -1f;

            Hediff hediff = victim.health.hediffSet?.GetFirstHediffOfDef(def);
            if (hediff != null)
            {
                hediff.Severity = Mathf.Clamp(hediff.Severity + amount, MinSeverity(def), MaxSeverity(def));
                NotifyApplied(hediff);
                return hediff.Severity;
            }

            hediff = HediffMaker.MakeHediff(def, victim);
            hediff.Severity = Mathf.Clamp(amount, MinSeverity(def), MaxSeverity(def));
            victim.health.AddHediff(hediff);
            NotifyApplied(hediff);
            return hediff.Severity;
        }

        /// <summary>
        /// Tells the hediff it was just topped up.
        ///
        /// HediffComp_Disappears counts down from creation and is NOT reset by raising severity, so
        /// without this a pawn under continuous fire loses its suppression 3-5 seconds after the
        /// FIRST round regardless of how many follow. Measured in game: a pawn taking seven rounds
        /// in a row went 219 -> 214 -> 207 -> 199 -> 193 -> 186 ticks remaining and then expired
        /// mid-burst. Suppression that cannot be sustained cannot pin anything down, which is the
        /// entire point of the mechanic.
        /// </summary>
        private static void NotifyApplied(Hediff hediff)
        {
            HediffComp_Disappears disappears = hediff?.TryGetComp<HediffComp_Disappears>();
            if (disappears != null)
            {
                disappears.ticksToDisappear = disappears.Props.disappearsAfterTicks.RandomInRange;
            }

            HediffComp_SuppressionDecay decay = hediff?.TryGetComp<HediffComp_SuppressionDecay>();
            decay?.Notify_Applied();
        }

        /// <summary>
        /// Full path for one shot landing near one pawn: matrix, hediff, and the Sharpshot warmup
        /// reset that models a marksman losing the shot when rounds crack past.
        /// </summary>
        public static void SuppressPawn(Pawn shooter, Pawn victim)
        {
            float amount = CalculateSuppressionAmount(shooter, victim);

            float before = LogEvents ? GetSeverity(victim) : 0f;
            float after = ApplySuppression(victim, amount);

            // Sharpshot pays for its accuracy by being fragile under fire.
            AimStanceTracker.Notify_Suppressed(victim);

            if (LogEvents && after >= 0f)
            {
                LogSuppressionEvent(shooter, victim, amount, before, after);
            }
        }

        /// <summary>
        /// Dev-only live trace. Suppression has no on-screen indicator during a firefight, so
        /// without this the only way to observe it is to pause and open a health tab, which changes
        /// what you are measuring. Off by default and toggled from the debug menu.
        /// </summary>
        public static bool LogEvents;

        private static void LogSuppressionEvent(Pawn shooter, Pawn victim, float amount, float before, float after)
        {
            string shooterPart = shooter != null
                ? $"{shooter.LabelShort}[{AimStanceTracker.GetStance(shooter)}]"
                : "unknown";

            // Exposes whether sustained fire actually extends the hediff. HediffComp_Disappears
            // counts down from creation, so a value that keeps falling while a pawn is still being
            // shot at means the duration is NOT being refreshed.
            string remaining = "n/a";
            HediffDef def = SuppressedDef;
            if (def != null && victim?.health?.hediffSet != null)
            {
                Hediff hediff = victim.health.hediffSet.GetFirstHediffOfDef(def);
                HediffComp_Disappears disappears = hediff?.TryGetComp<HediffComp_Disappears>();
                if (disappears != null) remaining = disappears.ticksToDisappear.ToString();
            }

            Log.Message($"[FD suppress] t={Find.TickManager?.TicksGame} "
                + $"{shooterPart} -> {victim.LabelShort}[{AimStanceTracker.GetStance(victim)}] "
                + $"amount={amount:F3} severity {before:F3}->{after:F3} ticksToDisappear={remaining}");
        }

        public static float GetSeverity(Pawn pawn)
        {
            HediffDef def = SuppressedDef;
            if (def == null || pawn?.health?.hediffSet == null) return 0f;

            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(def);
            return hediff?.Severity ?? 0f;
        }

        /// <summary>
        /// Read from the Def rather than hardcoded, matching XML <minSeverity>0</minSeverity>.
        /// </summary>
        public static float MinSeverity(HediffDef def)
        {
            return def?.minSeverity ?? 0f;
        }

        /// <summary>
        /// Read from the Def rather than hardcoded, so the severity scale can be retuned in XML
        /// without the engine silently clamping against a stale ceiling.
        /// </summary>
        public static float MaxSeverity(HediffDef def)
        {
            return def?.maxSeverity ?? 1.0f;
        }
    }
}
