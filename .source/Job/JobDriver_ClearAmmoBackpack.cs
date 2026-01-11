using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Exosuit
{
    // 清空弹药背包的工作驱动
    // 用于切换弹种时先清空旧弹药
    public class JobDriver_ClearAmmoBackpack : JobDriver
    {
        private const int ClearDuration = 120;
        
        private Building_MaintenanceBay Bay => TargetThingA as Building_MaintenanceBay;
        
        private int ticksSpentDoingRecipeWork;
        
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, errorOnFailed: errorOnFailed);
        }
        
        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            
            // 移动到龙门架交互点
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            
            // 执行清空操作
            var clearToil = ToilMaker.MakeToil("ClearAmmoBackpack");
            clearToil.initAction = () =>
            {
                ticksSpentDoingRecipeWork = 0;
                var clearable = Bay?.GetFirstNeedClearAmmoBackpack();
                if (clearable == null || !clearable.NeedsClear)
                {
                    EndJobWith(JobCondition.Succeeded);
                }
            };
            clearToil.tickAction = () =>
            {
                ticksSpentDoingRecipeWork++;
                if (ticksSpentDoingRecipeWork >= ClearDuration)
                {
                    var clearable = Bay?.GetFirstNeedClearAmmoBackpack();
                    if (clearable != null && clearable.NeedsClear)
                    {
                        // 使用带龙门架参数的方法
                        clearable.EjectCurrentAmmoAt(Bay);
                    }
                    EndJobWith(JobCondition.Succeeded);
                }
            };
            clearToil.handlingFacing = true;
            clearToil.defaultCompleteMode = ToilCompleteMode.Never;
            clearToil.WithProgressBar(TargetIndex.A, () => (float)ticksSpentDoingRecipeWork / ClearDuration);
            yield return clearToil;
        }
        
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksSpentDoingRecipeWork, "ticksSpentDoingRecipeWork", 0);
        }
    }
}
