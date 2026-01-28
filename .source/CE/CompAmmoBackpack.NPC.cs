// 当白昼倾坠之时
using System.Collections.Generic;
using System.Linq;
using CombatExtended;
using Exosuit;
using RimWorld;
using Verse;

namespace Exosuit.CE
{
    public partial class CompAmmoBackpack
    {
        // 为敌对机兵初始化弹药
        public void InitializeHostileAmmo()
        {
            if (isMixMode && mixEntries.Count > 0) return;

            var wearer = Wearer;
            if (wearer == null) return;

            // 1. 寻找合适的弹药组
            AmmoSetDef ammoSet = GetGetCurrentAmmoSetNPC();
            if (ammoSet == null) return;

            // 2. 挑选预设 (考虑优先级、权重和生成概率)
            NPCAmmoPresetDef bestPreset = null;
            var allPresets = DefDatabase<NPCAmmoPresetDef>.AllDefs
                .Where(p => p.IsCompatible(ammoSet) && Rand.Chance(p.generateChance))
                .GroupBy(p => p.priority)
                .OrderByDescending(g => g.Key)
                .FirstOrDefault();

            if (allPresets != null)
            {
                bestPreset = allPresets.RandomElementByWeight(p => p.selectionWeight);
            }

            if (bestPreset != null && bestPreset.entries != null && bestPreset.entries.Count > 0)
            {
                // 尝试应用预设配置
                LinkedAmmoSet = ammoSet;
                EnableMixMode();
                ClearMixEntries();
                
                int matchedCount = 0;
                foreach (var entry in bestPreset.entries)
                {
                    AmmoDef targetAmmo = entry.ammo;
                    
                    // 1. 优先使用标准分类匹配 (精准匹配)
                    if (targetAmmo == null && entry.ammoClass != null)
                    {
                        targetAmmo = ammoSet.ammoTypes.FirstOrDefault(l => l.ammo.ammoClass == entry.ammoClass)?.ammo;
                    }

                    // 2. 回退到关键词匹配 (模糊查找)
                    if (targetAmmo == null && !string.IsNullOrEmpty(entry.searchKey))
                    {
                        targetAmmo = ammoSet.ammoTypes
                            .FirstOrDefault(l => l.ammo.defName.ToUpper().Contains(entry.searchKey.ToUpper()) 
                                              || l.ammo.label.ToUpper().Contains(entry.searchKey.ToUpper()))?.ammo;
                    }

                    if (targetAmmo != null || entry.isWildcard)
                    {
                        AddMixEntryFromPreset(targetAmmo, entry.ratio, entry.isWildcard);
                        matchedCount++;
                    }
                }
                
                // 如果混装配置成功匹配到了至少一个弹种，则完成初始化
                if (matchedCount > 0)
                {
                    RecalculateMixCapacities();
                    float fillLevel = bestPreset.fillRatio;
                    foreach (var entry in mixEntries)
                    {
                        entry.CurrentCount = (int)(entry.MaxCount * fillLevel);
                    }
                    Log.Message($"[AmmoBackpack] NPC 初始化完成 (使用预设 {bestPreset.defName}): {wearer.LabelShort}, 槽位: {mixEntries.Count}");
                    return;
                }
                
                // 若预设内所有条目都匹配失败，则关闭混装模式并回退到单装逻辑
                isMixMode = false;
                Log.Warning($"[AmmoBackpack] 预设 {bestPreset.defName} 匹配失败，已回退。");
            }

            // 3. 回退方案：单装模式 (优先使用标准分类匹配)
            // 匹配优先级：常规弹(FMJ/Ball) -> 穿甲弹(AP) -> 独头弹/散弹(Slug/Buck)
            AmmoCategoryDef[] priorityClasses = { 
                DefDatabase<AmmoCategoryDef>.GetNamedSilentFail("FullMetalJacket"),
                DefDatabase<AmmoCategoryDef>.GetNamedSilentFail("ArmorPiercing"),
                DefDatabase<AmmoCategoryDef>.GetNamedSilentFail("Ball"),
                DefDatabase<AmmoCategoryDef>.GetNamedSilentFail("Slug"),
                DefDatabase<AmmoCategoryDef>.GetNamedSilentFail("BuckShot")
            };
            
            AmmoDef ammo = null;
            foreach (var cls in priorityClasses.Where(c => c != null))
            {
                ammo = ammoSet.ammoTypes.FirstOrDefault(l => l.ammo.ammoClass == cls)?.ammo;
                if (ammo != null) break;
            }

            // 若分类匹配失败，回退到第一个可用弹种
            ammo ??= ammoSet.ammoTypes.FirstOrDefault()?.ammo;

            if (ammo == null) return;
            
            LinkedAmmoSet = ammoSet;
            SelectedAmmo = ammo;
            CurrentAmmoCount = (int)(MaxCapacity * Rand.Range(0.6f, 0.9f));

            Log.Message($"[AmmoBackpack] NPC 初始化完成 (单装模式回退): {wearer.LabelShort}, 弹药: {ammo.label}");
        }

        private AmmoSetDef GetGetCurrentAmmoSetNPC()
        {
            // 尝试通过玩家装备推断
            var wearer = Wearer;
            if (wearer == null) return null;

            // 检查主手武器
            if (wearer.equipment?.Primary != null)
            {
                var comp = wearer.equipment.Primary.TryGetComp<CompAmmoUser>();
                if (comp?.Props?.ammoSet != null && IsAmmoSetCompatible(comp.Props.ammoSet))
                {
                    return comp.Props.ammoSet;
                }
            }

            // 检查模块武器 (CompModuleWeapon 已有实现可参考)
            return GetWeaponModuleAmmoSet();
        }
    }
}
