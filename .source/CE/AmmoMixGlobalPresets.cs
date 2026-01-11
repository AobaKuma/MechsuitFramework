using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Exosuit.CE
{
    // 全局预设存储，跨存档保存
    public class AmmoMixGlobalPresets : ModSettings
    {
        #region 字段
        
        private List<AmmoMixPreset> globalPresets = new();
        
        #endregion
        
        #region 属性
        
        public IReadOnlyList<AmmoMixPreset> Presets => globalPresets;
        
        #endregion
        
        #region 公共方法
        
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref globalPresets, "globalAmmoMixPresets", LookMode.Deep);
            
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (globalPresets == null)
                    globalPresets = new List<AmmoMixPreset>();
                
                // 确保所有全局预设标记正确
                foreach (var preset in globalPresets)
                    preset.IsGlobal = true;
            }
        }
        
        public bool AddPreset(AmmoMixPreset preset)
        {
            if (preset == null || !preset.IsValid) return false;
            if (globalPresets.Any(p => p.Name == preset.Name)) return false;
            
            preset.IsGlobal = true;
            globalPresets.Add(preset);
            Write();
            return true;
        }
        
        public bool RemovePreset(string name)
        {
            var preset = globalPresets.FirstOrDefault(p => p.Name == name);
            if (preset == null) return false;
            
            globalPresets.Remove(preset);
            Write();
            return true;
        }
        
        public AmmoMixPreset GetPreset(string name)
        {
            return globalPresets.FirstOrDefault(p => p.Name == name);
        }
        
        public bool RenamePreset(string oldName, string newName)
        {
            if (string.IsNullOrEmpty(newName)) return false;
            if (globalPresets.Any(p => p.Name == newName)) return false;
            
            var preset = globalPresets.FirstOrDefault(p => p.Name == oldName);
            if (preset == null) return false;
            
            preset.Name = newName;
            Write();
            return true;
        }
        
        #endregion
    }
    
    // Mod主类，持有全局设置
    public class ExosuitCEMod : Mod
    {
        #region 单例
        
        private static AmmoMixGlobalPresets settings;
        public static AmmoMixGlobalPresets GlobalPresets => settings;
        
        #endregion
        
        #region 构造函数
        
        public ExosuitCEMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<AmmoMixGlobalPresets>();
        }
        
        #endregion
        
        #region 公共方法
        
        public override string SettingsCategory() => "Exosuit CE";
        
        public override void DoSettingsWindowContents(UnityEngine.Rect inRect)
        {
            // 暂不需要设置界面
        }
        
        #endregion
    }
}
