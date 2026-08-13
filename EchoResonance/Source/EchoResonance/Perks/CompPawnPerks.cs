using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;
using EchoResonance.Core;
using EchoResonance.UI;

namespace EchoResonance.Perks
{
    public class CompPawnPerks : ThingComp
    {
        private List<PerkDef> activePerks = new List<PerkDef>();

        public IReadOnlyList<PerkDef> ActivePerks => activePerks;

        public Pawn PawnOwner => parent as Pawn;

        public bool HasPerk(PerkDef perk)
        {
            return activePerks.Contains(perk);
        }

        public float CalculatePerkCost(PerkDef perk)
        {
            int n = activePerks.Count + 1;
            float exponent = EchoResonanceMod.Settings?.costMultiplierExponent ?? EchoTuning.EscalatingExponent;
            float basePrice = perk.baseCost;

            // Price = Base * (Exponent ^ (N - 1))
            float cost = basePrice * Mathf.Pow(exponent, n - 1);

            // Specialization discount: -25% (*0.75) if pawn already has a perk from the same branch
            bool sameBranchDiscount = activePerks.Any(p => p.branch == perk.branch);
            if (sameBranchDiscount)
            {
                float discount = EchoResonanceMod.Settings?.specializationDiscount ?? EchoTuning.SpecializationDiscountFactor;
                cost *= discount;
            }

            // Trade-off discount: -40% (*0.60)
            if (perk.isTradeOff)
            {
                cost *= EchoTuning.TradeOffDiscountFactor;
            }

            return Mathf.Max(1f, Mathf.Round(cost));
        }

        public bool TryUnlockPerk(PerkDef perk)
        {
            if (HasPerk(perk)) return false;

            float cost = CalculatePerkCost(perk);
            if (EchoWorldComponent.Instance.TrySpendEcho(cost))
            {
                activePerks.Add(perk);
                PerkApplier.ApplyPerkToPawn(PawnOwner, perk);
                Messages.Message($"[Echo Resonance] {PawnOwner.LabelShort} unlocked perk '{perk.label}' for {cost:F0} Echo!", MessageTypeDefOf.PositiveEvent, true);
                return true;
            }
            else
            {
                Messages.Message($"[Echo Resonance] Insufficient Echo points! Need {cost:F0} Echo.", MessageTypeDefOf.RejectInput, false);
                return false;
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra())
            {
                yield return g;
            }

            if (PawnOwner != null && PawnOwner.IsColonistPlayerControlled && PawnOwner.Drafted == false)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Echo Perks",
                    defaultDesc = "Open the Echo Perk Tree to unlock powerful attributes and abilities.",
                    icon = ContentFinder<Texture2D>.Get("UI/Icons/Medical/HealthTab", false) ?? BaseContent.BadTex,
                    action = () =>
                    {
                        Find.WindowStack.Add(new Dialog_PawnPerks(PawnOwner, this));
                    }
                };
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref activePerks, "activePerks", LookMode.Def);
            if (activePerks == null) activePerks = new List<PerkDef>();
        }
    }

    public class CompProperties_PawnPerks : CompProperties
    {
        public CompProperties_PawnPerks()
        {
            compClass = typeof(CompPawnPerks);
        }
    }
}
