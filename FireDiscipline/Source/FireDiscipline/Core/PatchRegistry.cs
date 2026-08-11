using System;
using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace FireDiscipline.Core
{
    /// <summary>
    /// Central manager for manual Harmony patches and module lifecycle.
    /// Strictly avoids Harmony.PatchAll() to ensure disabled features leave zero overhead.
    /// </summary>
    public static class PatchRegistry
    {
        public const string HarmonyId = "william.firediscipline";
        private static Harmony harmonyInstance;
        private static readonly List<IModule> modules = new List<IModule>();
        private static readonly Dictionary<string, IModule> modulesById = new Dictionary<string, IModule>();
        private static readonly HashSet<string> patchedAtStartup = new HashSet<string>();

        public static IReadOnlyList<IModule> Modules => modules;

        public static void RegisterModule(IModule module)
        {
            if (module != null && !modules.Contains(module))
            {
                modules.Add(module);
                modulesById[module.ModuleId] = module;
            }
        }

        public static IModule GetModule(string moduleId)
        {
            return moduleId != null && modulesById.TryGetValue(moduleId, out IModule module) ? module : null;
        }

        /// <summary>
        /// Live enablement check for runtime guards (StatParts, patch bodies). Reads the IsEnabled
        /// flag maintained by InitializeAll and by the settings window, so switching a module OFF
        /// mid-session takes effect on the next stat recalculation without a restart.
        /// Unknown ids return true so a guard can never silently suppress an unregistered feature.
        /// </summary>
        public static bool IsModuleEnabled(string moduleId)
        {
            IModule module = GetModule(moduleId);
            return module == null || module.IsEnabled;
        }

        /// <summary>
        /// True if the module was enabled at game start and therefore actually had its Harmony
        /// patches registered. Switching a module ON mid-session cannot register patches
        /// retroactively - the settings window uses this to warn that a restart is required.
        /// </summary>
        public static bool WasPatchedAtStartup(string moduleId)
        {
            return moduleId != null && patchedAtStartup.Contains(moduleId);
        }

        public static void InitializeAll()
        {
            harmonyInstance = new Harmony(HarmonyId);
            Log.Message($"[Fire Discipline] Initializing modules (PackageId: {HarmonyId})...");

            // Core stability patches
            Patch_FloodFiller_NestedGuard.ApplyPatch(harmonyInstance);

            foreach (var module in modules)
            {
                try
                {
                    // Enablement is resolved BEFORE any setup runs. OnStartup() injects StatParts into
                    // shared vanilla StatDefs, so running it for a disabled module would leave that
                    // module affecting the game with no way to switch it off - which breaks the
                    // "all features off == vanilla" regression baseline the tuning harness depends on.
                    bool enabled = module.ShouldEnable();
                    module.IsEnabled = enabled;

                    if (enabled)
                    {
                        module.OnStartup();
                        module.ApplyPatches(harmonyInstance);
                        patchedAtStartup.Add(module.ModuleId);
                        Log.Message($"[Fire Discipline] Module [{module.DisplayName}] ENABLED & Patched.");
                    }
                    else
                    {
                        Log.Message($"[Fire Discipline] Module [{module.DisplayName}] DISABLED (no setup, no patches).");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"[Fire Discipline] Failed to initialize module [{module.DisplayName}]: {ex}");
                }
            }
        }
    }
}
