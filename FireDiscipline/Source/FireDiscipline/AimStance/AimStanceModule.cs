using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FireDiscipline.Core;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FireDiscipline.AimStance
{
    public class AimStanceModule : IModule
    {
        public const string Id = "AimStance";

        public string ModuleId => Id;
        public string DisplayName => "Aim Mode & Stances v2";
        public string Description => "Adds 4 tactical stances (Snap, Rapid, Sharpshot, Prone) with derived formulas, transition costs, and anti-power-creep passive stances.";
        public bool DefaultEnabled => true;
        public bool IsEnabled { get; set; } = true;

        public bool ShouldEnable()
        {
            return FireDisciplineMod.Settings?.IsModuleEnabled(this) ?? DefaultEnabled;
        }

        public void OnStartup()
        {
            if (StatDefOf.AimingDelayFactor != null)
            {
                if (StatDefOf.AimingDelayFactor.parts == null)
                    StatDefOf.AimingDelayFactor.parts = new List<StatPart>();

                if (!StatDefOf.AimingDelayFactor.parts.Any(p => p is StatPart_AimingDelay))
                {
                    StatDefOf.AimingDelayFactor.parts.Add(new StatPart_AimingDelay());
                    Log.Message("[Fire Discipline] StatPart_AimingDelay injected into AimingDelayFactor.");
                }
            }

            if (StatDefOf.ShootingAccuracyPawn != null)
            {
                if (StatDefOf.ShootingAccuracyPawn.parts == null)
                    StatDefOf.ShootingAccuracyPawn.parts = new List<StatPart>();

                if (!StatDefOf.ShootingAccuracyPawn.parts.Any(p => p is StatPart_ShootingAccuracy))
                {
                    StatDefOf.ShootingAccuracyPawn.parts.Add(new StatPart_ShootingAccuracy());
                    Log.Message("[Fire Discipline] StatPart_ShootingAccuracy injected into ShootingAccuracyPawn.");
                }
            }
        }

        public void ApplyPatches(Harmony harmony)
        {
            // Manual registration of Harmony patches (Principle 4)
            var pawnGetGizmos = typeof(Pawn).GetMethod(nameof(Pawn.GetGizmos), BindingFlags.Instance | BindingFlags.Public);
            if (pawnGetGizmos != null)
            {
                var postfix = typeof(Patch_Pawn_GetGizmos).GetMethod(nameof(Patch_Pawn_GetGizmos.Postfix), BindingFlags.Static | BindingFlags.Public);
                harmony.Patch(pawnGetGizmos, postfix: new HarmonyMethod(postfix));
            }

            var hitReportMethod = AccessTools.Method(typeof(ShotReport), nameof(ShotReport.HitReportFor), new[] { typeof(Thing), typeof(Verb), typeof(LocalTargetInfo) });
            if (hitReportMethod != null)
            {
                var postfix = typeof(Patch_ShotReport).GetMethod(nameof(Patch_ShotReport.Postfix), BindingFlags.Static | BindingFlags.Public);
                harmony.Patch(hitReportMethod, postfix: new HarmonyMethod(postfix));
                Log.Message("[Fire Discipline] Patch_ShotReport successfully patched onto ShotReport.HitReportFor.");
            }
            else
            {
                Log.Error("[Fire Discipline] Failed to find ShotReport.HitReportFor method!");
            }

            var startPathMethod = AccessTools.Method(typeof(Verse.AI.Pawn_PathFollower), nameof(Verse.AI.Pawn_PathFollower.StartPath));
            if (startPathMethod != null)
            {
                var prefix = typeof(Patch_Pawn_PathFollower).GetMethod(nameof(Patch_Pawn_PathFollower.Prefix), BindingFlags.Static | BindingFlags.Public);
                harmony.Patch(startPathMethod, prefix: new HarmonyMethod(prefix));
                Log.Message("[Fire Discipline] Patch_Pawn_PathFollower successfully patched onto StartPath.");
            }

            Log.Message("[Fire Discipline] AimStance v2 patches successfully registered manually.");
        }
    }
}
