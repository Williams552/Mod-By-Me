using RimWorld;
using Verse;

namespace MatrilinealGene
{
    [DefOf]
    public static class MatrilinealGeneDefOf
    {
        [MayRequireBiotech]
        public static GeneDef Gene_MatrilinealBirth;

        static MatrilinealGeneDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(MatrilinealGeneDefOf));
        }
    }
}
