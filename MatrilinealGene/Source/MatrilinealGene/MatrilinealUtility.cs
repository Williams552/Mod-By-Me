using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MatrilinealGene
{
    public static class MatrilinealUtility
    {
        [ThreadStatic]
        public static bool GeneratingMatrilinealBirth;

        [ThreadStatic]
        public static Pawn CurrentGeneticMother;

        [ThreadStatic]
        public static Pawn CurrentFather;

        public static bool HasMatrilinealGene(Pawn pawn)
        {
            if (pawn?.genes == null) return false;
            return MatrilinealGeneDefOf.Gene_MatrilinealBirth != null &&
                   pawn.genes.HasActiveGene(MatrilinealGeneDefOf.Gene_MatrilinealBirth);
        }

        public static bool HasMatrilinealGene(List<GeneDef> genes)
        {
            if (genes == null) return false;
            return MatrilinealGeneDefOf.Gene_MatrilinealBirth != null &&
                   genes.Contains(MatrilinealGeneDefOf.Gene_MatrilinealBirth);
        }

        public static bool IsMatrilinealActive(Pawn mother, Pawn father, List<GeneDef> genes = null)
        {
            if (HasMatrilinealGene(mother)) return true;
            if (HasMatrilinealGene(father)) return true;
            if (HasMatrilinealGene(genes)) return true;
            return false;
        }

        public static List<GeneDef> GetMatrilinealInheritedGenes(Pawn father, Pawn mother, out bool success)
        {
            List<GeneDef> inherited = new List<GeneDef>();

            if (mother?.genes == null)
            {
                // Fallback to father or empty if no mother
                if (father?.genes != null)
                {
                    foreach (Gene gene in father.genes.Endogenes)
                    {
                        if (gene.def.endogeneCategory != EndogeneCategory.Melanin && gene.def.biostatArc <= 0)
                        {
                            inherited.Add(gene.def);
                        }
                    }
                }
                if (MatrilinealGeneDefOf.Gene_MatrilinealBirth != null && !inherited.Contains(MatrilinealGeneDefOf.Gene_MatrilinealBirth))
                {
                    inherited.Add(MatrilinealGeneDefOf.Gene_MatrilinealBirth);
                }
                success = true;
                return inherited;
            }

            // 1. Inherit all Endogenes of the genetic mother
            foreach (Gene endogene in mother.genes.Endogenes)
            {
                if (endogene.def.endogeneCategory != EndogeneCategory.Melanin && endogene.def.biostatArc <= 0)
                {
                    if (!inherited.Contains(endogene.def))
                    {
                        inherited.Add(endogene.def);
                    }
                }
            }

            // 2. If settings allow and mother's xenotype has defined genes (e.g. Highmate, Sanguophage, custom xenotype), ensure daughter inherits them
            if (MatrilinealGeneMod.Settings.inheritMotherXenotype)
            {
                if (mother.genes.Xenotype != null && mother.genes.Xenotype != XenotypeDefOf.Baseliner && mother.genes.Xenotype.genes != null)
                {
                    foreach (GeneDef geneDef in mother.genes.Xenotype.genes)
                    {
                        if (geneDef.endogeneCategory != EndogeneCategory.Melanin && geneDef.biostatArc <= 0 && !inherited.Contains(geneDef))
                        {
                            inherited.Add(geneDef);
                        }
                    }
                }
                else if (mother.genes.CustomXenotype != null && mother.genes.CustomXenotype.genes != null)
                {
                    foreach (GeneDef geneDef in mother.genes.CustomXenotype.genes)
                    {
                        if (geneDef.endogeneCategory != EndogeneCategory.Melanin && geneDef.biostatArc <= 0 && !inherited.Contains(geneDef))
                        {
                            inherited.Add(geneDef);
                        }
                    }
                }
            }

            // 3. Ensure the Matrilineal Gene itself is passed down to the daughter
            if (MatrilinealGeneDefOf.Gene_MatrilinealBirth != null && !inherited.Contains(MatrilinealGeneDefOf.Gene_MatrilinealBirth))
            {
                inherited.Add(MatrilinealGeneDefOf.Gene_MatrilinealBirth);
            }

            // 4. Resolve prerequisites (clean up any missing prereqs)
            inherited.RemoveAll(x => x.prerequisite != null && !inherited.Contains(x.prerequisite));

            // 5. Skin color inheritance directly from mother
            GeneDef motherMelanin = mother.genes.GetMelaninGene();
            if (motherMelanin != null)
            {
                inherited.Add(motherMelanin);
            }
            else if (PawnSkinColors.SkinColorsFromParents(father, mother).TryRandomElement(out var skinColorGene))
            {
                inherited.Add(skinColorGene);
            }

            // 6. Hair color inheritance directly from mother
            GeneDef motherHair = mother.genes.GetFirstEndogeneByCategory(EndogeneCategory.HairColor);
            if (motherHair != null)
            {
                inherited.Add(motherHair);
            }
            else if (DefDatabase<GeneDef>.AllDefs.Where(x => x.endogeneCategory == EndogeneCategory.HairColor).TryRandomElementByWeight(x => x.selectionWeight, out var randomHairGene))
            {
                inherited.Add(randomHairGene);
            }

            success = true;
            return inherited;
        }

        public static void ApplyMatrilinealPostBirth(Pawn child, Pawn geneticMother, Pawn father)
        {
            if (child == null) return;

            // Enforce female gender
            if (MatrilinealGeneMod.Settings.forceAllFemale && child.RaceProps.hasGenders)
            {
                child.gender = Gender.Female;
            }

            // Enforce xenotype inheritance from genetic mother
            if (MatrilinealGeneMod.Settings.inheritMotherXenotype && geneticMother?.genes != null && child.genes != null)
            {
                // Custom xenotype name & icon
                if (!geneticMother.genes.xenotypeName.NullOrEmpty())
                {
                    child.genes.xenotypeName = geneticMother.genes.xenotypeName;
                    child.genes.iconDef = geneticMother.genes.iconDef;
                }
                else if (geneticMother.genes.iconDef != null)
                {
                    child.genes.iconDef = geneticMother.genes.iconDef;
                }

                // XenotypeDef
                if (geneticMother.genes.Xenotype != null)
                {
                    child.genes.SetXenotypeDirect(geneticMother.genes.Xenotype);
                }

                // Hybrid status matches mother
                child.genes.hybrid = geneticMother.genes.hybrid;
            }
        }
    }
}
