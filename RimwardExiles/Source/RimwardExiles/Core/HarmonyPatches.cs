using HarmonyLib;
using RimWorld;
using Verse;

namespace RimwardExiles.Core
{
    [HarmonyPatch(typeof(IncidentWorker), nameof(IncidentWorker.TryExecute))]
    public static class Patch_IncidentWorker
    {
        public static void Postfix(IncidentWorker __instance, bool __result, IncidentParms parms)
        {
            if (!__result || __instance.def == null) return;
            ReactionResolver.HandleIncident(__instance.def.defName, parms);
        }
    }

    [HarmonyPatch(typeof(Recipe_InstallArtificialBodyPart), nameof(Recipe_InstallArtificialBodyPart.ApplyOnPawn))]
    public static class Patch_Recipe_InstallArtificialBodyPart
    {
        public static void Postfix(Pawn pawn, BodyPartRecord part, Pawn billDoer)
        {
            if (pawn == null) return;
            ReactionResolver.HandleAction("BionicInstall_Performed", billDoer, pawn);
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class Patch_PawnKill
    {
        public static void Postfix(Pawn __instance, DamageInfo? dinfo)
        {
            if (__instance == null || __instance.Faction != Faction.OfPlayer || !__instance.RaceProps.Humanlike)
                return;

            // Kích hoạt phanh an toàn P2: 5 ngày miễn nhiễm rời đi sau thảm hoạ
            var heroes = GameComponent_Exiles.Instance?.AllHeroes;
            if (heroes != null)
            {
                for (int i = 0; i < heroes.Count; i++)
                {
                    heroes[i].TriggerDisasterImmunity(300000);
                }
            }

            Pawn instigator = dinfo.HasValue ? dinfo.Value.Instigator as Pawn : null;
            ReactionResolver.HandleAction("ColonistDied", instigator, __instance);
        }
    }
}
