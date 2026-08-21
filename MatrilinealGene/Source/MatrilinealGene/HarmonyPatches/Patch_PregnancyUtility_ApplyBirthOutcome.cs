using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MatrilinealGene.HarmonyPatches
{
    [HarmonyPatch(typeof(PregnancyUtility), nameof(PregnancyUtility.ApplyBirthOutcome))]
    public static class Patch_PregnancyUtility_ApplyBirthOutcome
    {
        [HarmonyPrefix]
        public static void Prefix(List<GeneDef> genes, Pawn geneticMother, Thing birtherThing, Pawn father)
        {
            if (MatrilinealUtility.IsMatrilinealActive(geneticMother, father, genes))
            {
                MatrilinealUtility.GeneratingMatrilinealBirth = true;
                MatrilinealUtility.CurrentGeneticMother = geneticMother;
                MatrilinealUtility.CurrentFather = father;
            }
        }

        [HarmonyPostfix]
        public static void Postfix(Thing __result, List<GeneDef> genes, Pawn geneticMother, Thing birtherThing, Pawn father)
        {
            if (MatrilinealUtility.GeneratingMatrilinealBirth)
            {
                Pawn child = (__result is Corpse corpse) ? corpse.InnerPawn : (__result as Pawn);
                if (child != null)
                {
                    MatrilinealUtility.ApplyMatrilinealPostBirth(child, geneticMother, father);

                    if (MatrilinealGeneMod.Settings.enableBirthNotification &&
                        (child.Faction == Faction.OfPlayer || geneticMother?.Faction == Faction.OfPlayer || (birtherThing as Pawn)?.Faction == Faction.OfPlayer))
                    {
                        string motherLabel = geneticMother != null ? geneticMother.LabelShort : (birtherThing as Pawn)?.LabelShort ?? "Unknown";
                        Messages.Message(
                            "Matrilineal_Message_DaughterBorn".Translate(child.LabelShort.Named("BABY"), motherLabel.Named("MOTHER")),
                            child,
                            MessageTypeDefOf.PositiveEvent
                        );
                    }
                }
            }

            // Always cleanup thread-static state
            MatrilinealUtility.GeneratingMatrilinealBirth = false;
            MatrilinealUtility.CurrentGeneticMother = null;
            MatrilinealUtility.CurrentFather = null;
        }
    }
}
