using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Exosuit
{
    // 在整备架上安装模块的工作驱动
    public class JobDriver_InstallModuleAtGantry : JobDriver
    {
        #region 常量

        private const int WorkPerModule = 200;

        #endregion

        #region 属性

        private TargetIndex GantryInd => TargetIndex.A;
        private TargetIndex ModuleInd => TargetIndex.B;

        private Building_MaintenanceBay Gantry => job.GetTarget(GantryInd).Thing as Building_MaintenanceBay;
        private Thing Module => job.GetTarget(ModuleInd).Thing;

        #endregion

        #region 字段

        private int workDone;

        #endregion

        #region 公共方法

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(job.GetTarget(GantryInd), job, errorOnFailed: errorOnFailed))
                return false;
            if (!pawn.Reserve(job.GetTarget(ModuleInd), job, errorOnFailed: errorOnFailed))
                return false;
            return true;
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(GantryInd);
            this.FailOnDespawnedNullOrForbidden(ModuleInd);

            // 直接去整备架的交互点
            yield return Toils_Goto.GotoThing(GantryInd, PathEndMode.InteractionCell);

            // 在整备架旁边工作安装模块
            Toil installToil = ToilMaker.MakeToil("InstallModule");
            installToil.initAction = delegate
            {
                workDone = 0;
            };
            installToil.tickIntervalAction = (interval) =>
            {
                Pawn actor = installToil.actor;
                actor.rotationTracker.FaceTarget(actor.CurJob.GetTarget(GantryInd));
                actor.skills?.Learn(SkillDefOf.Crafting, 0.05f * interval);

                float workSpeed = actor.GetStatValue(StatDefOf.WorkSpeedGlobal);
                float efficiency = Gantry.GetStatValue(StatDefOf.WorkTableEfficiencyFactor, true, 1);
                workDone += Math.Max((int)(workSpeed * efficiency * interval), 1);

                if (workDone >= WorkPerModule)
                {
                    // 完成安装 - 直接从储物架消耗模块
                    Thing module = Module;
                    if (module != null && module.Spawned)
                    {
                        Gantry.CompleteInstallModule(module);
                    }
                    actor.jobs.EndCurrentJob(JobCondition.Succeeded);
                }
            };
            installToil.FailOnCannotTouch(GantryInd, PathEndMode.InteractionCell);
            installToil.WithEffect(EffecterDefOf.ConstructMetal, GantryInd);
            installToil.defaultCompleteMode = ToilCompleteMode.Never;
            installToil.activeSkill = () => SkillDefOf.Crafting;
            installToil.handlingFacing = true;
            yield return installToil;
        }

        #endregion
    }
}
