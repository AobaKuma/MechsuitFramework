using System.Collections.Generic;
using System.Linq;
using CombatExtended;
using Verse;

namespace Exosuit.CE
{
    // 混装预设数据，存储混装配置以便复用
    public class AmmoMixPreset : IExposable
    {
        #region 字段
        
        // 预设名称
        public string Name = "";
        
        // 关联的弹药组 defName
        public string AmmoSetDefName = "";
        
        // 槽位配置列表
        public List<AmmoMixPresetEntry> Entries = new();
        
        // 是否跨存档保存
        public bool IsGlobal;
        
        #endregion
        
        #region 属性
        
        // 获取关联的弹药组
        public AmmoSetDef AmmoSet => DefDatabase<AmmoSetDef>.GetNamedSilentFail(AmmoSetDefName);
        
        // 预设是否有效
        public bool IsValid => !string.IsNullOrEmpty(Name) && Entries.Count > 0;
        
        #endregion
        
        #region 公共方法
        
        public void ExposeData()
        {
            Scribe_Values.Look(ref Name, "name", "");
            Scribe_Values.Look(ref AmmoSetDefName, "ammoSetDefName", "");
            Scribe_Values.Look(ref IsGlobal, "isGlobal", false);
            Scribe_Collections.Look(ref Entries, "entries", LookMode.Deep);
            
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (Entries == null)
                    Entries = new List<AmmoMixPresetEntry>();
            }
        }
        
        // 从弹药背包创建预设
        public static AmmoMixPreset FromBackpack(CompAmmoBackpack backpack, string name)
        {
            if (backpack == null || !backpack.IsMixMode) return null;
            
            var preset = new AmmoMixPreset
            {
                Name = name,
                AmmoSetDefName = backpack.LinkedAmmoSet?.defName ?? ""
            };
            
            foreach (var entry in backpack.MixEntries)
            {
                preset.Entries.Add(new AmmoMixPresetEntry
                {
                    AmmoDefName = entry.AmmoDef?.defName ?? "",
                    Ratio = entry.Ratio,
                    IsWildcard = entry.IsWildcard
                });
            }
            
            return preset;
        }
        
        // 应用预设到弹药背包
        public bool ApplyTo(CompAmmoBackpack backpack)
        {
            if (backpack == null) return false;
            if (backpack.TotalAmmoCount > 0) return false;
            
            // 检查弹药组是否匹配
            var ammoSet = AmmoSet;
            if (ammoSet == null) return false;
            
            // 启用混装模式
            if (!backpack.IsMixMode)
                backpack.EnableMixMode();
            
            // 设置弹药组
            backpack.LinkedAmmoSet = ammoSet;
            
            // 清空现有槽位
            backpack.ClearMixEntries();
            
            // 添加预设槽位
            foreach (var presetEntry in Entries)
            {
                AmmoDef ammoDef = null;
                if (!string.IsNullOrEmpty(presetEntry.AmmoDefName))
                    ammoDef = DefDatabase<AmmoDef>.GetNamedSilentFail(presetEntry.AmmoDefName);
                
                backpack.AddMixEntryFromPreset(ammoDef, presetEntry.Ratio, presetEntry.IsWildcard);
            }
            
            backpack.RecalculateMixCapacities();
            return true;
        }
        
        // 获取预设描述
        public string GetDescription()
        {
            if (Entries.Count == 0) return "WG_AmmoBackpack_PresetEmpty".Translate();
            
            var parts = new List<string>();
            foreach (var entry in Entries)
            {
                string label = entry.IsWildcard 
                    ? "?" 
                    : (entry.AmmoDef?.ammoClass?.LabelCap ?? "?");
                parts.Add($"{label}×{entry.Ratio}");
            }
            
            return string.Join(" : ", parts);
        }
        
        // 获取预设描述（多行格式）
        public string GetDescriptionMultiLine()
        {
            if (Entries.Count == 0) return "WG_AmmoBackpack_PresetEmpty".Translate();
            
            var lines = new List<string>();
            foreach (var entry in Entries)
            {
                string label = entry.IsWildcard 
                    ? "?" 
                    : (entry.AmmoDef?.ammoClass?.LabelCap ?? "?");
                lines.Add($"  {label} ×{entry.Ratio}");
            }
            
            return string.Join("\n", lines);
        }
        
        #endregion
    }
    
    // 预设槽位条目
    public class AmmoMixPresetEntry : IExposable
    {
        public string AmmoDefName = "";
        public int Ratio = 1;
        public bool IsWildcard;
        
        // 获取弹药定义
        public AmmoDef AmmoDef => DefDatabase<AmmoDef>.GetNamedSilentFail(AmmoDefName);
        
        public void ExposeData()
        {
            Scribe_Values.Look(ref AmmoDefName, "ammoDefName", "");
            Scribe_Values.Look(ref Ratio, "ratio", 1);
            Scribe_Values.Look(ref IsWildcard, "isWildcard", false);
        }
    }
}
