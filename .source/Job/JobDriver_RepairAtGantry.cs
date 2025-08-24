using RimWorld;
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
            repair.tickIntervalAction = (interval) =>
            {
                Pawn actor = repair.actor;
                if (Gantry.NeedRepair)
                {
                    actor.skills?.Learn(SkillDefOf.Crafting, 0.05f * interval);
                    actor.rotationTracker.FaceTarget(actor.CurJob.GetTarget(TargetIndex.A));

                    worksDone += Math.Max((int)(actor.GetStatValue(StatDefOf.WorkSpeedGlobal) * Gantry.GetStatValue(StatDefOf.WorkTableEfficiencyFactor, true, 1)) * interval, 1);
                    if (worksDone >= workPerRepair) Gantry.Repair(Math.DivRem(worksDone, workPerRepair, out worksDone));
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
}
