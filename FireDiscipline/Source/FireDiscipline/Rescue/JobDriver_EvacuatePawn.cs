using System.Collections.Generic;
using FireDiscipline.Suppression;
using RimWorld;
using Verse;
using Verse.AI;

namespace FireDiscipline.Rescue
{
    public class JobDriver_EvacuatePawn : JobDriver
    {
        private const TargetIndex TargetPawnInd = TargetIndex.A;
        private const TargetIndex TargetCellInd = TargetIndex.B;

        protected Pawn TargetPawn => (Pawn)job.GetTarget(TargetPawnInd).Thing;
        protected IntVec3 TargetCell => job.GetTarget(TargetCellInd).Cell;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(TargetPawn, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetPawnInd);
            this.FailOn(() => TargetPawn != null && (!TargetPawn.Downed || TargetPawn.Dead));
            this.FailOn(() => TargetCell.IsValid && !TargetCell.Walkable(Map));
            this.FailOn(() =>
            {
                float pinnedSev = FireDisciplineMod.Settings?.pinnedSeverityThreshold ?? 7.0f;
                return SuppressionEngine.GetSeverity(pawn) >= pinnedSev;
            });

            // 1. Move to downed pawn
            yield return Toils_Goto.GotoThing(TargetPawnInd, PathEndMode.Touch)
                .FailOnCannotTouch(TargetPawnInd, PathEndMode.Touch);

            // 2. Pick up downed pawn
            Toil carryToil = ToilMaker.MakeToil("StartCarryEvacuateTarget");
            carryToil.initAction = delegate
            {
                Pawn targetPawn = TargetPawn;
                if (targetPawn != null)
                {
                    pawn.carryTracker.TryStartCarry(targetPawn);
                }
            };
            carryToil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return carryToil;

            // 3. Move to evacuation cell B
            yield return Toils_Goto.GotoCell(TargetCellInd, PathEndMode.OnCell);

            // 4. Drop pawn at destination cell
            Toil dropToil = ToilMaker.MakeToil("DropEvacuateTarget");
            dropToil.initAction = delegate
            {
                Pawn targetPawn = TargetPawn;
                if (targetPawn != null && pawn.carryTracker.CarriedThing == targetPawn)
                {
                    pawn.carryTracker.TryDropCarriedThing(TargetCell, ThingPlaceMode.Direct, out _);
                }
            };
            dropToil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return dropToil;
        }
    }
}
