using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using EchoResonance.Core;

namespace EchoResonance.Buildings
{
    public class Building_Resonator : Building
    {
        private CompPowerTrader powerComp;

        public bool IsActive => powerComp == null || powerComp.PowerOn;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            powerComp = GetComp<CompPowerTrader>();
        }

        public float CalculatePylonMultiplier()
        {
            if (!IsActive || Map == null) return 1.0f;

            int activePylons = 0;
            var pylons = Map.listerBuildings.AllBuildingsColonistOfClass<Building_AttunementPylon>().Cast<Building_AttunementPylon>();

            foreach (var pylon in pylons)
            {
                if (pylon.IsActive && pylon.Position.InHorDistOf(Position, EchoTuning.PylonRadius))
                {
                    activePylons++;
                    if (activePylons >= EchoTuning.MaxPylonsPerResonator) break;
                }
            }

            return 1.0f + (activePylons * EchoTuning.PylonMultiplierBonus);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            EchoWorldComponent.Instance?.WipePool("Archotech Resonator Destroyed");
            base.Destroy(mode);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos())
            {
                yield return g;
            }

            float currentEcho = EchoWorldComponent.Instance?.StoredEcho ?? 0f;
            float multiplier = CalculatePylonMultiplier();

            yield return new Command_Action
            {
                defaultLabel = $"Echo Pool: {currentEcho:F1}",
                defaultDesc = $"Archotech Resonator Status:\n- Current Echo: {currentEcho:F1}\n- Current Pylon Multiplier: x{multiplier:F1}\n- Active Status: {(IsActive ? "ONLINE" : "OFFLINE (No Power)")}",
                icon = ContentFinder<Texture2D>.Get("UI/Icons/Medical/HealthTab", false) ?? BaseContent.BadTex,
                action = () => { }
            };
        }
    }
}
