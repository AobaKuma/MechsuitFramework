using UnityEngine;
using Verse;

namespace WalkerGear
{
    public class PawnRenderNode_ApparelColor : PawnRenderNode
    {
        public PawnRenderNode_ApparelColor(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree) : base(pawn, props, tree)
        {
        }
        public override Color ColorFor(Pawn pawn)
        {
            return this.apparel.DrawColor;
        }
    }
}
