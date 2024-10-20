using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using static UnityEngine.GraphicsBuffer;

namespace WalkerGear
{
    //WG_GetOffWalkerCore;
    public class JobDriver_GetOffWalkerCore : JobDriver
    {
        protected const TargetIndex maintenanceBay = TargetIndex.A;
        protected Building_MaintenanceBay Bay => (Building_MaintenanceBay)job.GetTarget(TargetIndex.A).Thing;
        protected int Wait => (int)(200 + 200 * (1 - Bay.GetStatValue(StatDefOf.WorkTableWorkSpeedFactor, true)));

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return this.pawn.Reserve(this.job.GetTarget(maintenanceBay), this.job, errorOnFailed: errorOnFailed);
        }

        //还在写
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(maintenanceBay);
            yield return Toils_Goto.GotoThing(maintenanceBay, Bay.Position);
            Toil toilWait = Toils_General.WaitWith(maintenanceBay, Wait, true);
            toilWait.tickAction = () =>
            {
                Log.Message(Bay.Rotation);
                GetActor().rotationTracker.FaceCell(Bay.Position + Bay.rotationInt.FacingCell);
            };
            yield return toilWait;
            Toil gearDown = new()
            {
                initAction = () =>
                {
                    Pawn actor = this.pawn;
                    Bay.GearDown(actor);
                    actor.drafter.Drafted = false;//自動解除徵招
                }
            };
            yield return gearDown;
        }
    }
}
