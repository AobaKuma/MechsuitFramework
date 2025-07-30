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
            nodeClass = typeof(PawnRenderNode_WeaponHolder);
            workerClass = typeof(PawnRenderNodeWorker_WeaponHolder);
        }
    }
    public class PawnRenderNode_WeaponHolder(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree) : PawnRenderNode(pawn, props, tree)
    {
        private ThingWithComps weapon;

        public ThingWithComps Weapon {
            get {
                return weapon??= apparel?.GetComp<CompModuleWeapon>()?.Weapon;
            } 
        }

        public override IEnumerable<Graphic> GraphicsFor(Pawn pawn)
        {
            if (Weapon != null) 
                yield return Weapon.Graphic;
        }
        public override Graphic GraphicFor(Pawn pawn)
        {
            if (Weapon != null)
                return Weapon.Graphic;
            return null;
        }
    }

    public class PawnRenderNodeWorker_WeaponHolder: PawnRenderNodeWorker
    {
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            if(!base.CanDrawNow(node, parms)) return false;

            if (node is not PawnRenderNode_WeaponHolder nodeHolder) return false;
            if (nodeHolder.Weapon == null) return false;
            //在架子上时挂武器
            if (!parms.pawn.Spawned) return true;
            //在非征召状态挂武器
            if (!parms.pawn.Drafted) return true;
            //主武器不是时挂武器
            if (nodeHolder.Weapon != parms.pawn.equipment.Primary) return true;
            return false;
        }
    }
}
