using Verse;
using RimWorld;
using System.Linq;

namespace EchoResonance.Buildings
{
    public class PlaceWorker_SingleResonator : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            var existingResonator = map.listerBuildings.AllBuildingsColonistOfClass<Building_Resonator>().Cast<Building_Resonator>().FirstOrDefault();
            if (existingResonator != null && existingResonator != thingToIgnore)
            {
                return new AcceptanceReport("Only one Archotech Resonator is allowed per map.");
            }
            return true;
        }
    }
}
