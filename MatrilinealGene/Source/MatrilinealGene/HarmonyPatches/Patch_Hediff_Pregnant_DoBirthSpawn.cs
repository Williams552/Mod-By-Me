using HarmonyLib;
using Verse;

namespace MatrilinealGene.HarmonyPatches
{
    [HarmonyPatch(typeof(Hediff_Pregnant), nameof(Hediff_Pregnant.DoBirthSpawn))]
    public static class Patch_Hediff_Pregnant_DoBirthSpawn
    {
        [HarmonyPrefix]
        public static void Prefix(Pawn mother, Pawn father)
        {
            if (MatrilinealUtility.IsMatrilinealActive(mother, father))
            {
                MatrilinealUtility.GeneratingMatrilinealBirth = true;
                MatrilinealUtility.CurrentGeneticMother = mother;
                MatrilinealUtility.CurrentFather = father;
            }
        }

        [HarmonyPostfix]
        public static void Postfix(Pawn mother, Pawn father)
        {
            MatrilinealUtility.GeneratingMatrilinealBirth = false;
            MatrilinealUtility.CurrentGeneticMother = null;
            MatrilinealUtility.CurrentFather = null;
        }
    }
}
