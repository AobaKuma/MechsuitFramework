using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;
using System.Configuration;
using Verse.Sound;
using RimWorld.Utility;

namespace WalkerGear
{
    public class DMS_AbilityVerb_QuickJump : Verb_CastAbility
    {
        private float cachedEffectiveRange = -1f;

        public override bool MultiSelect => true;
        public virtual ThingDef JumpFlyerDef => RimWorld.ThingDefOf.PawnFlyer;

        public override float EffectiveRange
        {
            get
            {
                if (cachedEffectiveRange < 0f)
                {
                    if (base.EquipmentSource != null)
                    {
                        cachedEffectiveRange = base.EquipmentSource.GetStatValue(StatDefOf.JumpRange);
                    }
                    else
                    {
                        cachedEffectiveRange = verbProps.range;
                    }
                }

                return cachedEffectiveRange;
            }
        }

        protected override bool TryCastShot()
        {
            if (base.TryCastShot())
            {
                return JumpUtility.DoJump(CasterPawn, currentTarget, base.ReloadableCompSource, verbProps, ability, base.CurrentTarget, JumpFlyerDef);
            }

            return false;
        }

        /// <summary>
        /// 进行跳跃
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="targetMap">目标地图</param>
        /// <param name="actionTarget">目标（行为的）</param>
        /// <param name="currentTarget">目标</param>
        /// <param name="isLanding">是否为着陆</param>
        /// <param name="isToMap">是否为投射到大地图</param>
        /// <returns></returns>
        public static bool DoJump(Pawn pawn, Map targetMap, LocalTargetInfo actionTarget, LocalTargetInfo currentTarget, bool isLanding, bool isToMap)
        {
            pawn.Rotation = Rot4.South;
            IntVec3 position = pawn.Position;
            IntVec3 cell = currentTarget.Cell;
            Map map = pawn.Map;
            bool flag = Find.Selector.IsSelected(pawn);
            DMS_PawnFlyer pawnFlyer = DMS_PawnFlyer.MakeFlyer(ThingDefOf.DMS_PawnFlyer, pawn, cell, actionTarget, targetMap, DefDatabase<EffecterDef>.GetNamed("JumpMechFlightEffect"), SoundDefOf.TabClose, isLanding, isToMap, true);
            pawnFlyer.eBay = targetMap.listerBuildings.allBuildingsColonist.FirstOrDefault(o => o.GetType() == typeof(Building_EjectorBay));

            if (pawnFlyer != null)
            {
                FleckMaker.ThrowDustPuff(position.ToVector3Shifted() + Gen.RandomHorizontalVector(0.5f), map, 2f);
                GenSpawn.Spawn(pawnFlyer, cell, map);
                if (flag)
                {
                    Find.Selector.Select(pawn, playSound: false, forceDesignatorDeselect: false);
                }

                return true;
            }

            return false;
        }
        public static bool DoJump(Pawn pawn, Map targetMap, LocalTargetInfo actionTarget, LocalTargetInfo currentTarget, bool isLanding)
        {
            pawn.Rotation = Rot4.South;
            IntVec3 position = pawn.Position;
            IntVec3 cell = currentTarget.Cell;
            Map map = pawn.Map;
            bool flag = Find.Selector.IsSelected(pawn);
            DMS_PawnFlyer pawnFlyer = DMS_PawnFlyer.MakeFlyer(ThingDefOf.DMS_PawnFlyer, pawn, cell, actionTarget, targetMap, DefDatabase<EffecterDef>.GetNamed("JumpMechFlightEffect"), SoundDefOf.TabClose, isLanding, true);
            if (pawnFlyer != null)
            {
                FleckMaker.ThrowDustPuff(position.ToVector3Shifted() + Gen.RandomHorizontalVector(0.5f), map, 2f);
                GenSpawn.Spawn(pawnFlyer, cell, map);
                if (flag)
                {
                    Find.Selector.Select(pawn, playSound: false, forceDesignatorDeselect: false);
                }

                return true;
            }

            return false;
        }

        public override void OnGUI(LocalTargetInfo target)
        {
            if (CanHitTarget(target) && JumpUtility.ValidJumpTarget(caster.Map, target.Cell))
            {
                base.OnGUI(target);
            }
            else
            {
                GenUI.DrawMouseAttachment(TexCommand.CannotShoot);
            }
        }

        public override void OrderForceTarget(LocalTargetInfo target)
        {
            OrderJump(CasterPawn, target, this, EffectiveRange);
        }
        public static void OrderJump(Pawn pawn, LocalTargetInfo target, Verb verb, float range)
        {
            Map map = pawn.Map;
            IntVec3 intVec = target.Cell;
            Job job = JobMaker.MakeJob(RimWorld.JobDefOf.CastJump, intVec);
            job.verbToUse = verb;
            if (pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc))
            {
                FleckMaker.Static(intVec, map, FleckDefOf.FeedbackGoto);
            }
        }

        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (caster == null)
            {
                return false;
            }

            if (!CanHitTarget(target) || !JumpUtility.ValidJumpTarget(caster.Map, target.Cell))
            {
                return false;
            }

            if (!ReloadableUtility.CanUseConsideringQueuedJobs(CasterPawn, base.EquipmentSource))
            {
                return false;
            }

            return true;
        }

        public override bool CanHitTarget(LocalTargetInfo targ)
        {
            if (verbProps.range <= 0f)
            {
                return true;
            }
            if (caster == null || !caster.Spawned)
            {
                return false;
            }

            if (targ == caster)
            {
                return true;
            }

            return CanHitTargetFrom(caster.Position, targ);
        }

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            return CanHitTargetFrom(CasterPawn, root, targ, EffectiveRange);
        }

        public bool CanHitTargetFrom(Pawn pawn, IntVec3 root, LocalTargetInfo targ, float range)
        {
            float num = range * range;
            IntVec3 cell = targ.Cell;
            if ((float)pawn.Position.DistanceToSquared(cell) <= num)
            {
                return GenSight.LineOfSight(root, cell, pawn.Map);
            }

            return false;
        }

        public override void DrawHighlight(LocalTargetInfo target)
        {
            if (target.IsValid && JumpUtility.ValidJumpTarget(caster.Map, target.Cell))
            {
                GenDraw.DrawTargetHighlightWithLayer(target.CenterVector3, AltitudeLayer.MetaOverlays);

            }

            GenDraw.DrawRadiusRing(caster.Position, EffectiveRange, Color.white);


        }
    }
}
