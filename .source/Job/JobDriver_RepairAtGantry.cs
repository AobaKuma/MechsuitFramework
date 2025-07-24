using RimWorld;
using RimWorld.Utility;
using Steamworks;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Exosuit
{
    public class JobDriver_RepairAtGantry : JobDriver
    {
        protected int worksDone;
        protected int workPerRepair = 100;
        protected Building_MaintenanceBay Gantry => (Target as Building_MaintenanceBay);
        protected Thing Target
        {
            get
            {
                return job.targetA.Thing;
            }
        }
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return this.pawn.Reserve(this.job.GetTarget(TargetIndex.A), this.job, errorOnFailed: errorOnFailed);
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            Toil repair = ToilMaker.MakeToil("Repair");
            repair.initAction = delegate
            {
                worksDone = 80;
            };
            repair.tickIntervalAction = delegate(int delta)
            {
                Pawn actor = repair.actor;
                if (Gantry.NeedRepair)
                {
                    actor.skills?.Learn(SkillDefOf.Crafting, 0.05f*delta);
                    actor.rotationTracker.FaceTarget(actor.CurJob.GetTarget(TargetIndex.A));

                    worksDone += (int)(actor.GetStatValue(StatDefOf.WorkSpeedGlobal) * Gantry.GetStatValue(StatDefOf.WorkTableEfficiencyFactor, true, 1) * delta);
                    if (worksDone>workPerRepair)
                    {
                        Gantry.Repair(Math.DivRem(worksDone, workPerRepair, out worksDone));
                    }
                    return;
                }
                actor.records?.Increment(RecordDefOf.ThingsRepaired);
                actor.jobs?.EndCurrentJob(JobCondition.Succeeded);
            };
            repair.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            repair.WithEffect(EffecterDefOf.ConstructMetal, TargetIndex.A);
            repair.defaultCompleteMode = ToilCompleteMode.Never;
            repair.activeSkill = () => SkillDefOf.Crafting;
            repair.handlingFacing = true;
            yield return repair;
        }
    }

    public class JobDriver_LoadAmmoAtGantry:JobDriver
    {
        Building_MaintenanceBay Bay => TargetA.Thing as Building_MaintenanceBay;
        TargetIndex BayInd => TargetIndex.A;
        TargetIndex AmmoInd => TargetIndex.B;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {

            pawn.ReserveAsManyAsPossible(job.GetTargetQueue(AmmoInd), job, 1);
            return pawn.Reserve(job.GetTarget(BayInd), job, 1);
        }
        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            IReloadableComp reloadable = Bay.GetFirstNeedReload();
            this.FailOn(()=>reloadable==null|| Bay.Core==null||!reloadable.NeedsReload(true));
            this.AddFinishAction(jc =>
            {
                if (!pawn.carryTracker.CarriedThing.DestroyedOrNull())
                {
                    pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out var _);
                }
            });
            var checkHand = ToilMaker.MakeToil("CheckCarriedThing");
            checkHand.initAction = delegate
            {
                if (!pawn.carryTracker.CarriedThing.DestroyedOrNull()&&reloadable.AmmoDef!= pawn.carryTracker.CarriedThing.def)
                {
                    pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out var _);
                }
            };
            yield return checkHand;


            Toil LoadAmmo = Toils_General.Label();

            //手上没有弹药就去找
            Toil FindAmmo = Toils_General.Label();
            yield return FindAmmo;
            yield return Toils_Jump.JumpIf(LoadAmmo, () => job.GetTargetQueue(AmmoInd).NullOrEmpty() || pawn.carryTracker.Full || pawn.carryTracker.CarriedCount(reloadable.AmmoDef)>=reloadable.MaxAmmoNeeded(true));
            yield return Toils_JobTransforms.ExtractNextTargetFromQueue(AmmoInd);
            yield return Toils_Goto.GotoThing(AmmoInd, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(AmmoInd).FailOnSomeonePhysicallyInteracting(AmmoInd);
            yield return Toils_Haul.StartCarryThing(AmmoInd, putRemainderInQueue: false, subtractNumTakenFromJobCount: true).FailOnDestroyedNullOrForbidden(AmmoInd);
            
            yield return LoadAmmo;
            LoadAmmo.AddFailCondition(() => pawn.carryTracker.CarriedThing.DestroyedOrNull());
            //有就去维护坞
            yield return Toils_Goto.GotoThing(BayInd,peMode:PathEndMode.Touch);
            yield return Toils_General.WaitWith(BayInd, reloadable.BaseReloadTicks >> 2, true, true, face: BayInd);
            Toil load = ToilMaker.MakeToil("LoadAmmo");
            load.initAction = delegate
            {
                reloadable.ReloadFrom(pawn.carryTracker.CarriedThing);
            };
            load.defaultCompleteMode = ToilCompleteMode.Instant;
            load.AddEndCondition(() => {
                if (!reloadable.NeedsReload(true))
                {
                    return JobCondition.Succeeded;
                }
                return JobCondition.Ongoing;
            });
            //装弹
            yield return load;
            yield return Toils_Jump.Jump(FindAmmo);

        }

    }
}
