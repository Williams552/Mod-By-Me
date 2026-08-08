using System.Reflection;
using FireDiscipline.Core;
using HarmonyLib;
using Verse;

namespace FireDiscipline.Shock
{
    public class ShockModule : IModule
    {
        public const string Id = "Shock";

        public string ModuleId => Id;
        public string DisplayName => "Shock & Shell Shock System";
        public string Description => "Triggers Combat Shock on nearby allies when a pawn is downed/killed, and Proportional Shell Shock on explosive impact.";
        public bool DefaultEnabled => true;
        public bool IsEnabled { get; set; } = true;

        public bool ShouldEnable()
        {
            return FireDisciplineMod.Settings?.IsModuleEnabled(this) ?? DefaultEnabled;
        }

        public void OnStartup()
        {
            Log.Message("[Fire Discipline] Shock & Shell Shock Module initialized.");
        }

        public void ApplyPatches(Harmony harmony)
        {
            // 1. Ally Downed/Killed Combat Shock
            var killMethod = AccessTools.Method(typeof(Pawn), nameof(Pawn.Kill));
            if (killMethod != null)
            {
                var postfix = typeof(Patch_Pawn_Kill_Down).GetMethod(nameof(Patch_Pawn_Kill_Down.Postfix_Kill), BindingFlags.Static | BindingFlags.Public);
                harmony.Patch(killMethod, postfix: new HarmonyMethod(postfix));
                Log.Message("[Fire Discipline] Patched Pawn.Kill for Combat Shock.");
            }

            var downedMethod = AccessTools.Method(typeof(Pawn_HealthTracker), "MakeDowned");
            if (downedMethod != null)
            {
                var postfix = typeof(Patch_Pawn_Kill_Down).GetMethod(nameof(Patch_Pawn_Kill_Down.Postfix_Downed), BindingFlags.Static | BindingFlags.Public);
                harmony.Patch(downedMethod, postfix: new HarmonyMethod(postfix));
                Log.Message("[Fire Discipline] Patched Pawn_HealthTracker.MakeDowned for Combat Shock.");
            }

            // 2. Proportional Explosive Shell Shock
            var explosionMethod = AccessTools.Method(typeof(Explosion), nameof(Explosion.StartExplosion));
            if (explosionMethod != null)
            {
                var postfix = typeof(Patch_Explosion).GetMethod(nameof(Patch_Explosion.Postfix), BindingFlags.Static | BindingFlags.Public);
                harmony.Patch(explosionMethod, postfix: new HarmonyMethod(postfix));
                Log.Message("[Fire Discipline] Patched Explosion.StartExplosion for Proportional Shell Shock.");
            }
        }
    }
}
