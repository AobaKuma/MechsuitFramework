// 当白昼倾坠之时
using System.Collections.Generic;
using System.Linq;
using CombatExtended;
using HarmonyLib;
using Mechsuit;
using RimWorld;
using Verse;

namespace Exosuit.CE
{
    // CE炮塔弹药Patch
    // 拦截炮塔射击消耗弹药
    [HarmonyPatch]
    public static class CEPatches_Turret
    {
        private const bool DebugLog = false;
        
        private static void Log(string message)
        {
            if (DebugLog)
                Verse.Log.Message($"[TurretAmmo] {message}");
        }
        
        #region 炮塔射击消耗弹药
        
        // 获取炮塔的弹药组件
        public static CompTurretAmmo GetTurretAmmo(Thing weapon)
        {
            if (weapon == null) return null;
            
            // 如果是服装形态的炮塔
            if (weapon is Apparel apparel)
                return apparel.TryGetComp<CompTurretAmmo>();
            
            // 如果是通过gun注册的炮塔
            if (Mechsuit.CompTurretGun.subGunRegistry.TryGetValue(weapon, out var turretComp))
            {
                var parentApparel = turretComp.parent as Apparel;
                return parentApparel?.TryGetComp<CompTurretAmmo>();
            }
            
            return null;
        }
        
        // 获取可用弹药背包
        public static CompAmmoBackpack GetBackpackForTurret(CompTurretAmmo turretAmmo)
        {
            if (turretAmmo == null) 
            {
                Log("GetBackpackForTurret: turretAmmo is null");
                return null;
            }
            if (!turretAmmo.AllowBackpackFeed) 
            {
                Log("GetBackpackForTurret: AllowBackpackFeed is false");
                return null;
            }
            
            var wearer = turretAmmo.Wearer;
            if (wearer == null) 
            {
                Log("GetBackpackForTurret: Wearer is null");
                return null;
            }
            
            var ammoSet = turretAmmo.LinkedAmmoSet;
            if (ammoSet == null) 
            {
                Log("GetBackpackForTurret: LinkedAmmoSet is null");
                return null;
            }
            
            Log($"GetBackpackForTurret: 查找弹药箱, wearer={wearer.Name}, ammoSet={ammoSet.defName}");
            
            var result = CEPatches.GetAmmoBackpackForAmmoSet(wearer, ammoSet);
            Log($"GetBackpackForTurret: 结果={result?.parent.def.defName ?? "null"}");
            return result;
        }
        
        // Patch Notify_ShotFired 拦截炮塔射击
        [HarmonyPatch(typeof(CompAmmoUser), nameof(CompAmmoUser.Notify_ShotFired))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.High)]
        public static bool Notify_ShotFired_Turret_Prefix(CompAmmoUser __instance, int ammoConsumedPerShot)
        {
            var weapon = __instance.parent;
            var turretAmmo = GetTurretAmmo(weapon);
            
            if (turretAmmo == null) return true;
            
            Log($"炮塔射击: weapon={weapon.def.defName}, selectedAmmo={turretAmmo.SelectedAmmo?.defName}");
            
            int toConsume = ammoConsumedPerShot > 0 ? ammoConsumedPerShot : 1;
            
            // 优先从炮塔内部弹仓消耗
            if (turretAmmo.TryConsumeAmmo(toConsume))
            {
                Log($"从炮塔弹仓消耗: 剩余={turretAmmo.CurrentAmmo}");
                return false;
            }
            
            // 如果允许弹药箱供弹，从弹药箱消耗
            if (turretAmmo.AllowBackpackFeed)
            {
                var backpack = GetBackpackForTurret(turretAmmo);
                if (backpack != null && backpack.HasAmmo)
                {
                    // 检查弹药兼容性
                    AmmoDef backpackAmmo = backpack.IsMixMode ? backpack.CurrentFireAmmo : backpack.SelectedAmmo;
                    if (backpackAmmo != null && IsAmmoInSet(__instance, backpackAmmo))
                    {
                        if (backpack.IsMixMode)
                        {
                            if (backpack.TryConsumeMixAmmoWithType(toConsume, out _))
                            {
                                Log($"从弹药箱消耗(混装)");
                                return false;
                            }
                        }
                        else
                        {
                            if (backpack.CurrentAmmoCount >= toConsume)
                            {
                                backpack.CurrentAmmoCount -= toConsume;
                                Log($"从弹药箱消耗: 剩余={backpack.CurrentAmmoCount}");
                                return false;
                            }
                        }
                    }
                }
            }
            
            // 弹药不足
            Log("炮塔弹药不足");
            return true;
        }
        
        #endregion
        
        #region 炮塔弹药检查
        
        // Patch HasAmmo 检查炮塔弹药
        [HarmonyPatch(typeof(CompAmmoUser), nameof(CompAmmoUser.HasAmmo), MethodType.Getter)]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.High)]
        public static void HasAmmo_Turret_Postfix(CompAmmoUser __instance, ref bool __result)
        {
            if (__result) return;
            
            var turretAmmo = GetTurretAmmo(__instance.parent);
            if (turretAmmo == null) return;
            
            // 检查炮塔弹仓
            if (turretAmmo.HasAmmoAvailable && IsAmmoInSet(__instance, turretAmmo.SelectedAmmo))
            {
                __result = true;
                return;
            }
            
            // 检查弹药箱
            if (turretAmmo.AllowBackpackFeed)
            {
                var backpack = GetBackpackForTurret(turretAmmo);
                if (backpack != null && backpack.HasAmmo)
                {
                    AmmoDef backpackAmmo = backpack.IsMixMode ? backpack.CurrentFireAmmo : backpack.SelectedAmmo;
                    if (backpackAmmo != null && IsAmmoInSet(__instance, backpackAmmo))
                    {
                        __result = true;
                    }
                }
            }
        }
        
        // Patch CanBeFiredNow 检查炮塔是否可射击
        [HarmonyPatch(typeof(CompAmmoUser), nameof(CompAmmoUser.CanBeFiredNow), MethodType.Getter)]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.High)]
        public static void CanBeFiredNow_Turret_Postfix(CompAmmoUser __instance, ref bool __result)
        {
            if (__result) return;
            
            var turretAmmo = GetTurretAmmo(__instance.parent);
            if (turretAmmo == null) return;
            
            // 同样的检查逻辑
            if (turretAmmo.HasAmmoAvailable && IsAmmoInSet(__instance, turretAmmo.SelectedAmmo))
            {
                __result = true;
                return;
            }
            
            if (turretAmmo.AllowBackpackFeed)
            {
                var backpack = GetBackpackForTurret(turretAmmo);
                if (backpack != null && backpack.HasAmmo)
                {
                    AmmoDef backpackAmmo = backpack.IsMixMode ? backpack.CurrentFireAmmo : backpack.SelectedAmmo;
                    if (backpackAmmo != null && IsAmmoInSet(__instance, backpackAmmo))
                    {
                        __result = true;
                    }
                }
            }
        }
        
        // Patch EmptyMagazine 炮塔有弹药时弹匣不算空
        [HarmonyPatch(typeof(CompAmmoUser), nameof(CompAmmoUser.EmptyMagazine), MethodType.Getter)]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.High)]
        public static void EmptyMagazine_Turret_Postfix(CompAmmoUser __instance, ref bool __result)
        {
            if (!__result) return;
            
            var turretAmmo = GetTurretAmmo(__instance.parent);
            if (turretAmmo == null) return;
            
            if (turretAmmo.HasAmmoAvailable && IsAmmoInSet(__instance, turretAmmo.SelectedAmmo))
            {
                __result = false;
                return;
            }
            
            if (turretAmmo.AllowBackpackFeed)
            {
                var backpack = GetBackpackForTurret(turretAmmo);
                if (backpack != null && backpack.HasAmmo)
                {
                    AmmoDef backpackAmmo = backpack.IsMixMode ? backpack.CurrentFireAmmo : backpack.SelectedAmmo;
                    if (backpackAmmo != null && IsAmmoInSet(__instance, backpackAmmo))
                    {
                        __result = false;
                    }
                }
            }
        }
        
        // Patch CurMagCount 返回炮塔弹药数量
        [HarmonyPatch(typeof(CompAmmoUser), nameof(CompAmmoUser.CurMagCount), MethodType.Getter)]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.High)]
        public static void CurMagCount_Turret_Postfix(CompAmmoUser __instance, ref int __result)
        {
            if (__result > 0) return;
            
            var turretAmmo = GetTurretAmmo(__instance.parent);
            if (turretAmmo == null) return;
            
            if (turretAmmo.HasAmmoAvailable && IsAmmoInSet(__instance, turretAmmo.SelectedAmmo))
            {
                __result = turretAmmo.CurrentAmmo;
                return;
            }
            
            if (turretAmmo.AllowBackpackFeed)
            {
                var backpack = GetBackpackForTurret(turretAmmo);
                if (backpack != null && backpack.HasAmmo)
                {
                    AmmoDef backpackAmmo = backpack.IsMixMode ? backpack.CurrentFireAmmo : backpack.SelectedAmmo;
                    if (backpackAmmo != null && IsAmmoInSet(__instance, backpackAmmo))
                    {
                        __result = backpack.TotalAmmoCount;
                    }
                }
            }
        }
        
        // Patch TryStartReload 阻止炮塔自动装填
        [HarmonyPatch(typeof(CompAmmoUser), nameof(CompAmmoUser.TryStartReload))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.High)]
        public static bool TryStartReload_Turret_Prefix(CompAmmoUser __instance)
        {
            var turretAmmo = GetTurretAmmo(__instance.parent);
            if (turretAmmo == null) return true;
            
            // 炮塔有弹药时阻止装填动作
            if (turretAmmo.HasAmmoAvailable && IsAmmoInSet(__instance, turretAmmo.SelectedAmmo))
                return false;
            
            if (turretAmmo.AllowBackpackFeed)
            {
                var backpack = GetBackpackForTurret(turretAmmo);
                if (backpack != null && backpack.HasAmmo)
                {
                    AmmoDef backpackAmmo = backpack.IsMixMode ? backpack.CurrentFireAmmo : backpack.SelectedAmmo;
                    if (backpackAmmo != null && IsAmmoInSet(__instance, backpackAmmo))
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }
        
        // Patch TryPrepareShot
        [HarmonyPatch(typeof(CompAmmoUser), nameof(CompAmmoUser.TryPrepareShot))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.High)]
        public static bool TryPrepareShot_Turret_Prefix(CompAmmoUser __instance, ref bool __result)
        {
            var turretAmmo = GetTurretAmmo(__instance.parent);
            if (turretAmmo == null) return true;
            
            AmmoDef ammoToUse = null;
            
            // 优先使用炮塔弹仓
            if (turretAmmo.HasAmmoAvailable && IsAmmoInSet(__instance, turretAmmo.SelectedAmmo))
            {
                ammoToUse = turretAmmo.SelectedAmmo;
            }
            else if (turretAmmo.AllowBackpackFeed)
            {
                var backpack = GetBackpackForTurret(turretAmmo);
                if (backpack != null && backpack.HasAmmo)
                {
                    AmmoDef backpackAmmo = backpack.IsMixMode ? backpack.CurrentFireAmmo : backpack.SelectedAmmo;
                    if (backpackAmmo != null && IsAmmoInSet(__instance, backpackAmmo))
                    {
                        ammoToUse = backpackAmmo;
                    }
                }
            }
            
            if (ammoToUse == null) return true;
            
            // 同步武器弹药类型
            if (__instance.CurrentAmmo != ammoToUse)
                __instance.CurrentAmmo = ammoToUse;
            
            __result = true;
            return false;
        }
        
        // Patch CurAmmoProjectile 返回正确弹丸
        [HarmonyPatch(typeof(CompAmmoUser), nameof(CompAmmoUser.CurAmmoProjectile), MethodType.Getter)]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.High)]
        public static void CurAmmoProjectile_Turret_Postfix(CompAmmoUser __instance, ref ThingDef __result)
        {
            var turretAmmo = GetTurretAmmo(__instance.parent);
            if (turretAmmo == null) return;
            
            AmmoDef targetAmmo = null;
            
            // 优先使用炮塔弹仓
            if (turretAmmo.HasAmmoAvailable && IsAmmoInSet(__instance, turretAmmo.SelectedAmmo))
            {
                targetAmmo = turretAmmo.SelectedAmmo;
            }
            else if (turretAmmo.AllowBackpackFeed)
            {
                var backpack = GetBackpackForTurret(turretAmmo);
                if (backpack != null && backpack.HasAmmo)
                {
                    AmmoDef backpackAmmo = backpack.IsMixMode 
                        ? (backpack.LastConsumedAmmo ?? backpack.CurrentFireAmmo) 
                        : backpack.SelectedAmmo;
                    if (backpackAmmo != null && IsAmmoInSet(__instance, backpackAmmo))
                    {
                        targetAmmo = backpackAmmo;
                    }
                }
            }
            
            if (targetAmmo == null) return;
            
            // 从弹药组找到对应弹丸
            var ammoSet = __instance.Props?.ammoSet;
            if (ammoSet == null) return;
            
            foreach (var link in ammoSet.ammoTypes)
            {
                if (link.ammo == targetAmmo)
                {
                    __result = link.projectile;
                    return;
                }
            }
        }
        
        #endregion
        
        #region 辅助方法
        
        private static bool IsAmmoInSet(CompAmmoUser compAmmo, AmmoDef ammoDef)
        {
            if (ammoDef == null) return false;
            
            var ammoSet = compAmmo.Props?.ammoSet;
            if (ammoSet == null) return false;
            
            foreach (var link in ammoSet.ammoTypes)
            {
                if (link.ammo == ammoDef)
                    return true;
            }
            
            return false;
        }
        
        #endregion
    }
}
