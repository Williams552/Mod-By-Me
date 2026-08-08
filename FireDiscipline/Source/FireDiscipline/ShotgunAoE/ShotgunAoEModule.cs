using System.Linq;
using System.Reflection;
using FireDiscipline.Core;
using HarmonyLib;
using Verse;

namespace FireDiscipline.ShotgunAoE
{
    /// <summary>
    /// Wave B2. Shotguns spray: a hit sprays reduced damage onto other pawns within a fixed radius.
    ///
    /// OFF by default. This code shipped inside the suppression patch and would have entered v1.0
    /// as a side effect of turning suppression on, carrying an undecided friendly-fire rule with
    /// it. It is now its own module with its own toggle so it enters the game when someone decides
    /// it should, not when someone enables something else.
    ///
    /// Design 5.5(b) also requires a UI danger-zone overlay before this is considered finished -
    /// without it players will read the splash as a bug. That is tracked as B8 and is NOT done.
    /// </summary>
    public class ShotgunAoEModule : IModule
    {
        public const string Id = "ShotgunAoE";

        public string ModuleId => Id;
        public string DisplayName => "Shotgun Spread (experimental)";
        public string Description => "Shotgun hits spray reduced damage onto nearby pawns. No danger-zone UI yet - splash can surprise you.";
        public bool DefaultEnabled => false;
        public bool IsEnabled { get; set; } = false;

        public bool ShouldEnable()
        {
            return FireDisciplineMod.Settings?.IsModuleEnabled(this) ?? DefaultEnabled;
        }

        public void OnStartup()
        {
            Log.Message("[Fire Discipline] Shotgun Spread module initialized.");
        }

        public void ApplyPatches(Harmony harmony)
        {
            var impactMethod = AccessTools.Method(typeof(Projectile), "Impact", new[] { typeof(Thing), typeof(bool) });
            if (impactMethod == null)
            {
                impactMethod = AccessTools.Method(typeof(Projectile), "Impact");
            }

            if (impactMethod != null)
            {
                var prefix = typeof(Patch_Projectile_Impact_Shotgun).GetMethod(nameof(Patch_Projectile_Impact_Shotgun.Prefix), BindingFlags.Static | BindingFlags.Public);
                harmony.Patch(impactMethod, prefix: new HarmonyMethod(prefix));
                Log.Message($"[Fire Discipline] Patched {impactMethod.DeclaringType.Name}.{impactMethod.Name}({string.Join(", ", impactMethod.GetParameters().Select(x => x.ParameterType.Name).ToArray())}) for shotgun spread.");
            }
            else
            {
                Log.Error("[Fire Discipline] Failed to find Projectile.Impact - shotgun spread will not run.");
            }

            // Danger-zone overlay (design 5.5b / B8). Draw-only: if this ever fails to attach the
            // weapon still works, it just stops warning the player.
            var drawHighlight = AccessTools.Method(typeof(Verb), nameof(Verb.DrawHighlight));
            if (drawHighlight != null)
            {
                var postfix = typeof(Patch_Verb_DrawHighlight).GetMethod(nameof(Patch_Verb_DrawHighlight.Postfix), BindingFlags.Static | BindingFlags.Public);
                harmony.Patch(drawHighlight, postfix: new HarmonyMethod(postfix));
                Log.Message("[Fire Discipline] Patched Verb.DrawHighlight for the shotgun danger overlay.");
            }
            else
            {
                Log.Warning("[Fire Discipline] Verb.DrawHighlight not found - shotgun spread will fire with no on-screen warning.");
            }
        }
    }
}
