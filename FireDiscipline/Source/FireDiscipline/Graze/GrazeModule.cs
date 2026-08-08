using System.Reflection;
using FireDiscipline.Core;
using HarmonyLib;
using Verse;

namespace FireDiscipline.Graze
{
    public class GrazeModule : IModule
    {
        public string ModuleId => "Graze";
        public string DisplayName => "Graze System (Anti-One-Shot)";
        public string Description => "Converts fatal organ/brain shots from low-skill ranged attacks into non-lethal grazing blows (reducing damage by 65%).";
        public bool DefaultEnabled => true;
        public bool IsEnabled { get; set; } = true;

        public bool ShouldEnable()
        {
            return FireDisciplineMod.Settings?.IsModuleEnabled(this) ?? DefaultEnabled;
        }

        public void OnStartup()
        {
            Log.Message("[Fire Discipline] Graze System Module initialized.");
        }

        public void ApplyPatches(Harmony harmony)
        {
            var applyMethod = AccessTools.Method(typeof(DamageWorker_AddInjury), nameof(DamageWorker_AddInjury.Apply));
            if (applyMethod != null)
            {
                var prefix = typeof(Patch_DamageWorker_AddInjury).GetMethod(nameof(Patch_DamageWorker_AddInjury.Prefix), BindingFlags.Static | BindingFlags.Public);
                harmony.Patch(applyMethod, prefix: new HarmonyMethod(prefix));
                Log.Message("[Fire Discipline] Patched DamageWorker_AddInjury.Apply for Graze calculations.");
            }
        }
    }
}
