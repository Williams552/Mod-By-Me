using HarmonyLib;

namespace FireDiscipline.Core
{
    /// <summary>
    /// Contract for all modular features in Fire Discipline.
    /// Ensures manual patch lifecycle management (No PatchAll).
    /// </summary>
    public interface IModule
    {
        string ModuleId { get; }
        string DisplayName { get; }
        string Description { get; }
        bool DefaultEnabled { get; }
        bool IsEnabled { get; set; }

        /// <summary>
        /// Evaluates whether the module should active given current ModSettings and active mods.
        /// </summary>
        bool ShouldEnable();

        /// <summary>
        /// Called at startup for non-patch setup (e.g. injecting dynamic StatParts, Hediffs).
        /// Only invoked when ShouldEnable() returned true - a disabled module must leave no trace
        /// on shared vanilla state. Do not rely on this running for bookkeeping that must always happen.
        /// </summary>
        void OnStartup();

        /// <summary>
        /// Registers Harmony patches manually for this feature.
        /// </summary>
        void ApplyPatches(Harmony harmony);
    }
}
