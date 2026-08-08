using FireDiscipline.Core;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FireDiscipline.Suppression
{
    /// <summary>
    /// Harmony Postfix on Projectile.Impact - suppression only.
    ///
    /// This file used to also carry the shotgun spread AoE. The two were registered as one block,
    /// which meant enabling suppression would have dragged an unreviewed Wave B feature into v1.0
    /// along with an undecided friendly-fire rule. They are now separate modules with separate
    /// patches and separate toggles.
    /// </summary>
    public static class Patch_Projectile_Impact
    {
        /// <summary>
        /// PREFIX, not postfix. Projectile.Impact ends by destroying the projectile, so by the time
        /// a postfix runs the thing is despawned: Map is null and Position is stale. The engine read
        /// as "working" in isolation while contributing nothing during actual gunfire, because it
        /// returned on the null map check for every single round fired.
        ///
        /// This is a void prefix - it never returns false and never blocks the original method
        /// (architecture rule 5).
        /// </summary>
        public static void Prefix(Projectile __instance, Thing hitThing)
        {
            if (!PatchRegistry.IsModuleEnabled(SuppressionCoreModule.Id)) return;

            if (__instance?.def?.projectile == null) return;

            // Overhead shells are handled by the explosion path, not by near-miss suppression.
            if (__instance.def.projectile.flyOverhead) return;

            Map map = __instance.Map ?? hitThing?.Map;
            if (map == null)
            {
                if (SuppressionEngine.LogEvents)
                {
                    Log.Message("[FD suppress] impact skipped: no map on projectile or hit thing.");
                }
                return;
            }

            Pawn shooter = __instance.Launcher as Pawn;
            IntVec3 impactCell = hitThing?.Position ?? __instance.Position;
            float radius = FireDisciplineMod.Settings?.suppressionRadius ?? 3.5f;

            if (SuppressionEngine.LogEvents)
            {
                Log.Message($"[FD suppress] impact at {impactCell} r={radius:F1} "
                    + $"shooter={(shooter != null ? shooter.LabelShort : "none")} "
                    + $"hitThing={(hitThing != null ? hitThing.LabelShort : "none")}");
            }

            foreach (Thing thing in GenRadial.RadialDistinctThingsAround(impactCell, map, radius, true))
            {
                if (!(thing is Pawn victim)) continue;
                if (victim.Dead || !victim.RaceProps.Humanlike) continue;

                // Friendly pawns near the impact are not suppressed by their own side's fire,
                // unless the round actually hit them.
                if (shooter != null && victim.Faction == shooter.Faction && victim != hitThing) continue;

                SuppressionEngine.SuppressPawn(shooter, victim);
            }
        }
    }
}
