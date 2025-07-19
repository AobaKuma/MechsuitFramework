using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Verse;

namespace Exosuit
{
    [HarmonyPatch]
    public static class PawnRender_Patch
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(PawnRenderTree), "AdjustParms");
        }
        [HarmonyPostfix()]
        public static void AdjustParms_Patch(ref PawnDrawParms parms)
        {
            if (parms.pawn == null) return;
            if (Building_MaintenanceBay.PawnInBuilding(parms.pawn))
            {
                parms.skipFlags |= MiscDefOf.Head | MiscDefOf.Body;
                return;
            }
            if (!parms.flags.HasFlag(PawnRenderFlags.StylingStation) 
                || !parms.pawn.PawnWearingExosuitCore()) return;
            if (Find.WindowStack.currentlyDrawnWindow is not Dialog_StylingStation dialog_Styling)
                return;

            parms.skipFlags |= MiscDefOf.WGRoot;



        }
        
    }
    /*    [HarmonyPatch]
        static class StylingStation_Patch
        {
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(Dialog_StylingStation), "DrawBottomButtons")]
            static IEnumerable<CodeInstruction> NoDyeNeedForExosuit(IEnumerable<CodeInstruction> instructions) => instructions.MethodReplacer(typeof(Dialog_StylingStation).PropertyGetter("DevMode"),typeof(StylingStation_Patch).Method(nameof(DevModeOrExosuit)));
            static bool DevModeOrExosuit(Dialog_StylingStation instance)
            {
                return instance.stylingStation is Building_MaintenanceBay or null;
            }
        }*/

    /// <summary>
    /// 修复画面缩小时pawn显示不全的问题
    /// </summary>
    [HarmonyPatch(typeof(PawnRenderer), "ParallelGetPreRenderResults")]
    [HarmonyPriority(Priority.LowerThanNormal)]
    public static class ParallelGetPreRenderResults_Patch
    {
        public static void Prefix(ref bool disableCache, Pawn ___pawn)
        {
            if (!disableCache && ___pawn.PawnWearingExosuitCore())
            {
                disableCache = true;
            }
        }
    }

}
