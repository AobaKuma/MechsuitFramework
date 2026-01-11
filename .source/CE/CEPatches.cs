using System.Collections.Generic;
using CombatExtended;
using HarmonyLib;
using Verse;

namespace Exosuit.CE
{
    // CE弹药系统Patch，实现弹链供弹
    [HarmonyPatch]
    public static class CEPatches
    {
        // 调试开关
        private const bool DebugLog = true;
        
        private static void Log(string message)
        {
            if (DebugLog)
                Verse.Log.Message($"[AmmoBackpack] {message}");
        }
        
        #region Notify_ShotFired Patch，开火时从背包消耗弹药
        
        // Patch CompAmmoUser.Notify_ShotFired
        // 弹链供弹：先消耗背包弹药，背包打空后才消耗武器弹匣
        [HarmonyPatch(typeof(CompAmmoUser), nameof(CompAmmoUser.Notify_ShotFired))]
        [HarmonyPrefix]
        public static bool Notify_ShotFired_Prefix(CompAmmoUser __instance, int ammoConsumedPerShot)
        {
            var holder = __instance.Holder;
            if (holder == null) return true;
            
            var backpack = GetAmmoBackpackForWeapon(holder, __instance);
            if (backpack == null) return true;
            
            // 背包没弹药了，让原方法消耗弹匣
            if (!backpack.HasAmmo) return true;
            
            int toConsume = ammoConsumedPerShot > 0 ? ammoConsumedPerShot : 1;
            
            // 混装模式
            if (backpack.IsMixMode)
            {
                // 使用带类型返回的消耗方法（支持有啥用啥模式）
                if (backpack.TryConsumeMixAmmoWithType(toConsume, out var consumedAmmo))
                {
                    // consumedAmmo 已记录到 LastConsumedAmmo，CurAmmoProjectile 会使用它
                    return false;
                }
                return true;
            }
            
            // 普通模式
            if (!IsAmmoCompatible(__instance, backpack)) return true;
            
            if (backpack.CurrentAmmoCount >= toConsume)
            {
                backpack.CurrentAmmoCount -= toConsume;
                return false;
            }
            else if (backpack.CurrentAmmoCount > 0)
            {
                backpack.CurrentAmmoCount = 0;
                return true;
            }
            
            return true;
        }
        
        #endregion
        
        #region HasAmmo/CanBeFiredNow Patch，让武器认为有弹药
        
        // Patch CompAmmoUser.HasAmmo getter
        // 当背包有弹药时返回 true
        [HarmonyPatch(typeof(CompAmmoUser), nameof(CompAmmoUser.HasAmmo), MethodType.Getter)]
        [HarmonyPostfix]
        public static void HasAmmo_Postfix(CompAmmoUser __instance, ref bool __result)
        {
            if (__result) return;
            
            var holder = __instance.Holder;
            if (holder == null) return;
            
            var backpack = GetAmmoBackpackForWeapon(holder, __instance);
            if (backpack == null) return;
            if (!backpack.HasAmmo) return;
            
            // 混装模式检查
            if (backpack.IsMixMode)
            {
                var currentAmmo = backpack.CurrentFireAmmo;
                if (currentAmmo != null && IsAmmoInSet(__instance, currentAmmo))
                {
                    Log($"HasAmmo_Postfix: 混装模式有弹药 {currentAmmo.defName}，返回 true");
                    __result = true;
                }
                return;
            }
            
            // 普通模式检查
            if (IsAmmoCompatible(__instance, backpack))
            {
                __result = true;
            }
        }
        
        // Patch CompAmmoUser.CanBeFiredNow getter
        // 当背包有弹药时可以开火
        [HarmonyPatch(typeof(CompAmmoUser), nameof(CompAmmoUser.CanBeFiredNow), MethodType.Getter)]
        [HarmonyPostfix]
        public static void CanBeFiredNow_Postfix(CompAmmoUser __instance, ref bool __result)
        {
            if (__result) return;
            
            var holder = __instance.Holder;
            if (holder == null) return;
            
            var backpack = GetAmmoBackpackForWeapon(holder, __instance);
            if (backpack == null) return;
            if (!backpack.HasAmmo) return;
            
            // 混装模式检查
            if (backpack.IsMixMode)
            {
                var currentAmmo = backpack.CurrentFireAmmo;
                if (currentAmmo != null && IsAmmoInSet(__instance, currentAmmo))
                {
                    Log($"CanBeFiredNow_Postfix: 混装模式有弹药 {currentAmmo.defName}，返回 true");
                    __result = true;
                }
                return;
            }
            
            // 普通模式检查
            if (IsAmmoCompatible(__instance, backpack))
            {
                __result = true;
            }
        }
        
        #endregion
        
        #region TryFindAmmoInInventory Patch，装填时使用背包弹药
        
        // 记录来自弹药背包的虚拟弹药
        private static readonly Dictionary<Thing, CompAmmoBackpack> AmmoFromBackpack = new();
        
        [HarmonyPatch(typeof(CompAmmoUser), nameof(CompAmmoUser.TryFindAmmoInInventory))]
        [HarmonyPostfix]
        public static void TryFindAmmoInInventory_Postfix(
            CompAmmoUser __instance, 
            ref Thing ammoThing, 
            ref bool __result)
        {
            if (__result) return;
            
            var holder = __instance.Holder;
            if (holder == null) return;
            
            var backpack = GetAmmoBackpackForWeapon(holder, __instance);
            if (backpack == null) return;
            if (!backpack.HasAmmo) return;
            
            // 混装模式不允许装填到武器弹匣（只能弹链供弹）
            if (backpack.IsMixMode) return;
            
            // 检查弹药兼容性
            if (!IsAmmoCompatible(__instance, backpack)) return;
            
            // 如果背包弹药与武器选择不同，更新武器选择
            if (backpack.SelectedAmmo != __instance.SelectedAmmo)
            {
                __instance.SelectedAmmo = backpack.SelectedAmmo;
            }
            
            ammoThing = CreateVirtualAmmo(backpack);
            if (ammoThing != null)
            {
                __result = true;
                AmmoFromBackpack[ammoThing] = backpack;
            }
        }
        
        #endregion
        
        #region LoadAmmo Patch，装填时从背包扣除
        
        [HarmonyPatch(typeof(CompAmmoUser), nameof(CompAmmoUser.LoadAmmo))]
        [HarmonyPrefix]
        public static void LoadAmmo_Prefix(CompAmmoUser __instance, Thing ammo)
        {
            if (ammo == null) return;
            if (!AmmoFromBackpack.TryGetValue(ammo, out var backpack)) return;
            
            int magSize = __instance.MagSize;
            int curMag = __instance.CurMagCount;
            int needed = __instance.Props.reloadOneAtATime ? 1 : (magSize - curMag);
            
            int toConsume = System.Math.Min(needed, backpack.CurrentAmmoCount);
            backpack.CurrentAmmoCount -= toConsume;
            ammo.stackCount = toConsume;
        }
        
        [HarmonyPatch(typeof(CompAmmoUser), nameof(CompAmmoUser.LoadAmmo))]
        [HarmonyPostfix]
        public static void LoadAmmo_Postfix(Thing ammo)
        {
            if (ammo != null)
            {
                AmmoFromBackpack.Remove(ammo);
            }
        }
        
        #endregion
        
        #region TryPrepareShot Patch，混装模式下跳过弹匣检查
        
        // 使用 Prefix 拦截，混装模式下直接返回 true 跳过原方法
        [HarmonyPatch(typeof(CompAmmoUser), nameof(CompAmmoUser.TryPrepareShot))]
        [HarmonyPrefix]
        public static bool TryPrepareShot_Prefix(CompAmmoUser __instance, ref bool __result)
        {
            var holder = __instance.Holder;
            if (holder == null) return true;
            
            var backpack = GetAmmoBackpackForWeapon(holder, __instance);
            if (backpack == null) return true;
            if (!backpack.HasAmmo) return true;
            
            // 混装模式：跳过原方法的弹匣检查（不修改武器的 CurrentAmmo）
            if (backpack.IsMixMode)
            {
                var currentAmmo = backpack.CurrentFireAmmo;
                if (currentAmmo == null) return true;
                if (!IsAmmoInSet(__instance, currentAmmo)) return true;
                
                // 直接返回 true，不修改武器状态
                __result = true;
                return false; // 跳过原方法
            }
            
            // 普通模式：让原方法执行
            return true;
        }
        
        // Postfix 处理普通模式
        [HarmonyPatch(typeof(CompAmmoUser), nameof(CompAmmoUser.TryPrepareShot))]
        [HarmonyPostfix]
        public static void TryPrepareShot_Postfix(CompAmmoUser __instance, ref bool __result)
        {
            var holder = __instance.Holder;
            if (holder == null) return;
            
            var backpack = GetAmmoBackpackForWeapon(holder, __instance);
            if (backpack == null) return;
            if (!backpack.HasAmmo) return;
            
            // 混装模式已在 Prefix 处理
            if (backpack.IsMixMode) return;
            
            // 普通模式
            if (!IsAmmoCompatible(__instance, backpack)) return;
            
            if (__instance.CurrentAmmo != backpack.SelectedAmmo)
            {
                __instance.CurrentAmmo = backpack.SelectedAmmo;
            }
            
            __result = true;
        }
        
        #endregion
        
        #region EmptyMagazine Patch，混装模式下弹匣不算空
        
        // Patch CompAmmoUser.EmptyMagazine getter
        // 混装模式下，如果背包有弹药，弹匣不算空（不触发重装）
        [HarmonyPatch(typeof(CompAmmoUser), nameof(CompAmmoUser.EmptyMagazine), MethodType.Getter)]
        [HarmonyPostfix]
        public static void EmptyMagazine_Postfix(CompAmmoUser __instance, ref bool __result)
        {
            // 如果弹匣不空，直接返回
            if (!__result) return;
            
            var holder = __instance.Holder;
            if (holder == null) return;
            
            var backpack = GetAmmoBackpackForWeapon(holder, __instance);
            if (backpack == null) return;
            
            // 混装模式下，如果背包有弹药，弹匣不算空
            if (backpack.IsMixMode && backpack.HasAmmo)
            {
                Log($"EmptyMagazine_Postfix: 混装模式有弹药，弹匣不算空");
                __result = false;
            }
        }
        
        #endregion
        
        #region CurMagCount Patch，混装模式下弹匣数量不为0
        
        // Patch CompAmmoUser.CurMagCount getter
        // 混装模式下，如果背包有弹药，返回虚拟弹匣数量（防止触发重装）
        [HarmonyPatch(typeof(CompAmmoUser), nameof(CompAmmoUser.CurMagCount), MethodType.Getter)]
        [HarmonyPostfix]
        public static void CurMagCount_Postfix(CompAmmoUser __instance, ref int __result)
        {
            // 如果弹匣有弹药，直接返回
            if (__result > 0) return;
            
            var holder = __instance.Holder;
            if (holder == null) return;
            
            var backpack = GetAmmoBackpackForWeapon(holder, __instance);
            if (backpack == null) return;
            
            // 混装模式下，如果背包有弹药，返回虚拟弹匣数量
            if (backpack.IsMixMode && backpack.HasAmmo)
            {
                Log($"CurMagCount_Postfix: 混装模式有弹药，返回虚拟弹匣数量 1");
                __result = 1; // 返回 1 防止触发重装
            }
        }
        
        #endregion
        
        #region TryStartReload Patch，混装模式下阻止重装
        
        // Patch CompAmmoUser.TryStartReload
        // 混装模式下，如果背包有弹药，阻止重装
        [HarmonyPatch(typeof(CompAmmoUser), nameof(CompAmmoUser.TryStartReload))]
        [HarmonyPrefix]
        public static bool TryStartReload_Prefix(CompAmmoUser __instance)
        {
            var holder = __instance.Holder;
            if (holder == null) return true;
            
            var backpack = GetAmmoBackpackForWeapon(holder, __instance);
            if (backpack == null) return true;
            
            // 混装模式下，如果背包有弹药，阻止重装
            if (backpack.IsMixMode && backpack.HasAmmo)
            {
                Log($"TryStartReload_Prefix: 混装模式有弹药，阻止重装");
                return false; // 跳过原方法
            }
            
            return true;
        }
        
        #endregion
        
        #region CurAmmoProjectile Patch，混装模式下返回正确的弹丸
        
        // Patch CompAmmoUser.CurAmmoProjectile getter
        // 混装模式下，返回当前混装弹药对应的弹丸（用于伤害计算）
        [HarmonyPatch(typeof(CompAmmoUser), nameof(CompAmmoUser.CurAmmoProjectile), MethodType.Getter)]
        [HarmonyPostfix]
        public static void CurAmmoProjectile_Postfix(CompAmmoUser __instance, ref ThingDef __result)
        {
            var holder = __instance.Holder;
            if (holder == null) return;
            
            var backpack = GetAmmoBackpackForWeapon(holder, __instance);
            if (backpack == null) return;
            if (!backpack.IsMixMode) return;
            if (!backpack.HasAmmo) return;
            
            // 优先使用上次消耗的弹药类型（有啥用啥模式）
            var currentAmmo = backpack.LastConsumedAmmo ?? backpack.CurrentFireAmmo;
            if (currentAmmo == null) return;
            
            // 从弹药组中找到对应的弹丸
            var ammoSet = __instance.Props?.ammoSet;
            if (ammoSet == null) return;
            
            foreach (var link in ammoSet.ammoTypes)
            {
                if (link.ammo == currentAmmo)
                {
                    __result = link.projectile;
                    return;
                }
            }
        }
        
        #endregion
        
        #region 辅助方法
        
        // 获取与武器弹药组匹配的激活背包
        public static CompAmmoBackpack GetAmmoBackpackForWeapon(Pawn pawn, CompAmmoUser compAmmo)
        {
            if (pawn?.apparel == null || compAmmo == null) return null;
            
            var weaponAmmoSet = compAmmo.Props?.ammoSet;
            if (weaponAmmoSet == null) return null;
            
            CompAmmoBackpack matchingActiveBackpack = null;
            CompAmmoBackpack matchingFirstBackpack = null;
            
            foreach (var apparel in pawn.apparel.WornApparel)
            {
                var comp = apparel.TryGetComp<CompAmmoBackpack>();
                if (comp == null) continue;
                
                // 检查背包弹药组是否与武器匹配
                var backpackAmmoSet = comp.GetCurrentAmmoSet();
                if (backpackAmmoSet != weaponAmmoSet) continue;
                
                // 记录第一个匹配的背包
                matchingFirstBackpack ??= comp;
                
                // 如果这个背包是激活状态，使用它
                if (comp.IsActiveBackpack)
                {
                    matchingActiveBackpack = comp;
                    break;
                }
            }
            
            // 优先返回激活的匹配背包，否则返回第一个匹配的
            return matchingActiveBackpack ?? matchingFirstBackpack;
        }
        
        // 获取当前激活的弹药背包，不检查弹药组用于UI
        public static CompAmmoBackpack GetAmmoBackpack(Pawn pawn)
        {
            if (pawn?.apparel == null) return null;
            
            CompAmmoBackpack activeBackpack = null;
            CompAmmoBackpack firstBackpack = null;
            
            foreach (var apparel in pawn.apparel.WornApparel)
            {
                var comp = apparel.TryGetComp<CompAmmoBackpack>();
                if (comp == null) continue;
                
                // 记录第一个找到的背包
                firstBackpack ??= comp;
                
                // 如果这个背包是激活状态，使用它
                if (comp.IsActiveBackpack)
                {
                    activeBackpack = comp;
                    break;
                }
            }
            
            // 如果没有激活的背包，返回第一个找到的
            return activeBackpack ?? firstBackpack;
        }
        
        // 获取所有弹药背包
        public static List<CompAmmoBackpack> GetAllAmmoBackpacks(Pawn pawn)
        {
            var result = new List<CompAmmoBackpack>();
            if (pawn?.apparel == null) return result;
            
            foreach (var apparel in pawn.apparel.WornApparel)
            {
                var comp = apparel.TryGetComp<CompAmmoBackpack>();
                if (comp != null)
                    result.Add(comp);
            }
            
            return result;
        }
        
        // 获取指定弹药组的激活背包
        public static CompAmmoBackpack GetAmmoBackpackForAmmoSet(Pawn pawn, AmmoSetDef ammoSet)
        {
            if (pawn?.apparel == null || ammoSet == null) return null;
            
            var backpacks = GetAllAmmoBackpacks(pawn);
            if (backpacks.Count == 0) return null;
            
            // 优先返回激活的、且弹药组匹配的背包
            foreach (var bp in backpacks)
            {
                if (!bp.IsActiveBackpack) continue;
                
                var bpAmmoSet = bp.GetCurrentAmmoSet();
                if (bpAmmoSet == ammoSet)
                    return bp;
            }
            
            // 其次返回任意弹药组匹配的背包
            foreach (var bp in backpacks)
            {
                var bpAmmoSet = bp.GetCurrentAmmoSet();
                if (bpAmmoSet == ammoSet)
                    return bp;
            }
            
            return null;
        }
        
        // 检查背包弹药是否与武器兼容，普通模式
        private static bool IsAmmoCompatible(CompAmmoUser compAmmo, CompAmmoBackpack backpack)
        {
            if (backpack.SelectedAmmo == null) return false;
            return IsAmmoInSet(compAmmo, backpack.SelectedAmmo);
        }
        
        // 检查弹药是否在武器的弹药组中
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
        
        private static Thing CreateVirtualAmmo(CompAmmoBackpack backpack)
        {
            if (backpack.SelectedAmmo == null) return null;
            if (backpack.CurrentAmmoCount <= 0) return null;
            
            var ammo = ThingMaker.MakeThing(backpack.SelectedAmmo);
            ammo.stackCount = backpack.CurrentAmmoCount;
            return ammo;
        }
        
        #endregion
        
        #region MechData回调，模块转换时保存/恢复弹药背包数据
        
        // 缓存弹药背包数据，用于模块转换
        private static AmmoDef cachedSelectedAmmo;
        private static AmmoDef cachedPendingAmmo;
        private static int cachedAmmoCount;
        private static AmmoSetDef cachedLinkedAmmoSet;
        private static bool cachedIsMixMode;
        private static List<AmmoMixEntry> cachedMixEntries;
        private static int cachedMixFireIndex;
        private static bool cachedNeedsEjectToEmpty;
        private static bool cachedIsActiveBackpack;
        
        // 注册回调，在CEMod静态构造函数中调用
        public static void RegisterMechDataCallbacks()
        {
            MechData.OnInitData += SaveBackpackData;
            MechData.OnRestoreToItem += RestoreBackpackData;
            MechData.OnRestoreToMech += RestoreBackpackData;
        }
        
        // 保存弹药背包数据
        private static void SaveBackpackData(Thing thing)
        {
            // 重置缓存
            cachedSelectedAmmo = null;
            cachedPendingAmmo = null;
            cachedAmmoCount = 0;
            cachedLinkedAmmoSet = null;
            cachedIsMixMode = false;
            cachedMixEntries = null;
            cachedMixFireIndex = 0;
            cachedNeedsEjectToEmpty = false;
            cachedIsActiveBackpack = true;
            
            var comp = thing.TryGetComp<CompAmmoBackpack>();
            if (comp == null)
            {
                Log($"SaveBackpackData: thing={thing.def.defName} 没有CompAmmoBackpack组件");
                return;
            }
            
            // 保存所有数据
            cachedSelectedAmmo = comp.SelectedAmmo;
            cachedPendingAmmo = comp.PendingAmmo;
            cachedAmmoCount = comp.CurrentAmmoCount;
            cachedLinkedAmmoSet = comp.LinkedAmmoSet;
            cachedIsMixMode = comp.IsMixMode;
            cachedNeedsEjectToEmpty = comp.NeedsClear;
            cachedIsActiveBackpack = comp.IsActiveBackpack;
            
            // 深拷贝混装条目
            if (comp.IsMixMode && comp.MixEntries != null)
            {
                cachedMixEntries = new List<AmmoMixEntry>();
                foreach (var entry in comp.MixEntries)
                {
                    cachedMixEntries.Add(entry.DeepCopy());
                }
                cachedMixFireIndex = comp.GetMixFireIndex();
                
                // 计算混装模式总弹药数
                int totalMixAmmo = 0;
                foreach (var entry in comp.MixEntries)
                {
                    totalMixAmmo += entry.CurrentCount;
                }
                Log($"SaveBackpackData: 混装模式, 槽位数={comp.MixEntries.Count}, 总弹药={totalMixAmmo}, LinkedAmmoSet={cachedLinkedAmmoSet?.defName}");
                foreach (var entry in comp.MixEntries)
                {
                    Log($"  槽位: ammo={entry.AmmoDef?.defName}, count={entry.CurrentCount}/{entry.MaxCount}, ratio={entry.Ratio}, wildcard={entry.IsWildcard}");
                }
            }
            else
            {
                Log($"SaveBackpackData: 普通模式, 弹药={cachedSelectedAmmo?.defName}, 数量={cachedAmmoCount}");
            }
        }
        
        // 恢复弹药背包数据
        private static void RestoreBackpackData(Thing target)
        {
            var comp = target.TryGetComp<CompAmmoBackpack>();
            if (comp == null)
            {
                Log($"RestoreBackpackData: target={target.def.defName} 没有CompAmmoBackpack组件");
                return;
            }
            
            Log($"RestoreBackpackData: 开始恢复, 缓存混装={cachedIsMixMode}, 缓存槽位数={cachedMixEntries?.Count ?? 0}, 缓存弹药={cachedSelectedAmmo?.defName}, 缓存数量={cachedAmmoCount}");
            
            // 恢复基础数据
            if (cachedLinkedAmmoSet != null)
            {
                comp.LinkedAmmoSet = cachedLinkedAmmoSet;
                Log($"RestoreBackpackData: 恢复LinkedAmmoSet={cachedLinkedAmmoSet.defName}");
            }
            
            comp.IsActiveBackpack = cachedIsActiveBackpack;
            
            // 恢复混装模式数据
            if (cachedIsMixMode && cachedMixEntries != null && cachedMixEntries.Count > 0)
            {
                Log($"RestoreBackpackData: 恢复混装模式, 槽位数={cachedMixEntries.Count}");
                foreach (var entry in cachedMixEntries)
                {
                    Log($"  恢复槽位: ammo={entry.AmmoDef?.defName}, count={entry.CurrentCount}/{entry.MaxCount}");
                }
                comp.RestoreMixMode(cachedMixEntries, cachedMixFireIndex);
            }
            else if (cachedSelectedAmmo != null)
            {
                // 恢复普通模式数据
                Log($"RestoreBackpackData: 恢复普通模式, 弹药={cachedSelectedAmmo.defName}, 数量={cachedAmmoCount}");
                comp.SelectedAmmo = cachedSelectedAmmo;
                comp.CurrentAmmoCount = cachedAmmoCount;
            }
            else
            {
                Log($"RestoreBackpackData: 没有缓存数据可恢复");
            }
            
            // 恢复待切换弹药
            if (cachedPendingAmmo != null)
            {
                comp.SetPendingAmmo(cachedPendingAmmo);
            }
            
            // 恢复需要清空标记
            if (cachedNeedsEjectToEmpty)
            {
                comp.SetNeedsEjectToEmpty(true);
            }
            
            // 验证恢复结果
            Log($"RestoreBackpackData: 恢复完成, comp.IsMixMode={comp.IsMixMode}, comp.TotalAmmoCount={comp.TotalAmmoCount}");
        }
        
        #endregion
    }
}
