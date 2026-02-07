// 当白昼倾坠之时
using System.Collections.Generic;
using CombatExtended;
using Verse;

namespace Exosuit.CE
{
    // NPC 弹药背包初始化预设 Def
    public class NPCAmmoPresetDef : Def
    {
        // 适用的弹药组列表 (如果是混装预设则必须匹配)
        public List<AmmoSetDef> ammoSets;

        // 混装条目
        public List<NPCAmmoPresetEntry> entries;

        // 填充比例 (0.0 - 1.0)
        public float fillRatio = 0.8f;

        // 优先级 (数字越大越优先)
        public int priority = 0;

        // 随机权重 (在相同优先级内进行随机挑选)
        public float selectionWeight = 1f;

        // 生成该预设的概率 (0.0 - 1.0)
        public float generateChance = 1f;

        // 检查是否适用于指定的弹药组
        public bool IsCompatible(AmmoSetDef ammoSet)
        {
            // 1. 如果明确指定了适用的弹药组列表，优先检查
            if (ammoSets != null && ammoSets.Count > 0)
            {
                if (!ammoSets.Contains(ammoSet)) return false;
            }

            // 2. 检查弹药组内是否包含预设要求的所有弹药类型 (非通配符条目)
            if (entries == null || entries.Count == 0) return true;

            foreach (var entry in entries)
            {
                if (entry.isWildcard) continue;
                
                bool matched = false;

                // 按优先级匹配：指定弹药 -> 弹药分类 -> 关键词
                if (entry.ammo != null)
                {
                    matched = ammoSet.ammoTypes.Any(l => l.ammo == entry.ammo);
                }
                else if (entry.ammoClass != null)
                {
                    matched = ammoSet.ammoTypes.Any(l => l.ammo.ammoClass == entry.ammoClass);
                }
                else if (!string.IsNullOrEmpty(entry.searchKey))
                {
                    matched = ammoSet.ammoTypes.Any(l => l.ammo.defName.ToUpper().Contains(entry.searchKey.ToUpper()) 
                                                    || l.ammo.label.ToUpper().Contains(entry.searchKey.ToUpper()));
                }

                if (!matched) return false; // 只要有一个条目无法满足，该预设就不兼容
            }

            return true;
        }
    }

    public class NPCAmmoPresetEntry
    {
        // 具体的弹药定义 (优先级最高)
        public AmmoDef ammo;

        // 搜索关键词 (如 "FMJ", "AP", "EMP", "Sabo")
        // 会在当前弹药组的可用弹种中进行关键词检索
        public string searchKey;

        // 弹药分类 (优先级高于关键词匹配)
        // 使用 CombatExtended 的标准分类进行精准匹配
        public AmmoCategoryDef ammoClass;

        // 如果是通配符条目
        public bool isWildcard = false;

        public int ratio = 1;
    }
}
