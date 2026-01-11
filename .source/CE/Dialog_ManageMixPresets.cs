using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Exosuit.CE
{
    // 管理混装预设对话框
    public class Dialog_ManageMixPresets : Window
    {
        #region 常量
        
        private const float ButtonWidth = 60f;
        
        #endregion
        
        #region 字段
        
        private Vector2 scrollPosition;
        private string renamingPreset;
        private string newName = "";
        
        #endregion
        
        #region 属性
        
        public override Vector2 InitialSize => new(450f, 400f);
        
        #endregion
        
        #region 构造函数
        
        public Dialog_ManageMixPresets()
        {
            doCloseButton = true;
            doCloseX = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
        }
        
        #endregion
        
        #region 公共方法
        
        public override void DoWindowContents(Rect inRect)
        {
            var manager = AmmoMixPresetManager.Instance;
            if (manager == null)
            {
                Widgets.Label(inRect, "WG_AmmoBackpack_NoPresetManager".Translate());
                return;
            }
            
            Text.Font = GameFont.Small;
            float curY = 0f;
            
            // 标题
            Rect titleRect = new(0f, curY, inRect.width, 28f);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(titleRect, "WG_AmmoBackpack_ManagePresetsTitle".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            curY += 32f;
            
            // 预设列表
            var presets = manager.Presets.ToList();
            
            if (presets.Count == 0)
            {
                Rect emptyRect = new(0f, curY, inRect.width, 40f);
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = Color.gray;
                Widgets.Label(emptyRect, "WG_AmmoBackpack_NoPresets".Translate());
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }
            
            float listHeight = inRect.height - curY - 40f;
            float viewHeight = CalculateTotalHeight(presets);
            Rect viewRect = new(0f, 0f, inRect.width - 16f, viewHeight);
            Rect scrollRect = new(0f, curY, inRect.width, listHeight);
            
            Widgets.BeginScrollView(scrollRect, ref scrollPosition, viewRect);
            
            float itemY = 0f;
            foreach (var preset in presets)
            {
                float rowHeight = CalculateRowHeight(preset);
                Rect rowRect = new(0f, itemY, viewRect.width, rowHeight);
                DrawPresetRow(rowRect, preset, manager);
                itemY += rowHeight + 4f;
            }
            
            Widgets.EndScrollView();
        }
        
        #endregion
        
        #region 私有方法
        
        private float CalculateTotalHeight(List<AmmoMixPreset> presets)
        {
            float total = 0f;
            foreach (var preset in presets)
            {
                total += CalculateRowHeight(preset) + 4f;
            }
            return total;
        }
        
        private float CalculateRowHeight(AmmoMixPreset preset)
        {
            // 名称行 + 混装列表行 + 按钮行
            int entryCount = Mathf.Max(preset.Entries.Count, 1);
            return 24f + entryCount * 18f + 26f;
        }
        
        private void DrawPresetRow(Rect rect, AmmoMixPreset preset, AmmoMixPresetManager manager)
        {
            // 背景
            Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.1f, 0.1f, 0.5f));
            Widgets.DrawHighlightIfMouseover(rect);
            
            // 正在重命名
            if (renamingPreset == preset.Name)
            {
                DrawRenameRow(rect, preset, manager);
                return;
            }
            
            float curY = rect.y + 2f;
            float innerWidth = rect.width - 8f;
            
            // 第一行：预设名称 + 跨存档标记
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            
            string nameLabel = preset.Name;
            if (preset.IsGlobal)
                nameLabel += " [" + "WG_AmmoBackpack_Global".Translate() + "]";
            
            Rect nameRect = new(rect.x + 4f, curY, innerWidth, 22f);
            Widgets.Label(nameRect, nameLabel);
            curY += 24f;
            
            // 第二行：混装列表（多行）
            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            
            foreach (var entry in preset.Entries)
            {
                string label = entry.IsWildcard 
                    ? "?" 
                    : (entry.AmmoDef?.ammoClass?.LabelCap ?? "?");
                string entryText = $"  {label} ×{entry.Ratio}";
                
                Rect entryRect = new(rect.x + 4f, curY, innerWidth, 18f);
                Widgets.Label(entryRect, entryText);
                curY += 18f;
            }
            
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            
            // 第三行：按钮
            float btnY = curY + 2f;
            float btnX = rect.x + 4f;
            
            // 跨存档复选框
            Rect globalRect = new(btnX, btnY, 100f, 22f);
            bool newGlobal = preset.IsGlobal;
            Widgets.CheckboxLabeled(globalRect, "WG_AmmoBackpack_Global".Translate(), ref newGlobal);
            if (newGlobal != preset.IsGlobal)
            {
                manager.ToggleGlobal(preset.Name);
            }
            btnX += 104f;
            
            // 重命名按钮
            Rect renameRect = new(rect.xMax - ButtonWidth * 2 - 12f, btnY, ButtonWidth, 22f);
            if (Widgets.ButtonText(renameRect, "WG_AmmoBackpack_Rename".Translate()))
            {
                renamingPreset = preset.Name;
                newName = preset.Name;
            }
            
            // 删除按钮
            Rect deleteRect = new(rect.xMax - ButtonWidth - 4f, btnY, ButtonWidth, 22f);
            GUI.color = new Color(1f, 0.5f, 0.5f);
            if (Widgets.ButtonText(deleteRect, "WG_AmmoBackpack_Delete".Translate()))
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    "WG_AmmoBackpack_DeletePresetConfirm".Translate(preset.Name),
                    () => manager.RemovePreset(preset.Name),
                    true));
            }
            GUI.color = Color.white;
            
            Text.Anchor = TextAnchor.UpperLeft;
        }
        
        private void DrawRenameRow(Rect rect, AmmoMixPreset preset, AmmoMixPresetManager manager)
        {
            float x = rect.x + 4f;
            float y = rect.y + 4f;
            
            // 名称输入框
            Rect inputRect = new(x, y, 180f, 24f);
            newName = Widgets.TextField(inputRect, newName);
            x += 184f;
            
            // 确认按钮
            Rect confirmRect = new(x, y, 50f, 24f);
            bool canConfirm = !string.IsNullOrWhiteSpace(newName) && newName != preset.Name;
            
            if (!canConfirm) GUI.color = Color.gray;
            if (Widgets.ButtonText(confirmRect, "✓") && canConfirm)
            {
                if (manager.RenamePreset(preset.Name, newName))
                {
                    renamingPreset = null;
                    newName = "";
                }
                else
                {
                    Messages.Message("WG_AmmoBackpack_RenameExists".Translate(), 
                        MessageTypeDefOf.RejectInput, false);
                }
            }
            GUI.color = Color.white;
            x += 54f;
            
            // 取消按钮
            Rect cancelRect = new(x, y, 50f, 24f);
            if (Widgets.ButtonText(cancelRect, "✗"))
            {
                renamingPreset = null;
                newName = "";
            }
        }
        
        #endregion
    }
}
