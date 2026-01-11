using System.Collections.Generic;
using System.Linq;
using CombatExtended;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Exosuit.CE
{
    // 有啥用啥模式专用装填JobDriver
    // 一次性从多种弹药类型各抓取一部分，然后一起装填到弹药背包
    public class JobDriver_LoadWildcardAmmo : JobDriver
    {
        #region 常量
        
        private const TargetIndex BayInd = TargetIndex.A;
        private const TargetIndex AmmoInd = TargetIndex.B;
        
        #endregion
        
        #region 属性
        
        private Building_MaintenanceBay Bay => job.GetTarget(BayInd).Thing as Building_MaintenanceBay;
        
        #endregion
        
        #region 重写方法
        
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            pawn.ReserveAsManyAsPossible(job.GetTargetQueue(AmmoInd), job, 1);
            return pawn.Reserve(job.GetTarget(BayInd), job, 1);
        }
        
        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(BayInd);
            
            // 标签：开始抓取弹药循环
            Toil labelStartGrab = Toils_General.Label();
            yield return labelStartGrab;
            
            // 从队列中取出下一个弹药目标
            yield return Toils_JobTransforms.ExtractNextTargetFromQueue(AmmoInd)
                .EndOnNoTargetInQueue(AmmoInd);
            
            // 前往弹药位置
            yield return Toils_Goto.GotoThing(AmmoInd, PathEndMode.ClosestTouch)
                .EndOnDespawnedOrNull(AmmoInd);
            
            // 捡起弹药
            yield return MakePickUpAmmoToil();
            
            // 如果队列中还有弹药且还能携带，继续抓取
            yield return Toils_Jump.JumpIf(labelStartGrab, () => 
                !job.GetTargetQueue(AmmoInd).NullOrEmpty() && 
                !pawn.carryTracker.Full);
            
            // 前往整备架
            yield return Toils_Goto.GotoBuild(BayInd);
            
            // 等待并装填
            yield return Toils_General.Wait(60, BayInd).WithProgressBarToilDelay(BayInd);
            
            // 执行装填
            yield return MakeLoadAmmoToil();
        }
        
        #endregion
        
        #region 私有方法
        
        private Toil MakePickUpAmmoToil()
        {
            var toil = ToilMaker.MakeToil("PickUpWildcardAmmo");
            toil.initAction = delegate
            {
                var thing = job.GetTarget(AmmoInd).Thing;
                if (thing == null || thing.Destroyed) return;
                
                Toils_Haul.ErrorCheckForCarry(pawn, thing);
                
                // 计算可以捡起的数量
                int canPick = pawn.carryTracker.AvailableStackSpace(thing.def);
                canPick = Mathf.Min(canPick, thing.stackCount);
                
                if (canPick <= 0) return;
                
                // 分离并捡起
                var splitOff = thing.SplitOff(canPick);
                
                if (pawn.IsCarrying())
                {
                    // 如果已经在携带同类物品，尝试合并
                    if (pawn.carryTracker.CarriedThing.def == splitOff.def)
                    {
                        pawn.carryTracker.CarriedThing.TryAbsorbStack(splitOff, true);
                    }
                    else
                    {
                        // 不同类型的弹药，先把当前的放到背包
                        var carried = pawn.carryTracker.CarriedThing;
                        pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out _);
                        pawn.inventory.innerContainer.TryAdd(carried);
                        pawn.carryTracker.TryStartCarry(splitOff);
                    }
                }
                else
                {
                    pawn.carryTracker.TryStartCarry(splitOff);
                }
            };
            return toil;
        }
        
        private Toil MakeLoadAmmoToil()
        {
            var toil = ToilMaker.MakeToil("LoadWildcardAmmoToBay");
            toil.initAction = delegate
            {
                if (Bay == null) return;
                
                // 获取弹药背包组件
                var backpack = GetWildcardBackpack();
                if (backpack == null) return;
                
                // 装填手上的弹药
                if (pawn.carryTracker.CarriedThing != null)
                {
                    backpack.ReloadFrom(pawn.carryTracker.CarriedThing);
                }
                
                // 装填背包里的弹药
                var toRemove = new List<Thing>();
                foreach (var item in pawn.inventory.innerContainer)
                {
                    if (item.def is AmmoDef ammoDef && backpack.IsWildcardCompatible(ammoDef))
                    {
                        backpack.ReloadFrom(item);
                        if (item.stackCount <= 0)
                            toRemove.Add(item);
                    }
                }
                
                foreach (var item in toRemove)
                {
                    pawn.inventory.innerContainer.Remove(item);
                }
            };
            return toil;
        }
        
        private CompAmmoBackpack GetWildcardBackpack()
        {
            if (Bay?.Dummy?.apparel == null) return null;
            
            foreach (var apparel in Bay.Dummy.apparel.WornApparel)
            {
                if (apparel is not ThingWithComps twc) continue;
                
                var comp = twc.GetComp<CompAmmoBackpack>();
                if (comp != null && comp.IsMixMode && comp.HasWildcardSlot)
                    return comp;
            }
            return null;
        }
        
        #endregion
    }
}
