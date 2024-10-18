using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace WalkerGear
{
    public class JobDriver_RepairAtGantry : JobDriver
    {
        protected float ticksToNextRepair;
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

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            Toil repair = ToilMaker.MakeToil("MakeNewToils");
            repair.initAction = delegate
            {
                ticksToNextRepair = 80f;
            };
            repair.tickAction = delegate
            {
                Pawn actor = repair.actor;
                if (Gantry.NeedRepair)
                {
                    actor.skills?.Learn(SkillDefOf.Crafting, 0.05f);
                    actor.rotationTracker.FaceTarget(actor.CurJob.GetTarget(TargetIndex.A));

                    float num = actor.GetStatValue(StatDefOf.WorkSpeedGlobal) * Gantry.GetStatValue(StatDefOf.WorkTableEfficiencyFactor, true, 1);
                    ticksToNextRepair -= num;
                    if (ticksToNextRepair <= 0f)
                    {
                        ticksToNextRepair += 100f;
                        Gantry.Repair();
                    }
                    return;
                }
                actor.records?.Increment(RecordDefOf.ThingsRepaired);
                actor.jobs?.EndCurrentJob(JobCondition.Succeeded);
            };
            repair.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            repair.WithEffect(EffecterDefOf.MechRepairing, TargetIndex.A);
            repair.defaultCompleteMode = ToilCompleteMode.Never;
            repair.activeSkill = () => SkillDefOf.Crafting;
            repair.handlingFacing = true;
            yield return repair;
        }
    }
}
