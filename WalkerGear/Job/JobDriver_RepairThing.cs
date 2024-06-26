using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace WalkerGear
{
    public class JobDriver_RepairThing : JobDriver
    {
        protected float ticksToNextRepair;
        protected Thing Target
        {
            get
            {
                return job.targetA.Thing;
            }
        }
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return ReservationUtility.Reserve(this.pawn, this.Target, this.job, 1, -1, null, errorOnFailed);
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
                actor.skills?.Learn(SkillDefOf.Construction, 0.05f);
                actor.rotationTracker.FaceTarget(actor.CurJob.GetTarget(TargetIndex.A));

                float num = actor.GetStatValue(StatDefOf.WorkSpeedGlobal) * 1.7f;
                ticksToNextRepair -= num;
                if (ticksToNextRepair <= 0f)
                {
                    ticksToNextRepair += 20f;
                    base.TargetThingA.HitPoints++;
                    base.TargetThingA.HitPoints = Mathf.Min(base.TargetThingA.HitPoints, base.TargetThingA.MaxHitPoints);
                    if (base.TargetThingA.HitPoints == base.TargetThingA.MaxHitPoints)
                    {
                        actor.records.Increment(RecordDefOf.ThingsRepaired);
                        actor.jobs.EndCurrentJob(JobCondition.Succeeded);
                    }
                }
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
