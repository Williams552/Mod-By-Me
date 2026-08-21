using HarmonyLib;
using RimWorld;
using Verse;

namespace MatrilinealGene.HarmonyPatches
{
    [HarmonyPatch(typeof(PregnancyUtility), "TryGetInheritedXenotype")]
    public static class Patch_PregnancyUtility_TryGetInheritedXenotype
    {
        [HarmonyPrefix]
        public static bool Prefix(Pawn mother, Pawn father, ref XenotypeDef xenotype, ref bool __result)
        {
            if (MatrilinealUtility.IsMatrilinealActive(mother, father) && MatrilinealGeneMod.Settings.inheritMotherXenotype)
            {
                if (mother?.genes?.Xenotype != null)
                {
                    xenotype = mother.genes.Xenotype;
                    __result = true;
                    return false;
                }
            }

            return true;
        }
    }
}
