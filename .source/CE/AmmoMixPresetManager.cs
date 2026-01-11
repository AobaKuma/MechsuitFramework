using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Exosuit.CE
{
    // 混装预设管理器，合并本地和全局预设
    public class AmmoMixPresetManager : GameComponent
    {
        #region 单例
        
        private static AmmoMixPresetManager instance;
        public static AmmoMixPresetManager Instance => instance;
        
        #endregion
        
        #region 字段
        
        private List<AmmoMixPreset> localPresets = new();
        
        #endregion
        
        #region 属性
        
        // 合并本地和全局预设
        public IReadOnlyList<AmmoMixPreset> Presets
        {
            get
            {
                var all = new List<AmmoMixPreset>();
                
                // 添加全局预设
                var global = ExosuitCEMod.GlobalPresets;
                if (global != null)
                    all.AddRange(global.Presets);
                
                // 添加本地预设
                all.AddRange(localPresets);
                
                return all;
            }
        }
        
        #endregion
        
        #region 构造函数
        
        public AmmoMixPresetManager(Game game)
        {
            instance = this;
        }
        
        #endregion
        
        #region 公共方法
        
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref localPresets, "ammoMixPresets", LookMode.Deep);
            
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (localPresets == null)
                    localPresets = new List<AmmoMixPreset>();
                
                // 确保本地预设标记正确
                foreach (var preset in localPresets)
                    preset.IsGlobal = false;
                
                instance = this;
            }
        }
        
        // 添加预设
        public bool AddPreset(AmmoMixPreset preset)
        {
            if (preset == null || !preset.IsValid) return false;
            
            // 检查名称是否重复（包括全局）
            if (Presets.Any(p => p.Name == preset.Name))
            {
                Log.Warning($"[AmmoBackpack] 预设名称已存在: {preset.Name}");
                return false;
            }
            
            if (preset.IsGlobal)
            {
                var global = ExosuitCEMod.GlobalPresets;
                if (global == null) return false;
                return global.AddPreset(preset);
            }
            
            localPresets.Add(preset);
            Log.Message($"[AmmoBackpack] 保存本地预设: {preset.Name}");
            return true;
        }
        
        // 删除预设
        public bool RemovePreset(string name)
        {
            // 先检查全局
            var global = ExosuitCEMod.GlobalPresets;
            if (global?.GetPreset(name) != null)
                return global.RemovePreset(name);
            
            // 再检查本地
            var preset = localPresets.FirstOrDefault(p => p.Name == name);
            if (preset == null) return false;
            
            localPresets.Remove(preset);
            Log.Message($"[AmmoBackpack] 删除本地预设: {name}");
            return true;
        }
        
        // 获取预设
        public AmmoMixPreset GetPreset(string name)
        {
            // 先检查全局
            var global = ExosuitCEMod.GlobalPresets;
            var preset = global?.GetPreset(name);
            if (preset != null) return preset;
            
            // 再检查本地
            return localPresets.FirstOrDefault(p => p.Name == name);
        }
        
        // 获取指定弹药组的预设
        public IEnumerable<AmmoMixPreset> GetPresetsForAmmoSet(string ammoSetDefName)
        {
            var all = Presets;
            
            if (string.IsNullOrEmpty(ammoSetDefName))
                return all;
            
            return all.Where(p => 
                string.IsNullOrEmpty(p.AmmoSetDefName) || 
                p.AmmoSetDefName == ammoSetDefName);
        }
        
        // 重命名预设
        public bool RenamePreset(string oldName, string newName)
        {
            if (string.IsNullOrEmpty(newName)) return false;
            if (Presets.Any(p => p.Name == newName)) return false;
            
            // 先检查全局
            var global = ExosuitCEMod.GlobalPresets;
            if (global?.GetPreset(oldName) != null)
                return global.RenamePreset(oldName, newName);
            
            // 再检查本地
            var preset = localPresets.FirstOrDefault(p => p.Name == oldName);
            if (preset == null) return false;
            
            preset.Name = newName;
            return true;
        }
        
        // 切换预设的全局/本地状态
        public bool ToggleGlobal(string name)
        {
            var global = ExosuitCEMod.GlobalPresets;
            if (global == null) return false;
            
            // 检查是否在全局列表
            var globalPreset = global.GetPreset(name);
            if (globalPreset != null)
            {
                // 从全局移到本地
                global.RemovePreset(name);
                globalPreset.IsGlobal = false;
                localPresets.Add(globalPreset);
                Log.Message($"[AmmoBackpack] 预设 {name} 已移至本地");
                return true;
            }
            
            // 检查是否在本地列表
            var localPreset = localPresets.FirstOrDefault(p => p.Name == name);
            if (localPreset != null)
            {
                // 从本地移到全局
                localPresets.Remove(localPreset);
                localPreset.IsGlobal = true;
                global.AddPreset(localPreset);
                Log.Message($"[AmmoBackpack] 预设 {name} 已移至全局");
                return true;
            }
            
            return false;
        }
        
        // 覆盖预设
        public bool OverwritePreset(string name, AmmoMixPreset newPreset)
        {
            var global = ExosuitCEMod.GlobalPresets;
            
            // 检查全局
            if (global?.GetPreset(name) != null)
            {
                global.RemovePreset(name);
                newPreset.Name = name;
                newPreset.IsGlobal = true;
                return global.AddPreset(newPreset);
            }
            
            // 检查本地
            var existing = localPresets.FirstOrDefault(p => p.Name == name);
            if (existing == null) return false;
            
            int index = localPresets.IndexOf(existing);
            newPreset.Name = name;
            newPreset.IsGlobal = false;
            localPresets[index] = newPreset;
            return true;
        }
        
        #endregion
    }
}
