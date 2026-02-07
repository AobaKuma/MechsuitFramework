// 当白昼倾坠之时
using System.Linq;
using CombatExtended;
using RimWorld;
using UnityEngine;
using Verse;

namespace Exosuit.CE
{
    // 炮塔弹药NPC初始化
    public partial class CompTurretAmmo
    {
        public void InitializeHostileAmmo()
        {
            // 如果已经有弹药则跳过
            if (currentAmmoCount > 0) return;

            var wearer = Wearer;
            if (wearer == null) return;

            // 获取可用弹药组
            AmmoSetDef ammoSet = LinkedAmmoSet;
            if (ammoSet == null) return;

            // 随机挑选弹药预设
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

            AmmoDef targetAmmo = null;

            if (bestPreset != null && bestPreset.entries != null && bestPreset.entries.Count > 0)
            {
                // 匹配高比例预设弹药
                var bestEntry = bestPreset.entries.OrderByDescending(e => e.ratio).FirstOrDefault();
                if (bestEntry != null)
                {
                    targetAmmo = bestEntry.ammo;
                    
                    // 优先使用标准分类匹配
                    if (targetAmmo == null && bestEntry.ammoClass != null)
                    {
                        targetAmmo = ammoSet.ammoTypes.FirstOrDefault(l => l.ammo.ammoClass == bestEntry.ammoClass)?.ammo;
                    }

                    // 使用关键词模糊匹配
                    if (targetAmmo == null && !string.IsNullOrEmpty(bestEntry.searchKey))
                    {
                        targetAmmo = ammoSet.ammoTypes
                            .FirstOrDefault(l => (l.ammo.defName != null && l.ammo.defName.ToUpper().Contains(bestEntry.searchKey.ToUpper())) 
                                              || (l.ammo.label != null && l.ammo.label.ToUpper().Contains(bestEntry.searchKey.ToUpper())))?.ammo;
                    }
                }
            }

            // 执行单装模式保底方案
            if (targetAmmo == null)
            {
                AmmoCategoryDef[] priorityClasses = { 
                    DefDatabase<AmmoCategoryDef>.GetNamedSilentFail("FullMetalJacket"),
                    DefDatabase<AmmoCategoryDef>.GetNamedSilentFail("ArmorPiercing"),
                    DefDatabase<AmmoCategoryDef>.GetNamedSilentFail("Ball"),
                    DefDatabase<AmmoCategoryDef>.GetNamedSilentFail("Slug"),
                    DefDatabase<AmmoCategoryDef>.GetNamedSilentFail("BuckShot")
                };

                foreach (var cls in priorityClasses.Where(c => c != null))
                {
                    targetAmmo = ammoSet.ammoTypes.FirstOrDefault(l => l.ammo.ammoClass == cls)?.ammo;
                    if (targetAmmo != null) break;
                }
                
                // 使用首个弹种保底
                targetAmmo ??= ammoSet.ammoTypes.FirstOrDefault()?.ammo;
            }

            if (targetAmmo == null) return;

            // 应用最终弹药配置
            selectedAmmo = targetAmmo;
            
            // 计算随机弹药填充量
            float fillLevel = bestPreset?.fillRatio ?? 0.8f;
            currentAmmoCount = (int)(MaxAmmo * fillLevel * Rand.Range(0.8f, 1.1f));
            currentAmmoCount = UnityEngine.Mathf.Clamp(currentAmmoCount, 0, MaxAmmo);

            Log.Message($"[TurretAmmo] NPC 初始化完成: {wearer.LabelShort}, 弹药: {targetAmmo.label}, 数量: {currentAmmoCount}/{MaxAmmo}");
        }
    }
}
