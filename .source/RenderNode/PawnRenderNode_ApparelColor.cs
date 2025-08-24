using UnityEngine;
using Verse;

namespace Exosuit
{
    public class PawnRenderNode_ApparelColor(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree) : PawnRenderNode(pawn, props, tree)
  {
        public override Color ColorFor(Pawn pawn)
        {
            return apparel.DrawColor;
        }
    }
}
