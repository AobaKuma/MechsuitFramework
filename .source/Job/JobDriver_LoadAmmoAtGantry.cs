using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Exosuit
{
    public class JobDriver_LoadAmmoAtGantry:JobDriver
    {
        TargetIndex BayInd => TargetIndex.A;
        TargetIndex AmmoInd => TargetIndex.B;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {

            pawn.ReserveAsManyAsPossible(job.GetTargetQueue(AmmoInd), job, 1);
            return pawn.Reserve(job.GetTarget(BayInd), job, 1);
        }
        
        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            
            // 当没有需要装填的组件且 pawn 没有携带弹药时结束工作
            this.FailOn(() => {
                if (job.targetA.Thing is not Building_MaintenanceBay bay) return true;
                // 如果 pawn 正在携带弹药，不要因为 GetFirstNeedReload 为 null 而失败
                // 让 LoadAmmo toil 处理这种情况
                if (pawn.carryTracker.CarriedThing != null) return false;
                return bay.GetFirstNeedReload() == null;
            });
            //需要的弹药量
            //this.job.count
            //弹药物品堆
            //targetQueueB
            //逻辑：从QueueB取物品，装填
            Toil TryFindAmmo = Toils_General.Label();
            yield return TryFindAmmo;
            Toil ExtractAmmoFromQueue = Toils_JobTransforms.ExtractNextTargetFromQueue(TargetIndex.B).EndOnNoTargetInQueue(TargetIndex.B);
            //获取弹药到ind
            yield return ExtractAmmoFromQueue;
            //前往弹药thing
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).EndOnDespawnedOrNull(TargetIndex.B);
            //捡到手里
            Toil PickUpAmmo = ToilMaker.MakeToil("PickingUpAmmo");
            PickUpAmmo.initAction = delegate
            {
                var thing = pawn.CurJob.GetTarget(TargetIndex.B).Thing;
                Toils_Haul.ErrorCheckForCarry(pawn, thing);
                var canpick = pawn.carryTracker.AvailableStackSpace(thing.def);
                canpick = Mathf.Min(canpick, thing.stackCount, job.count);
                thing = thing.SplitOff(canpick);
                if (pawn.IsCarrying())
                {
                    pawn.carryTracker.CarriedThing.TryAbsorbStack(thing, true);
                }
                else
                {
                    pawn.carryTracker.TryStartCarry(thing);
                }
                job.count -= canpick;
            };
            yield return PickUpAmmo;
            yield return Toils_Jump.JumpIf(TryFindAmmo, delegate
            {
                return job.count > 0 && !pawn.carryTracker.Full;
            });
            //前往Bay
            yield return Toils_Goto.GotoBuild(TargetIndex.A);
            //等待并装弹
            yield return Toils_General.Wait(60, TargetIndex.A).WithProgressBarToilDelay(TargetIndex.A);
            var LoadAmmo = ToilMaker.MakeToil("LoadAmmoToBayModule");
            LoadAmmo.initAction = delegate {
                if (job.targetA.Thing is not Building_MaintenanceBay bay) return;
                
                var carriedThing = pawn.carryTracker.CarriedThing;
                if (carriedThing == null) return;
                
                var reloadComp = bay.GetFirstNeedReload();
                if (reloadComp == null)
                {
                    // 没有需要装填的组件，将弹药放到地上
                    pawn.carryTracker.TryDropCarriedThing(bay.InteractionCell, ThingPlaceMode.Near, out _);
                    return;
                }
                
                reloadComp.ReloadFrom(carriedThing);
            };
            yield return LoadAmmo;
            //如果还能装就继续去找弹药
            yield return Toils_Jump.JumpIf(TryFindAmmo, delegate
            {
                return job.count > 0;
            }) ;

        }

    }
}
