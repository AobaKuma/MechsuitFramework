using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;

namespace Exosuit
{
    public class PawnRenderNodeWorker_InhertHead : PawnRenderNodeWorker
    {
        public override bool ShouldListOnGraph(PawnRenderNode node, PawnDrawParms parms)
        {
            return parms.pawn.HasCore();
        }
        public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
        {
            pivot = PivotFor(node, parms);
            if (MechUtility.HasCore(parms.pawn, out var p) && p.def.GetModExtension<ApparelRenderOffsets>()?.headData != null)
            {
                var ext = p.def.GetModExtension<ApparelRenderOffsets>();
                return ext.headData.OffsetForRot(parms.facing);
            }
            return parms.pawn.Drawer.renderer.BaseHeadOffsetAt(parms.facing);
        }
    }
    public class PawnRenderSubWorker_Offset : PawnRenderSubWorker
    {
        public override void TransformOffset(PawnRenderNode node, PawnDrawParms parms, ref Vector3 offset, ref Vector3 pivot)
        {
            if (MechUtility.HasCore(parms.pawn, out var p) && p.def.GetModExtension<ApparelRenderOffsets>()?.headData != null)
            {
                var ext = p.def.GetModExtension<ApparelRenderOffsets>();
                offset += ext.headData.OffsetForRot(parms.facing) == Vector3.zero ? offset : ext.headData.OffsetForRot(parms.facing);
            }
        }
        //public override void TransformLayer(PawnRenderNode node, PawnDrawParms parms, ref float layer)
        //{
        //    if (MechUtility.HasCore(parms.pawn, out var p) && p.def.GetModExtension<ApparelRenderOffsets>()?.headData != null)
        //    {
        //        var ext = p.def.GetModExtension<ApparelRenderOffsets>();
        //        layer += ext.headData.LayerForRot(parms.facing, layer);
        //    }
        //}
        public override bool CanDrawNowSub(PawnRenderNode node, PawnDrawParms parms)
        {
            if (!MechUtility.HasCore(parms.pawn, out var p)) return base.CanDrawNowSub(node, parms);
            if (p != null && p.def.GetModExtension<ApparelRenderOffsets>()?.headHideFor != null)
            {
                var ext = p.def.GetModExtension<ApparelRenderOffsets>();
                if (!ext.headHideFor.NullOrEmpty() && ext.headHideFor.Contains(parms.facing))
                {
                    return false;
                }
            }
            return base.CanDrawNowSub(node, parms);
        }
    }
    public class PawnRenderSubWorker_OffsetRoot : PawnRenderSubWorker
    {
        public override void TransformOffset(PawnRenderNode node, PawnDrawParms parms, ref Vector3 offset, ref Vector3 pivot)
        {
            if (!MechUtility.HasCore(parms.pawn, out var p)) return;
            var ext = p.def.GetModExtension<ApparelRenderOffsets>();
            if (ext != null && ext.rootData != null)
            {
                offset += ext.rootData.OffsetForRot(parms.facing);
            }
        }
        public override void TransformLayer(PawnRenderNode node, PawnDrawParms parms, ref float layer)
        {
            if (!MechUtility.HasCore(parms.pawn, out var p)) return;
            var ext = p.def.GetModExtension<ApparelRenderOffsets>();
            if (ext != null && ext.rootData != null)
            {
                layer = ext.rootData.LayerForRot(parms.facing, layer);
            }
        }
    }
}