using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace WalkerGear
{
    [HarmonyPatch]
    public static class PawnRender_Patch
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(PawnRenderTree), "AdjustParms");
        }
        [HarmonyPostfix()]
        public static void AdjustParms_Patch(ref PawnDrawParms __0)
        {
            if (__0.pawn == null) return;
            if (Building_MaintenanceBay.PawnInBuilding(__0.pawn))
            {
                __0.skipFlags |= MiscDefOf.Head | MiscDefOf.Body;
            }

        }
    }
}
