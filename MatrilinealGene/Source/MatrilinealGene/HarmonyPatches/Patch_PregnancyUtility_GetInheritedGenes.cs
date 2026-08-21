using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MatrilinealGene.HarmonyPatches
{
    [HarmonyPatch(typeof(PregnancyUtility), nameof(PregnancyUtility.GetInheritedGenes), new[] { typeof(Pawn), typeof(Pawn), typeof(bool) }, new[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out })]
    public static class Patch_PregnancyUtility_GetInheritedGenes
    {
        [HarmonyPrefix]
        public static bool Prefix(Pawn father, Pawn mother, out bool success, ref List<GeneDef> __result)
        {
            if (MatrilinealUtility.IsMatrilinealActive(mother, father) && MatrilinealGeneMod.Settings.inheritMotherXenotype)
            {
                __result = MatrilinealUtility.GetMatrilinealInheritedGenes(father, mother, out success);
                return false;
            }

            success = true;
            return true;
        }
    }
}
