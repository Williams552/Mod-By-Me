using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace FireDiscipline.Shock
{
    /// <summary>
    /// Harmony Postfix on Pawn.Kill and Pawn_HealthTracker.MakeDowned.
    /// Triggers Combat Shock (FD_CombatShock) on nearby allies within shock radius when a pawn is downed or killed.
    /// Prevents double-triggering if a pawn is downed and killed in rapid succession.
    /// </summary>
    public static class Patch_Pawn_Kill_Down
    {
        /// <summary>
        /// Cooldown duration in ticks (120 ticks = 2 seconds) to prevent double-triggering Combat Shock when a pawn is downed and killed in rapid succession.
        /// </summary>
        public const int DoubleTriggerCooldownTicks = 120;

        private static readonly Dictionary<int, int> lastShockTicks = new Dictionary<int, int>();

        public static void ClearCache()
        {
            lastShockTicks.Clear();
        }

        public static void CleanupStaleEntries()
        {
            if (lastShockTicks.Count == 0) return;

            int currentTick = Find.TickManager?.TicksGame ?? 0;
            List<int> expiredKeys = null;

            foreach (var kvp in lastShockTicks)
            {
                if (currentTick - kvp.Value > DoubleTriggerCooldownTicks)
                {
                    if (expiredKeys == null) expiredKeys = new List<int>();
                    expiredKeys.Add(kvp.Key);
                }
            }

            if (expiredKeys != null)
            {
                for (int i = 0; i < expiredKeys.Count; i++)
                {
                    lastShockTicks.Remove(expiredKeys[i]);
                }
            }
        }

        public static void Postfix_Kill(Pawn __instance)
        {
            if (!Core.PatchRegistry.IsModuleEnabled(ShockModule.Id))
                return;

            TriggerCombatShock(__instance, "Ally Killed!");
        }

        public static void Postfix_Downed(Pawn_HealthTracker __instance, Pawn ___pawn)
        {
            if (!Core.PatchRegistry.IsModuleEnabled(ShockModule.Id))
                return;

            TriggerCombatShock(___pawn, "Ally Downed!");
        }

        private static void TriggerCombatShock(Pawn victim, string moteText)
        {
            if (victim == null || victim.Map == null || !victim.RaceProps.Humanlike) return;

            int currentTick = Find.TickManager?.TicksGame ?? 0;
            if (lastShockTicks.TryGetValue(victim.thingIDNumber, out int lastTick) && (currentTick - lastTick) < DoubleTriggerCooldownTicks)
            {
                return; // Prevent double-triggering when pawn is downed and killed within 2 seconds
            }
            lastShockTicks[victim.thingIDNumber] = currentTick;

            Map map = victim.Map;
            IntVec3 pos = victim.Position;
            Faction faction = victim.Faction;
            if (faction == null) return;

            HediffDef shockDef = DefDatabase<HediffDef>.GetNamedSilentFail("FD_CombatShock");
            if (shockDef == null) return;

            float radius = FireDisciplineMod.Settings?.allyShockRadius ?? 6.0f;
            float initialSev = shockDef.initialSeverity;
            float minSev = shockDef.minSeverity;
            float maxSev = shockDef.maxSeverity;

            foreach (Thing thing in GenRadial.RadialDistinctThingsAround(pos, map, radius, true))
            {
                if (thing is Pawn ally && !ally.Dead && ally != victim && ally.Faction == faction && ally.RaceProps.Humanlike)
                {
                    Hediff hediff = ally.health.hediffSet.GetFirstHediffOfDef(shockDef);
                    if (hediff != null)
                    {
                        hediff.Severity = Mathf.Clamp(hediff.Severity + initialSev, minSev, maxSev);
                    }
                    else
                    {
                        hediff = HediffMaker.MakeHediff(shockDef, ally);
                        hediff.Severity = initialSev;
                        ally.health.AddHediff(hediff);
                    }

                    if (map != null && Find.CameraDriver != null)
                    {
                        MoteMaker.ThrowText(ally.DrawPos, map, moteText, Color.red);
                    }
                }
            }
        }
    }
}
