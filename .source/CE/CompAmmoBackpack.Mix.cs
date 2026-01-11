using System.Collections.Generic;
using System.Linq;
using CombatExtended;
using UnityEngine;
using Verse;

namespace Exosuit.CE
{
    // CompAmmoBackpack的混装模式逻辑部分
    public partial class CompAmmoBackpack
    {
        #region 混装模式方法
        
        public IEnumerable<AmmoDef> GetSameSetAmmoTypes()
        {
            var ammoSet = GetCurrentAmmoSet();
            if (ammoSet == null) yield break;
            
            foreach (var link in ammoSet.ammoTypes)
            {
                if (IsAmmoCompatible(link.ammo))
                    yield return link.ammo;
            }
        }
        
        public void EnableMixMode()
        {
            if (TotalAmmoCount > 0) return;
            
            isMixMode = true;
            mixEntries.Clear();
            mixFireIndex = 0;
            mixCycleCounter = 0;
            
            var ammoSet = GetCurrentAmmoSet();
            if (ammoSet != null)
            {
                linkedAmmoSet = ammoSet;
                AddMixEntry(null, 1);
            }
        }
        
        public void DisableMixMode()
        {
            if (TotalAmmoCount > 0) return;
            
            isMixMode = false;
            mixEntries.Clear();
            mixFireIndex = 0;
            mixCycleCounter = 0;
        }
        
        public void AddMixEntry(AmmoDef ammoDef, int ratio)
        {
            if (ratio <= 0) return;
            if (!isMixMode) return;
            
            if (ammoDef != null && !IsAmmoCompatible(ammoDef)) return;
            
            if (ammoDef != null && linkedAmmoSet != null)
            {
                bool inSet = linkedAmmoSet.ammoTypes.Any(l => l.ammo == ammoDef);
                if (!inSet) return;
            }
            
            var entry = new AmmoMixEntry
            {
                AmmoDef = ammoDef,
                Ratio = ratio,
                CurrentCount = 0
            };
            
            mixEntries.Add(entry);
            RecalculateMixCapacities();
        }
        
        public void SetMixEntryAmmo(int index, AmmoDef ammoDef)
        {
            if (index < 0 || index >= mixEntries.Count) return;
            if (!isMixMode) return;
            
            var entry = mixEntries[index];
            
            if (entry.CurrentCount > 0) return;
            if (ammoDef != null && !IsAmmoCompatible(ammoDef)) return;
            
            if (ammoDef != null && linkedAmmoSet != null)
            {
                bool inSet = linkedAmmoSet.ammoTypes.Any(l => l.ammo == ammoDef);
                if (!inSet) return;
            }
            
            entry.AmmoDef = ammoDef;
            RecalculateMixCapacities();
        }
        
        public void RemoveMixEntryAt(int index)
        {
            if (!isMixMode) return;
            if (index < 0 || index >= mixEntries.Count) return;
            
            var entry = mixEntries[index];
            if (entry.CurrentCount > 0) return;
            
            mixEntries.RemoveAt(index);
            RecalculateMixCapacities();
        }
        
        public void RemoveMixEntry(AmmoDef ammoDef)
        {
            if (!isMixMode) return;
            
            var entry = mixEntries.FirstOrDefault(e => e.AmmoDef == ammoDef);
            if (entry == null) return;
            if (entry.CurrentCount > 0) return;
            
            mixEntries.Remove(entry);
            RecalculateMixCapacities();
        }
        
        // 清空所有槽位，用于应用预设
        public void ClearMixEntries()
        {
            if (!isMixMode) return;
            if (TotalAmmoCount > 0) return;
            
            mixEntries.Clear();
            mixFireIndex = 0;
            mixCycleCounter = 0;
        }
        
        // 从预设添加槽位
        public void AddMixEntryFromPreset(AmmoDef ammoDef, int ratio, bool isWildcard)
        {
            if (!isMixMode) return;
            if (ratio <= 0) ratio = 1;
            
            var entry = new AmmoMixEntry
            {
                AmmoDef = ammoDef,
                Ratio = ratio,
                IsWildcard = isWildcard,
                CurrentCount = 0
            };
            
            mixEntries.Add(entry);
        }
        
        public void SetMixRatio(AmmoDef ammoDef, int ratio)
        {
            if (!isMixMode || ratio <= 0) return;
            
            var entry = mixEntries.FirstOrDefault(e => e.AmmoDef == ammoDef);
            if (entry == null) return;
            
            entry.Ratio = ratio;
            RecalculateMixCapacities();
        }
        
        public void RecalculateMixCapacities()
        {
            if (!isMixMode || mixEntries.Count == 0) return;
            
            int totalRatio = mixEntries.Sum(e => e.Ratio);
            if (totalRatio <= 0) totalRatio = 1;
            
            foreach (var entry in mixEntries)
            {
                float mass = GetAmmoMass(entry.AmmoDef);
                if (mass <= 0) mass = Props.minMass;
                
                float ratioFraction = (float)entry.Ratio / totalRatio;
                float allocatedMass = Props.totalMassCapacity * ratioFraction;
                entry.MaxCount = Mathf.FloorToInt(allocatedMass / mass);
                
                if (entry.MaxCount < entry.Ratio) entry.MaxCount = entry.Ratio;
            }
        }
        
        private AmmoDef GetNextMixAmmo()
        {
            if (mixEntries.Count == 0) return null;
            
            int totalCycle = mixEntries.Sum(e => e.Ratio);
            if (totalCycle <= 0) return null;
            
            int cyclePos = mixCycleCounter % totalCycle;
            
            int accumulated = 0;
            foreach (var entry in mixEntries)
            {
                accumulated += entry.Ratio;
                if (cyclePos < accumulated && entry.CurrentCount > 0)
                {
                    if (entry.IsWildcard)
                        return entry.GetRandomWildcardAmmo();
                    
                    if (entry.AmmoDef != null)
                        return entry.AmmoDef;
                }
            }
            
            foreach (var entry in mixEntries)
            {
                if (entry.CurrentCount > 0)
                {
                    if (entry.IsWildcard)
                        return entry.GetRandomWildcardAmmo();
                    if (entry.AmmoDef != null)
                        return entry.AmmoDef;
                }
            }
            
            return null;
        }
        
        private int GetNextMixSlotIndex()
        {
            if (mixEntries.Count == 0) return -1;
            
            int totalCycle = mixEntries.Sum(e => e.Ratio);
            if (totalCycle <= 0) return -1;
            
            int cyclePos = mixCycleCounter % totalCycle;
            
            int accumulated = 0;
            for (int i = 0; i < mixEntries.Count; i++)
            {
                var entry = mixEntries[i];
                accumulated += entry.Ratio;
                if (cyclePos < accumulated && entry.CurrentCount > 0)
                {
                    if (entry.IsWildcard || entry.AmmoDef != null)
                        return i;
                }
            }
            
            for (int i = 0; i < mixEntries.Count; i++)
            {
                var entry = mixEntries[i];
                if (entry.CurrentCount > 0 && (entry.IsWildcard || entry.AmmoDef != null))
                    return i;
            }
            
            return -1;
        }
        
        public bool TryConsumeMixAmmo(int count = 1)
        {
            if (!isMixMode) return false;
            
            int slotIndex = GetNextMixSlotIndex();
            if (slotIndex < 0) return false;
            
            var entry = mixEntries[slotIndex];
            if (entry.CurrentCount < count) return false;
            
            if (entry.IsWildcard)
            {
                for (int i = 0; i < count; i++)
                {
                    if (!entry.TryConsumeWildcard(out _))
                        return false;
                }
            }
            else
            {
                entry.CurrentCount -= count;
            }
            
            mixCycleCounter++;
            
            return true;
        }
        
        public bool TryConsumeMixAmmoWithType(int count, out AmmoDef consumedAmmo)
        {
            consumedAmmo = null;
            if (!isMixMode) return false;
            
            int slotIndex = GetNextMixSlotIndex();
            if (slotIndex < 0) return false;
            
            var entry = mixEntries[slotIndex];
            if (entry.CurrentCount < count) return false;
            
            if (entry.IsWildcard)
            {
                if (entry.TryConsumeWildcard(out consumedAmmo))
                {
                    lastConsumedAmmo = consumedAmmo;
                    for (int i = 1; i < count; i++)
                        entry.TryConsumeWildcard(out _);
                }
                else
                {
                    return false;
                }
            }
            else
            {
                consumedAmmo = entry.AmmoDef;
                lastConsumedAmmo = consumedAmmo;
                entry.CurrentCount -= count;
            }
            
            mixCycleCounter++;
            
            return true;
        }
        
        public IEnumerable<(AmmoDef ammo, int needed)> GetMixAmmoNeeded()
        {
            if (!isMixMode) yield break;
            
            foreach (var entry in mixEntries)
            {
                int needed = entry.MaxCount - entry.CurrentCount;
                if (needed > 0)
                {
                    if (entry.IsWildcard)
                        yield return (null, needed);
                    else
                        yield return (entry.AmmoDef, needed);
                }
            }
        }
        
        #endregion
    }
}
