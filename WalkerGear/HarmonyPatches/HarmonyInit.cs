using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static HarmonyLib.AccessTools;

namespace WalkerGear
{
    public class WalkerGear : Mod
    {
        public WalkerGear(ModContentPack content) : base(content)
        {
            var ins = new Harmony("WalkerGear");
            ins.PatchAllUncategorized();
            if (ModLister.GetActiveModWithIdentifier("petetimessix.simplesidearms", true) != null)
            {
                ins.Patch(Method(TypeByName("WeaponAssingment"), "SetPrimary"), prefix: Method(typeof(SimpleSidearms), nameof(SimpleSidearms.SetPrimary)));
            }
            if (ModLister.GetActiveModWithIdentifier("usagirei.pocketsand", true)!=null)
            {
                ins.Patch(Method("PocketSand.Patches.Pawn_GetGizmos:IsValidTarget"), postfix: typeof(PocketSand).Method(nameof(PocketSand.IsValidTarget)));
            }
            if (ModLister.GetActiveModWithIdentifier("issaczhuang.ce.easyswitchweapon",true)!=null)
            {
                ins.Patch(Method("EesySwitchWeapon.CompEasySwitchWeapon:EquipWeapon"),prefix:typeof(EasySwitchWeapon).Method(nameof(EasySwitchWeapon.EquipWeapon)));
            }
        }
        [HarmonyPatchCategory("ModPatches")]
        internal static class SimpleSidearms
        {
            [HarmonyPrefix]
            internal static bool SetPrimary(Pawn pawn,ref bool __result)
            {
                return pawn.equipment.Primary == null || !pawn.equipment.Primary.HasComp<CompApparelForcedWeapon>() || (__result = false);
            }
        }

        [HarmonyPatchCategory("ModPatches")]
        internal static class PocketSand
        {
            internal static void IsValidTarget(ref bool __result, Pawn pawn)
            {
                if (__result)
                {
                    __result = !pawn.GetWalkerCore(out _);
                }
                
            }
        }

        [HarmonyPatchCategory("ModPatches")]
        internal static class EasySwitchWeapon
        {
            internal static bool EquipWeapon(ThingComp __instance)
            {
                return __instance.parent is not Pawn p|| !p.equipment.Primary.HasComp<CompApparelForcedWeapon>();
            }
        }
    }
}
