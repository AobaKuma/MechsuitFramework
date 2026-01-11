using System.Collections.Generic;
using System.Linq;
using CombatExtended;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Exosuit.CE
{
    // 有啥用啥模式专用WorkGiver
    // 检测弹药背包是否有有啥用啥槽位需要装填，并创建多弹药抓取工作
    public class WorkGiver_LoadWildcardAmmo : WorkGiver_Scanner
    {
        #region 重写属性
        
        public override ThingRequest PotentialWorkThingRequest => 
            ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);
        
        public override PathEndMode PathEndMode => PathEndMode.Touch;
        
        #endregion
        
        #region 重写方法
        
        public override Danger MaxPathDanger(Pawn pawn) => Danger.Deadly;
        
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return pawn.Map.listerBuildings.AllBuildingsColonistOfClass<Building_MaintenanceBay>();
        }
        
        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t is not Building_MaintenanceBay bay) return false;
            if (!pawn.CanReserve(t, ignoreOtherReservations: forced)) return false;
            
            // 检查是否有有啥用啥槽位需要装填
            var backpack = GetWildcardBackpack(bay);
            if (backpack == null) return false;
            if (!backpack.WildcardNeedsReload) return false;
            
            // 检查是否有可用弹药
            var ammoList = FindWildcardAmmo(pawn, bay, backpack);
            return ammoList.Count > 0;
        }
        
        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t is not Building_MaintenanceBay bay) return null;
            
            var backpack = GetWildcardBackpack(bay);
            if (backpack == null) return null;
            if (!backpack.WildcardNeedsReload) return null;
            
            var ammoList = FindWildcardAmmo(pawn, bay, backpack);
            if (ammoList.Count == 0) return null;
            
            var job = JobMaker.MakeJob(JobDefOf_CE.WG_LoadWildcardAmmo, bay);
            job.targetQueueB = ammoList.Select(t => new LocalTargetInfo(t)).ToList();
            job.count = ammoList.Sum(t => t.stackCount);
            
            return job;
        }
        
        #endregion
        
        #region 私有方法
        
        // 获取有有啥用啥槽位的弹药背包
        private CompAmmoBackpack GetWildcardBackpack(Building_MaintenanceBay bay)
        {
            if (bay?.Dummy?.apparel == null) return null;
            
            foreach (var apparel in bay.Dummy.apparel.WornApparel)
            {
                if (apparel is not ThingWithComps twc) continue;
                
                var comp = twc.GetComp<CompAmmoBackpack>();
                if (comp != null && comp.IsMixMode && comp.HasWildcardSlot)
                    return comp;
            }
            return null;
        }
        
        // 查找可用的弹药（多种类型）
        private List<Thing> FindWildcardAmmo(Pawn pawn, Building_MaintenanceBay bay, CompAmmoBackpack backpack)
        {
            var result = new List<Thing>();
            var compatibleTypes = backpack.GetWildcardCompatibleAmmoTypes();
            
            if (compatibleTypes.Count == 0) return result;
            
            int remainingCapacity = backpack.WildcardRemainingCapacity;
            if (remainingCapacity <= 0) return result;
            
            // 计算每种弹药应该抓取的数量（平均分配）
            int perTypeAmount = Mathf.Max(1, remainingCapacity / compatibleTypes.Count);
            
            // 记录每种弹药已找到的数量
            var foundAmounts = new Dictionary<AmmoDef, int>();
            foreach (var ammoDef in compatibleTypes)
                foundAmounts[ammoDef] = 0;
            
            // 在地图上查找弹药
            var map = pawn.Map;
            foreach (var ammoDef in compatibleTypes)
            {
                int needed = perTypeAmount - foundAmounts[ammoDef];
                if (needed <= 0) continue;
                
                foreach (var thing in map.listerThings.ThingsOfDef(ammoDef))
                {
                    if (!thing.Spawned) continue;
                    if (thing.IsForbidden(pawn)) continue;
                    if (!pawn.CanReserve(thing)) continue;
                    
                    int toTake = Mathf.Min(thing.stackCount, needed);
                    result.Add(thing);
                    foundAmounts[ammoDef] += toTake;
                    needed -= toTake;
                    
                    if (needed <= 0) break;
                }
            }
            
            return result;
        }
        
        #endregion
    }
}
