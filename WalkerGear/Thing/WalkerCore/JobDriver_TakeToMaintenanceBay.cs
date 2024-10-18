using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using static UnityEngine.GraphicsBuffer;

namespace WalkerGear
{
    /// <summary>
    /// 將倒地的自家龍騎兵搬回整備架，否則不下來
    /// </summary>
    public class JobDriver_TakeToMaintenanceBay : JobDriver
    {
        private const TargetIndex BuildingInd = TargetIndex.A;
        private const TargetIndex TakeeInd = TargetIndex.B;
        private Pawn Takee => (Pawn)job.GetTarget(TargetIndex.B).Thing;
        private Building_MaintenanceBay Building => (Building_MaintenanceBay)job.GetTarget(TargetIndex.A).Thing;

        private const int wait = 200;
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (pawn.Reserve(Building, job, 1, 1, null, errorOnFailed))
            {
                return pawn.Reserve(Takee, job, 1, 1, null, errorOnFailed);
            }
            return false;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoThing(TakeeInd, PathEndMode.Touch).FailOnDespawnedOrNull(TakeeInd).FailOnDespawnedOrNull(BuildingInd);
            Toil haul = Toils_Haul.StartCarryThing(TakeeInd);
            haul.FailOnNotDowned(TakeeInd);
            yield return haul;

            yield return Toils_Goto.GotoThing(BuildingInd, PathEndMode.InteractionCell).FailOnDespawnedOrNull(BuildingInd);
            Toil toil = Toils_General.WaitWith(TargetIndex.A, wait, true, true, face: BuildingInd);
            toil.FailOnDespawnedOrNull(TargetIndex.A);
            toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            toil.handlingFacing = true;
            yield return toil;
            Toil gearDown = new()
            {
                initAction = () =>
                {
                    Pawn actor = this.Takee;
                    Building.GearDown(actor);
                    pawn.drafter.Drafted = false;
                }
            };
            yield return gearDown;
        }
    }
}
