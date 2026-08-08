using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace FireDiscipline.Core
{
    /// <summary>
    /// Every judgement Fire Discipline makes about what KIND of weapon something is.
    ///
    /// Architecture rule 2: these are derived from vanilla accuracy stats so weapons from any mod
    /// classify without a hardcoded list. Audited against 163 ranged weapons across 6 sources on
    /// 2026-08-06: 12 shotguns detected, 0 false positives, 1 false negative (Gun_Scattergun, whose
    /// range collides exactly with a gauss magnum pistol - accepted, see handoff section 3).
    ///
    /// Lives in Core rather than beside a patch because three unrelated systems consume it:
    /// the Rapid stance falloff, the shotgun AoE module, and the debug harness audit.
    /// </summary>
    public static class WeaponClassification
    {
        /// <summary>
        /// Rapid stance falloff threshold, derived from how steeply accuracy decays with range.
        ///
        /// Continuous, not a branch. The old binary form returned 12 or 5 from a single
        /// AccuracyTouch >= AccuracyMedium comparison, which put an assault rifle (60/70/65/55) on
        /// d0 = 5 and a charge rifle (55/64/55/45) on d0 = 12 purely because one comparison landed
        /// on equality. 64% of all ranged weapons ended up on the wide branch, LMGs included.
        ///
        ///   closeBias = clamp(0, 1, (touch - long) / max(touch, 0.01))
        ///   d0        = d0Base + closeBias * d0Span
        /// </summary>
        public static float CalculateD0(ThingDef weaponDef)
        {
            FireDisciplineSettings settings = FireDisciplineMod.Settings;
            float d0Base = settings?.d0Base ?? 4f;
            float d0Span = settings?.d0Span ?? 12f;

            if (weaponDef == null) return d0Base;

            float accTouch = weaponDef.GetStatValueAbstract(StatDefOf.AccuracyTouch);
            float accLong = weaponDef.GetStatValueAbstract(StatDefOf.AccuracyLong);

            float closeBias = Mathf.Clamp01((accTouch - accLong) / Mathf.Max(accTouch, 0.01f));
            return d0Base + closeBias * d0Span;
        }

        /// <summary>
        /// A shotgun is a weapon with a FLAT accuracy curve at short physical range: roughly as
        /// accurate at its maximum range as up close, because its limit is spread rather than
        /// precision. That is the opposite shape from the one that earns a wide d0, which is why
        /// the two must not share a predicate - the previous single test
        /// (AccuracyTouch >= AccuracyMedium AND range &lt;= 25) flagged 48 weapons of which roughly
        /// 35 were SMGs, pistols, bows, thrown weapons and turret guns.
        ///
        /// Five gates:
        ///   1. The curve must be a real curve. Identical accuracy at every band means "always
        ///      hits" ordnance - grenades, mortars, incinerators, foam - not a shotgun.
        ///   2. Physical range in the shotgun band. Below it are thrown weapons and flamers; above
        ///      it are carbines, SMGs and magnum pistols.
        ///   3. The curve must peak somewhere respectable, or it is just an inaccurate weapon.
        ///   4. Long / Short is the flatness measure. Shotguns keep half their short-range accuracy
        ///      at long range; SMG curves collapse to a fifth.
        ///   5. Touch >= Medium, the original test, kept as final confirmation not as the whole
        ///      judgement.
        /// </summary>
        public static bool HasShotgunProfile(ThingDef weaponDef)
        {
            if (!IsPawnRangedWeapon(weaponDef)) return false;

            FireDisciplineSettings settings = FireDisciplineMod.Settings;

            float t = weaponDef.GetStatValueAbstract(StatDefOf.AccuracyTouch);
            float s = weaponDef.GetStatValueAbstract(StatDefOf.AccuracyShort);
            float m = weaponDef.GetStatValueAbstract(StatDefOf.AccuracyMedium);
            float l = weaponDef.GetStatValueAbstract(StatDefOf.AccuracyLong);

            float peak = Mathf.Max(t, s);

            // 1. Flat line.
            if (t == s && s == m && m == l) return false;

            // 2. Short, but not a shove-it-in-their-face weapon.
            float range = GetWeaponRange(weaponDef);
            if (range > (settings?.shotgunMaxRange ?? 17f)) return false;
            if (range < (settings?.shotgunMinRange ?? 8f)) return false;

            // 3. Too inaccurate to be a shotgun.
            if (peak < (settings?.shotgunMinPeakAccuracy ?? 0.55f)) return false;

            // 4. Falling off a cliff: SMG, pistol, thrown weapon.
            if (s <= 0f) return false;
            if (l / s < (settings?.shotgunMinLongShortRatio ?? 0.50f)) return false;

            // 5. Must not favour long range over close.
            if (t < m) return false;

            return true;
        }

        /// <summary>
        /// Weapons that represent pawn tactics. Turret and artillery armaments are ThingDefs with
        /// accuracy stats like any other, so an unfiltered scan classifies a foam turret with a
        /// 41-shot burst as a shotgun.
        ///
        /// Deliberately NOT filtered on destroyOnDrop: that flag marks weapons welded to their
        /// carrier, which includes mech armament - and mechs are pawns that fight, so their weapons
        /// belong in the tactical layer. Filtering on it silently dropped two genuine shotguns.
        /// </summary>
        public static bool IsPawnRangedWeapon(ThingDef weaponDef)
        {
            return GetFilterReason(weaponDef) == null;
        }

        /// <summary>
        /// Human-readable reason a weapon is excluded, or null if it passes. Sharing one
        /// implementation with IsPawnRangedWeapon keeps the debug harness honest: a weapon can
        /// never be filtered for a reason the report cannot name.
        /// </summary>
        public static string GetFilterReason(ThingDef weaponDef)
        {
            if (weaponDef == null) return "null def";
            if (!weaponDef.IsWeaponUsingProjectiles) return "not a projectile weapon";

            // Mounted armaments are tagged rather than named, so this reads a data field rather
            // than pattern-matching a defName.
            if (weaponDef.weaponTags != null)
            {
                foreach (string tag in weaponDef.weaponTags)
                {
                    if (tag == null) continue;
                    if (tag.IndexOf("turret", StringComparison.OrdinalIgnoreCase) >= 0) return $"weaponTag '{tag}'";
                    if (tag.IndexOf("artillery", StringComparison.OrdinalIgnoreCase) >= 0) return $"weaponTag '{tag}'";
                }
            }

            float range = GetWeaponRange(weaponDef);
            float maxRange = FireDisciplineMod.Settings?.weaponFilterMaxRange ?? 100f;
            if (range > maxRange) return $"range {range:F1} > {maxRange:F0}";

            return null;
        }

        /// <summary>
        /// Seconds between the start of one burst and the start of the next.
        ///
        /// Cooldown comes from the RangedWeapon_Cooldown STAT, not from verbProps.defaultCooldownTime.
        /// The verbProps field is only a fallback and reads 0 on most weapons, which made a derringer
        /// look like a 10-rounds-per-second weapon in the suppression audit.
        /// </summary>
        public static float GetCycleSeconds(ThingDef weaponDef)
        {
            if (weaponDef?.Verbs == null || weaponDef.Verbs.Count == 0) return 0f;

            VerbProperties verb = weaponDef.Verbs[0];

            float cooldown = weaponDef.GetStatValueAbstract(StatDefOf.RangedWeapon_Cooldown);
            if (cooldown <= 0f) cooldown = verb.defaultCooldownTime;

            int burst = Mathf.Max(1, verb.burstShotCount);
            float withinBurst = (burst - 1) * verb.ticksBetweenBurstShots / 60f;

            return Mathf.Max(0.1f, verb.warmupTime + withinBurst + cooldown);
        }

        /// <summary>Seconds between the last round of a burst and the first of the next.</summary>
        public static float GetBurstGapSeconds(ThingDef weaponDef)
        {
            if (weaponDef?.Verbs == null || weaponDef.Verbs.Count == 0) return 0f;

            VerbProperties verb = weaponDef.Verbs[0];
            float cooldown = weaponDef.GetStatValueAbstract(StatDefOf.RangedWeapon_Cooldown);
            if (cooldown <= 0f) cooldown = verb.defaultCooldownTime;

            return verb.warmupTime + cooldown;
        }

        public static float GetWeaponRange(ThingDef weaponDef)
        {
            if (weaponDef?.Verbs == null || weaponDef.Verbs.Count == 0) return 0f;
            return weaponDef.Verbs[0].range;
        }
    }
}
