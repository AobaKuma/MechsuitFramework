// 当白昼倾坠之时
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Mechsuit
{
    public class PawnRenderNode_TurretGun(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree) : PawnRenderNode(pawn, props, tree)
    {
        public CompTurretGun turretComp;

        public override bool FlipGraphic(PawnDrawParms parms) => base.FlipGraphic(parms);

        public override Mesh GetMesh(PawnDrawParms parms)
        {
            if (meshSet == null) return null;
            // 始终使用东向 Mesh，避免 Rot4.West 自动触发的水平镜像
            return meshSet.MeshAt(Rot4.East);
        }

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