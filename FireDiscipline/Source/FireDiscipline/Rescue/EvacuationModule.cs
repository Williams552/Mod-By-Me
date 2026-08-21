using System.Linq;
using System.Reflection;
using FireDiscipline.Core;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FireDiscipline.Rescue
{
    public class EvacuationModule : IModule
    {
        public const string Id = "Evacuation";

        public string ModuleId => Id;
        public string DisplayName => "Evacuate Downed Ally";
        public string Description => "Allows players to order a pawn to carry a downed ally out of active fire to a specified position.";
        public bool DefaultEnabled => false;
        public bool IsEnabled { get; set; } = false;

        public bool ShouldEnable()
        {
            return FireDisciplineMod.Settings?.enableEvacuation ?? DefaultEnabled;
        }

        public void OnStartup()
        {
        }

        public void ApplyPatches(Harmony harmony)
        {
            var targetMethod = AccessTools.Method(typeof(FloatMenuMakerMap), "AddHumanlikeOrders");
            if (targetMethod != null)
            {
                var postfix = typeof(Patch_FloatMenuMakerMap).GetMethod(nameof(Patch_FloatMenuMakerMap.Postfix), BindingFlags.Static | BindingFlags.Public);
                harmony.Patch(targetMethod, postfix: new HarmonyMethod(postfix));
                Log.Message($"[Fire Discipline] Patched FloatMenuMakerMap.AddHumanlikeOrders for Evacuation.");
            }
            else
            {
                Log.Error("[Fire Discipline] Failed to find FloatMenuMakerMap.AddHumanlikeOrders - Evacuation order will not appear.");
            }
        }
    }
}
