using RimWorld;
using Unity.Jobs;
using Verse;
using Verse.AI;
using static HarmonyLib.Code;

namespace WalkerGear
{
    public class CompProperties_SleepCover : CompProperties
    {
        public CompProperties_SleepCover()
        {
            this.compClass = typeof(CompSleepCover);
        }

    }
    public class CompSleepCover : ThingComp
    {
        public CompProperties_SleepCover Props => (CompProperties_SleepCover)props;

        public bool isSleeping = false;
        public Apparel Parent => (Apparel)this.parent;

        /// <summary>
        /// 睡眠情况下取消睡眠并走站着睡的job
        /// </summary>

        public override void CompTick()
        {
            base.CompTick();
            if (Parent.Wearer != null&&Parent.Wearer.apparel.WornApparel.Any(o=>o.def.defName.Contains("Helmet")))
            {
                if ((Parent.Wearer.CurJobDef == RimWorld.JobDefOf.Wait_Asleep || Parent.Wearer.CurJobDef == RimWorld.JobDefOf.LayDown)&& !isSleeping)
                {
                    Job jobA = JobMaker.MakeJob(JobDefOf.WG_SleepInWalkerCore);
                    Parent.Wearer.jobs.TryTakeOrderedJob(jobA);
                }
                base.CompTick();
            }

        }
        public static Toil StandAndRest()
        {
            Toil standAndRest = ToilMaker.MakeToil("StandAndRest");
            Pawn wearer=new Pawn();
            standAndRest.initAction = delegate
            {
                Pawn wearer = standAndRest.actor;
                wearer.pather?.StopDead();
                wearer.jobs.posture = PawnPosture.Standing;
                PortraitsCache.SetDirty(wearer);
            };
            standAndRest.tickAction = delegate
            {
                Pawn actor = standAndRest.actor;
                Job curJob = actor.CurJob;
                JobDriver curDriver = actor.jobs.curDriver;
                curDriver.asleep = true;
                curJob.startInvoluntarySleep = false;

                ApplyBedRelatedEffects(actor, true, true);
                if (actor.IsHashIntervalTick(60000) || actor.needs.rest.CurLevel == actor.needs.rest.MaxLevel)
                {
                    actor.jobs.CheckForJobOverride();
                }
            };
            standAndRest.defaultCompleteMode = ToilCompleteMode.Never;
            standAndRest.AddFinishAction(delegate
            {
                
            });
            return standAndRest;
        }

        private static void ApplyBedRelatedEffects(Pawn p, bool asleep, bool gainRest)
        {
            p.GainComfortFromCellIfPossible();
            if (asleep && gainRest && p.needs.rest != null)
            {
                float restEffectiveness = 1;
                p.needs.rest.TickResting(restEffectiveness);
            }

            Thing spawnedParentOrMe;
            if (p.IsHashIntervalTick(100) && (spawnedParentOrMe = p.SpawnedParentOrMe) != null && !spawnedParentOrMe.Position.Fogged(spawnedParentOrMe.Map))
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

            bool flag = false;
        }

    }
}
