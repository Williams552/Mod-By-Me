using HarmonyLib;
using Verse;
using FireDiscipline.Core;

namespace FireDiscipline.Variance
{
    public class VarianceModule : IModule
    {
        public const string Id = "Variance";

        public string ModuleId => Id;
        public string DisplayName => "Hit Variance Mitigation (Wave B8)";
        public string Description => "Reduces hit randomness using Quota-Carry for single shots and Pity-Symmetric for bursts.";
        public bool DefaultEnabled => false;
        public bool IsEnabled { get; set; } = false;

        public bool ShouldEnable()
        {
            return FireDisciplineMod.Settings?.enableHitVariance ?? DefaultEnabled;
        }

        public void OnStartup()
        {
        }

        public void ApplyPatches(Harmony harmony)
        {
            var tryCastShotMethod = AccessTools.Method(typeof(Verb_LaunchProjectile), "TryCastShot");
            if (tryCastShotMethod != null)
            {
                var prefix = typeof(Patch_Verb_TryCastShot).GetMethod(nameof(Patch_Verb_TryCastShot.Prefix), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                var finalizer = typeof(Patch_Verb_TryCastShot).GetMethod(nameof(Patch_Verb_TryCastShot.Finalizer), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

                harmony.Patch(tryCastShotMethod,
                    prefix: new HarmonyMethod(prefix),
                    finalizer: new HarmonyMethod(finalizer));

                Log.Message("[Fire Discipline] Patch_Verb_TryCastShot successfully patched onto Verb_LaunchProjectile.TryCastShot with Finalizer.");
            }
            else
            {
                Log.Error("[Fire Discipline] Failed to find Verb_LaunchProjectile.TryCastShot method!");
            }
        }
    }
}
