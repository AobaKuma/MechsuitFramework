// 当白昼倾坠之时
using System.Collections.Generic;
using CombatExtended;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Exosuit.CE
{
    // CE弹药系统Patch，实现弹链供弹
    [HarmonyPatch]
    public static class CEPatches
    {
        // 调试开关
        private const bool DebugLog = false;
        
        private static void Log(string message)
        {
            if (DebugLog)
                Verse.Log.Message($"[AmmoBackpack] {message}");
        }
        
        // 记录来自弹药背包的虚拟弹药
        private static readonly Dictionary<Thing, CompAmmoBackpack> AmmoFromBackpack = new();
        
        #region 背包实例缓存
        
        // Pawn ID → 已穿戴的全部弹药背包
        internal static readonly Dictionary<int, List<CompAmmoBackpack>> PawnBackpackCache = new();
        
        [HarmonyPatch(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.Wear))]
        [HarmonyPostfix]
        public static void Wear_Postfix(Pawn_ApparelTracker __instance, Apparel newApparel)
        {
            var comp = newApparel.TryGetComp<CompAmmoBackpack>();
            if (comp == null) return;
            
            int id = __instance.pawn.thingIDNumber;
            if (!PawnBackpackCache.TryGetValue(id, out var list))
            {
                list = new List<CompAmmoBackpack>();
                PawnBackpackCache[id] = list;
            }
            if (!list.Contains(comp))
                list.Add(comp);
        }
        
        [HarmonyPatch(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.Remove))]
        [HarmonyPostfix]
        public static void Remove_Postfix(Pawn_ApparelTracker __instance, Apparel ap)
        {
            var comp = ap.TryGetComp<CompAmmoBackpack>();
            if (comp == null) return;
            
            int id = __instance.pawn.thingIDNumber;
            if (!PawnBackpackCache.TryGetValue(id, out var list)) return;
            
            list.Remove(comp);
            if (list.Count == 0)
                PawnBackpackCache.Remove(id);
        }
        
        // 注册单个como，供加载和NPC入口调用
        internal static void RegisterBackpack(Pawn pawn, CompAmmoBackpack comp)
        {
            if (pawn == null || comp == null) return;
            
            int id = pawn.thingIDNumber;
            if (!PawnBackpackCache.TryGetValue(id, out var list))
            {
                list = new List<CompAmmoBackpack>();
                PawnBackpackCache[id] = list;
            }
            if (!list.Contains(comp))
                list.Add(comp);
        }
        
        #endregion
        
        #region Notify_ShotFired Patch，开火时从背包消耗弹药
        
        [HarmonyPatch(typeof(CompAmmoUser), nameof(CompAmmoUser.Notify_ShotFired))]
        [HarmonyPrefix]
        public static bool Notify_ShotFired_Prefix(CompAmmoUser __instance, int ammoConsumedPerShot)
        {
            var holder = __instance.Holder;
            if (holder == null) return true;
            
            var backpack = GetAmmoBackpackForWeapon(holder, __instance);
            Log($"ShotFired: backpack={backpack != null}, weaponAmmoSet={__instance.Props?.ammoSet?.defName}");
            if (backpack == null) return true;
            
            Log($"ShotFired: IsMixMode={backpack.IsMixMode}, HasAmmo={backpack.HasAmmo}, SelectedAmmo={backpack.SelectedAmmo?.defName}");
            if (!backpack.HasAmmo) return true;
            
            int toConsume = ammoConsumedPerShot > 0 ? ammoConsumedPerShot : 1;
            
            // 根据模式分流处理
            if (backpack.IsMixMode)
                return HandleMixModeShotFired(backpack, toConsume);
            else
                return HandleNormalModeShotFired(__instance, backpack, toConsume);
        }
        
        // 混装模式：消耗弹药
        private static bool HandleMixModeShotFired(CompAmmoBackpack backpack, int toConsume)
        {
            if (backpack.TryConsumeMixAmmoWithType(toConsume, out _))
                return false;
            return true;
        }
        
        // 单装模式：消耗弹药
        private static bool HandleNormalModeShotFired(CompAmmoUser compAmmo, CompAmmoBackpack backpack, int toConsume)
        {
            Log($"NormalMode ShotFired: SelectedAmmo={backpack.SelectedAmmo?.defName}, Count={backpack.CurrentAmmoCount}");
            if (backpack.SelectedAmmo == null) return true;
            
            bool inSet = IsAmmoInSet(compAmmo, backpack.SelectedAmmo);
            Log($"NormalMode ShotFired: IsAmmoInSet={inSet}, weaponAmmoSet={compAmmo.Props?.ammoSet?.defName}");
            if (!inSet) return true;
            
            if (backpack.CurrentAmmoCount >= toConsume)
            {
                backpack.CurrentAmmoCount -= toConsume;
                Log($"NormalMode ShotFired: 消耗成功, 剩余={backpack.CurrentAmmoCount}");
                return false;
            }
            
            Log($"NormalMode ShotFired: 弹药不足");
            return true;
        }
        
        #endregion
        
        #region HasAmmo Patch
        
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
            
            if (backpack.IsMixMode)
            {
                var currentAmmo = backpack.CurrentFireAmmo;
                if (currentAmmo != null && IsAmmoInSet(__instance, currentAmmo))
                    __result = true;
            }
            else
            {
                if (backpack.SelectedAmmo != null && IsAmmoInSet(__instance, backpack.SelectedAmmo))
                    __result = true;
            }
        }
        
        #endregion
        
        #region CanBeFiredNow Patch
        
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
            
            if (backpack.IsMixMode)
            {
                var currentAmmo = backpack.CurrentFireAmmo;
                if (currentAmmo != null && IsAmmoInSet(__instance, currentAmmo))
                    __result = true;
            }
            else
            {
                if (backpack.SelectedAmmo != null && IsAmmoInSet(__instance, backpack.SelectedAmmo))
                    __result = true;
            }
        }
        
        #endregion
        
        #region EmptyMagazine Patch，背包有弹药时弹匣不算空
        
        [HarmonyPatch(typeof(CompAmmoUser), nameof(CompAmmoUser.EmptyMagazine), MethodType.Getter)]
        [HarmonyPostfix]
        public static void EmptyMagazine_Postfix(CompAmmoUser __instance, ref bool __result)
        {
            if (!__result) return;
            
            var holder = __instance.Holder;
            if (holder == null) return;
            
            var backpack = GetAmmoBackpackForWeapon(holder, __instance);
            if (backpack == null) return;
            if (!backpack.HasAmmo) return;
            
            // 两种模式都需要：背包有弹药时弹匣不算空
            if (backpack.IsMixMode)
            {
                var currentAmmo = backpack.CurrentFireAmmo;
                if (currentAmmo != null && IsAmmoInSet(__instance, currentAmmo))
                    __result = false;
            }
            else
            {
                if (backpack.SelectedAmmo != null && IsAmmoInSet(__instance, backpack.SelectedAmmo))
                    __result = false;
            }
        }
        
        #endregion
        
        #region CurMagCount Patch，背包有弹药时返回虚拟弹匣数量
        
        [HarmonyPatch(typeof(CompAmmoUser), nameof(CompAmmoUser.CurMagCount), MethodType.Getter)]
        [HarmonyPostfix]
        public static void CurMagCount_Postfix(CompAmmoUser __instance, ref int __result)
        {
            if (__result > 0) return;
            
            var holder = __instance.Holder;
            if (holder == null) return;
            
            var backpack = GetAmmoBackpackForWeapon(holder, __instance);
            if (backpack == null) return;
            if (!backpack.HasAmmo) return;
            
            // 两种模式都需要：背包有弹药时返回虚拟弹匣数量
            if (backpack.IsMixMode)
            {
                var currentAmmo = backpack.CurrentFireAmmo;
                if (currentAmmo != null && IsAmmoInSet(__instance, currentAmmo))
                    __result = 1;
            }
            else
            {
                if (backpack.SelectedAmmo != null && IsAmmoInSet(__instance, backpack.SelectedAmmo))
                    __result = 1;
            }
        }
        
        #endregion
        
        #region TryStartReload Patch，背包有弹药时阻止重装
        
        [HarmonyPatch(typeof(CompAmmoUser), nameof(CompAmmoUser.TryStartReload))]
        [HarmonyPrefix]
        public static bool TryStartReload_Prefix(CompAmmoUser __instance)
        {
            var holder = __instance.Holder;
            if (holder == null) return true;
            
            var backpack = GetAmmoBackpackForWeapon(holder, __instance);
            if (backpack == null) return true;
            if (!backpack.HasAmmo) return true;
            
            // 两种模式都需要：背包有弹药时阻止重装
            if (backpack.IsMixMode)
            {
                var currentAmmo = backpack.CurrentFireAmmo;
                if (currentAmmo != null && IsAmmoInSet(__instance, currentAmmo))
                    return false;
            }
            else
            {
                if (backpack.SelectedAmmo != null && IsAmmoInSet(__instance, backpack.SelectedAmmo))
                    return false;
            }
            
            return true;
        }
        
        #endregion
        
        #region TryPrepareShot Patch
        
        [HarmonyPatch(typeof(CompAmmoUser), nameof(CompAmmoUser.TryPrepareShot))]
        [HarmonyPrefix]
        public static bool TryPrepareShot_Prefix(CompAmmoUser __instance, ref bool __result)
        {
            var holder = __instance.Holder;
            if (holder == null) return true;
            
            var backpack = GetAmmoBackpackForWeapon(holder, __instance);
            if (backpack == null) return true;
            if (!backpack.HasAmmo) return true;
            
            if (backpack.IsMixMode)
            {
                // 混装模式：跳过原方法
                var currentAmmo = backpack.CurrentFireAmmo;
                if (currentAmmo == null) return true;
                if (!IsAmmoInSet(__instance, currentAmmo)) return true;
                
                __result = true;
                return false;
            }
            else
            {
                // 单装模式：跳过原方法，设置正确的弹药类型
                if (backpack.SelectedAmmo == null) return true;
                if (!IsAmmoInSet(__instance, backpack.SelectedAmmo)) return true;
                
                // 同步武器的当前弹药类型
                if (__instance.CurrentAmmo != backpack.SelectedAmmo)
                    __instance.CurrentAmmo = backpack.SelectedAmmo;
                
                __result = true;
                return false;
            }
        }
        
        #endregion
        
        #region CurAmmoProjectile Patch，返回正确的弹丸
        
        [HarmonyPatch(typeof(CompAmmoUser), nameof(CompAmmoUser.CurAmmoProjectile), MethodType.Getter)]
        [HarmonyPostfix]
        public static void CurAmmoProjectile_Postfix(CompAmmoUser __instance, ref ThingDef __result)
        {
            var holder = __instance.Holder;
            if (holder == null) return;
            
            var backpack = GetAmmoBackpackForWeapon(holder, __instance);
            if (backpack == null) return;
            if (!backpack.HasAmmo) return;
            
            AmmoDef targetAmmo = null;
            
            if (backpack.IsMixMode)
            {
                // 混装模式：优先使用上次消耗的弹药
                targetAmmo = backpack.LastConsumedAmmo ?? backpack.CurrentFireAmmo;
            }
            else
            {
                // 单装模式：使用选择的弹药
                targetAmmo = backpack.SelectedAmmo;
            }
            
            if (targetAmmo == null) return;
            
            // 从弹药组中找到对应的弹丸
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
        
        #region TryFindAmmoInInventory Patch，装填时使用背包弹药（仅单装模式）
        
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
            
            // 混装模式不允许装填到武器弹匣
            if (backpack.IsMixMode) return;
            
            // 单装模式：检查弹药兼容性
            if (backpack.SelectedAmmo == null) return;
            if (!IsAmmoInSet(__instance, backpack.SelectedAmmo)) return;
            
            // 同步武器选择
            if (backpack.SelectedAmmo != __instance.SelectedAmmo)
                __instance.SelectedAmmo = backpack.SelectedAmmo;
            
            ammoThing = CreateVirtualAmmo(backpack);
            if (ammoThing != null)
            {
                __result = true;
                AmmoFromBackpack[ammoThing] = backpack;
            }
        }
        
        #endregion
        
        #region LoadAmmo Patch
        
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
                AmmoFromBackpack.Remove(ammo);
        }
        
        #endregion
        
        #region 辅助方法
        
        // 获取与武器弹药组匹配的激活背包
        public static CompAmmoBackpack GetAmmoBackpackForWeapon(Pawn pawn, CompAmmoUser compAmmo)
        {
            if (pawn == null || compAmmo == null) return null;
            if (!PawnBackpackCache.TryGetValue(pawn.thingIDNumber, out var backpacks)) return null;
            
            var weaponAmmoSet = compAmmo.Props?.ammoSet;
            if (weaponAmmoSet == null) return null;
            
            CompAmmoBackpack firstMatch = null;
            bool hasDestroyed = false;
            foreach (var bp in backpacks)
            {
                if (bp.parent.Destroyed) { hasDestroyed = true; continue; }
                if (bp.GetCurrentAmmoSet() != weaponAmmoSet) continue;
                if (bp.IsActiveBackpack) return bp;
                firstMatch ??= bp;
            }
            // 延迟清理已销毁引用
            if (hasDestroyed)
            {
                backpacks.RemoveAll(b => b.parent.Destroyed);
                if (backpacks.Count == 0) PawnBackpackCache.Remove(pawn.thingIDNumber);
            }
            
            if (DebugLog) Log($"GetBackpack: 返回={firstMatch != null}, IsActive={firstMatch?.IsActiveBackpack}");
            return firstMatch;
        }
        
        // 获取当前激活的弹药背包
        public static CompAmmoBackpack GetAmmoBackpack(Pawn pawn)
        {
            if (pawn == null) return null;
            if (!PawnBackpackCache.TryGetValue(pawn.thingIDNumber, out var backpacks)) return null;
            
            CompAmmoBackpack first = null;
            foreach (var bp in backpacks)
            {
                if (bp.parent.Destroyed) continue;
                first ??= bp;
                if (bp.IsActiveBackpack) return bp;
            }
            return first;
        }
        
        public static List<IAmmoStorage> GetAllAmmoStorages(Pawn pawn)
        {
            var result = new List<IAmmoStorage>();
            if (pawn == null) return result;
            if (!PawnBackpackCache.TryGetValue(pawn.thingIDNumber, out var backpacks)) return result;
            
            foreach (var bp in backpacks)
            {
                if (!bp.parent.Destroyed && bp is IAmmoStorage storage)
                    result.Add(storage);
            }
            return result;
        }

        // 获取所有弹药背包
        public static List<CompAmmoBackpack> GetAllAmmoBackpacks(Pawn pawn)
        {
            if (pawn == null) return new List<CompAmmoBackpack>();
            if (!PawnBackpackCache.TryGetValue(pawn.thingIDNumber, out var cached)) return new List<CompAmmoBackpack>();
            
            var result = new List<CompAmmoBackpack>(cached.Count);
            foreach (var bp in cached)
            {
                if (!bp.parent.Destroyed)
                    result.Add(bp);
            }
            return result;
        }
        
        // 获取指定弹药组的激活背包
        public static CompAmmoBackpack GetAmmoBackpackForAmmoSet(Pawn pawn, AmmoSetDef ammoSet)
        {
            if (pawn == null || ammoSet == null) return null;
            if (!PawnBackpackCache.TryGetValue(pawn.thingIDNumber, out var backpacks)) return null;
            
            CompAmmoBackpack firstMatch = null;
            foreach (var bp in backpacks)
            {
                if (bp.parent.Destroyed) continue;
                if (bp.GetCurrentAmmoSet() != ammoSet) continue;
                if (bp.IsActiveBackpack) return bp;
                firstMatch ??= bp;
            }
            return firstMatch;
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
        
        private static AmmoDef cachedSelectedAmmo;
        private static AmmoDef cachedPendingAmmo;
        private static int cachedAmmoCount;
        private static AmmoSetDef cachedLinkedAmmoSet;
        private static bool cachedIsMixMode;
        private static List<AmmoMixEntry> cachedMixEntries;
        private static int cachedMixFireIndex;
        private static bool cachedNeedsEjectToEmpty;
        private static bool cachedIsActiveBackpack;
        
        public static void RegisterMechDataCallbacks()
        {
            MechData.OnInitData += SaveBackpackData;
            MechData.OnRestoreToItem += RestoreBackpackData;
            MechData.OnRestoreToMech += RestoreBackpackData;
        }
        
        private static void SaveBackpackData(Thing thing)
        {
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
            if (comp == null) return;
            
            cachedSelectedAmmo = comp.SelectedAmmo;
            cachedPendingAmmo = comp.PendingAmmo;
            cachedAmmoCount = comp.CurrentAmmoCount;
            cachedLinkedAmmoSet = comp.LinkedAmmoSet;
            cachedIsMixMode = comp.IsMixMode;
            cachedNeedsEjectToEmpty = comp.NeedsClear;
            cachedIsActiveBackpack = comp.IsActiveBackpack;
            
            if (comp.IsMixMode && comp.MixEntries != null)
            {
                cachedMixEntries = new List<AmmoMixEntry>();
                foreach (var entry in comp.MixEntries)
                    cachedMixEntries.Add(entry.DeepCopy());
                cachedMixFireIndex = comp.GetMixFireIndex();
            }
        }
        
        private static void RestoreBackpackData(Thing target)
        {
            var comp = target.TryGetComp<CompAmmoBackpack>();
            if (comp == null) return;
            
            if (cachedLinkedAmmoSet != null)
                comp.LinkedAmmoSet = cachedLinkedAmmoSet;
            
            comp.IsActiveBackpack = cachedIsActiveBackpack;
            
            if (cachedIsMixMode && cachedMixEntries != null && cachedMixEntries.Count > 0)
            {
                comp.RestoreMixMode(cachedMixEntries, cachedMixFireIndex);
            }
            else if (cachedSelectedAmmo != null)
            {
                comp.SelectedAmmo = cachedSelectedAmmo;
                comp.CurrentAmmoCount = cachedAmmoCount;
            }
            
            if (cachedPendingAmmo != null)
                comp.SetPendingAmmo(cachedPendingAmmo);
            
            if (cachedNeedsEjectToEmpty)
                comp.SetNeedsEjectToEmpty(true);
        }
        
        #endregion
    }
}
