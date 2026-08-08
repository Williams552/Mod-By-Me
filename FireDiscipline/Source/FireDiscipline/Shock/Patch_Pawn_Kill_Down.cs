using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace FireDiscipline.Shock
{
    /// <summary>
    /// Harmony Postfix on Pawn.Kill.
    /// Triggers Combat Shock (FD_CombatShock) on nearby allies within 6.0 cells when a pawn is killed.
    /// </summary>
    public static class Patch_Pawn_Kill_Down
    {
        public static void Postfix_Kill(Pawn __instance)
        {
            if (!Core.PatchRegistry.IsModuleEnabled(ShockModule.Id))
                return;

            if (__instance == null || __instance.Map == null || !__instance.RaceProps.Humanlike)
                return;

            Map map = __instance.Map;
            IntVec3 pos = __instance.Position;
            Faction faction = __instance.Faction;

            if (faction == null) return;

            HediffDef shockDef = DefDatabase<HediffDef>.GetNamedSilentFail("FD_CombatShock");
            if (shockDef == null) return;

            float radius = FireDisciplineMod.Settings?.allyShockRadius ?? 6.0f;

            foreach (Thing thing in GenRadial.RadialDistinctThingsAround(pos, map, radius, true))
            {
                if (thing is Pawn ally && !ally.Dead && ally != __instance && ally.Faction == faction && ally.RaceProps.Humanlike)
                {
                    Hediff hediff = ally.health.hediffSet.GetFirstHediffOfDef(shockDef);
                    if (hediff != null)
                    {
                        hediff.Severity = Mathf.Clamp(hediff.Severity + 0.35f, 0.1f, 1.0f);
                    }
                    else
                    {
                        hediff = HediffMaker.MakeHediff(shockDef, ally);
                        hediff.Severity = 0.35f;
                        ally.health.AddHediff(hediff);
                    }

                    if (map != null && Find.CameraDriver != null)
                    {
                        MoteMaker.ThrowText(ally.DrawPos, map, "Ally Downed!", Color.red);
                    }
                }
            }
        }
    }
}
