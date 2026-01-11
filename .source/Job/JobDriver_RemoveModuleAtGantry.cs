using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Exosuit
{
    // 从整备架上卸载模块的工作驱动
    public class JobDriver_RemoveModuleAtGantry : JobDriver
    {
        #region 常量

        private const int WorkPerModule = 150;

        #endregion

        #region 属性

        private TargetIndex GantryInd => TargetIndex.A;
        
        // 使用 job.targetQueueB 存储要卸载的 SlotDef 名称（通过 job.count 传递 slot 的 uiPriority）
        private Building_MaintenanceBay Gantry => job.GetTarget(GantryInd).Thing as Building_MaintenanceBay;
        private SlotDef TargetSlot => Gantry?.GetPendingRemoveSlot();

        #endregion

        #region 字段

        private int workDone;

        #endregion

        #region 公共方法

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.GetTarget(GantryInd), job, errorOnFailed: errorOnFailed);
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(GantryInd);

            // 去整备架
            yield return Toils_Goto.GotoThing(GantryInd, PathEndMode.InteractionCell)
                .FailOnDespawnedNullOrForbidden(GantryInd);

            // 卸载模块
            Toil removeToil = ToilMaker.MakeToil("RemoveModule");
            removeToil.initAction = delegate
            {
                workDone = 0;
                if (TargetSlot == null)
                {
                    pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                }
            };
            removeToil.tickIntervalAction = (interval) =>
            {
                Pawn actor = removeToil.actor;
                
                if (TargetSlot == null)
                {
                    actor.jobs.EndCurrentJob(JobCondition.Succeeded);
                    return;
                }

                actor.rotationTracker.FaceTarget(actor.CurJob.GetTarget(GantryInd));
                actor.skills?.Learn(SkillDefOf.Crafting, 0.05f * interval);

                float workSpeed = actor.GetStatValue(StatDefOf.WorkSpeedGlobal);
                float efficiency = Gantry.GetStatValue(StatDefOf.WorkTableEfficiencyFactor, true, 1);
                workDone += Math.Max((int)(workSpeed * efficiency * interval), 1);

                if (workDone >= WorkPerModule)
                {
                    // 完成卸载
                    Gantry.CompleteRemoveModule();
                    actor.jobs.EndCurrentJob(JobCondition.Succeeded);
                }
            };
            removeToil.FailOnCannotTouch(GantryInd, PathEndMode.InteractionCell);
            removeToil.WithEffect(EffecterDefOf.ConstructMetal, GantryInd);
            removeToil.defaultCompleteMode = ToilCompleteMode.Never;
            removeToil.activeSkill = () => SkillDefOf.Crafting;
            removeToil.handlingFacing = true;
            yield return removeToil;
        }

        #endregion
    }
}
