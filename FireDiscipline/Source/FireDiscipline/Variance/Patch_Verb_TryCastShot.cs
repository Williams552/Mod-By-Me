using System;
using HarmonyLib;
using Verse;

namespace FireDiscipline.Variance
{
    /// <summary>
    /// Harmony Prefix & Finalizer on Verb_LaunchProjectile.TryCastShot (Wave B8).
    /// Sets HitVarianceState.InRealShot = true during real shot execution.
    ///
    /// Rule 5 Compliance: Prefix returns void (does NOT suppress or replace execution).
    /// AC2 Compliance: Uses Harmony Finalizer to guarantee InRealShot is reset to false
    /// even if an unhandled exception occurs during shot processing.
    /// </summary>
    public static class Patch_Verb_TryCastShot
    {
        public static void Prefix()
        {
            if (FireDisciplineMod.Settings == null || !FireDisciplineMod.Settings.enableHitVariance)
                return;

            HitVarianceState.InRealShot = true;
        }

        public static Exception Finalizer(Exception __exception)
        {
            HitVarianceState.InRealShot = false;
            return __exception;
        }
    }
}
