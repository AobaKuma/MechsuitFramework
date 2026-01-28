// 当白昼倾坠之时
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Mechsuit
{
    public class PawnRenderNodeProperties_TurretGun : PawnRenderNodeProperties
    {
        public PawnRenderNodeProperties_TurretGun()
        {
            nodeClass = typeof(PawnRenderNode_TurretGun);
            workerClass = typeof(PawnRenderNodeWorker_TurretGun);
        }
    }

    public class PawnRenderNodeWorker_TurretGun : PawnRenderNodeWorker
    {
        public override void AppendDrawRequests(PawnRenderNode node, PawnDrawParms parms, List<PawnGraphicDrawRequest> requests)
        {
            // 强制为东向。炮塔是 360 度旋转的，不需要（也不应该）根据 Pawn 朝向进行镜像或切换材质面。
            // 使用 East 作为基准面可以确保始终获得侧向贴图，且不受系统 West 镜像干扰。
            parms.facing = Rot4.East;
            base.AppendDrawRequests(node, parms, requests);
        }

        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            if (!base.CanDrawNow(node, parms))
            {
                return false;
            }
            var pawnRenderNode = node as PawnRenderNode_TurretGun;
            var pawnRenderNodeProperties = node.Props as PawnRenderNodeProperties_TurretGun;
            pawnRenderNode.turretComp = pawnRenderNode.apparel.TryGetComp<CompTurretGun>();
            if (pawnRenderNode.turretComp != null)
            {
                return true;
            }
            return false;
        }

        public override Quaternion RotationFor(PawnRenderNode node, PawnDrawParms parms)
        {
            var pawnRenderNode = node as PawnRenderNode_TurretGun;
            if (pawnRenderNode == null) return Quaternion.identity;

            if (pawnRenderNode.turretComp == null)
            {
                pawnRenderNode.turretComp = pawnRenderNode.apparel?.TryGetComp<CompTurretGun>();
            }
            
            float angle;
            if (pawnRenderNode.turretComp != null)
            {
                // 如果当前没有瞄准目标且小人处于非激活状态(如正在龙门架上展示)，强制指向面向
                if (!pawnRenderNode.turretComp.isAiming && !parms.pawn.Spawned)
                {
                    angle = parms.facing.AsAngle + pawnRenderNode.turretComp.Props.angleOffset - 90f;
                }
                else
                {
                    angle = pawnRenderNode.turretComp.curRotation;
                }
            }
            else
            {
                var pawnRenderNodeProperties = node.Props as PawnRenderNodeProperties_TurretGun;
                return pawnRenderNodeProperties?.drawData?.RotationOffsetForRot(parms.facing).ToQuat() ?? Quaternion.identity;
            }

            // 炮塔独立旋转 不需要镜像修正
            return angle.ToQuat();
        }

        public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
        {
            Vector3 offset = base.OffsetFor(node, parms, out pivot);
            offset.z -= 0.5f;
            return offset;
        }
    }
}