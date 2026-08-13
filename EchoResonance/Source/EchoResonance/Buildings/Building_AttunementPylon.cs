using RimWorld;
using Verse;

namespace EchoResonance.Buildings
{
    public class Building_AttunementPylon : Building
    {
        private CompPowerTrader powerComp;

        public bool IsActive => powerComp == null || powerComp.PowerOn;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            powerComp = GetComp<CompPowerTrader>();
        }
    }
}
