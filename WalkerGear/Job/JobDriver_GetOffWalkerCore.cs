using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace WalkerGear
{
    //WG_GetOffWalkerCore;
    public class JobDriver_GetOffWalkerCore : JobDriver
    {
        private const TargetIndex maintenanceBay = TargetIndex.A;
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
                    if (actor.CurJob.GetTarget(TargetIndex.A).Thing is Building_MaintenanceBay bay && !bay.HasGearCore)
                    {
                        //actor.Position = bay.Position;
                        bay.GearDown(actor);
                    }
                }
            };
            yield return gearDown;
        }
    }

/*    public class JobDriver_LoadAmmo : JobDriver
    {
        protected Thing Module
        {
            get
            {
                return job.GetTarget(TargetIndex.A).Thing;
            }
        }
        protected CompWalkerComponent WGComp
        {
            get
            {
                return Module.TryGetComp<CompWalkerComponent>();
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            pawn.ReserveAsManyAsPossible(job.GetTargetQueue(TargetIndex.B), job);
            return pawn.Reserve(Module, job, 1, -1, null, errorOnFailed);
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            base.AddEndCondition(delegate
            {
                if (this.WGComp.NeedAmmo)
                {
                    return JobCondition.Ongoing;
                }
                return JobCondition.Succeeded;
            });
            base.AddFailCondition(() => !this.job.playerForced);
            yield return Toils_General.DoAtomic(delegate
            {
                job.count = WGComp.NeedAmmoCount;
            });
            Toil getNextIngredient = Toils_General.Label();
            yield return getNextIngredient;
            yield return Toils_JobTransforms.ExtractNextTargetFromQueue(TargetIndex.B, true);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch, false).FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, true, false, true, false).FailOnDestroyedNullOrForbidden(TargetIndex.B);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch, false);
            Toil findPlaceTarget = Toils_JobTransforms.SetTargetToIngredientPlaceCell(TargetIndex.A, TargetIndex.B, TargetIndex.C);
            yield return findPlaceTarget;
            yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.C, findPlaceTarget, false, false);
            yield return Toils_Jump.JumpIf(getNextIngredient, () => !this.job.GetTargetQueue(TargetIndex.B).NullOrEmpty<LocalTargetInfo>());
            findPlaceTarget = null;
            yield return Toils_General.Wait(240, TargetIndex.None).FailOnDestroyedNullOrForbidden(TargetIndex.A).FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch).WithProgressBarToilDelay(TargetIndex.A, false, -0.5f);

            Toil toil = ToilMaker.MakeToil("FinalizeReloading");
            toil.initAction = delegate ()
            {
                Job curJob = toil.actor.CurJob;
                Thing thing = Module;
                if (curJob.placedThings.NullOrEmpty<ThingCountClass>())
                {
                    WGComp.Refuel(new List<Thing>
                    {
                        curJob.GetTarget(fuelInd).Thing
                    });
                    return;
                }
                thing.TryGetComp<CompRefuelable>().Refuel((from p in toil.actor.CurJob.placedThings
                                                           select p.thing).ToList<Thing>());
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return toil;
            yield return Toils_Refuel.FinalizeRefueling(TargetIndex.A, TargetIndex.None);
        }
    }*/
}
