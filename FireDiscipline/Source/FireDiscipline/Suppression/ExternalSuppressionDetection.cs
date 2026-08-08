using System.Collections.Generic;
using System.Linq;
using Verse;

namespace FireDiscipline.Suppression
{
    /// <summary>
    /// Detects other mods that already implement suppression.
    ///
    /// This does NOT gate behaviour. The internal suppression engine is always available and the
    /// player chooses whether it runs. Detection is used for exactly two things:
    ///   (a) picking the default value the first time the mod is installed
    ///   (b) showing an informed warning in the settings window
    ///
    /// That is a deliberate reversal of the earlier design, where the engine switched itself off
    /// based on what else was installed. Silently deciding for the player produced the worst
    /// outcome available: a player with an external suppression mod lost the stance matrix without
    /// ever being told it existed.
    ///
    /// Detection is by packageId through ModsConfig.IsActive - no hard dependency, no assembly
    /// reference, no load-order requirement (rule 6).
    /// </summary>
    public static class ExternalSuppressionDetection
    {
        /// <summary>
        /// Mods that add their own suppression mechanic. Fire Discipline's engine stacked on top of
        /// these produces double suppression.
        ///
        /// These ids need verifying against a real install - the previous list contained
        /// "suppression.mod", which does not appear to be a real packageId, and "CombatExtended",
        /// which is missing the CETeam prefix. Use the "Print Active Mod PackageIds" debug action
        /// to read the real ids off a live modlist and correct this list.
        /// </summary>
        public static readonly string[] SuppressionModPackageIds =
        {
            "mlie.suppression",
        };

        /// <summary>
        /// Combat Extended replaces the whole combat model rather than layering on it. It is called
        /// out separately because the warning it deserves is stronger than for a suppression-only
        /// mod, and because its default is always OFF regardless.
        /// </summary>
        public static readonly string[] CombatExtendedPackageIds =
        {
            "ceteam.combatextended",
            "combatextended",
        };

        /// <summary>
        /// Hediff defNames used by external suppression mods.
        ///
        /// DIAGNOSTIC USE ONLY. This is the one place in the mod that names another mod's def, and
        /// it exists so the debug harness can report "who is actually suppressing this pawn" instead
        /// of leaving the player to guess. Nothing in the gameplay path may read this list -
        /// architecture rule 2 still holds for anything that affects balance.
        /// </summary>
        public static readonly string[] ExternalSuppressedHediffDefNames =
        {
            "Suppressed", // Suppression (Continued), Mlie.Suppression - severity scale 0..9
        };

        /// <summary>
        /// The external suppression hediff present in this modlist, or null. Diagnostic only.
        /// </summary>
        public static HediffDef FindExternalSuppressedHediff()
        {
            foreach (string defName in ExternalSuppressedHediffDefNames)
            {
                HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail(defName);
                if (def != null) return def;
            }
            return null;
        }

        public static bool IsCombatExtendedActive()
        {
            return CombatExtendedPackageIds.Any(ModsConfig.IsActive);
        }

        public static bool IsSuppressionModActive()
        {
            return SuppressionModPackageIds.Any(ModsConfig.IsActive);
        }

        public static bool IsAnyExternalSuppressionActive()
        {
            return IsCombatExtendedActive() || IsSuppressionModActive();
        }

        /// <summary>
        /// Names of the detected mods, for use in warning text. Returns the packageIds rather than
        /// display names so the player can match them against their own mod list.
        /// </summary>
        public static List<string> DetectedPackageIds()
        {
            return CombatExtendedPackageIds.Concat(SuppressionModPackageIds)
                .Where(ModsConfig.IsActive)
                .ToList();
        }

        /// <summary>
        /// The value enableSuppressionEngine takes on a fresh install. Combat Extended always
        /// yields false; any other suppression mod also yields false; otherwise true.
        /// </summary>
        public static bool RecommendedDefault()
        {
            return !IsAnyExternalSuppressionActive();
        }
    }
}
