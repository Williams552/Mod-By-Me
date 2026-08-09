using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace FireDiscipline.AimStance
{
    /// <summary>
    /// Harmony Postfix on Pawn.GetGizmos to inject the Fire Discipline Stance Gizmo when drafted.
    /// </summary>
    public static class Patch_Pawn_GetGizmos
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
        {
            foreach (var g in __result)
            {
                yield return g;
            }

            if (__instance != null && __instance.Drafted && __instance.Faction == Faction.OfPlayer
                && FireDiscipline.Core.PatchRegistry.IsModuleEnabled(AimStanceModule.Id))
            {
                AimStanceMode currentStance = AimStanceTracker.GetStance(__instance);

                string label = "Stance: Standard";
                string desc = "Baseline vanilla firing speed and accuracy.";

                if (currentStance == AimStanceMode.Rapid)
                {
                    label = "Stance: Rapid Fire";
                    desc = "Fast close-range hipfire (derived warmup reduction, accuracy penalty at long range).";
                }
                else if (currentStance == AimStanceMode.Sharpshot)
                {
                    label = "Stance: Sharpshot";
                    desc = "Long-range precision stance (distance exponent d*0.8, warmup x1.4, resets on suppression).";
                }
                else if (currentStance == AimStanceMode.Prone)
                {
                    label = "Stance: Prone";
                    desc = "Bunker/trench posture (Target size x0.65, no movement, suppression x0.5).";
                }

                string iconPath = "UI/Commands/FireDiscipline/Stance_Standard";
                if (currentStance == AimStanceMode.Rapid) iconPath = "UI/Commands/FireDiscipline/Stance_Rapid";
                else if (currentStance == AimStanceMode.Sharpshot) iconPath = "UI/Commands/FireDiscipline/Stance_Sharpshot";
                else if (currentStance == AimStanceMode.Prone) iconPath = "UI/Commands/FireDiscipline/Stance_Prone";

                Pawn targetPawn = __instance;
                Command_Action stanceCmd = new Command_Action
                {
                    defaultLabel = label,
                    defaultDesc = desc + "\nClick to cycle stance (Standard -> Rapid -> Sharpshot -> Prone).",
                    icon = ContentFinder<Texture2D>.Get(iconPath, true),
                    action = () =>
                    {
                        AimStanceTracker.CycleStance(targetPawn);
                    }
                };

                yield return stanceCmd;
            }
        }
    }
}
