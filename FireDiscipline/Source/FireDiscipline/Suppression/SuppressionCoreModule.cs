using System.Linq;
using System.Reflection;
using FireDiscipline.Core;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FireDiscipline.Suppression
{
    /// <summary>
    /// Owns the internal suppression engine: the FD_Suppressed hediff, the stance matrix, and the
    /// optional Pinned state.
    ///
    /// The engine is ALWAYS present. Whether it runs is the player's choice, not a consequence of
    /// what else they installed. Detecting another suppression mod only sets the first-run default
    /// and drives a warning in the settings window - see ExternalSuppressionDetection.
    ///
    /// Replaces SuppressionIntegrationModule, which enabled itself only when an external
    /// suppression mod was present. That was backwards in two ways at once: it inverted the design
    /// document, and it named itself after integration it never performed.
    /// </summary>
    public class SuppressionCoreModule : IModule
    {
        public const string Id = "SuppressionCore";

        public string ModuleId => Id;
        public string DisplayName => "Suppression Engine";
        public string Description => "Incoming fire pins pawns down. Carries the stance matrix: Rapid inflicts more, Sharpshot suffers more, Prone resists.";
        public bool DefaultEnabled => true;
        public bool IsEnabled { get; set; } = true;

        public bool ShouldEnable()
        {
            // No external-mod check here by design. The player decides.
            return FireDisciplineMod.Settings?.enableSuppressionEngine ?? DefaultEnabled;
        }

        public void OnStartup()
        {
            if (SuppressionEngine.SuppressedDef == null)
            {
                Log.Error("[Fire Discipline] HediffDef 'FD_Suppressed' not found - the suppression engine cannot run.");
                return;
            }

            if (StatDefOf.MoveSpeed != null)
            {
                if (StatDefOf.MoveSpeed.parts == null)
                {
                    StatDefOf.MoveSpeed.parts = new System.Collections.Generic.List<RimWorld.StatPart>();
                }
                if (!StatDefOf.MoveSpeed.parts.Any(p => p is StatPart_SuppressionMoveSpeed))
                {
                    StatDefOf.MoveSpeed.parts.Add(new StatPart_SuppressionMoveSpeed());
                }
            }

            if (ExternalSuppressionDetection.IsAnyExternalSuppressionActive())
            {
                Log.Warning("[Fire Discipline] Suppression engine is ENABLED while another suppression mod is active ("
                    + string.Join(", ", ExternalSuppressionDetection.DetectedPackageIds().ToArray())
                    + "). Both will apply suppression to the same pawns.");
            }
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
                var prefix = typeof(Patch_Projectile_Impact).GetMethod(nameof(Patch_Projectile_Impact.Prefix), BindingFlags.Static | BindingFlags.Public);
                harmony.Patch(impactMethod, prefix: new HarmonyMethod(prefix));
                Log.Message($"[Fire Discipline] Patched {impactMethod.DeclaringType.Name}.{impactMethod.Name}({string.Join(", ", impactMethod.GetParameters().Select(x => x.ParameterType.Name).ToArray())}) for suppression.");
            }
            else
            {
                Log.Error("[Fire Discipline] Failed to find Projectile.Impact - suppression will not run.");
            }

            var availableMethod = AccessTools.Method(typeof(Verb), nameof(Verb.Available));
            if (availableMethod != null)
            {
                var postfix = typeof(Patch_Verb_Available).GetMethod(nameof(Patch_Verb_Available.Postfix), BindingFlags.Static | BindingFlags.Public);
                harmony.Patch(availableMethod, postfix: new HarmonyMethod(postfix));
                Log.Message($"[Fire Discipline] Patched {availableMethod.DeclaringType.Name}.{availableMethod.Name} for pinned state.");
            }
            else
            {
                Log.Error("[Fire Discipline] Failed to find Verb.Available - pinned state will not run.");
            }
        }
    }
}
