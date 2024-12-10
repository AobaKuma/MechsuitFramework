using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    [HarmonyPatch(typeof(PawnRenderer), "ParallelGetPreRenderResults")]
    [HarmonyPriority(Priority.LowerThanNormal)]
    public static class ParallelGetPreRenderResults_Patch
    {
        public static void Prefix(ref bool disableCache, Pawn ___pawn)
        {
            if (!disableCache && ___pawn.apparel!=null && ___pawn.apparel.LockedApparel.Any(a => a is WalkerGear_Core))
            {
                disableCache = true;
            }
        }
    }
}
