using System.Collections.Generic;
using System.Linq;
using CombatExtended;
using Exosuit;
using RimWorld;
using RimWorld.Utility;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Exosuit.CE
{
    // 弹药背包组件，为CE武器提供弹链供弹
    // 实现IModuleExtensionUI在龙门架UI中显示弹药管理面板
    // 实现IReloadableComp支持龙门架自动搬运弹药
    // 实现IAmmoBackpackClearable支持龙门架清空弹药
    // 实现IModuleDataTransfer支持模块转换时保存和恢复弹药数据
    public partial class CompAmmoBackpack : ThingComp, IModuleExtensionUI, IReloadableComp, IAmmoBackpackClearable, IModuleDataTransfer
    {
        #region 常量
        
        // 不支持弹链供弹的弹药类型关键词
        private static readonly string[] ExcludedAmmoClasses = 
        {
            "Rocket", "Missile", "Grenade", "Mortar", "Shell", "Bomb",
            "Arrow", "Bolt", "Plasma", "Charge", "Ion", "Laser", "Energy"
        };
        
        // 支持弹链供弹的弹药类型关键词
        private static readonly string[] AllowedAmmoPatterns =
        {
            "mm", "NATO", "Magnum", "ACP", "Parabellum", "Mauser", 
            "Rifle", "Pistol", "SMG", "MG", "Cannon", "Autocannon"
        };
        
        private const float ExtensionWidth = 400f;
        
        #endregion
        
        #region 字段
        
        private AmmoDef selectedAmmo;
        private int currentAmmoCount;
        private AmmoSetDef linkedAmmoSet;
        private AmmoDef pendingAmmo;
        private int cachedMaxCapacity;
        private Vector2 mixEntriesScrollPos;
        
        // 混装模式字段
        private bool isMixMode;
        private List<AmmoMixEntry> mixEntries = new();
        private int mixFireIndex;
        private int mixCycleCounter;
        private bool needsEjectToEmpty;
        private AmmoDef lastConsumedAmmo;
        
        // 多背包支持字段
        private bool isActiveBackpack;  // 默认 false，在 PostSpawnSetup 中初始化
        
        #endregion
        
        #region 属性
        
        public CompProperties_AmmoBackpack Props => props as CompProperties_AmmoBackpack;
        
        public AmmoDef SelectedAmmo
        {
            get => selectedAmmo;
            set
            {
                if (selectedAmmo != value)
                {
                    selectedAmmo = value;
                    cachedMaxCapacity = CalculateMaxCapacity(value);
                    if (currentAmmoCount > cachedMaxCapacity)
                        currentAmmoCount = cachedMaxCapacity;
                }
            }
        }
        
        public int CurrentAmmoCount
        {
            get => currentAmmoCount;
            set => currentAmmoCount = Mathf.Clamp(value, 0, MaxCapacity);
        }
        
        public int MaxCapacity
        {
            get
            {
                if (cachedMaxCapacity <= 0 && selectedAmmo != null)
                    cachedMaxCapacity = CalculateMaxCapacity(selectedAmmo);
                return cachedMaxCapacity > 0 ? cachedMaxCapacity : 500;
            }
        }
        
        public AmmoSetDef LinkedAmmoSet
        {
            get => linkedAmmoSet;
            set
            {
                if (linkedAmmoSet != value)
                {
                    linkedAmmoSet = value;
                    if (value != null && selectedAmmo != null)
                    {
                        if (!value.ammoTypes.Any(l => l.ammo == selectedAmmo))
                        {
                            selectedAmmo = value.ammoTypes.FirstOrDefault()?.ammo;
                            currentAmmoCount = 0;
                        }
                    }
                }
            }
        }
        
        public Pawn Wearer => (parent as Apparel)?.Wearer;
        public bool HasAmmo => isMixMode ? HasMixAmmo : (currentAmmoCount > 0 && selectedAmmo != null);
        public bool NeedsClear => (pendingAmmo != null && TotalAmmoCount > 0) || (needsEjectToEmpty && TotalAmmoCount > 0);
        public AmmoDef PendingAmmo => pendingAmmo;
        
        // 混装模式属性
        public bool IsMixMode => isMixMode;
        public IReadOnlyList<AmmoMixEntry> MixEntries => mixEntries;
        public bool HasMixAmmo => mixEntries.Any(e => e.CurrentCount > 0);
        public int TotalAmmoCount => isMixMode ? mixEntries.Sum(e => e.CurrentCount) : currentAmmoCount;
        public AmmoDef LastConsumedAmmo => lastConsumedAmmo;
        
        public AmmoDef CurrentFireAmmo
        {
            get
            {
                if (!isMixMode) return selectedAmmo;
                if (mixEntries.Count == 0) return null;
                return GetNextMixAmmo();
            }
        }
        
        // 多背包支持属性
        public bool IsActiveBackpack
        {
            get => isActiveBackpack;
            set => isActiveBackpack = value;
        }
        
        // 获取背包显示名称，用于UI区分多个背包
        public string BackpackDisplayName
        {
            get
            {
                var defName = parent.def.defName;
                if (defName.Contains("Heavy"))
                    return "WG_AmmoBackpack_HeavyPack".Translate();
                if (defName.Contains("Light"))
                    return "WG_AmmoBackpack_LightBox".Translate();
                
                return "WG_AmmoBackpack_MainPack".Translate();
            }
        }
        
        #endregion

        #region 序列化
        
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            
            // 加载时不重新初始化激活状态
            if (respawningAfterLoad) return;
            
            // 新安装的背包：检查是否已有同弹药组的激活背包
            var wearer = Wearer;
            if (wearer == null)
            {
                // 不在穿戴状态，默认激活
                isActiveBackpack = true;
                return;
            }
            
            // 获取当前背包的弹药组
            var myAmmoSet = GetCurrentAmmoSet();
            
            // 检查是否已有同弹药组的激活背包
            bool hasActiveBackpackForSameSet = false;
            foreach (var apparel in wearer.apparel.WornApparel)
            {
                if (apparel == parent) continue;
                
                var otherComp = apparel.TryGetComp<CompAmmoBackpack>();
                if (otherComp == null) continue;
                
                var otherAmmoSet = otherComp.GetCurrentAmmoSet();
                
                // 如果弹药组相同且已激活，则当前背包不激活
                if (otherAmmoSet == myAmmoSet && otherComp.IsActiveBackpack)
                {
                    hasActiveBackpackForSameSet = true;
                    break;
                }
            }
            
            // 如果没有同弹药组的激活背包，则激活当前背包
            isActiveBackpack = !hasActiveBackpackForSameSet;
        }
        
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Defs.Look(ref selectedAmmo, "selectedAmmo");
            Scribe_Values.Look(ref currentAmmoCount, "currentAmmoCount", 0);
            Scribe_Defs.Look(ref linkedAmmoSet, "linkedAmmoSet");
            Scribe_Defs.Look(ref pendingAmmo, "pendingAmmo");
            Scribe_Values.Look(ref needsEjectToEmpty, "needsEjectToEmpty", false);
            
            Scribe_Values.Look(ref isMixMode, "isMixMode", false);
            Scribe_Values.Look(ref mixFireIndex, "mixFireIndex", 0);
            Scribe_Values.Look(ref mixCycleCounter, "mixCycleCounter", 0);
            Scribe_Collections.Look(ref mixEntries, "mixEntries", LookMode.Deep);
            Scribe_Values.Look(ref isActiveBackpack, "isActiveBackpack", true);
            
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (selectedAmmo != null)
                    cachedMaxCapacity = CalculateMaxCapacity(selectedAmmo);
                
                if (mixEntries == null)
                    mixEntries = new List<AmmoMixEntry>();
            }
        }
        
        #endregion
        
        #region 公共方法
        
        public int CalculateMaxCapacity(AmmoDef ammoDef)
        {
            if (ammoDef == null) return 500;
            
            float mass = GetAmmoMass(ammoDef);
            if (mass <= 0) mass = Props.minMass;
            
            return Mathf.FloorToInt(Props.totalMassCapacity / mass);
        }
        
        public static float GetAmmoMass(AmmoDef ammoDef)
        {
            if (ammoDef == null) return 0.025f;
            
            var massStat = ammoDef.statBases?.FirstOrDefault(s => s.stat == StatDefOf.Mass);
            if (massStat != null)
                return massStat.value;
            
            return 0.025f;
        }
        
        public bool TryConsumeAmmo(AmmoDef ammoDef, int count = 1)
        {
            if (ammoDef != selectedAmmo) return false;
            if (currentAmmoCount < count) return false;
            
            currentAmmoCount -= count;
            return true;
        }
        
        public int AddAmmo(AmmoDef ammoDef, int count)
        {
            if (isMixMode)
            {
                var entry = mixEntries.FirstOrDefault(e => !e.IsWildcard && e.AmmoDef == ammoDef);
                if (entry != null)
                {
                    int canAdd = entry.MaxCount - entry.CurrentCount;
                    int toAdd = Mathf.Min(canAdd, count);
                    entry.CurrentCount += toAdd;
                    return toAdd;
                }
                
                var wildcardEntry = mixEntries.FirstOrDefault(e => e.IsWildcard && !e.IsFull);
                if (wildcardEntry != null && linkedAmmoSet != null)
                {
                    bool inSet = linkedAmmoSet.ammoTypes.Any(l => l.ammo == ammoDef);
                    if (inSet)
                        return wildcardEntry.AddWildcardAmmo(ammoDef, count);
                }
                
                return 0;
            }
            
            if (ammoDef != selectedAmmo) return 0;
            
            int canAddNormal = MaxCapacity - currentAmmoCount;
            int toAddNormal = Mathf.Min(canAddNormal, count);
            currentAmmoCount += toAddNormal;
            return toAddNormal;
        }
        
        public void SetSelectedAmmo(AmmoDef ammo)
        {
            if (selectedAmmo == ammo)
            {
                pendingAmmo = null;
                return;
            }
            
            if (currentAmmoCount > 0)
            {
                pendingAmmo = ammo;
                SoundDefOf.Click.PlayOneShotOnCamera();
                return;
            }
            
            selectedAmmo = ammo;
            pendingAmmo = null;
            cachedMaxCapacity = CalculateMaxCapacity(ammo);
            SoundDefOf.Click.PlayOneShotOnCamera();
        }
        
        public void CompletePendingAmmoSwitch()
        {
            if (pendingAmmo == null) return;
            if (currentAmmoCount > 0) return;
            
            selectedAmmo = pendingAmmo;
            pendingAmmo = null;
            cachedMaxCapacity = CalculateMaxCapacity(selectedAmmo);
        }
        
        // 获取混装模式射击索引，用于模块转换时保存
        public int GetMixFireIndex() => mixFireIndex;
        
        // 内部方法：设置待切换弹药，供CEPatches调用避免反射
        internal void SetPendingAmmo(AmmoDef ammo) => pendingAmmo = ammo;
        
        // 内部方法：设置需要清空标记，供CEPatches调用避免反射
        internal void SetNeedsEjectToEmpty(bool value) => needsEjectToEmpty = value;
        
        // 恢复混装模式数据，用于模块转换时恢复
        public void RestoreMixMode(List<AmmoMixEntry> entries, int fireIndex)
        {
            if (entries == null || entries.Count == 0) return;
            
            isMixMode = true;
            mixEntries.Clear();
            foreach (var entry in entries)
            {
                mixEntries.Add(entry.DeepCopy());
            }
            mixFireIndex = fireIndex;
            mixCycleCounter = 0;
        }
        
        public override string CompInspectStringExtra()
        {
            if (isMixMode)
            {
                int totalCurrent = mixEntries.Sum(e => e.CurrentCount);
                int totalMax = mixEntries.Sum(e => e.MaxCount);
                return "WG_AmmoBackpack_MixStatus".Translate(totalCurrent, totalMax);
            }
            
            if (selectedAmmo == null)
                return "WG_AmmoBackpack_NoAmmoSelected".Translate();
            
            return "WG_AmmoBackpack_AmmoStatus".Translate(
                selectedAmmo.LabelCap, 
                currentAmmoCount, 
                MaxCapacity);
        }
        
        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            if (Wearer == null) yield break;
            
            UpdateLinkedWeapon();
            
            // 获取同弹药组的所有背包
            var myAmmoSet = GetCurrentAmmoSet();
            var allBackpacks = CEPatches.GetAllAmmoBackpacks(Wearer);
            
            // 找到同弹药组的背包列表
            var sameSetBackpacks = new List<CompAmmoBackpack>();
            foreach (var bp in allBackpacks)
            {
                if (bp.GetCurrentAmmoSet() == myAmmoSet)
                    sameSetBackpacks.Add(bp);
            }
            
            // 只有同弹药组的第一个背包才生成 Gizmo（避免重复）
            if (sameSetBackpacks.Count > 0 && sameSetBackpacks[0] == this)
            {
                yield return new Gizmo_AmmoBackpack 
                { 
                    compBackpack = this,
                    sameSetBackpacks = sameSetBackpacks
                };
            }
            else if (sameSetBackpacks.Count == 0)
            {
                // 没有弹药组的背包单独显示
                yield return new Gizmo_AmmoBackpack { compBackpack = this };
            }
        }
        
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (Wearer != null) yield break;
            
            yield return new Gizmo_AmmoBackpack { compBackpack = this };
        }
        
        #endregion
        
        #region 弹药名称格式化
        
        // 获取弹药的简短显示名称
        // caliber: 是否包含口径如"7.62mm FMJ"，否则只显示类型如"FMJ"
        public static string GetAmmoShortLabel(AmmoDef ammoDef, bool caliber = true)
        {
            if (ammoDef == null) return "";
            
            // 获取弹药类型名称（如 FMJ、AP、HP）
            string typeName = ammoDef.ammoClass?.LabelCap ?? "";
            
            if (!caliber || string.IsNullOrEmpty(typeName))
                return string.IsNullOrEmpty(typeName) ? ammoDef.LabelCap : typeName;
            
            // 尝试从 defName 提取口径信息
            string caliberStr = ExtractCaliber(ammoDef.defName);
            
            if (string.IsNullOrEmpty(caliberStr))
                return typeName;
            
            return $"{caliberStr} {typeName}";
        }
        
        // 从 defName 提取口径
        private static string ExtractCaliber(string defName)
        {
            if (string.IsNullOrEmpty(defName)) return "";
            
            // 常见口径模式
            var patterns = new[]
            {
                // 20x102mm 格式保持原样
                (@"(\d+)x(\d+)mm", "$1x$2mm"),
                // 762mm 转换为 7.62mm
                (@"(\d)(\d{2})mm", "$1.$2mm"),
                // 9mm 格式
                (@"(\d+)mm", "$1mm"),
                // .45 格式
                (@"\.(\d+)", ".$1"),
            };
            
            foreach (var (pattern, replacement) in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(defName, pattern, 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return System.Text.RegularExpressions.Regex.Replace(match.Value, pattern, replacement);
                }
            }
            
            return "";
        }
        
        #endregion
        
        #region 弹药兼容性检查
        
        public static bool IsAmmoCompatible(AmmoDef ammoDef)
        {
            if (ammoDef == null) return false;
            
            var defName = ammoDef.defName ?? "";
            var ammoClassName = ammoDef.ammoClass?.defName ?? "";
            
            if (ExcludedAmmoClasses.Any(ex => 
                ammoClassName.IndexOf(ex, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                defName.IndexOf(ex, System.StringComparison.OrdinalIgnoreCase) >= 0))
                return false;
            
            bool matchesPattern = AllowedAmmoPatterns.Any(pattern =>
                defName.IndexOf(pattern, System.StringComparison.OrdinalIgnoreCase) >= 0);
            
            if (!matchesPattern) return false;
            
            return true;
        }
        
        public static bool IsAmmoSetCompatible(AmmoSetDef ammoSet)
        {
            if (ammoSet == null) return false;
            if (ammoSet.isMortarAmmoSet) return false;
            
            foreach (var link in ammoSet.ammoTypes)
            {
                if (!IsAmmoCompatible(link.ammo))
                    return false;
            }
            
            return true;
        }
        
        #endregion
        
        #region 武器关联
        
        public void UpdateLinkedWeapon()
        {
            if (linkedAmmoSet != null) return;
            
            var wearer = Wearer;
            if (wearer?.equipment?.Primary == null) return;
            
            var compAmmo = wearer.equipment.Primary.TryGetComp<CompAmmoUser>();
            if (compAmmo?.Props?.ammoSet == null) return;
            
            var ammoSet = compAmmo.Props.ammoSet;
            if (!IsAmmoSetCompatible(ammoSet)) return;
            
            LinkedAmmoSet = ammoSet;
            
            if (selectedAmmo == null && linkedAmmoSet != null)
                selectedAmmo = linkedAmmoSet.ammoTypes.FirstOrDefault()?.ammo;
        }
        
        public AmmoSetDef GetCurrentAmmoSet()
        {
            // 优先使用已绑定的弹药组
            if (linkedAmmoSet != null)
                return linkedAmmoSet;
            
            // 从当前选择的弹药推断弹药组
            if (selectedAmmo != null)
            {
                var sets = selectedAmmo.AmmoSetDefs;
                if (sets != null && sets.Count > 0)
                    return sets.FirstOrDefault(s => IsAmmoSetCompatible(s));
            }
            
            // 从混装条目推断弹药组
            if (isMixMode && mixEntries.Count > 0)
            {
                var firstAmmo = mixEntries[0].AmmoDef;
                if (firstAmmo != null)
                {
                    var sets = firstAmmo.AmmoSetDefs;
                    if (sets != null && sets.Count > 0)
                        return sets.FirstOrDefault(s => IsAmmoSetCompatible(s));
                }
            }
            
            return GetWeaponModuleAmmoSet();
        }
        
        public AmmoSetDef GetWeaponModuleAmmoSet()
        {
            var wearer = Wearer;
            if (wearer?.apparel == null) return null;
            
            foreach (var apparel in wearer.apparel.WornApparel)
            {
                var compWeapon = apparel.TryGetComp<CompModuleWeapon>();
                if (compWeapon?.Weapon == null) continue;
                
                var compAmmo = compWeapon.Weapon.TryGetComp<CompAmmoUser>();
                if (compAmmo?.Props?.ammoSet != null && IsAmmoSetCompatible(compAmmo.Props.ammoSet))
                    return compAmmo.Props.ammoSet;
            }
            
            if (wearer.equipment?.Primary != null)
            {
                var compAmmo = wearer.equipment.Primary.TryGetComp<CompAmmoUser>();
                if (compAmmo?.Props?.ammoSet != null && IsAmmoSetCompatible(compAmmo.Props.ammoSet))
                    return compAmmo.Props.ammoSet;
            }
            
            return null;
        }
        
        public IEnumerable<AmmoDef> GetAllCompatibleAmmoTypes()
        {
            var weaponAmmoSet = GetWeaponModuleAmmoSet();
            var priorityAmmo = new HashSet<AmmoDef>();
            
            if (weaponAmmoSet != null)
            {
                foreach (var link in weaponAmmoSet.ammoTypes)
                {
                    if (IsAmmoCompatible(link.ammo))
                    {
                        priorityAmmo.Add(link.ammo);
                        yield return link.ammo;
                    }
                }
            }
            
            foreach (var ammoDef in DefDatabase<AmmoDef>.AllDefs)
            {
                if (priorityAmmo.Contains(ammoDef)) continue;
                if (!IsAmmoCompatible(ammoDef)) continue;
                
                yield return ammoDef;
            }
        }
        
        public IEnumerable<AmmoDef> GetAvailableAmmoTypes() => GetAllCompatibleAmmoTypes();
        
        #endregion
        
        #region 私有方法
        
        private IEnumerable<Thing> GetAvailableAmmoFromStorage()
        {
            if (selectedAmmo == null) yield break;
            
            var wearer = Wearer;
            if (wearer == null) yield break;
            
            var map = wearer.MapHeld;
            if (map == null) yield break;
            
            foreach (var thing in map.listerThings.ThingsOfDef(selectedAmmo))
            {
                if (thing.Spawned && !thing.IsForbidden(Faction.OfPlayer))
                    yield return thing;
            }
        }
        
        private void LoadAmmoFromStack(Thing ammoStack)
        {
            if (ammoStack == null || ammoStack.def != selectedAmmo) return;
            
            int needed = MaxCapacity - currentAmmoCount;
            if (needed <= 0) return;
            
            int toLoad = Mathf.Min(needed, ammoStack.stackCount);
            
            ammoStack.SplitOff(toLoad).Destroy(DestroyMode.Vanish);
            currentAmmoCount += toLoad;
            
            SoundDefOf.Click.PlayOneShotOnCamera();
        }
        
        #endregion
        
        #region IModuleDataTransfer 实现
        
        // 调试开关
        private const bool TransferDebugLog = false;
        
        private static void LogTransfer(string message)
        {
            if (TransferDebugLog)
                Verse.Log.Message($"[AmmoBackpack.Transfer] {message}");
        }
        
        // 临时存储转换数据
        private AmmoDef savedSelectedAmmo;
        private int savedCurrentAmmoCount;
        private AmmoSetDef savedLinkedAmmoSet;
        private AmmoDef savedPendingAmmo;
        private bool savedIsMixMode;
        private List<AmmoMixEntry> savedMixEntries;
        private int savedMixFireIndex;
        private bool savedNeedsEjectToEmpty;
        private bool savedIsActiveBackpack;
        
        public void SaveDataFrom(Thing source)
        {
            LogTransfer($"SaveDataFrom: source={source.def.defName}, this.parent={parent.def.defName}");
            
            // 直接使用 this 的数据，因为 this 就是 source 的组件
            savedSelectedAmmo = selectedAmmo;
            savedCurrentAmmoCount = currentAmmoCount;
            savedLinkedAmmoSet = linkedAmmoSet;
            savedPendingAmmo = pendingAmmo;
            savedIsMixMode = isMixMode;
            savedMixFireIndex = mixFireIndex;
            savedNeedsEjectToEmpty = needsEjectToEmpty;
            savedIsActiveBackpack = isActiveBackpack;
            
            // 深拷贝混装条目
            savedMixEntries = new List<AmmoMixEntry>();
            if (mixEntries != null)
            {
                foreach (var entry in mixEntries)
                {
                    savedMixEntries.Add(entry.DeepCopy());
                }
            }
            
            if (savedIsMixMode)
            {
                int totalAmmo = savedMixEntries?.Sum(e => e.CurrentCount) ?? 0;
                LogTransfer($"SaveDataFrom: 混装模式, 槽位数={savedMixEntries?.Count ?? 0}, 总弹药={totalAmmo}");
            }
            else
            {
                LogTransfer($"SaveDataFrom: 普通模式, 弹药={savedSelectedAmmo?.defName}, 数量={savedCurrentAmmoCount}");
            }
        }
        
        public void RestoreDataTo(Thing target)
        {
            LogTransfer($"RestoreDataTo: target={target.def.defName}, this.parent={parent.def.defName}");
            
            var targetComp = target.TryGetComp<CompAmmoBackpack>();
            if (targetComp == null)
            {
                LogTransfer($"RestoreDataTo: target 没有 CompAmmoBackpack 组件!");
                return;
            }
            
            LogTransfer($"RestoreDataTo: 恢复数据, 混装={savedIsMixMode}, 弹药={savedSelectedAmmo?.defName}, 数量={savedCurrentAmmoCount}");
            
            targetComp.selectedAmmo = savedSelectedAmmo;
            targetComp.currentAmmoCount = savedCurrentAmmoCount;
            targetComp.linkedAmmoSet = savedLinkedAmmoSet;
            targetComp.pendingAmmo = savedPendingAmmo;
            targetComp.isMixMode = savedIsMixMode;
            targetComp.mixFireIndex = savedMixFireIndex;
            targetComp.needsEjectToEmpty = savedNeedsEjectToEmpty;
            targetComp.isActiveBackpack = savedIsActiveBackpack;
            
            // 恢复混装条目
            targetComp.mixEntries = new List<AmmoMixEntry>();
            if (savedMixEntries != null)
            {
                foreach (var entry in savedMixEntries)
                {
                    targetComp.mixEntries.Add(entry.DeepCopy());
                }
            }
            
            // 重新计算缓存
            if (targetComp.selectedAmmo != null)
            {
                targetComp.cachedMaxCapacity = targetComp.CalculateMaxCapacity(targetComp.selectedAmmo);
            }
            
            LogTransfer($"RestoreDataTo: 完成, targetComp.IsMixMode={targetComp.isMixMode}, targetComp.TotalAmmoCount={targetComp.TotalAmmoCount}");
        }
        
        #endregion
    }
}
