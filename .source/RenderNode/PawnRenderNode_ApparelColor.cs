using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Exosuit
{
    public class PawnRenderNode_ApparelColor : Verse.PawnRenderNode_Apparel
    {
        public PawnRenderNode_ApparelColor(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree, Apparel apparel) : base(pawn, props, tree, apparel)
        {
        }
        public PawnRenderNode_ApparelColor(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree, Apparel apparel, bool useHeadMesh) : base(pawn, props, tree, apparel, useHeadMesh)
        {
        }
        protected override IEnumerable<Graphic> GraphicsFor(Pawn pawn)
        {
            if (HasGraphic(tree.pawn))
            {
                yield return GraphicFor(pawn);
            }
            else
            {
                foreach (var graphic in base.GraphicsFor(pawn))
                {
                    yield return graphic;
                }
            }
        }
        public override Color ColorFor(Pawn pawn)
        {
            return this.apparel.DrawColor;
        }
    }
}
