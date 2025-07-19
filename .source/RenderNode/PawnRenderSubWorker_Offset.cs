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
            return parms.pawn.apparel.WornApparel.HasCore();
        }
        public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
        {
            pivot = PivotFor(node, parms);
            Apparel p = parms.pawn?.apparel?.WornApparel?.FirstOrDefault(a => a is Exosuit_Core c);
            if (p != null && p.def.HasModExtension<ApparelRenderOffsets>())
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
            Apparel p = parms.pawn?.apparel?.WornApparel?.FirstOrDefault(a => a is Exosuit_Core c);
            if (p != null && p.def.HasModExtension<ApparelRenderOffsets>())
            {
                var ext = p.def.GetModExtension<ApparelRenderOffsets>();
                offset += ext.headData.OffsetForRot(parms.facing) == Vector3.zero ? offset : ext.headData.OffsetForRot(parms.facing);
            }
        }
        public override void TransformLayer(PawnRenderNode node, PawnDrawParms parms, ref float layer)
        {
            Apparel p = parms.pawn?.apparel?.WornApparel?.FirstOrDefault(a => a is Exosuit_Core c);
            if (p != null && p.def.HasModExtension<ApparelRenderOffsets>())
            {
                var ext = p.def.GetModExtension<ApparelRenderOffsets>();
                layer += ext.headData.LayerForRot(parms.facing, layer);
            }
        }
        public override bool CanDrawNowSub(PawnRenderNode node, PawnDrawParms parms)
        {
            Apparel p = parms.pawn?.apparel?.WornApparel?.FirstOrDefault(a => a is Exosuit_Core c);
            if (p != null && p.def.HasModExtension<ApparelRenderOffsets>())
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
            Apparel p = parms.pawn?.apparel?.WornApparel?.FirstOrDefault(a => a is Exosuit_Core c);
            if (p == null) return;
            var ext = p.def.GetModExtension<ApparelRenderOffsets>();
            if(ext!=null)
            {
                offset = ext.rootData.OffsetForRot(parms.facing) == Vector3.zero ? offset : ext.rootData.OffsetForRot(parms.facing);
            }
        }
        public override void TransformLayer(PawnRenderNode node, PawnDrawParms parms, ref float layer)
        {
            Apparel p = parms.pawn?.apparel?.WornApparel?.FirstOrDefault(a => a is Exosuit_Core c);
            if (p == null) return;
            var ext = p.def.GetModExtension<ApparelRenderOffsets>();
            if (ext != null)
            {
                layer = ext.rootData.LayerForRot(parms.facing, layer);
            }

        }
    }
}

