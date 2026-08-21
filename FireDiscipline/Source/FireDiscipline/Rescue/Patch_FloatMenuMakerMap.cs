using System.Collections.Generic;
using System.Linq;
using FireDiscipline.Core;
using FireDiscipline.Suppression;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace FireDiscipline.Rescue
{
    public static class Patch_FloatMenuMakerMap
    {
        public static void Postfix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        {
            if (pawn == null || pawn.Map == null || !pawn.IsColonistPlayerControlled) return;
            if (!PatchRegistry.IsModuleEnabled(EvacuationModule.Id)) return;

            IntVec3 clickCell = IntVec3.FromVector3(clickPos);
            List<Pawn> targetsAtCell = clickCell.GetThingList(pawn.Map).OfType<Pawn>().ToList();

            foreach (Pawn target in targetsAtCell)
            {
                if (target == null || target == pawn || !target.Downed || target.Dead) continue;

                string failReason = GetEvacuationFailureReason(pawn, target);
                if (failReason != null)
                {
                    string disabledLabel = "FD_Evacuate_DisabledOptionLabel".Translate(target.LabelShort, failReason);
                    opts.Add(new FloatMenuOption(disabledLabel, null));
                }
                else
                {
                    string optionLabel = "FD_Evacuate_OptionLabel".Translate(target.LabelShort);
                    Pawn t = target;
                    opts.Add(new FloatMenuOption(optionLabel, () => StartEvacuateTargeting(pawn, t)));
                }
            }
        }

        public static string GetEvacuationFailureReason(Pawn carrier, Pawn target)
        {
            if (carrier == null || target == null) return "FD_Evacuate_CannotReachOrReserve".Translate();

            // Gate 1: Non-hostile target
            if (carrier.HostileTo(target))
            {
                return "FD_Evacuate_HostileTarget".Translate();
            }

            // Gate 2: Carrier not carrying anything
            if (carrier.carryTracker?.CarriedThing != null)
            {
                return "FD_Evacuate_AlreadyCarrying".Translate();
            }

            // Gate 3: Carrier capable of manipulation
            if (carrier.health?.capacities == null || !carrier.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                return "FD_Evacuate_IncapableOfManipulation".Translate();
            }

            // Gate 4: CanReach and CanReserve target
            if (!carrier.CanReach(target, PathEndMode.Touch, Danger.Deadly) || !carrier.CanReserve(target))
            {
                return "FD_Evacuate_CannotReachOrReserve".Translate();
            }

            // Gate 5: Carrier suppression stage gate (if enabled)
            FireDisciplineSettings settings = FireDisciplineMod.Settings;
            bool requiresLower = settings?.evacuationRequiresLowerSuppression ?? true;
            if (requiresLower)
            {
                int carrierStage = GetSuppressionStageIndex(carrier);
                int targetStage = GetSuppressionStageIndex(target);
                if (carrierStage >= targetStage)
                {
                    return "FD_Evacuate_CarrierMustBeLowerSuppression".Translate();
                }
            }

            return null; // All gates passed
        }

        public static int GetSuppressionStageIndex(Pawn pawn)
        {
            if (pawn == null) return 0;
            float sev = SuppressionEngine.GetSeverity(pawn);
            float pinnedThreshold = FireDisciplineMod.Settings?.pinnedSeverityThreshold ?? 7.0f;

            if (sev >= pinnedThreshold) return 5;
            if (sev >= 5.5f) return 4;
            if (sev >= 2.0f) return 3;
            if (sev >= 1.0f) return 2;
            if (sev >= 0.5f) return 1;
            return 0;
        }

        private static void StartEvacuateTargeting(Pawn carrier, Pawn target)
        {
            if (carrier == null || target == null || carrier.Map == null) return;

            float maxDist = FireDisciplineMod.Settings?.evacuationMaxDistance ?? 30f;
            TargetingParameters parms = new TargetingParameters
            {
                canTargetLocations = true,
                canTargetPawns = false,
                canTargetBuildings = false,
                validator = (TargetInfo x) =>
                {
                    if (!x.Cell.IsValid || !x.Cell.Walkable(carrier.Map) || x.Cell.Fogged(carrier.Map)) return false;
                    if (x.Cell.DistanceTo(target.Position) > maxDist) return false;
                    return carrier.CanReach(x.Cell, PathEndMode.OnCell, Danger.Deadly);
                }
            };

            Find.Targeter.BeginTargeting(parms, (LocalTargetInfo targetCell) =>
            {
                JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail("FD_EvacuatePawn");
                if (jobDef != null)
                {
                    Job job = JobMaker.MakeJob(jobDef, target, targetCell.Cell);
                    carrier.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                }
            });
        }
    }
}
