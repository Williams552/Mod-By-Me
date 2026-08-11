using System.Collections.Generic;
using HarmonyLib;
using FireDiscipline.AimStance;
using FireDiscipline.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace FireDiscipline.Shock
{
    /// <summary>
    /// Harmony Postfix Can thiệp vào vụ nổ (Explosion.StartExplosion) để tạo sóng xung kích gây choáng.
    /// 
    /// [TÍNH NĂNG / FEATURE]: Module Sốc Chiến đấu & Sóng Xung Kích (ShockModule - Proportional Shell Shock).
    /// [TẠI SAO LÀM THẾ / RATIONALE]: Giả lập thực tế hiệu ứng sóng xung kích concussive từ vụ nổ lớn (như pháo kích Mortar hay tên lửa Doomsday) 
    ///     khiến Pawn xung quanh bị choáng váng, ù tai, giảm tốc độ di chuyển và tăng thời gian ngắm bắn thay vì đứng ngơ ngác không bị ảnh hưởng.
    /// [ĐIỀU CHỈNH MẶC ĐỊNH / DEFAULTS]: Bán kính sóng xung kích tính theo công thức phi tuyến tính: shockRadius = min(20, r + 2*sqrt(r)). Đạn pháo 4.9 ô -> sóng xung kích 9.3 ô (Trần 20 ô).
    /// [Ý NGHĨA & CƠ CHẾ / MECHANICS]: Khi nổ xảy ra, quét các Pawn nằm trong bán kính sóng xung kích (kiểm tra Line of Sight và lọc các sát thương vật lý). 
    ///     Gán Hediff `FD_ShellShock` với mức độ suy giảm dần theo khoảng cách đến tâm nổ.
    /// </summary>
    public static class Patch_Explosion
    {
        public static float CalculateShockRadius(float dmgRadius)
        {
            float cap = FireDisciplineMod.Settings?.shellShockRadiusCap ?? 20f;
            float coefficient = FireDisciplineMod.Settings?.shellShockRadiusCoefficient ?? 2f;
            return Mathf.Min(cap, dmgRadius + coefficient * Mathf.Sqrt(dmgRadius));
        }

        public static float CalculatePowerFactor(float damAmount)
        {
            return Mathf.Clamp(damAmount / 50f, 0.4f, 1.0f);
        }

        public static void Postfix(Explosion __instance)
        {
            if (!Core.PatchRegistry.IsModuleEnabled(ShockModule.Id))
                return;

            if (__instance == null || __instance.Map == null) return;

            // Gate 2.2: Damage Type Filter (Physical/Explosive only, damAmount >= 10)
            if (__instance.damAmount < 10) return;
            DamageDef damDef = __instance.damType;
            if (damDef == null || (!damDef.isExplosive && damDef != DamageDefOf.Bullet && damDef != DamageDefOf.Bomb && damDef != DamageDefOf.Cut && damDef != DamageDefOf.Blunt))
            {
                return; // Skip firefoam, smoke, EMP, extinguish, foam
            }

            Map map = __instance.Map;
            IntVec3 center = __instance.Position;
            float dmgRadius = __instance.radius;

            if (dmgRadius <= 0.5f) return;

            float shockRadius = CalculateShockRadius(dmgRadius);
            float powerFactor = CalculatePowerFactor(__instance.damAmount);

            HediffDef shellShockDef = DefDatabase<HediffDef>.GetNamedSilentFail("FD_ShellShock");
            if (shellShockDef == null) return;

            int processedPawns = 0;

            foreach (Thing thing in GenRadial.RadialDistinctThingsAround(center, map, shockRadius, true))
            {
                // Gate 2.5: Cap at 40 pawns per explosion
                if (processedPawns >= 40) break;

                if (thing is Pawn victim && !victim.Dead && victim.RaceProps.Humanlike)
                {
                    // Gate 2.4: Line of Sight (LOS) Check
                    if (!GenSight.LineOfSight(center, victim.Position, map, true))
                        continue;

                    float dist = (victim.Position - center).LengthHorizontal;
                    float baseSeverity = 0.25f;

                    if (dist <= dmgRadius)
                    {
                        // Direct blast zone
                        baseSeverity = 0.85f * powerFactor;
                    }
                    else
                    {
                        // Proportional shockwave falloff zone
                        float frac = (dist - dmgRadius) / (shockRadius - dmgRadius);
                        baseSeverity = Mathf.Clamp(1.0f - frac, 0.10f, 0.85f) * powerFactor;
                    }

                    // Gate 2.3: Non-drafted pawns multiplier (x0.3)
                    if (!victim.Drafted)
                    {
                        baseSeverity *= 0.30f;
                    }

                    // Gate 2.3b: Active Energy Shield Absorption (Law 2 - CompShield)
                    float activeShieldFraction = ShieldUtility.GetActiveShieldEnergyFraction(victim);
                    if (activeShieldFraction > 0f)
                    {
                        baseSeverity *= (1.0f - 0.85f * activeShieldFraction);
                    }

                    // Gate 2.1: Floor cut: Ignore if severity < 0.15
                    if (baseSeverity < 0.15f)
                        continue;

                    processedPawns++;

                    // Gate 2.5: Refresh severity instead of stacking
                    Hediff hediff = victim.health.hediffSet.GetFirstHediffOfDef(shellShockDef);
                    if (hediff != null)
                    {
                        hediff.Severity = Mathf.Max(hediff.Severity, baseSeverity);
                    }
                    else
                    {
                        hediff = HediffMaker.MakeHediff(shellShockDef, victim);
                        hediff.Severity = baseSeverity;
                        victim.health.AddHediff(hediff);
                    }
                    hediff.TryGetComp<HediffComp_TimedDecay>()?.Notify_Applied();

                    // Sharpshot warmup reset from concussive shockwave
                    AimStanceTracker.Notify_Suppressed(victim);

                    if (map != null && Find.CameraDriver != null && dist <= dmgRadius)
                    {
                        MoteMaker.ThrowText(victim.DrawPos, map, "Shell Shocked!", Color.yellow);
                    }
                }
            }
        }
    }
}
