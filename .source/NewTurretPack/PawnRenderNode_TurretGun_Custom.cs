using Verse;

namespace Exosuit
{
    public class PawnRenderNode_TurretGun_Custom(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree) : PawnRenderNode(pawn, props, tree)
    {
        public CompTurretGun_Custom turretComp;

        public override Graphic GraphicFor(Pawn pawn)
        {
            if (Props.texPath != null)
            {
                return GraphicDatabase.Get<Graphic_Multi>(Props.texPath, ShaderDatabase.Cutout);
            }
            return GraphicDatabase.Get<Graphic_Single>(turretComp.Props.turretDef.graphicData.texPath, ShaderDatabase.Cutout);
        }
    }
}