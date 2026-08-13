using Verse;
using RimWorld;
using LudeonTK;
using EchoResonance.Perks;
using EchoResonance.UI;
using EchoResonance.Buildings;

namespace EchoResonance.Core
{
    [StaticConstructorOnStartup]
    public static class EchoResonanceBootstrap
    {
        static EchoResonanceBootstrap()
        {
            // Auto-inject CompPawnPerks into all Humanlike races so Gizmo shows up on all colonists
            foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.race != null && def.race.Humanlike)
                {
                    if (def.comps == null)
                    {
                        def.comps = new System.Collections.Generic.List<CompProperties>();
                    }

                    if (!def.comps.Exists(c => c is CompProperties_PawnPerks))
                    {
                        def.comps.Add(new CompProperties_PawnPerks());
                    }
                }
            }
            Log.Message("[Echo Resonance] Auto-injected CompPawnPerks to all Humanlike PawnDefs.");
        }
    }

    public static class DebugActionsEcho
    {
        [DebugAction("Echo Resonance", "Add +100 Echo Points", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void Add100Echo()
        {
            if (EchoWorldComponent.Instance != null)
            {
                EchoWorldComponent.Instance.AddEcho(100f, "Dev Mode Cheat", true);
                Messages.Message("Dev Mode: Added +100 Echo points to pool!", MessageTypeDefOf.PositiveEvent, false);
            }
        }

        [DebugAction("Echo Resonance", "Open Perk Tree for Selected Pawn", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void OpenPerkTreeForSelectedPawn()
        {
            Pawn pawn = Find.Selector.SingleSelectedThing as Pawn;
            if (pawn == null)
            {
                Messages.Message("Dev Mode: Select a Pawn first!", MessageTypeDefOf.RejectInput, false);
                return;
            }

            var comp = pawn.GetComp<CompPawnPerks>();
            if (comp != null)
            {
                Find.WindowStack.Add(new Dialog_PawnPerks(pawn, comp));
            }
            else
            {
                Messages.Message($"Dev Mode: {pawn.LabelShort} does not have CompPawnPerks component!", MessageTypeDefOf.RejectInput, false);
            }
        }

        [DebugAction("Echo Resonance", "Simulate Resonator Destruction (Wipe Pool)", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void SimulateResonatorDestruction()
        {
            if (EchoWorldComponent.Instance != null)
            {
                EchoWorldComponent.Instance.WipePool("Dev Mode Test Wipe");
            }
        }
    }
}
