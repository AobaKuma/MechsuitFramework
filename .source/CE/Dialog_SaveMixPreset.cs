using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Exosuit.CE
{
    // 保存混装预设对话框
    public class Dialog_SaveMixPreset : Window
    {
        #region 字段
        
        private readonly CompAmmoBackpack backpack;
        private string presetName = "";
        private bool focusField = true;
        
        #endregion
        
        #region 属性
        
        public override Vector2 InitialSize => new(300f, 150f);
        
        #endregion
        
        #region 构造函数
        
        public Dialog_SaveMixPreset(CompAmmoBackpack backpack)
        {
            this.backpack = backpack;
            
            doCloseButton = false;
            doCloseX = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            
            // 生成默认名称
            presetName = GenerateDefaultName();
        }
        
        #endregion
        
        #region 公共方法
        
        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            float curY = 0f;
            
            // 标题
            Rect titleRect = new(0f, curY, inRect.width, 28f);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(titleRect, "WG_AmmoBackpack_SavePresetTitle".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            curY += 32f;
            
            // 名称输入
            Rect labelRect = new(0f, curY, 60f, 28f);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, "WG_AmmoBackpack_PresetName".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            
            Rect inputRect = new(64f, curY, inRect.width - 64f, 28f);
            GUI.SetNextControlName("PresetNameField");
            presetName = Widgets.TextField(inputRect, presetName);
            
            if (focusField)
            {
                GUI.FocusControl("PresetNameField");
                focusField = false;
            }
            
            curY += 36f;
            
            // 按钮
            float buttonWidth = (inRect.width - 8f) / 2f;
            
            Rect cancelRect = new(0f, curY, buttonWidth, 30f);
            if (Widgets.ButtonText(cancelRect, "WG_AmmoBackpack_Cancel".Translate()))
                Close();
            
            Rect saveRect = new(buttonWidth + 8f, curY, buttonWidth, 30f);
            bool canSave = !string.IsNullOrWhiteSpace(presetName);
            
            if (!canSave) GUI.color = Color.gray;
            if (Widgets.ButtonText(saveRect, "WG_AmmoBackpack_Save".Translate()) && canSave)
                TrySavePreset();
            GUI.color = Color.white;
            
            // 回车确认
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return && canSave)
            {
                TrySavePreset();
                Event.current.Use();
            }
        }
        
        #endregion
        
        #region 私有方法
        
        private string GenerateDefaultName()
        {
            var manager = AmmoMixPresetManager.Instance;
            int index = manager?.Presets.Count + 1 ?? 1;
            return $"{"WG_AmmoBackpack_PresetDefault".Translate()} {index}";
        }
        
        private void TrySavePreset()
        {
            var manager = AmmoMixPresetManager.Instance;
            if (manager == null)
            {
                Messages.Message("WG_AmmoBackpack_PresetSaveFailed".Translate(), 
                    MessageTypeDefOf.RejectInput, false);
                return;
            }
            
            // 检查名称是否已存在
            var existing = manager.GetPreset(presetName);
            if (existing != null)
            {
                // 询问是否覆盖
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    "WG_AmmoBackpack_PresetOverwrite".Translate(presetName),
                    () => DoSavePreset(true),
                    true));
                return;
            }
            
            DoSavePreset(false);
        }
        
        private void DoSavePreset(bool overwrite)
        {
            var manager = AmmoMixPresetManager.Instance;
            if (manager == null) return;
            
            var preset = AmmoMixPreset.FromBackpack(backpack, presetName);
            if (preset == null)
            {
                Messages.Message("WG_AmmoBackpack_PresetSaveFailed".Translate(), 
                    MessageTypeDefOf.RejectInput, false);
                return;
            }
            
            bool success;
            if (overwrite)
                success = manager.OverwritePreset(presetName, preset);
            else
                success = manager.AddPreset(preset);
            
            if (success)
            {
                SoundDefOf.Click.PlayOneShotOnCamera();
                Messages.Message("WG_AmmoBackpack_PresetSaved".Translate(presetName), 
                    MessageTypeDefOf.PositiveEvent, false);
                Close();
            }
            else
            {
                Messages.Message("WG_AmmoBackpack_PresetSaveFailed".Translate(), 
                    MessageTypeDefOf.RejectInput, false);
            }
        }
        
        #endregion
    }
}
