using System.Collections.Generic;
using System.Linq;
using CombatExtended;
using RimWorld;
using UnityEngine;
using Verse;

namespace Exosuit.CE
{
    // CompAmmoBackpack的IReloadableComp实现和弹药退出逻辑
    public partial class CompAmmoBackpack
    {
        #region 有啥用啥模式属性
        
        // 是否有有啥用啥槽位
        public bool HasWildcardSlot => isMixMode && mixEntries.Any(e => e.IsWildcard);
        
        // 有啥用啥槽位是否需要装填
        public bool WildcardNeedsReload => HasWildcardSlot && 
            mixEntries.Any(e => e.IsWildcard && e.CurrentCount < e.MaxCount);
        
        // 获取有啥用啥槽位的剩余容量
        public int WildcardRemainingCapacity
        {
            get
            {
                if (!HasWildcardSlot) return 0;
                return mixEntries.Where(e => e.IsWildcard).Sum(e => e.MaxCount - e.CurrentCount);
            }
        }
        
        #endregion
        
        #region 有啥用啥模式公共方法
        
        // 检查弹药是否与有啥用啥槽位兼容
        public bool IsWildcardCompatible(AmmoDef ammoDef)
        {
            if (ammoDef == null) return false;
            if (!HasWildcardSlot) return false;
            if (linkedAmmoSet == null) return false;
            
            return linkedAmmoSet.ammoTypes.Any(l => l.ammo == ammoDef) && IsAmmoCompatible(ammoDef);
        }
        
        // 获取所有兼容的弹药类型，用于有啥用啥模式
        public List<AmmoDef> GetWildcardCompatibleAmmoTypes()
        {
            if (linkedAmmoSet == null) return new List<AmmoDef>();
            
            return linkedAmmoSet.ammoTypes
                .Where(l => IsAmmoCompatible(l.ammo))
                .Select(l => l.ammo)
                .ToList();
        }
        
        #endregion
        
        #region IReloadableComp 实现
        
        public Thing ReloadableThing => parent;
        
        public ThingDef AmmoDef
        {
            get
            {
                if (isMixMode)
                {
                    // 收集所有需要装填的弹药类型
                    var neededAmmo = new List<AmmoDef>();
                    
                    // 普通槽位
                    foreach (var entry in mixEntries)
                    {
                        if (entry.IsWildcard) continue;
                        if (entry.AmmoDef != null && entry.CurrentCount < entry.MaxCount)
                            neededAmmo.Add(entry.AmmoDef);
                    }
                    
                    // 有啥用啥槽位：返回弹药组中任意兼容弹药
                    foreach (var entry in mixEntries)
                    {
                        if (!entry.IsWildcard) continue;
                        if (entry.IsFull) continue;
                        
                        if (linkedAmmoSet != null)
                        {
                            foreach (var link in linkedAmmoSet.ammoTypes)
                            {
                                if (IsAmmoCompatible(link.ammo) && !neededAmmo.Contains(link.ammo))
                                    neededAmmo.Add(link.ammo);
                            }
                        }
                    }
                    
                    // 随机返回一种需要的弹药，让龙门架能搬运不同类型
                    if (neededAmmo.Count > 0)
                        return neededAmmo.RandomElement();
                    
                    return null;
                }
                return selectedAmmo;
            }
        }
        
        public int BaseReloadTicks => Props.baseReloadTicks;
        
        public int MaxCharges
        {
            get
            {
                if (isMixMode)
                    return mixEntries.Sum(e => e.MaxCount);
                return MaxCapacity;
            }
        }
        
        public int RemainingCharges
        {
            get
            {
                if (isMixMode)
                    return mixEntries.Sum(e => e.CurrentCount);
                return currentAmmoCount;
            }
            set
            {
                if (!isMixMode)
                    currentAmmoCount = Mathf.Clamp(value, 0, MaxCapacity);
            }
        }
        
        public string LabelRemaining
        {
            get
            {
                if (isMixMode)
                    return $"{TotalAmmoCount} / {mixEntries.Sum(e => e.MaxCount)}";
                return $"{currentAmmoCount} / {MaxCapacity}";
            }
        }
        
        public bool NeedsReload(bool allowForceReload)
        {
            if (NeedsClear) return true;
            
            if (isMixMode)
            {
                foreach (var entry in mixEntries)
                {
                    if (entry.IsWildcard)
                    {
                        if (allowForceReload && entry.CurrentCount < entry.MaxCount) return true;
                        if (entry.CurrentCount == 0) return true;
                        continue;
                    }
                    
                    if (entry.AmmoDef == null) continue;
                    if (allowForceReload && entry.CurrentCount < entry.MaxCount) return true;
                    if (entry.CurrentCount == 0) return true;
                }
                return false;
            }
            
            if (selectedAmmo == null) return false;
            if (allowForceReload) return currentAmmoCount < MaxCapacity;
            return currentAmmoCount == 0;
        }
        
        public int MinAmmoNeeded(bool allowForcedReload)
        {
            if (NeedsClear) return 0;
            if (!NeedsReload(allowForcedReload)) return 0;
            return 1;
        }
        
        public int MaxAmmoNeeded(bool allowForcedReload)
        {
            if (NeedsClear) return 0;
            if (!NeedsReload(allowForcedReload)) return 0;
            
            if (isMixMode)
            {
                foreach (var entry in mixEntries)
                {
                    if (entry.IsWildcard && entry.CurrentCount < entry.MaxCount)
                        return entry.MaxCount - entry.CurrentCount;
                    
                    if (entry.AmmoDef != null && entry.CurrentCount < entry.MaxCount)
                        return entry.MaxCount - entry.CurrentCount;
                }
                return 0;
            }
            
            return MaxCapacity - currentAmmoCount;
        }
        
        public int MaxAmmoAmount()
        {
            if (isMixMode)
                return mixEntries.Sum(e => e.MaxCount);
            return MaxCapacity;
        }
        
        public void ReloadFrom(Thing ammo)
        {
            if (NeedsClear) return;
            if (ammo == null) return;
            
            if (isMixMode)
            {
                var ammoDef = ammo.def as AmmoDef;
                if (ammoDef == null) return;
                
                // 先找精确匹配的普通槽位
                foreach (var entry in mixEntries)
                {
                    if (entry.IsWildcard) continue;
                    if (entry.AmmoDef != ammo.def) continue;
                    if (entry.CurrentCount >= entry.MaxCount) continue;
                    
                    int needed = entry.MaxCount - entry.CurrentCount;
                    int toConsume = Mathf.Min(ammo.stackCount, needed);
                    
                    if (toConsume <= 0) continue;
                    
                    ammo.SplitOff(toConsume).Destroy();
                    entry.CurrentCount += toConsume;
                    return;
                }
                
                // 再找有啥用啥槽位
                foreach (var entry in mixEntries)
                {
                    if (!entry.IsWildcard) continue;
                    if (entry.CurrentCount >= entry.MaxCount) continue;
                    
                    if (linkedAmmoSet == null) continue;
                    bool inSet = linkedAmmoSet.ammoTypes.Any(l => l.ammo == ammoDef);
                    if (!inSet) continue;
                    
                    int needed = entry.MaxCount - entry.CurrentCount;
                    int toConsume = Mathf.Min(ammo.stackCount, needed);
                    
                    if (toConsume <= 0) continue;
                    
                    ammo.SplitOff(toConsume).Destroy();
                    entry.AddWildcardAmmo(ammoDef, toConsume);
                    return;
                }
                return;
            }
            
            if (!NeedsReload(true)) return;
            if (ammo.def != selectedAmmo) return;
            
            int neededNormal = MaxCapacity - currentAmmoCount;
            int toConsumeNormal = Mathf.Min(ammo.stackCount, neededNormal);
            
            if (toConsumeNormal <= 0) return;
            
            ammo.SplitOff(toConsumeNormal).Destroy();
            currentAmmoCount += toConsumeNormal;
        }
        
        public string DisabledReason(int minNeeded, int maxNeeded) => "";
        
        public bool CanBeUsed(out string reason)
        {
            reason = "";
            return false;
        }
        
        #endregion
        
        #region IAmmoBackpackClearable 实现
        
        public void EjectCurrentAmmo()
        {
            EjectCurrentAmmoAt(null);
        }
        
        public void EjectCurrentAmmoAt(Building_MaintenanceBay gantry)
        {
            Map map = null;
            IntVec3 pos = IntVec3.Invalid;
            
            if (gantry != null)
            {
                map = gantry.Map;
                pos = gantry.InteractionCell;
            }
            else
            {
                var apparel = parent as Apparel;
                var wearerPawn = apparel?.Wearer;
                
                if (wearerPawn != null)
                {
                    map = wearerPawn.MapHeld;
                    pos = wearerPawn.PositionHeld;
                }
                else
                {
                    map = parent.MapHeld;
                    pos = parent.PositionHeld;
                }
            }
            
            if (map == null || !pos.IsValid)
            {
                Log.Warning($"[AmmoBackpack] 无法退出弹药: map={map}, pos={pos}");
                return;
            }
            
            if (isMixMode)
            {
                foreach (var entry in mixEntries)
                {
                    if (entry.IsWildcard)
                    {
                        // 有啥用啥槽位：从 WildcardAmmo 字典退出每种弹药
                        foreach (var kvp in entry.WildcardAmmo)
                        {
                            if (kvp.Value <= 0) continue;
                            var ammoThing = ThingMaker.MakeThing(kvp.Key);
                            ammoThing.stackCount = kvp.Value;
                            GenPlace.TryPlaceThing(ammoThing, pos, map, ThingPlaceMode.Near);
                            Log.Message($"[AmmoBackpack] 退出有啥用啥弹药: {kvp.Key.LabelCap} x{kvp.Value} 在 {pos}");
                        }
                        entry.WildcardAmmo.Clear();
                        entry.CurrentCount = 0;
                    }
                    else if (entry.AmmoDef != null && entry.CurrentCount > 0)
                    {
                        var ammoThing = ThingMaker.MakeThing(entry.AmmoDef);
                        ammoThing.stackCount = entry.CurrentCount;
                        GenPlace.TryPlaceThing(ammoThing, pos, map, ThingPlaceMode.Near);
                        Log.Message($"[AmmoBackpack] 退出混装弹药: {entry.AmmoDef.LabelCap} x{entry.CurrentCount} 在 {pos}");
                        entry.CurrentCount = 0;
                    }
                }
            }
            else
            {
                if (selectedAmmo != null && currentAmmoCount > 0)
                {
                    var ammoThing = ThingMaker.MakeThing(selectedAmmo);
                    ammoThing.stackCount = currentAmmoCount;
                    GenPlace.TryPlaceThing(ammoThing, pos, map, ThingPlaceMode.Near);
                    Log.Message($"[AmmoBackpack] 退出弹药: {selectedAmmo.LabelCap} x{currentAmmoCount} 在 {pos}");
                    currentAmmoCount = 0;
                }
            }
            
            if (needsEjectToEmpty)
            {
                selectedAmmo = null;
                pendingAmmo = null;
                needsEjectToEmpty = false;
                cachedMaxCapacity = 0;
                
                if (isMixMode)
                {
                    mixEntries.Clear();
                    mixFireIndex = 0;
                    mixCycleCounter = 0;
                    
                    var entry = new AmmoMixEntry
                    {
                        AmmoDef = null,
                        Ratio = 1,
                        CurrentCount = 0,
                        MaxCount = 0
                    };
                    mixEntries.Add(entry);
                    RecalculateMixCapacities();
                }
            }
            else
            {
                CompletePendingAmmoSwitch();
            }
        }
        
        #endregion
    }
}
