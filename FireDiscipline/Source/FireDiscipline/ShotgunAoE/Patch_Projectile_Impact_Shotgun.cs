using FireDiscipline.Core;
using FireDiscipline.Graze;
using FireDiscipline.Suppression;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace FireDiscipline.ShotgunAoE
{
    /// <summary>
    /// Harmony Postfix on Projectile.Impact - shotgun spread only (design 5.5).
    ///
    ///   radius        fixed per weapon, NOT scaled by skill
    ///   edge          e = lerp(edgeMin, edgeMax, shootingSkill / 20)
    ///   damage(d)     lerp(1.0, e, d / radius) * primary * primaryMultiplier
    ///
    /// Skill controls the EDGE, not the radius. If both scaled together the effect would grow
    /// quadratically with skill; a fixed radius is also a number the player can learn and position
    /// around.
    ///
    /// Splash never grazes and never hits vital organs: a splash victim had no hit roll, so there
    /// is no hit chance to derive a graze probability from.
    /// </summary>
    public static class Patch_Projectile_Impact_Shotgun
    {
        /// <summary>
        /// PREFIX, not postfix: Projectile.Impact destroys the projectile before it returns, so a
        /// postfix sees a despawned thing with a null Map. Void prefix, never blocks (rule 5).
        /// </summary>
        public static void Prefix(Projectile __instance, Thing hitThing)
        {
            if (!PatchRegistry.IsModuleEnabled(ShotgunAoEModule.Id)) return;

            if (__instance?.def?.projectile == null || __instance.def.projectile.flyOverhead) return;

            Map map = __instance.Map ?? hitThing?.Map;
            if (map == null) return;

            Pawn shooter = __instance.Launcher as Pawn;
            ThingDef weaponDef = shooter?.equipment?.Primary?.def;
            if (weaponDef == null) return;

            if (!IsShotgunShot(weaponDef, __instance)) return;

            FireDisciplineSettings settings = FireDisciplineMod.Settings;
            IntVec3 impactCell = hitThing?.Position ?? __instance.Position;

            // A wedge needs an origin and a direction. Without a pawn shooter there is neither.
            if (shooter == null || !shooter.Spawned) return;

            IntVec3 origin = shooter.Position;
            if (!ShotgunSpreadGeometry.TryResolve(origin, impactCell, weaponDef, out Vector3 direction,
                    out float length, out float spreadPerCell))
            {
                return;
            }

            int skill = shooter.skills?.GetSkill(SkillDefOf.Shooting)?.Level ?? 10;
            float edge = Mathf.Lerp(
                settings?.shotgunEdgeDamageMin ?? 0.15f,
                settings?.shotgunEdgeDamageMax ?? 0.55f,
                Mathf.Clamp01(skill / 20f));

            float primaryDamage = __instance.def.projectile.GetDamageAmount(null)
                * (settings?.shotgunPrimaryDamageMultiplier ?? 0.70f);

            bool friendlyFire = settings?.shotgunFriendlyFire ?? true;

            float maxDistSq = (length + 1f) * (length + 1f);

            // Scan all pawns, but rough-filter by distance before doing vector geometry.
            // A map has far fewer pawns than the ~900 cells a 17-radius radial scan would touch.
            foreach (Pawn victim in map.mapPawns.AllPawnsSpawned)
            {
                if (victim.Dead || !victim.RaceProps.Humanlike) continue;
                if (victim == hitThing) continue;

                // Never the shooter, whatever the friendly-fire setting says. Spread travels
                // forward from the muzzle; a pawn cannot shoot themselves.
                if (victim == shooter) continue;

                if (!friendlyFire && victim.Faction == shooter.Faction) continue;
                
                if (victim.Position.DistanceToSquared(origin) > maxDistSq) continue;

                // Same geometry the danger overlay draws - one implementation, so the picture
                // the player positions around cannot drift from the shape that actually hits.
                if (!ShotgunSpreadGeometry.Contains(origin, victim.Position, direction, length,
                        spreadPerCell, out float edgeFraction, out float densityFactor))
                {
                    continue;
                }

                // Damage falls off both laterally (pellets thin out sideways) and longitudinally
                // (pellets spread out over distance, reducing density).
                float dmgFactor = Mathf.Lerp(1.0f, edge, edgeFraction) * densityFactor;
                float splashDamage = Mathf.Max(1f, primaryDamage * dmgFactor);

                BodyPartRecord outerLimb = Patch_DamageWorker_AddInjury.FindOuterLimb(victim)
                    ?? victim.RaceProps.body.corePart;

                victim.TakeDamage(new DamageInfo(
                    DamageDefOf.Bullet,
                    splashDamage,
                    __instance.def.projectile.GetArmorPenetration(null),
                    -1f,
                    shooter,
                    outerLimb,
                    weaponDef));

                // Splash suppresses at a reduced rate. Without the reduction a shotgun would be the
                // strongest suppression tool in the game by a wide margin.
                float splashSuppression = SuppressionEngine.CalculateSuppressionAmount(shooter, victim)
                    * (settings?.shotgunSplashSuppressionMultiplier ?? 0.40f);
                SuppressionEngine.ApplySuppression(victim, splashSuppression);
            }
        }

        /// <summary>
        /// Weapon-shape test plus the explosive exclusion, which needs the live projectile.
        /// </summary>
        public static bool IsShotgunShot(ThingDef weaponDef, Projectile projectile)
        {
            if (projectile?.def?.projectile == null) return false;
            if (projectile.def.projectile.explosionRadius > 0.5f) return false;

            return WeaponClassification.HasShotgunProfile(weaponDef);
        }
    }
}
