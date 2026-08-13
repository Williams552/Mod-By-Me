using Verse;
using RimWorld;
using System.Linq;
using UnityEngine;
using EchoResonance.Core;

namespace EchoResonance.Buildings
{
    public class PlaceWorker_PylonPlacement : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            var resonator = map.listerBuildings.AllBuildingsColonistOfClass<Building_Resonator>().Cast<Building_Resonator>().FirstOrDefault();
            if (resonator == null)
            {
                return new AcceptanceReport("Must build an Archotech Resonator first.");
            }

            if (!loc.InHorDistOf(resonator.Position, EchoTuning.PylonRadius))
            {
                return new AcceptanceReport($"Must be within {EchoTuning.PylonRadius} cells of the Archotech Resonator.");
            }

            var pylons = map.listerBuildings.AllBuildingsColonistOfClass<Building_AttunementPylon>().Cast<Building_AttunementPylon>();
            int pylonCount = 0;
            foreach (var pylon in pylons)
            {
                if (pylon == thingToIgnore) continue;
                pylonCount++;

                if (loc.InHorDistOf(pylon.Position, EchoTuning.MinPylonSpacing))
                {
                    return new AcceptanceReport($"Must be at least {EchoTuning.MinPylonSpacing} cells away from other Attunement Pylons.");
                }
            }

            if (pylonCount >= EchoTuning.MaxPylonsPerResonator)
            {
                return new AcceptanceReport($"Maximum of {EchoTuning.MaxPylonsPerResonator} Attunement Pylons allowed per map.");
            }

            return true;
        }

        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            Map map = Find.CurrentMap;
            if (map == null) return;

            var resonator = map.listerBuildings.AllBuildingsColonistOfClass<Building_Resonator>().Cast<Building_Resonator>().FirstOrDefault();
            if (resonator != null)
            {
                GenDraw.DrawRadiusRing(resonator.Position, EchoTuning.PylonRadius, Color.cyan);
            }
        }
    }
}
