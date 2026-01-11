using System.Collections.Generic;
using System.Linq;
using CombatExtended;
using Verse;

namespace Exosuit.CE
{
    // 弹药混装条目，存储单个弹药类型的混装配置
    // 支持普通模式和随机模式
    public class AmmoMixEntry : IExposable
    {
        #region 字段
        
        // 弹药类型（IsWildcard 为 true 时可以为 null）
        public AmmoDef AmmoDef;
        
        // 混装比例（如 4:1 中的 4 或 1）
        public int Ratio = 1;
        
        // 当前数量
        public int CurrentCount;
        
        // 最大容量（根据比例分配）
        public int MaxCount;
        
        // 是否为随机模式（有啥压啥）
        public bool IsWildcard;
        
        // 随机模式下存储的弹药列表（弹药类型 -> 数量）
        public Dictionary<AmmoDef, int> WildcardAmmo = new();
        
        #endregion
        
        #region 属性
        
        // 是否已满
        public bool IsFull => CurrentCount >= MaxCount;
        
        // 是否为空
        public bool IsEmpty => CurrentCount <= 0;
        
        // 填充百分比
        public float FillPercent => MaxCount > 0 ? (float)CurrentCount / MaxCount : 0f;
        
        // 随机模式下获取总弹药数
        public int WildcardTotalCount => IsWildcard ? WildcardAmmo.Values.Sum() : 0;
        
        #endregion
        
        #region 公共方法
        
        public void ExposeData()
        {
            Scribe_Defs.Look(ref AmmoDef, "ammoDef");
            Scribe_Values.Look(ref Ratio, "ratio", 1);
            Scribe_Values.Look(ref CurrentCount, "currentCount", 0);
            Scribe_Values.Look(ref MaxCount, "maxCount", 0);
            Scribe_Values.Look(ref IsWildcard, "isWildcard", false);
            Scribe_Collections.Look(ref WildcardAmmo, "wildcardAmmo", LookMode.Def, LookMode.Value);
            
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (WildcardAmmo == null)
                    WildcardAmmo = new Dictionary<AmmoDef, int>();
            }
        }
        
        // 随机模式下随机抽取一种弹药
        public AmmoDef GetRandomWildcardAmmo()
        {
            if (!IsWildcard || WildcardAmmo.Count == 0) return null;
            
            int total = WildcardTotalCount;
            if (total <= 0) return null;
            
            int roll = Rand.Range(0, total);
            int cumulative = 0;
            
            foreach (var kvp in WildcardAmmo)
            {
                cumulative += kvp.Value;
                if (roll < cumulative)
                    return kvp.Key;
            }
            
            return WildcardAmmo.Keys.FirstOrDefault();
        }
        
        // 随机模式下消耗一发弹药
        public bool TryConsumeWildcard(out AmmoDef consumed)
        {
            consumed = GetRandomWildcardAmmo();
            if (consumed == null) return false;
            
            if (WildcardAmmo.TryGetValue(consumed, out int count) && count > 0)
            {
                WildcardAmmo[consumed] = count - 1;
                if (WildcardAmmo[consumed] <= 0)
                    WildcardAmmo.Remove(consumed);
                CurrentCount--;
                return true;
            }
            
            return false;
        }
        
        // 随机模式下添加弹药
        public int AddWildcardAmmo(AmmoDef ammo, int amount)
        {
            if (!IsWildcard || ammo == null) return 0;
            
            int canAdd = MaxCount - CurrentCount;
            int toAdd = System.Math.Min(canAdd, amount);
            if (toAdd <= 0) return 0;
            
            if (WildcardAmmo.ContainsKey(ammo))
                WildcardAmmo[ammo] += toAdd;
            else
                WildcardAmmo[ammo] = toAdd;
            
            CurrentCount += toAdd;
            return toAdd;
        }
        
        // 深拷贝
        public AmmoMixEntry DeepCopy()
        {
            var copy = new AmmoMixEntry
            {
                AmmoDef = AmmoDef,
                Ratio = Ratio,
                CurrentCount = CurrentCount,
                MaxCount = MaxCount,
                IsWildcard = IsWildcard,
                WildcardAmmo = new Dictionary<AmmoDef, int>(WildcardAmmo)
            };
            return copy;
        }
        
        #endregion
    }
}
