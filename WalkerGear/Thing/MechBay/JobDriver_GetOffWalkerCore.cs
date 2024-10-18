using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace WalkerGear
{
    //WG_GetOffWalkerCore;
    public class JobDriver_GetOffWalkerCore : JobDriver
    {
        private const TargetIndex maintenanceBay = TargetIndex.A;
        private Building_MaintenanceBay Building => (Building_MaintenanceBay)job.GetTarget(TargetIndex.A).Thing;
        private const int wait = 200;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return this.pawn.Reserve(this.job.GetTarget(maintenanceBay), this.job, errorOnFailed: errorOnFailed);
        }

        //还在写
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(maintenanceBay);
            yield return Toils_Goto.GotoThing(maintenanceBay, PathEndMode.InteractionCell);
            yield return Toils_General.WaitWith(maintenanceBay, wait, true, true, face: TargetIndex.A);
            Toil gearDown = new()
            {
                initAction = () =>
                {
                    Pawn actor = this.pawn;
                    Building.GearDown(actor);
                    actor.drafter.Drafted = false;//自動解除徵招
                }
            };
            yield return gearDown;
        }
    }
}
