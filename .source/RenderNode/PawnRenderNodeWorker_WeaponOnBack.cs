using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Exosuit
{
    public class PawnRenderNodeWorker_WeaponOnBack:PawnRenderNodeWorker
    {
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            
            return base.CanDrawNow(node, parms)&&false;
        }
    }
}
