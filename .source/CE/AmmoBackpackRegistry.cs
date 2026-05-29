using System.Collections.Generic;
using CombatExtended;
using RimWorld;
using Verse;

namespace Exosuit.CE
{
    // 弹药背包实例注册表
    // 增量维护缓存 缺失时懒加载重建
    public static class AmmoBackpackRegistry
    {
        private static readonly Dictionary<int, List<CompAmmoBackpack>> Cache = new();

        // 使缓存失效
        public static void Invalidate(Pawn pawn)
        {
            if (pawn == null) return;
            Cache.Remove(pawn.thingIDNumber);
        }

        // 获取或重建缓存
        private static List<CompAmmoBackpack> GetOrBuild(Pawn pawn)
        {
            if (pawn.apparel == null) return null;

            int id = pawn.thingIDNumber;
            if (Cache.TryGetValue(id, out var cached))
            {
                // 检测缓存过期
                bool stale = false;
                foreach (var bp in cached)
                {
                    if (bp.parent.Destroyed) { stale = true; break; }
                }
                if (!stale)
                {
                    foreach (var apparel in pawn.apparel.WornApparel)
                    {
                        var comp = apparel.TryGetComp<CompAmmoBackpack>();
                        if (comp != null && !cached.Contains(comp)) { stale = true; break; }
                    }
                }
                if (!stale) return cached;
                Cache.Remove(id);
            }

            List<CompAmmoBackpack> list = null;
            foreach (var apparel in pawn.apparel.WornApparel)
            {
                var comp = apparel.TryGetComp<CompAmmoBackpack>();
                if (comp == null) continue;
                list ??= new List<CompAmmoBackpack>();
                list.Add(comp);
            }
            if (list != null)
                Cache[id] = list;
            return list;
        }

        public static void OnWear(Pawn pawn, CompAmmoBackpack comp)
        {
            int id = pawn.thingIDNumber;
            if (!Cache.TryGetValue(id, out var list))
            {
                list = new List<CompAmmoBackpack>();
                Cache[id] = list;
            }
            if (!list.Contains(comp))
                list.Add(comp);
        }

        public static void OnRemove(Pawn pawn, CompAmmoBackpack comp)
        {
            int id = pawn.thingIDNumber;
            if (!Cache.TryGetValue(id, out var list)) return;
            list.Remove(comp);
            if (list.Count == 0)
                Cache.Remove(id);
        }

        // 获取匹配武器弹药组的激活背包
        public static CompAmmoBackpack GetForWeapon(Pawn pawn, CompAmmoUser compAmmo)
        {
            if (pawn == null || compAmmo == null) return null;
            var backpacks = GetOrBuild(pawn);
            if (backpacks == null) return null;

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
            if (hasDestroyed)
            {
                backpacks.RemoveAll(b => b.parent.Destroyed);
                if (backpacks.Count == 0) Cache.Remove(pawn.thingIDNumber);
            }
            return firstMatch;
        }

        // 获取当前激活的弹药背包
        public static CompAmmoBackpack GetActive(Pawn pawn)
        {
            if (pawn == null) return null;
            var backpacks = GetOrBuild(pawn);
            if (backpacks == null) return null;

            CompAmmoBackpack first = null;
            foreach (var bp in backpacks)
            {
                if (bp.parent.Destroyed) continue;
                first ??= bp;
                if (bp.IsActiveBackpack) return bp;
            }
            return first;
        }

        // 获取指定弹药组的激活背包
        public static CompAmmoBackpack GetForAmmoSet(Pawn pawn, AmmoSetDef ammoSet)
        {
            if (pawn == null || ammoSet == null) return null;
            var backpacks = GetOrBuild(pawn);
            if (backpacks == null) return null;

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

        // 获取所有弹药背包
        public static List<CompAmmoBackpack> GetAll(Pawn pawn)
        {
            if (pawn == null) return new List<CompAmmoBackpack>();
            var cached = GetOrBuild(pawn);
            if (cached == null) return new List<CompAmmoBackpack>();

            var result = new List<CompAmmoBackpack>(cached.Count);
            foreach (var bp in cached)
            {
                if (!bp.parent.Destroyed)
                    result.Add(bp);
            }
            return result;
        }

        // 获取所有IAmmoStorage实例
        public static List<IAmmoStorage> GetAllStorages(Pawn pawn)
        {
            var result = new List<IAmmoStorage>();
            if (pawn == null) return result;
            var backpacks = GetOrBuild(pawn);
            if (backpacks == null) return result;

            foreach (var bp in backpacks)
            {
                if (!bp.parent.Destroyed && bp is IAmmoStorage storage)
                    result.Add(storage);
            }
            return result;
        }
    }
}
