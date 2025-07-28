using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Exosuit
{
    public class PawnRenderNodeProperties_ApparelWeaponHolder : PawnRenderNodeProperties
    {
        public PawnRenderNodeProperties_ApparelWeaponHolder()
        {
            nodeClass = typeof(PawnRenderNode_ApparelWeaponHolder);
            workerClass = typeof(PawnRenderNodeWorker_ApparelWeaponHolder);
        }
    }
    public class PawnRenderNode_ApparelWeaponHolder : PawnRenderNode_Apparel
    {
        public ThingWithComps weapon;
        public PawnRenderNode_ApparelWeaponHolder(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree,Apparel ap) : base(pawn, props, tree,ap)
        {
            weapon = ap.GetComp<CompModuleWeapon>()?.Weapon;
        }
        
        public override IEnumerable<Graphic> GraphicsFor(Pawn pawn)
        {
            if (weapon != null) 
                yield return weapon.Graphic;
        }
        public override Graphic GraphicFor(Pawn pawn)
        {
            if (weapon != null)
                return weapon.Graphic;
            return null;
        }
    }

    public class PawnRenderNodeWorker_ApparelWeaponHolder: PawnRenderNodeWorker
    {
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            if(!base.CanDrawNow(node, parms)) return false;

            if (node is not PawnRenderNode_ApparelWeaponHolder nodeHolder) return false;
            if (nodeHolder.weapon == null) return false;
            //在架子上时挂武器
            if (!parms.pawn.Spawned) return true;
            //在非征召状态挂武器
            if (!parms.pawn.Drafted) return true;
            //主武器不是时挂武器
            if (nodeHolder.weapon != parms.pawn.equipment.Primary) return true;
            

            
            return false;
        }
    }
}
