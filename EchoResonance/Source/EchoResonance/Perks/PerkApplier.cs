using Verse;

namespace EchoResonance.Perks
{
    public static class PerkApplier
    {
        public static void ApplyPerkToPawn(Pawn pawn, PerkDef perk)
        {
            if (pawn == null || perk == null) return;

            if (perk.hediffDef != null)
            {
                // Ensure we don't add duplicate hediff
                if (!pawn.health.hediffSet.HasHediff(perk.hediffDef))
                {
                    Hediff hediff = HediffMaker.MakeHediff(perk.hediffDef, pawn);
                    pawn.health.AddHediff(hediff);
                }
            }
        }

        public static void RemovePerkFromPawn(Pawn pawn, PerkDef perk)
        {
            if (pawn == null || perk == null) return;

            if (perk.hediffDef != null)
            {
                Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(perk.hediffDef);
                if (hediff != null)
                {
                    pawn.health.RemoveHediff(hediff);
                }
            }
        }
    }
}
