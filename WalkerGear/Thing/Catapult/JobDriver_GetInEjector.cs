using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace WalkerGear
{
    public class JobDriver_GetInEjector : JobDriver_GetInWalkerCore
    {
        private const TargetIndex maintenanceBay = TargetIndex.A;
        public Building_EjectorBay Ejector => (Building_EjectorBay)job.GetTarget(maintenanceBay).Thing;
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(maintenanceBay);
            yield return Toils_Goto.GotoThing(maintenanceBay, PathEndMode.Touch);
            yield return Toils_General.Wait(200);
            yield return new Toil()
            {
                initAction = () =>
                {
                    Building_EjectorBay ejectorBay = Ejector;
                    pawn.CurJob.Clear();
                    pawn.DeSpawnOrDeselect();
                    ejectorBay.GetDirectlyHeldThings().TryAddOrTransfer(pawn);
                }
            };
        }
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return this.pawn.Reserve(this.job.GetTarget(maintenanceBay), this.job, errorOnFailed: errorOnFailed);
        }
    }
}
