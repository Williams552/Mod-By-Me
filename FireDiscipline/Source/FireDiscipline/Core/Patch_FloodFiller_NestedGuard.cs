using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace FireDiscipline.Core
{
    /// <summary>
    /// Harmony Prefix on Verse.FloodFiller.FloodFill.
    ///
    /// RimWorld's FloodFiller uses a single map-wide instance and logs:
    /// "Nested FloodFill calls are not allowed. This will cause bugs." when FloodFill is called while already working.
    ///
    /// This occurs when projectile impact, damage, or suppression triggers AI job determination / melee target search
    /// (e.g. AttackTargetFinder.FindBestReachableMeleeTarget) synchronously inside an outer FloodFill call.
    ///
    /// This guard intercepts nested calls, executes them on a standalone FloodFiller instance for that map,
    /// and returns cleanly without logging errors or corrupting the outer FloodFiller state.
    /// </summary>
    public static class Patch_FloodFiller_NestedGuard
    {
        private static readonly FieldInfo workingField = AccessTools.Field(typeof(FloodFiller), "working");
        private static readonly FieldInfo mapField = AccessTools.Field(typeof(FloodFiller), "map");

        public static bool Prefix(
            FloodFiller __instance,
            IntVec3 root,
            Predicate<IntVec3> passCheck,
            Func<IntVec3, int, bool> processor,
            // Parameter names must match Verse.FloodFiller.FloodFill exactly - Harmony binds prefix
            // arguments by name, and a mismatch makes the whole patch fail to apply at startup.
            int maxCellsToProcess,
            bool rememberParents,
            IEnumerable<IntVec3> extraRoots)
        {
            if (workingField == null || mapField == null || __instance == null) return true;

            bool isWorking = (bool)workingField.GetValue(__instance);
            if (!isWorking)
            {
                // Primary/outer call: let vanilla FloodFill handle it.
                return true;
            }

            // Nested call detected! Retrieve map and instantiate a standalone FloodFiller to process it safely.
            Map map = (Map)mapField.GetValue(__instance);
            if (map == null) return true;

            try
            {
                FloodFiller standaloneFiller = new FloodFiller(map);
                standaloneFiller.FloodFill(root, passCheck, processor, maxCellsToProcess, rememberParents, extraRoots);
            }
            catch (Exception ex)
            {
                Log.Error($"[Fire Discipline] Exception executing nested FloodFill fallback: {ex}");
            }

            // Skip original FloodFill execution on __instance to prevent error log and state corruption.
            return false;
        }

        public static void ApplyPatch(Harmony harmony)
        {
            if (harmony == null) return;

            try
            {
                var targetMethod = AccessTools.Method(
                    typeof(FloodFiller),
                    nameof(FloodFiller.FloodFill),
                    new Type[]
                    {
                        typeof(IntVec3),
                        typeof(Predicate<IntVec3>),
                        typeof(Func<IntVec3, int, bool>),
                        typeof(int),
                        typeof(bool),
                        typeof(IEnumerable<IntVec3>)
                    });

                if (targetMethod != null)
                {
                    var prefix = typeof(Patch_FloodFiller_NestedGuard).GetMethod(
                        nameof(Prefix), BindingFlags.Public | BindingFlags.Static);
                    harmony.Patch(targetMethod, prefix: new HarmonyMethod(prefix));
                    Log.Message("[Fire Discipline] Patched Verse.FloodFiller.FloodFill to guard against nested calls.");
                }
                else
                {
                    Log.Error("[Fire Discipline] Failed to locate Verse.FloodFiller.FloodFill target method.");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[Fire Discipline] Failed to apply Patch_FloodFiller_NestedGuard: {ex}");
            }
        }
    }
}
