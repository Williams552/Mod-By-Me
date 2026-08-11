using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace FireDiscipline.Graze
{
    /// <summary>
    /// Harmony Prefix Can thiệp vào quá trình gây sát thương (DamageWorker_AddInjury.Apply) để thực thi cơ chế Graze.
    /// 
    /// [TÍNH NĂNG / FEATURE]: Module Cơ chế Sượt Đạn (GrazeModule - Anti-One-Shot Protection).
    /// [TẠI SAO LÀM THẾ / RATIONALE]: Loại bỏ cảm giác ức chế phi lý trong Vanilla RimWorld khi lính kỳ cựu mặc giáp xịn bị đạn rác từ raider skill thấp bắn trúng não chết ngay lập tức (RNG death).
    /// [ĐIỀU CHỈNH MẶC ĐỊNH / DEFAULTS]: Bật mặc định. Trần Hit Chance = 0.65 (phát bắn trúng >= 65% không bao giờ Graze). Giảm 65% sát thương (chỉ nhận 35% sát thương gốc).
    /// [Ý NGHĨA & CƠ CHẾ / MECHANICS]: Kiểm tra các đòn tấn công tầm xa nhắm vào 7 cơ quan sinh tồn (Brain, Head, Eye, Heart, Neck, Spine, Liver). 
    ///     Nếu thỏa mãn công thức Graze Chance dựa trên Hit Chance, đòn đánh bị giảm 65% sát thương và vết thương được đổi hướng (reroute) sang các chi phụ bên ngoài.
    /// </summary>
    public static class Patch_DamageWorker_AddInjury
    {
        public static bool Prefix(DamageWorker_AddInjury __instance, ref DamageInfo dinfo, Thing thing)
        {
            if (!Core.PatchRegistry.IsModuleEnabled(GrazeModule.Id))
                return true;

            if (!(thing is Pawn victim) || victim.Dead || !victim.RaceProps.Humanlike)
                return true;

            // Only apply Graze to ranged bullet/projectile attacks
            if (dinfo.Instigator == null || dinfo.Weapon == null || !dinfo.Weapon.IsRangedWeapon)
                return true;

            BodyPartRecord hitPart = dinfo.HitPart;
            if (hitPart == null) return true;

            bool isVitalOrgan = IsVitalOrganOrHead(hitPart);
            if (!isVitalOrgan) return true;

            // Calculate p = TotalEstimatedHitChance via ShotReport
            float p = 0.425f; // Fallback yielding 0.50 grazeChance if shooter unavailable
            Pawn shooter = dinfo.Instigator as Pawn;

            if (shooter != null && shooter.equipment?.PrimaryEq?.PrimaryVerb != null)
            {
                Verb verb = shooter.equipment.PrimaryEq.PrimaryVerb;
                ShotReport report = ShotReport.HitReportFor(shooter, verb, victim);
                p = report.TotalEstimatedHitChance;
            }

            float grazeChance = CalculateGrazeChance(p);

            if (Rand.Chance(grazeChance))
            {
                // GRAZE ACTIVATED!
                float mult = FireDisciplineMod.Settings?.grazeDamageMultiplier ?? 0.35f;
                float originalDmg = dinfo.Amount;
                dinfo.SetAmount(Mathf.Max(1f, originalDmg * mult));

                // Reroute to outer limb
                if (FireDisciplineMod.Settings?.protectVitalOrgans ?? true)
                {
                    BodyPartRecord outerLimb = FindOuterLimb(victim);
                    if (outerLimb != null)
                    {
                        dinfo.SetHitPart(outerLimb);
                    }
                }

                // Visual Mote feedback
                if (victim.Map != null && Find.CameraDriver != null)
                {
                    MoteMaker.ThrowText(victim.DrawPos, victim.Map, $"Graze (-{(int)((1f - mult) * 100f)}%)", Color.cyan);
                }

                if (FireDisciplineMod.Settings != null && FireDisciplineMod.Settings.verboseCombatLogging)
                {
                    Log.Message($"[Fire Discipline Graze v3] {victim.LabelShort}'s {hitPart.def.defName} shot was grazing! p={p:P1}, GrazeChance={grazeChance:P1}. Dmg reduced from {originalDmg:F1} to {dinfo.Amount:F1}");
                }
            }

            return true;
        }

        /// <summary>
        /// grazeChance = clamp(0, 1, (0.65 - p) / 0.45), where p is the shot's estimated hit chance.
        ///
        /// A shot the shooter was likely to make lands normally; one they were unlikely to make gets
        /// downgraded. p >= 0.65 never grazes, p &lt;= 0.20 always does. Using hit chance rather than
        /// a stance flag means the rule covers distance, cover, lighting, skill and suppression at
        /// once, and applies symmetrically to raiders.
        ///
        /// Public so the debug harness measures this exact curve instead of a copy of it.
        /// </summary>
        public static float CalculateGrazeChance(float hitChance)
        {
            FireDisciplineSettings settings = FireDisciplineMod.Settings;
            float ceiling = settings?.grazeHitChanceCeiling ?? 0.65f;
            float span = Mathf.Max(0.01f, settings?.grazeChanceSpan ?? 0.45f);

            return Mathf.Clamp01((ceiling - hitChance) / span);
        }

        public static bool IsVitalOrganOrHead(BodyPartRecord part)
        {
            if (part == null || part.def == null) return false;

            // Rule 2: Derive from vanilla BodyPartRecord height, depth, and tags without string matching.
            if (part.height == BodyPartHeight.Top) return true;
            if (part.depth == BodyPartDepth.Inside) return true;

            if (part.def.tags != null)
            {
                for (int i = 0; i < part.def.tags.Count; i++)
                {
                    var tag = part.def.tags[i];
                    if (tag == BodyPartTagDefOf.ConsciousnessSource ||
                        tag == BodyPartTagDefOf.BloodPumpingSource ||
                        tag == BodyPartTagDefOf.BreathingSource ||
                        tag == BodyPartTagDefOf.SightSource)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static BodyPartRecord FindOuterLimb(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null) return null;

            var notMissing = pawn.health.hediffSet.GetNotMissingParts().ToList();
            // Vanilla Hediff_Injury.PostAdd throws an error if part.coverageAbs <= 0,
            // because those are structural parts that cannot be hit directly.
            var outerLimbs = notMissing.Where(p => p.depth == BodyPartDepth.Outside && p.height != BodyPartHeight.Top && p.coverageAbs > 0f).ToList();

            if (outerLimbs.Count > 0)
            {
                return outerLimbs.RandomElement();
            }

            return null;
        }
    }
}
