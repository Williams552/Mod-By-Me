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

        public bool HasConflict(PerkDef perk, out PerkDef conflictingPerk)
        {
            conflictingPerk = null;
            if (perk.exclusionTags.NullOrEmpty()) return false;

            foreach (var active in activePerks)
            {
                if (!active.exclusionTags.NullOrEmpty())
                {
                    foreach (var tag in perk.exclusionTags)
                    {
                        if (active.exclusionTags.Contains(tag))
                        {
                            conflictingPerk = active;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public bool HasPrerequisites(PerkDef perk, out List<PerkDef> missingReqs)
        {
            missingReqs = new List<PerkDef>();

            // Check single replaces requirement
            if (perk.replaces != null && !HasPerk(perk.replaces))
            {
                missingReqs.Add(perk.replaces);
            }

            // Check multiple replacesList requirement
            if (!perk.replacesList.NullOrEmpty())
            {
                foreach (var rep in perk.replacesList)
                {
                    if (rep != null && !HasPerk(rep) && !missingReqs.Contains(rep))
                    {
                        missingReqs.Add(rep);
                    }
                }
            }

            // Check requires requirements
            if (!perk.requires.NullOrEmpty())
            {
                foreach (var req in perk.requires)
                {
                    if (req != null && !HasPerk(req) && !missingReqs.Contains(req))
                    {
                        missingReqs.Add(req);
                    }
                }
            }

            return missingReqs.Count == 0;
        }

        public bool IsTechUnlocked(PerkDef perk, out string techReason)
        {
            techReason = null;
            if (perk.tier == 2)
            {
                var research = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("EchoAttunement");
                if (research != null && !research.IsFinished)
                {
                    techReason = "Requires Research: Echo Attunement";
                    return false;
                }
            }
            else if (perk.tier == 3)
            {
                var research = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("ArchotechResonance");
                if (research != null && !research.IsFinished)
                {
                    techReason = "Requires Research: Archotech Resonance";
                    return false;
                }
            }
            return true;
        }

        public bool HasCatalystItem(PerkDef perk)
        {
            if (perk.tier != 2) return true;

            Map map = PawnOwner?.MapHeld ?? Find.CurrentMap;
            if (map == null) return true;

            var focusDef = DefDatabase<ThingDef>.GetNamedSilentFail("ER_ResonanceFocus");
            if (focusDef == null) return true;

            int count = map.resourceCounter.GetCount(focusDef);
            return count >= 1;
        }

        public float CalculatePerkCost(PerkDef perk)
        {
            var replacedList = perk.GetAllReplacedPerks();
            int countReplacedOwned = replacedList.Count(p => HasPerk(p));

            // If replacing one or multiple existing perks, N decreases by count of consumed perks
            int n = Mathf.Max(1, activePerks.Count - countReplacedOwned + 1);
            
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

            // 1. Conflict check
            if (HasConflict(perk, out var conflict))
            {
                Messages.Message($"[Echo Resonance] Conflict! Cannot unlock '{perk.label}' due to existing perk '{conflict.label}'.", MessageTypeDefOf.RejectInput, false);
                return false;
            }

            // 2. Prerequisite check
            if (!HasPrerequisites(perk, out var missing))
            {
                string missingStr = string.Join(", ", missing.Select(m => m.label));
                Messages.Message($"[Echo Resonance] Missing prerequisite perk: {missingStr}.", MessageTypeDefOf.RejectInput, false);
                return false;
            }

            // 3. Tech gate check
            if (!IsTechUnlocked(perk, out var techReason))
            {
                Messages.Message($"[Echo Resonance] {techReason}", MessageTypeDefOf.RejectInput, false);
                return false;
            }

            // 4. Catalyst item check for Tier 2
            if (perk.tier == 2 && !HasCatalystItem(perk))
            {
                Messages.Message("[Echo Resonance] Requires 1 Resonance Focus Crystal in colony storage!", MessageTypeDefOf.RejectInput, false);
                return false;
            }

            float cost = CalculatePerkCost(perk);
            if (EchoWorldComponent.Instance.TrySpendEcho(cost))
            {
                // Consume Catalyst item if Tier 2
                if (perk.tier == 2)
                {
                    ConsumeCatalystItem();
                }

                // If replacing old perks, remove all old perk hediffs & entries
                var replacedList = perk.GetAllReplacedPerks();
                int replacedCount = 0;
                foreach (var oldPerk in replacedList)
                {
                    if (HasPerk(oldPerk))
                    {
                        PerkApplier.RemovePerkFromPawn(PawnOwner, oldPerk);
                        activePerks.Remove(oldPerk);
                        replacedCount++;
                    }
                }

                activePerks.Add(perk);
                PerkApplier.ApplyPerkToPawn(PawnOwner, perk);

                string msg = (replacedCount > 0)
                    ? $"[Echo Resonance] {PawnOwner.LabelShort} unlocked '{perk.label}' for {cost:F0} Echo, absorbing {replacedCount} previous perks!"
                    : $"[Echo Resonance] {PawnOwner.LabelShort} unlocked perk '{perk.label}' for {cost:F0} Echo!";

                Messages.Message(msg, MessageTypeDefOf.PositiveEvent, true);
                return true;
            }
            else
            {
                Messages.Message($"[Echo Resonance] Insufficient Echo points! Need {cost:F0} Echo.", MessageTypeDefOf.RejectInput, false);
                return false;
            }
        }

        private void ConsumeCatalystItem()
        {
            Map map = PawnOwner?.MapHeld ?? Find.CurrentMap;
            if (map == null) return;

            var focusDef = DefDatabase<ThingDef>.GetNamedSilentFail("ER_ResonanceFocus");
            if (focusDef == null) return;

            var item = map.listerThings.ThingsOfDef(focusDef).FirstOrDefault(t => !t.IsForbidden(Faction.OfPlayer));
            if (item != null)
            {
                item.SplitOff(1).Destroy();
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
                    icon = ContentFinder<Texture2D>.Get("UI/Gizmos/EchoPerkGizmo", false) ?? BaseContent.BadTex,
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
