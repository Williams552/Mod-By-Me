using RimWorld;
using Verse;

namespace FireDiscipline.Suppression
{
    public static class SuppressionStatDefOf
    {
        private static StatDef resistanceCache;
        private static StatDef recoverySpeedCache;
        private static bool queriedResistance;
        private static bool queriedRecoverySpeed;

        public static StatDef SuppressionResistance
        {
            get
            {
                if (!queriedResistance)
                {
                    resistanceCache = DefDatabase<StatDef>.GetNamedSilentFail("FD_SuppressionResistance");
                    queriedResistance = true;
                }
                return resistanceCache;
            }
        }

        public static StatDef SuppressionRecoverySpeed
        {
            get
            {
                if (!queriedRecoverySpeed)
                {
                    recoverySpeedCache = DefDatabase<StatDef>.GetNamedSilentFail("FD_SuppressionRecoverySpeed");
                    queriedRecoverySpeed = true;
                }
                return recoverySpeedCache;
            }
        }

        public static void ResetCache()
        {
            resistanceCache = null;
            recoverySpeedCache = null;
            queriedResistance = false;
            queriedRecoverySpeed = false;
        }
    }
}
