// 当白昼倾坠之时
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
        
        private static int selectedBackpackIndex = 0;
        
        public bool ShouldShowExtensionUI => true;
        public float ExtensionUIWidth => ExtensionWidth;
        public string ExtensionUITitle => "WG_AmmoBackpack_EditorTitle".Translate();
        
        public void DrawExtensionUI(Rect rect)
        {
            var wearer = Wearer;
            if (wearer == null) return;
            
            var allStorages = CEPatches.GetAllAmmoStorages(wearer);
            if (allStorages.Count == 0) return;
            
            if (selectedBackpackIndex >= allStorages.Count)
                selectedBackpackIndex = 0;
            
            var targetStorage = allStorages[selectedBackpackIndex];
            
            float curY = rect.y;
            
            // 顶层存储选择器 (无间距 Tabs)
            DrawStorageTabs(ref curY, rect, allStorages);
            curY += 6f;
            
            targetStorage.DrawUI(rect, ref curY);
        }
        
        public void DrawUI(Rect rect, ref float curY)
        {
            UpdateLinkedWeapon();
            
            // 武器信息 + 退弹按钮
            DrawWeaponAndEjectRow(ref curY, rect);
            curY += 6f;
            
            // 模式切换
            DrawModeToggle(ref curY, rect);
            curY += 6f;
            
            if (isMixMode)
                DrawMixModeUI(ref curY, rect);
            else
            {
                DrawAmmoSelector(ref curY, rect);
                curY += 6f;
                DrawAmmoBar(ref curY, rect);
            }
        }
        
        #endregion
        
        #region UI 绘制方法
        
        // 绘制顶层存储选择标签
        private void DrawStorageTabs(ref float curY, Rect rect, System.Collections.Generic.List<IAmmoStorage> storages)
        {
            Text.Font = GameFont.Tiny;
            float tabWidth = rect.width / storages.Count;
            float x = rect.x;
            
            for (int i = 0; i < storages.Count; i++)
            {
                var storage = storages[i];
                Rect tabRect = new(x, curY, tabWidth, 24f); 
                bool isSelected = (i == selectedBackpackIndex);
                
                if (isSelected)
                    Widgets.DrawBox(tabRect, 2);
                
                GUI.color = isSelected ? Color.green : Color.white;
                
                if (Widgets.ButtonText(tabRect, storage.StorageName))
                {
                    selectedBackpackIndex = i;
                    foreach (var s in storages) s.IsActive = false;
                    storage.IsActive = true;
                    SoundDefOf.Click.PlayOneShotOnCamera();
                }
                
                GUI.color = Color.white;
                x += tabWidth;
            }
            
            curY += 28f;
        }

        // 武器信息+退弹按钮
        private void DrawWeaponAndEjectRow(ref float curY, Rect rect)
        {
            Text.Font = GameFont.Tiny;
            var weaponAmmoSet = GetWeaponModuleAmmoSet();
            bool hasAmmo = TotalAmmoCount > 0;
            
            float ejectWidth = hasAmmo ? 70f : 0f;
            float infoWidth = rect.width - ejectWidth - (hasAmmo ? 4f : 0f);
            
            Rect infoRect = new(rect.x, curY, infoWidth, 24f);
            string weaponLabel = weaponAmmoSet != null 
                ? "WG_AmmoBackpack_WeaponModule".Translate(weaponAmmoSet.LabelCap)
                : "WG_AmmoBackpack_NoWeaponModule".Translate();
            
            GUI.color = weaponAmmoSet != null ? Color.green : Color.gray;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(infoRect, weaponLabel);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            
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
            float modeWidth = (rect.width - 4f) / 2f;
            
            Rect normalRect = new(rect.x, curY, modeWidth, 24f);
            GUI.color = isMixMode ? Color.gray : Color.green;
            if (Widgets.ButtonText(normalRect, "WG_AmmoBackpack_NormalMode".Translate()))
                if (isMixMode) DisableMixMode();
            
            Rect mixRect = new(rect.x + modeWidth + 4f, curY, modeWidth, 24f);
            GUI.color = isMixMode ? Color.green : Color.gray;
            if (Widgets.ButtonText(mixRect, "WG_AmmoBackpack_MixModeBtn".Translate()))
                if (!isMixMode) EnableMixMode();
            
            GUI.color = Color.white;
            curY += 28f;
        }

        private void DrawAmmoSelector(ref float curY, Rect rect)
        {
            Text.Font = GameFont.Small;
            string label = selectedAmmo == null 
                ? "WG_AmmoBackpack_SelectAmmo".Translate() 
                : GetAmmoShortLabel(selectedAmmo, true);
            
            Rect buttonRect = new(rect.x, curY, rect.width, 28f);
            if (selectedAmmo?.uiIcon != null)
            {
                Rect iconRect = new(buttonRect.x + 4f, buttonRect.y + 4f, 20f, 20f);
                GUI.DrawTexture(iconRect, selectedAmmo.uiIcon);
                buttonRect.x += 26f;
                buttonRect.width -= 26f;
            }
            
            if (Widgets.ButtonText(buttonRect, label))
                ShowAmmoMenu();
            
            curY += 32f;
        }
        
        private void DrawAmmoBar(ref float curY, Rect rect)
        {
            Rect barRect = new(rect.x, curY, rect.width, 16f);
            float fill = MaxCapacity > 0 ? (float)currentAmmoCount / MaxCapacity : 0f;
            
            Widgets.DrawBoxSolid(barRect, new Color(0.1f, 0.1f, 0.1f));
            Rect fillRect = new(barRect.x, barRect.y, barRect.width * fill, barRect.height);
            Color fillColor = fill > 0.5f ? new Color(0.2f, 0.6f, 0.2f) : (fill > 0.2f ? new Color(0.6f, 0.6f, 0.2f) : new Color(0.6f, 0.2f, 0.2f));
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
