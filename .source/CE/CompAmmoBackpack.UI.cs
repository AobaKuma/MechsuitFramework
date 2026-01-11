using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Exosuit.CE
{
    // CompAmmoBackpack的UI绘制部分
    public partial class CompAmmoBackpack
    {
        #region IModuleExtensionUI 实现
        
        // 当前选中显示的背包索引
        private static int selectedBackpackIndex = 0;
        
        public bool ShouldShowExtensionUI => true;
        
        public float ExtensionUIWidth => ExtensionWidth;
        
        public string ExtensionUITitle => "WG_AmmoBackpack_EditorTitle".Translate();
        
        public void DrawExtensionUI(Rect rect)
        {
            var wearer = Wearer;
            if (wearer == null) return;
            
            var allBackpacks = CEPatches.GetAllAmmoBackpacks(wearer);
            
            if (selectedBackpackIndex >= allBackpacks.Count)
                selectedBackpackIndex = 0;
            
            var targetBackpack = allBackpacks.Count > 1 ? allBackpacks[selectedBackpackIndex] : this;
            targetBackpack.UpdateLinkedWeapon();
            
            float curY = rect.y;
            
            // 多背包选择器
            DrawBackpackSelector(ref curY, rect, allBackpacks, targetBackpack);
            
            // 武器信息 + 退弹按钮（横向布局）
            targetBackpack.DrawWeaponAndEjectRow(ref curY, rect);
            curY += 6f;
            
            // 模式切换
            targetBackpack.DrawModeToggle(ref curY, rect);
            curY += 6f;
            
            if (targetBackpack.isMixMode)
                targetBackpack.DrawMixModeUI(ref curY, rect);
            else
            {
                targetBackpack.DrawAmmoSelector(ref curY, rect);
                curY += 6f;
                targetBackpack.DrawAmmoBar(ref curY, rect);
            }
        }
        
        #endregion
        
        #region UI 绘制方法
        
        // 多背包选择器
        private void DrawBackpackSelector(ref float curY, Rect rect, 
            System.Collections.Generic.List<CompAmmoBackpack> allBackpacks, CompAmmoBackpack targetBackpack)
        {
            Text.Font = GameFont.Tiny;
            
            // 只有一个背包时也显示，但不可点击切换
            if (allBackpacks.Count <= 1)
            {
                Rect btnRect = new(rect.x, curY, rect.width, 22f);
                GUI.color = Color.green;
                Widgets.ButtonText(btnRect, BackpackDisplayName);
                GUI.color = Color.white;
                curY += 26f;
                return;
            }
            
            float buttonWidth = rect.width / allBackpacks.Count;
            float x = rect.x;
            
            for (int i = 0; i < allBackpacks.Count; i++)
            {
                var bp = allBackpacks[i];
                Rect btnRect = new(x, curY, buttonWidth - 2f, 22f);
                
                bool isSelected = (i == selectedBackpackIndex);
                bool isActive = bp.IsActiveBackpack;
                
                if (isSelected)
                    Widgets.DrawBox(btnRect, 2);
                
                GUI.color = isActive ? Color.green : (isSelected ? Color.white : Color.gray);
                
                string label = bp.BackpackDisplayName;
                if (Widgets.ButtonText(btnRect, label))
                {
                    selectedBackpackIndex = i;
                    foreach (var b in allBackpacks)
                        b.IsActiveBackpack = false;
                    bp.IsActiveBackpack = true;
                    SoundDefOf.Click.PlayOneShotOnCamera();
                }
                
                GUI.color = Color.white;
                x += buttonWidth;
            }
            
            curY += 26f;
        }
        
        // 武器信息+退弹按钮
        private void DrawWeaponAndEjectRow(ref float curY, Rect rect)
        {
            Text.Font = GameFont.Tiny;
            
            var weaponAmmoSet = GetWeaponModuleAmmoSet();
            bool hasAmmo = TotalAmmoCount > 0;
            
            // 计算布局：左侧武器信息，右侧退弹按钮
            float ejectWidth = hasAmmo ? 70f : 0f;
            float infoWidth = rect.width - ejectWidth - (hasAmmo ? 4f : 0f);
            
            // 武器信息
            Rect infoRect = new(rect.x, curY, infoWidth, 24f);
            string weaponLabel;
            if (weaponAmmoSet != null)
            {
                weaponLabel = "WG_AmmoBackpack_WeaponModule".Translate(weaponAmmoSet.LabelCap);
                GUI.color = Color.green;
            }
            else
            {
                weaponLabel = "WG_AmmoBackpack_NoWeaponModule".Translate();
                GUI.color = Color.gray;
            }
            
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(infoRect, weaponLabel);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            
            // 退弹按钮
            if (hasAmmo)
            {
                Rect ejectRect = new(rect.x + infoWidth + 4f, curY, ejectWidth, 24f);
                GUI.color = new Color(1f, 0.6f, 0.3f);
                if (Widgets.ButtonText(ejectRect, "WG_AmmoBackpack_Eject".Translate()))
                    needsEjectToEmpty = true;
                GUI.color = Color.white;
            }
            
            curY += 28f;
        }
        
        private void DrawModeToggle(ref float curY, Rect rect)
        {
            Text.Font = GameFont.Small;
            
            // 单装模式下清空类型按钮
            bool showClearBtn = !isMixMode && selectedAmmo != null && TotalAmmoCount == 0;
            
            float clearWidth = showClearBtn ? 70f : 0f;
            float modeWidth = (rect.width - clearWidth - (showClearBtn ? 4f : 0f) - 4f) / 2f;
            float x = rect.x;
            
            // 普通模式按钮
            Rect normalRect = new(x, curY, modeWidth, 24f);
            GUI.color = isMixMode ? Color.gray : Color.green;
            if (Widgets.ButtonText(normalRect, "WG_AmmoBackpack_NormalMode".Translate()))
            {
                if (isMixMode) DisableMixMode();
            }
            x += modeWidth + 4f;
            
            // 混装模式按钮
            Rect mixRect = new(x, curY, modeWidth, 24f);
            GUI.color = isMixMode ? Color.green : Color.gray;
            if (Widgets.ButtonText(mixRect, "WG_AmmoBackpack_MixModeBtn".Translate()))
            {
                if (!isMixMode) EnableMixMode();
            }
            x += modeWidth + 4f;
            
            // 清空类型按钮
            if (showClearBtn)
            {
                Rect clearRect = new(x, curY, clearWidth, 24f);
                GUI.color = new Color(0.8f, 0.5f, 0.3f);
                if (Widgets.ButtonText(clearRect, "WG_AmmoBackpack_ClearType".Translate()))
                {
                    selectedAmmo = null;
                    pendingAmmo = null;
                    cachedMaxCapacity = 0;
                }
            }
            
            GUI.color = Color.white;
            curY += 28f;
        }
        
        private void DrawAmmoSelector(ref float curY, Rect rect)
        {
            Text.Font = GameFont.Small;
            
            string label = selectedAmmo == null 
                ? "WG_AmmoBackpack_SelectAmmo".Translate() 
                : CompAmmoBackpack.GetAmmoShortLabel(selectedAmmo, true);
            Texture2D icon = selectedAmmo?.uiIcon;
            
            Rect buttonRect = new(rect.x, curY, rect.width, 28f);
            
            if (icon != null)
            {
                Rect iconRect = new(buttonRect.x + 4f, buttonRect.y + 4f, 20f, 20f);
                GUI.DrawTexture(iconRect, icon);
                buttonRect.x += 26f;
                buttonRect.width -= 26f;
            }
            
            if (Widgets.ButtonText(buttonRect, label))
                ShowAmmoMenu();
            
            curY += 32f;
            
            // 待替换弹药信息
            if (NeedsClear && pendingAmmo != null)
            {
                Text.Font = GameFont.Tiny;
                GUI.color = new Color(1f, 0.6f, 0.2f);
                
                string pendingLabel = "WG_AmmoBackpack_PendingSwitch".Translate(pendingAmmo.LabelCap);
                float textHeight = Text.CalcHeight(pendingLabel, rect.width);
                Rect pendingRect = new(rect.x, curY, rect.width, textHeight);
                Widgets.Label(pendingRect, pendingLabel);
                
                GUI.color = Color.white;
                curY += textHeight + 2f;
            }
        }
        
        private void DrawAmmoBar(ref float curY, Rect rect)
        {
            Rect barRect = new(rect.x, curY, rect.width, 16f);
            
            float fillPercent = MaxCapacity > 0 
                ? (float)currentAmmoCount / MaxCapacity 
                : 0f;
            
            Widgets.DrawBoxSolid(barRect, new Color(0.1f, 0.1f, 0.1f));
            
            Rect fillRect = barRect;
            fillRect.width *= fillPercent;
            
            Color fillColor = fillPercent > 0.5f 
                ? new Color(0.2f, 0.6f, 0.2f)
                : fillPercent > 0.2f 
                    ? new Color(0.6f, 0.6f, 0.2f) 
                    : new Color(0.6f, 0.2f, 0.2f);
            
            Widgets.DrawBoxSolid(fillRect, fillColor);
            Widgets.DrawBox(barRect);
            
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(barRect, $"{currentAmmoCount} / {MaxCapacity}");
            Text.Anchor = TextAnchor.UpperLeft;
            
            curY += 20f;
        }
        
        private void ShowAmmoMenu()
        {
            Find.WindowStack.Add(new Dialog_AmmoSelector(this));
        }
        
        #endregion
    }
}
