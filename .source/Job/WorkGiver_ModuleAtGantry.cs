using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace Exosuit
{
    // 在整备架上安装/卸载模块的工作给予者
    public class WorkGiver_ModuleAtGantry : WorkGiver_Scanner
    {
        #region 属性

        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);

        public override PathEndMode PathEndMode => PathEndMode.Touch;

        #endregion

        #region 公共方法

        public override Danger MaxPathDanger(Pawn pawn)
        {
            return Danger.Deadly;
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return pawn.Map.listerBuildings.AllBuildingsColonistOfClass<Building_MaintenanceBay>();
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t is not Building_MaintenanceBay bay)
                return false;

            if (!pawn.CanReserve(t, ignoreOtherReservations: forced))
                return false;

            return bay.HasPendingModuleWork();
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t is not Building_MaintenanceBay bay)
                return null;

            // 优先处理卸载请求
            if (bay.HasPendingRemove())
            {
                return JobMaker.MakeJob(JobDefOf.WG_RemoveModuleAtGantry, t);
            }

            // 处理安装请求（只有当没有待卸载的冲突槽位时才能安装）
            var pendingInstall = bay.GetPendingInstall();
            if (pendingInstall != null)
            {
                // 检查是否还有冲突的槽位需要先卸载
                if (bay.HasConflictingPendingRemove(pendingInstall))
                {
                    return null; // 等待卸载完成
                }
                
                // 检查模块是否还存在且可达
                if (pendingInstall.Spawned && pawn.CanReserve(pendingInstall, ignoreOtherReservations: forced))
                {
                    var job = JobMaker.MakeJob(JobDefOf.WG_InstallModuleAtGantry, t, pendingInstall);
                    return job;
                }
                else
                {
                    // 模块不可用，取消这个请求
                    bay.CancelPendingInstall(pendingInstall);
                }
            }

            return null;
        }

        #endregion
    }
}
