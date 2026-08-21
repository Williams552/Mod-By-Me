using HarmonyLib;
using RimWorld;
using Verse;

namespace MatrilinealGene.HarmonyPatches
{
    [HarmonyPatch(typeof(PregnancyUtility), "ShouldByHybrid")]
    public static class Patch_PregnancyUtility_ShouldByHybrid
    {
        [HarmonyPrefix]
        public static bool Prefix(Pawn mother, Pawn father, ref bool __result)
        {
            if (MatrilinealUtility.IsMatrilinealActive(mother, father) && MatrilinealGeneMod.Settings.inheritMotherXenotype)
            {
                __result = mother?.genes?.hybrid == true;
                return false;
            }

            return true;
        }
    }
}
