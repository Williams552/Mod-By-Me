using FireDiscipline.Core;
using HarmonyLib;
using Verse;

namespace FireDiscipline.Suppression
{
    public class SuppressionMarkerModule : IModule
    {
        public const string Id = "SuppressionMarker";

        public string ModuleId => Id;
        public string DisplayName => "Suppression Stage Marker";
        public string Description => "Renders a visual stage marker above pawns under suppression on the map overlay.";
        public bool DefaultEnabled => false;
        public bool IsEnabled { get; set; } = false;

        public bool ShouldEnable()
        {
            return FireDisciplineMod.Settings?.enableSuppressionMarker ?? DefaultEnabled;
        }

        public void OnStartup()
        {
        }

        public void ApplyPatches(Harmony harmony)
        {
            // Pure visual MapComponentOnGUI overlay - 0 Harmony patches required.
        }
    }
}
