using HarmonyLib;
using Verse;

namespace MatrilinealGene.HarmonyPatches
{
    [HarmonyPatch(typeof(PawnGenerator), "GenerateNewPawnInternal")]
    public static class Patch_PawnGenerator_GenerateNewPawnInternal
    {
        [HarmonyPrefix]
        public static void Prefix(ref PawnGenerationRequest request)
        {
            if (MatrilinealUtility.GeneratingMatrilinealBirth && MatrilinealGeneMod.Settings.forceAllFemale)
            {
                request.FixedGender = Gender.Female;
            }
        }
    }
}
