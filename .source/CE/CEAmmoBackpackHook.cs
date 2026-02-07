// 当白昼倾坠之时
using RimWorld;
using Verse;
using CombatExtended;

namespace Exosuit.CE
{
    // CE 弹药背包兼容性钩子实现
    public class CEAmmoBackpackHook : IExosuitGenerationHook
    {
        public bool CheckAndHandleIncompatibility(Pawn pawn, Apparel apparel, out ThingDef fallbackDef)
        {
            fallbackDef = null;
            
            // 检查是否是弹药背包
            var comp = apparel.TryGetComp<CompAmmoBackpack>();
            if (comp == null) return false;

            // 检查武器是否支持
            bool weaponCompatible = false;
            if (pawn.equipment?.Primary != null)
            {
                var ammoUser = pawn.equipment.Primary.TryGetComp<CompAmmoUser>();
                if (ammoUser?.Props?.ammoSet != null)
                {
                    // 使用静态方法检查兼容性
                    weaponCompatible = CompAmmoBackpack.IsAmmoSetCompatible(ammoUser.Props.ammoSet);
                }
            }

            if (!weaponCompatible)
            {
                // 建议替换为货运背包
                fallbackDef = DefDatabase<ThingDef>.GetNamedSilentFail("DMS_Apparel_PackCargo");
                return true; // 返回 true 表示需要替换
            }

            return false;
        }
    }
}
