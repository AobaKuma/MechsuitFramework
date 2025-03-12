using Verse;

namespace WalkerGear
{
    public class PawnRenderNode_TurretGun_Custom : PawnRenderNode
    {
        public CompTurretGun_Custom turretComp;

        public PawnRenderNode_TurretGun_Custom(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
            : base(pawn, props, tree)
        {
        }

        public override Graphic GraphicFor(Pawn pawn)
        {
            if (base.Props.texPath != null)
            {
                return GraphicDatabase.Get<Graphic_Multi>(base.Props.texPath, ShaderDatabase.Cutout);
            }
            return GraphicDatabase.Get<Graphic_Single>(turretComp.Props.turretDef.graphicData.texPath, ShaderDatabase.Cutout);
        }
    }
}