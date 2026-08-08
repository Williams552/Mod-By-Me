using System.Collections.Generic;
using System.Linq;
using FireDiscipline.Core;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FireDiscipline.Encumbrance
{
    public class EncumbranceModule : IModule
    {
        public const string Id = "Encumbrance";

        public string ModuleId => Id;
        public string DisplayName => "Gear Encumbrance";
        public string Description => "Reduces combat move speed based on total apparel & weapon mass vs pawn carrying capacity.";
        public bool DefaultEnabled => true;
        public bool IsEnabled { get; set; } = true;

        public bool ShouldEnable()
        {
            return FireDisciplineMod.Settings?.IsModuleEnabled(this) ?? DefaultEnabled;
        }

        public void OnStartup()
        {
            if (StatDefOf.MoveSpeed != null)
            {
                if (StatDefOf.MoveSpeed.parts == null)
                {
                    StatDefOf.MoveSpeed.parts = new List<StatPart>();
                }

                if (!StatDefOf.MoveSpeed.parts.Any(p => p is StatPart_Encumbrance))
                {
                    StatDefOf.MoveSpeed.parts.Add(new StatPart_Encumbrance());
                    Log.Message("[Fire Discipline] Encumbrance StatPart dynamically injected into MoveSpeed.");
                }
            }
        }

        public void ApplyPatches(Harmony harmony)
        {
            // Encumbrance relies on dynamic StatPart injection on MoveSpeed, leaving zero Harmony patches.
        }
    }
}
