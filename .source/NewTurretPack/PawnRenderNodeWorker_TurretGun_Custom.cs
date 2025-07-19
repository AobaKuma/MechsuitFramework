using UnityEngine;
using Verse;

namespace Exosuit
{
    public class PawnRenderNodeProperties_TurretGun_Custom : PawnRenderNodeProperties
    {
        public PawnRenderNodeProperties_TurretGun_Custom()
        {
            nodeClass = typeof(PawnRenderNode_TurretGun_Custom);
            workerClass = typeof(PawnRenderNodeWorker_TurretGun_Custom);
        }
    }

    public class PawnRenderNodeWorker_TurretGun_Custom : PawnRenderNodeWorker
    {
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            if (!base.CanDrawNow(node, parms))
            {
                return false;
            }
            var pawnRenderNode = node as PawnRenderNode_TurretGun_Custom;
            var pawnRenderNodeProperties = node.Props as PawnRenderNodeProperties_TurretGun_Custom;
            pawnRenderNode.turretComp = pawnRenderNode.apparel.TryGetComp<CompTurretGun_Custom>();
            if (pawnRenderNode.turretComp != null)
            {
                return true;
            }
            return false;
        }

        public override Quaternion RotationFor(PawnRenderNode node, PawnDrawParms parms)
        {
            var pawnRenderNode = node as PawnRenderNode_TurretGun_Custom;
            var pawnRenderNodeProperties = node.Props as PawnRenderNodeProperties_TurretGun_Custom;

            pawnRenderNode.turretComp = pawnRenderNode.apparel.TryGetComp<CompTurretGun_Custom>();
            Pawn pawnOwner = pawnRenderNode.turretComp.PawnOwner;
            if (pawnRenderNode != null && pawnRenderNode.turretComp.currentTarget != null)
            {
                var offset = (parms.facing == Rot4.West) ? -180 : 0;
                return (offset + pawnRenderNode.turretComp.curRotation).ToQuat();
            }
            return pawnRenderNodeProperties.drawData.RotationOffsetForRot(parms.facing).ToQuat();
        }

        public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
        {
            var pawnRenderNode = node as PawnRenderNode_TurretGun_Custom;
            var pawnRenderNodeProperties = node.Props as PawnRenderNodeProperties_TurretGun_Custom;
            pawnRenderNode.turretComp = pawnRenderNode.apparel.TryGetComp<CompTurretGun_Custom>();
            if (pawnRenderNode != null && pawnRenderNode.turretComp != null)
            {
                return base.OffsetFor(node, parms, out pivot);
            }
            return base.OffsetFor(node, parms, out pivot);
        }
    }
}