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
                Hediff hediff = HediffMaker.MakeHediff(perk.hediffDef, pawn);
                pawn.health.AddHediff(hediff);
            }
        }
    }
}
