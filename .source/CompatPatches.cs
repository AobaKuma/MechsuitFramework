using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Exosuit
{
    [StaticConstructorOnStartup]
    [HarmonyPatchCategory("ModPatches")]
    static class CompatPatches
    {
        static List<(string mod,Action<Harmony> action)> patches = [
            ("nals.facialanimation",
            ins=> ins.Patch(AccessTools.PropertyGetter("FacialAnimation.DrawFaceGraphicsComp:EnableDrawing"),postfix:typeof(CompatPatches).Method("FacialAnimationIgnoreDummy"))),
            ("usagirei.pocketsand",
            ins=>ins.Patch(AccessTools.Method("PocketSand.Patches.Pawn_GetGizmos:IsValidTarget"), postfix: typeof(CompatPatches).Method(nameof(IsValidTarget)))),
            ("issaczhuang.ce.easyswitchweapon",ins=>ins.Patch(AccessTools.Method("EesySwitchWeapon.CompEasySwitchWeapon:EquipWeapon"),prefix:typeof(CompatPatches).Method(nameof(EquipWeapon))))
            ];
        static CompatPatches()
        {
            var ins = ExosuitMod.instance;
            //Log.Warning("Compating");
            foreach (var (mod, action) in patches)
            {
                try
                {
                    if(ModLister.GetActiveModWithIdentifier(mod,true)!=null)
                        action(ins);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex.Message+ ex.StackTrace);
                    
                }
            }

        }
        [HarmonyPostfix]
        [HarmonyPatch("DrawFaceGraphicsComp", "get_EnableDrawing")]
        static void FacialAnimationIgnoreDummy(ref bool __result,Pawn ___pawn)
        {
            if (__result && ___pawn.def == ThingDefOf.Dummy)
            {
                __result = false;
            }
            
        }
        [HarmonyPatch("PocketSand.Patches.Pawn_GetGizmos:IsValidTarget")]
        [HarmonyPostfix]
        internal static void IsValidTarget(ref bool __result, Pawn pawn)
        {
            if (__result)
            {
                __result = !pawn.TryGetExosuitCore(out _);
            }
        }

        [HarmonyPatch("EesySwitchWeapon.CompEasySwitchWeapon:EquipWeapon")]
        [HarmonyPrefix]
        internal static bool EquipWeapon(ThingComp __instance)
        {
            return __instance.parent is not Pawn p || !p.equipment.Primary.HasComp<CompApparelForcedWeapon>();
        }
        
    }
}
