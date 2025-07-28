using RimWorld;
using System.Collections.Generic;
using System;
using Verse;
using Verse.AI;

namespace Exosuit
{
    public class JobDriver_StandAndRest : JobDriver_Wait
    {


        public override IEnumerable<Toil> MakeNewToils()
        {
            Toil toil = StandAndRest();
            toil.initAction += delegate
            {
                Map.pawnDestinationReservationManager.Reserve(pawn, job, pawn.Position);
            };
            toil.tickIntervalAction += delegate
            {
                if (job.expiryInterval == -1 && job.def == RimWorld.JobDefOf.Wait_Combat && !pawn.Drafted)
                {
                    Log.Error(string.Concat(pawn, " in eternal WaitCombat without being drafted."));
                    ReadyForNextToil();
                }
                else
                {
                    if (job.forceSleep)
                    {
                        asleep = true;
                    }
                }
            };
            DecorateWaitToil(toil);
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            if (job.overrideFacing != Rot4.Invalid)
            {
                toil.handlingFacing = true;
                toil.tickIntervalAction += delegate
                {
                    pawn.rotationTracker.FaceTarget(pawn.Position + job.overrideFacing.FacingCell);
                };
            }
            else if (pawn.mindState != null && pawn.mindState.duty != null && pawn.mindState.duty.focus != null && job.def != RimWorld.JobDefOf.Wait_Combat)
            {
                LocalTargetInfo focusLocal = pawn.mindState.duty.focus;
                toil.handlingFacing = true;
                toil.tickIntervalAction += delegate
                {
                    pawn.rotationTracker.FaceTarget(focusLocal);
                };
            }
            toil.AddFinishAction(delegate
            {
                toil.actor.needs.mood.thoughts.memories.TryGainMemoryFast(ThoughtDefOf.SleptInMechGear);
            });
            yield return toil;
        }
        public static Toil StandAndRest()
        {
            Toil standAndRest = ToilMaker.MakeToil("StandAndRest");
            standAndRest.initAction = delegate
            {
                Pawn wearer = standAndRest.actor;
                wearer.pather?.StopDead();
                wearer.jobs.posture = PawnPosture.Standing;
                PortraitsCache.SetDirty(wearer);
            };
            standAndRest.tickIntervalAction = (delta)=>
            {
                Pawn actor = standAndRest.actor;
                Job curJob = actor.CurJob;
                JobDriver curDriver = actor.jobs.curDriver;
                curDriver.asleep = true;
                curJob.startInvoluntarySleep = false;

                ApplyBedRelatedEffects(delta,actor, true, true);
                if (actor.needs.rest.CurLevel == actor.needs.rest.MaxLevel || actor.IsHashIntervalTick(60000) )
                {
                    actor.jobs.CheckForJobOverride();
                }
            };
            standAndRest.defaultCompleteMode = ToilCompleteMode.Never;
            return standAndRest;
        }

        private static void ApplyBedRelatedEffects(int delta, Pawn p, bool asleep, bool gainRest)
        {
            p.GainComfortFromCellIfPossible(delta);
            if (asleep && gainRest && p.needs.rest != null)
            {
                p.needs.rest.TickResting(1);
            }

            Thing spawnedParentOrMe = p.SpawnedParentOrMe;
            if (p.IsHashIntervalTick(100, delta) && spawnedParentOrMe != null && !spawnedParentOrMe.Position.Fogged(spawnedParentOrMe.Map))
            {
                if (asleep && !p.IsColonyMech)
                {
                    FleckDef fleckDef = FleckDefOf.SleepZ;
                    float velocitySpeed = 0.42f;
                    if (p.ageTracker.CurLifeStage.developmentalStage == DevelopmentalStage.Baby || p.ageTracker.CurLifeStage.developmentalStage == DevelopmentalStage.Newborn)
                    {
                        fleckDef = FleckDefOf.SleepZ_Tiny;
                        velocitySpeed = 0.25f;
                    }
                    else if (p.ageTracker.CurLifeStage.developmentalStage == DevelopmentalStage.Child)
                    {
                        fleckDef = FleckDefOf.SleepZ_Small;
                        velocitySpeed = 0.33f;
                    }

                    FleckMaker.ThrowMetaIcon(spawnedParentOrMe.Position, spawnedParentOrMe.Map, fleckDef, velocitySpeed);
                }

                if (gainRest && p.health.hediffSet.GetNaturallyHealingInjuredParts().Any())
                {
                    FleckMaker.ThrowMetaIcon(spawnedParentOrMe.Position, spawnedParentOrMe.Map, FleckDefOf.HealingCross);
                }
            }

            if (p.mindState.applyBedThoughtsTick != 0 && p.mindState.applyBedThoughtsTick <= Find.TickManager.TicksGame)
            {
                p.mindState.applyBedThoughtsTick += 60000;
                p.mindState.applyBedThoughtsOnLeave = true;
            }
        }

    }
}
