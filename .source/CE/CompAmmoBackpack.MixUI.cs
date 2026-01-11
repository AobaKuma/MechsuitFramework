using System.Collections.Generic;
using System.Linq;
using CombatExtended;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Exosuit.CE
{
    // CompAmmoBackpack的混装模式UI绘制部分
    public partial class CompAmmoBackpack
    {
        #region 混装模式 UI
        
        private void DrawMixModeUI(ref float curY, Rect rect)
        {
            Text.Font = GameFont.Small;
            
            DrawMixAmmoSetSelector(ref curY, rect);
            curY += 6f;
            
            if (linkedAmmoSet == null)
            {
                Text.Font = GameFont.Tiny;
                GUI.color = Color.gray;
                string hint = "WG_AmmoBackpack_SelectAmmoSetFirst".Translate();
                float hintHeight = Text.CalcHeight(hint, rect.width);
                Rect hintRect = new(rect.x, curY, rect.width, hintHeight);
                Widgets.Label(hintRect, hint);
                GUI.color = Color.white;
                return;
            }
            
            Text.Font = GameFont.Tiny;
            
            // 射击循环 + 弹药条（横向布局）
            DrawCycleAndBarRow(ref curY, rect);
            curY += 4f;
            
            DrawMixEntries(ref curY, rect);
            
            // 添加槽位 + 预设按钮（横向布局）
            DrawAddAndPresetRow(ref curY, rect);
            
            curY += 6f;
        }
        
        // 射击循环+弹药条
        private void DrawCycleAndBarRow(ref float curY, Rect rect)
        {
            float cycleWidth = 0f;
            
            // 计算射击循环显示宽度
            if (mixEntries.Count > 0 && mixEntries.Any(e => e.AmmoDef != null))
            {
                var validEntries = mixEntries.Where(e => e.AmmoDef != null).ToList();
                Text.Font = GameFont.Tiny;
                foreach (var entry in validEntries)
                {
                    cycleWidth += Text.CalcSize(entry.Ratio.ToString()).x;
                }
                cycleWidth += (validEntries.Count - 1) * 8f + 8f;
            }
            
            float barWidth = rect.width - cycleWidth - (cycleWidth > 0 ? 8f : 0f);
            
            // 射击循环显示
            if (cycleWidth > 0)
            {
                Rect cycleRect = new(rect.x, curY, cycleWidth, 16f);
                DrawFireCycleDisplay(cycleRect);
            }
            
            // 弹药条
            Rect barRect = new(rect.x + (cycleWidth > 0 ? cycleWidth + 8f : 0f), curY, barWidth, 16f);
            DrawMixAmmoBarInRect(barRect);
            
            curY += 20f;
        }
        
        // 添加槽位+预设按钮
        private void DrawAddAndPresetRow(ref float curY, Rect rect)
        {
            float btnWidth = (rect.width - 8f) / 3f;
            float x = rect.x;
            
            // 添加槽位按钮
            Rect addRect = new(x, curY, btnWidth, 22f);
            if (Widgets.ButtonText(addRect, "WG_AmmoBackpack_AddSlot".Translate()))
                AddMixEntry(null, 1);
            x += btnWidth + 4f;
            
            // 保存预设按钮
            Rect saveRect = new(x, curY, btnWidth, 22f);
            bool canSave = mixEntries.Any(e => e.AmmoDef != null || e.IsWildcard);
            if (!canSave) GUI.color = Color.gray;
            if (Widgets.ButtonText(saveRect, "WG_AmmoBackpack_SavePreset".Translate()) && canSave)
                ShowSavePresetDialog();
            GUI.color = Color.white;
            x += btnWidth + 4f;
            
            // 加载预设按钮
            Rect loadRect = new(x, curY, btnWidth, 22f);
            var manager = AmmoMixPresetManager.Instance;
            bool hasPresets = manager != null && manager.Presets.Count > 0;
            if (!hasPresets) GUI.color = Color.gray;
            if (Widgets.ButtonText(loadRect, "WG_AmmoBackpack_LoadPreset".Translate()) && hasPresets)
                ShowLoadPresetMenu();
            GUI.color = Color.white;
            
            curY += 26f;
        }
        
        private void DrawMixAmmoSetSelector(ref float curY, Rect rect)
        {
            // 弹药组显示口径
            string label;
            if (linkedAmmoSet == null)
            {
                label = "WG_AmmoBackpack_SelectAmmoSet".Translate();
            }
            else
            {
                var firstAmmo = linkedAmmoSet.ammoTypes?.FirstOrDefault()?.ammo;
                string caliber = firstAmmo != null 
                    ? CompAmmoBackpack.GetAmmoShortLabel(firstAmmo, true).Replace(
                        firstAmmo.ammoClass?.LabelCap ?? "", "").Trim()
                    : "";
                label = !string.IsNullOrEmpty(caliber) ? caliber : linkedAmmoSet.LabelCap;
            }
            
            Texture2D icon = linkedAmmoSet?.ammoTypes?.FirstOrDefault()?.ammo?.uiIcon;
            
            Rect buttonRect = new(rect.x, curY, rect.width, 26f);
            
            if (icon != null)
            {
                Rect iconRect = new(buttonRect.x + 4f, buttonRect.y + 3f, 20f, 20f);
                GUI.DrawTexture(iconRect, icon);
                buttonRect.x += 26f;
                buttonRect.width -= 26f;
            }
            
            if (Widgets.ButtonText(buttonRect, label))
                ShowMixAmmoSetMenu();
            
            curY += 30f;
        }
        
        private void ShowMixAmmoSetMenu()
        {
            Find.WindowStack.Add(new Dialog_AmmoSetSelector(this, SelectMixAmmoSet));
        }
        
        private void SelectMixAmmoSet(AmmoSetDef ammoSet)
        {
            if (ammoSet == null || ammoSet == linkedAmmoSet) return;
            if (TotalAmmoCount > 0) return;
            
            linkedAmmoSet = ammoSet;
            mixEntries.Clear();
            mixFireIndex = 0;
            mixCycleCounter = 0;
            
            var entry = new AmmoMixEntry
            {
                AmmoDef = null,
                Ratio = 1,
                CurrentCount = 0,
                MaxCount = 0
            };
            mixEntries.Add(entry);
            RecalculateMixCapacities();
        }
        
        private void DrawFireCycleDisplay(Rect rect)
        {
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleLeft;
            
            var validEntries = mixEntries.Where(e => e.AmmoDef != null).ToList();
            if (validEntries.Count == 0)
            {
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }
            
            float x = rect.x;
            for (int i = 0; i < validEntries.Count; i++)
            {
                var entry = validEntries[i];
                Color ammoColor = GetAmmoColor(entry.AmmoDef);
                GUI.color = ammoColor;
                
                string numText = entry.Ratio.ToString();
                float textWidth = Text.CalcSize(numText).x;
                Rect numRect = new(x, rect.y, textWidth, rect.height);
                Widgets.Label(numRect, numText);
                x += textWidth;
                
                if (i < validEntries.Count - 1)
                {
                    GUI.color = Color.gray;
                    Rect sepRect = new(x, rect.y, 8f, rect.height);
                    Widgets.Label(sepRect, ":");
                    x += 8f;
                }
            }
            
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }
        
        private Color GetAmmoColor(AmmoDef ammo)
        {
            if (ammo == null) return Color.gray;
            
            string name = ammo.defName.ToLower();
            
            if (name.Contains("fmj") || name.Contains("ball"))
                return new Color(0.9f, 0.9f, 0.6f);
            if (name.Contains("ap") || name.Contains("sabot"))
                return new Color(0.6f, 0.8f, 1f);
            if (name.Contains("hp") || name.Contains("hollow"))
                return new Color(0.6f, 1f, 0.6f);
            if (name.Contains("he") || name.Contains("explosive"))
                return new Color(1f, 0.5f, 0.3f);
            if (name.Contains("incendiary") || name.Contains("fire"))
                return new Color(1f, 0.3f, 0.3f);
            if (name.Contains("tracer"))
                return new Color(1f, 1f, 0.3f);
            if (name.Contains("emp"))
                return new Color(0.5f, 0.5f, 1f);
            
            return new Color(0.8f, 0.8f, 0.8f);
        }
        
        private void DrawMixAmmoBar(ref float curY, Rect rect)
        {
            float barHeight = 16f;
            Rect barRect = new(rect.x, curY, rect.width, barHeight);
            DrawMixAmmoBarInRect(barRect);
            curY += barHeight + 4f;
        }
        
        // 在指定矩形内绘制混装弹药条
        private void DrawMixAmmoBarInRect(Rect barRect)
        {
            Widgets.DrawBoxSolid(barRect, new Color(0.1f, 0.1f, 0.1f));
            
            int totalMax = mixEntries.Sum(e => e.MaxCount);
            int totalCurrent = mixEntries.Sum(e => e.CurrentCount);
            
            if (totalMax > 0)
            {
                float x = barRect.x;
                foreach (var entry in mixEntries)
                {
                    if (entry.AmmoDef == null || entry.CurrentCount <= 0) continue;
                    
                    float widthRatio = (float)entry.CurrentCount / totalMax;
                    float segmentWidth = barRect.width * widthRatio;
                    
                    Rect segmentRect = new(x, barRect.y, segmentWidth, barRect.height);
                    Color ammoColor = GetAmmoColor(entry.AmmoDef);
                    Widgets.DrawBoxSolid(segmentRect, ammoColor);
                    
                    x += segmentWidth;
                }
            }
            
            Widgets.DrawBox(barRect);
            
            // 使用黑色文本
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Color.black;
            Widgets.Label(barRect, $"{totalCurrent} / {totalMax}");
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }
        
        private void DrawMixEntries(ref float curY, Rect rect)
        {
            float entryHeight = 26f;
            float entriesHeight = mixEntries.Count * entryHeight;
            float maxEntriesHeight = 130f;
            
            if (entriesHeight > maxEntriesHeight)
            {
                Rect scrollOuterRect = new(rect.x, curY, rect.width, maxEntriesHeight);
                Rect scrollInnerRect = new(0f, 0f, rect.width - 16f, entriesHeight);
                
                Widgets.BeginScrollView(scrollOuterRect, ref mixEntriesScrollPos, scrollInnerRect);
                
                float innerY = 0f;
                for (int i = 0; i < mixEntries.Count; i++)
                {
                    Rect entryRect = new(0f, innerY, scrollInnerRect.width, entryHeight);
                    DrawMixEntry(entryRect, mixEntries[i], i);
                    innerY += entryHeight;
                }
                
                Widgets.EndScrollView();
                curY += maxEntriesHeight + 4f;
            }
            else
            {
                for (int i = 0; i < mixEntries.Count; i++)
                {
                    Rect entryRect = new(rect.x, curY, rect.width, entryHeight);
                    DrawMixEntry(entryRect, mixEntries[i], i);
                    curY += entryHeight;
                }
                curY += 4f;
            }
        }
        
        private void DrawMixEntry(Rect rect, AmmoMixEntry entry, int index)
        {
            float rowHeight = rect.height;
            
            // 背景色
            if (entry.IsWildcard)
            {
                Color bgColor = Color.HSVToRGB((Time.realtimeSinceStartup * 0.1f) % 1f, 0.3f, 0.3f);
                bgColor.a = 0.2f;
                Widgets.DrawBoxSolid(rect, bgColor);
            }
            else if (entry.AmmoDef != null)
            {
                Color bgColor = GetAmmoColor(entry.AmmoDef);
                bgColor.a = 0.15f;
                Widgets.DrawBoxSolid(rect, bgColor);
            }
            
            Widgets.DrawHighlightIfMouseover(rect);
            
            Text.Font = GameFont.Tiny;
            
            // 计算各部分宽度
            float iconWidth = 24f;
            float ratioControlWidth = entry.CurrentCount == 0 ? 62f : 36f;
            string countText = $"{entry.CurrentCount}/{entry.MaxCount}";
            float countWidth = Text.CalcSize(countText).x + 4f;
            float deleteWidth = (entry.CurrentCount == 0 && mixEntries.Count > 1) ? 20f : 0f;
            float nameWidth = rect.width - iconWidth - ratioControlWidth - countWidth - deleteWidth - 8f;
            
            float x = rect.x;
            
            // 图标/随机模式切换
            Rect wildcardRect = new(x + 2f, rect.y + 3f, 20f, 20f);
            if (entry.CurrentCount == 0)
            {
                GUI.color = entry.IsWildcard ? Color.yellow : Color.gray;
                if (Widgets.ButtonText(wildcardRect, "?"))
                {
                    entry.IsWildcard = !entry.IsWildcard;
                    if (entry.IsWildcard)
                    {
                        entry.AmmoDef = null;
                        entry.WildcardAmmo.Clear();
                    }
                }
                GUI.color = Color.white;
                TooltipHandler.TipRegion(wildcardRect, "WG_AmmoBackpack_WildcardMode".Translate());
            }
            else
            {
                if (entry.IsWildcard)
                {
                    GUI.color = Color.yellow;
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(wildcardRect, "?");
                    Text.Anchor = TextAnchor.UpperLeft;
                    GUI.color = Color.white;
                }
                else if (entry.AmmoDef?.uiIcon != null)
                {
                    GUI.DrawTexture(wildcardRect, entry.AmmoDef.uiIcon);
                }
            }
            x += iconWidth;
            
            // 弹药名称
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect nameRect = new(x, rect.y, nameWidth, rowHeight);
            
            if (entry.IsWildcard)
            {
                int typeCount = entry.WildcardAmmo.Count;
                string label = typeCount > 0 
                    ? "WG_AmmoBackpack_WildcardTypes".Translate(typeCount)
                    : "WG_AmmoBackpack_WildcardEmpty".Translate();
                Widgets.Label(nameRect, label);
            }
            else
            {
                string buttonLabel = entry.AmmoDef != null
                    ? CompAmmoBackpack.GetAmmoShortLabel(entry.AmmoDef, false)
                    : "WG_AmmoBackpack_SelectType".Translate();
                
                if (entry.CurrentCount == 0)
                {
                    if (Widgets.ButtonText(nameRect, buttonLabel))
                        ShowSlotAmmoMenu(index);
                }
                else
                {
                    Widgets.Label(nameRect, buttonLabel);
                }
            }
            x += nameWidth + 2f;
            
            // Ratio 编辑
            if (entry.CurrentCount == 0)
            {
                Rect minusRect = new(x, rect.y + 3f, 18f, rowHeight - 6f);
                if (entry.Ratio > 1)
                {
                    if (Widgets.ButtonText(minusRect, "-"))
                    {
                        entry.Ratio--;
                        RecalculateMixCapacities();
                    }
                }
                else
                {
                    GUI.color = Color.gray;
                    Widgets.ButtonText(minusRect, "-");
                    GUI.color = Color.white;
                }
                x += 20f;
                
                Text.Anchor = TextAnchor.MiddleCenter;
                Rect ratioRect = new(x, rect.y, 20f, rowHeight);
                Widgets.Label(ratioRect, entry.Ratio.ToString());
                x += 22f;
                
                Rect plusRect = new(x, rect.y + 3f, 18f, rowHeight - 6f);
                if (Widgets.ButtonText(plusRect, "+"))
                {
                    entry.Ratio++;
                    RecalculateMixCapacities();
                }
                x += 20f;
            }
            else
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Rect ratioRect = new(x, rect.y, ratioControlWidth, rowHeight);
                Widgets.Label(ratioRect, $"×{entry.Ratio}");
                x += ratioControlWidth + 2f;
            }
            
            // 数量
            Text.Anchor = TextAnchor.MiddleRight;
            Rect countRect = new(x, rect.y, countWidth, rowHeight);
            Widgets.Label(countRect, countText);
            x += countWidth;
            
            // 删除按钮
            if (entry.CurrentCount == 0 && mixEntries.Count > 1)
            {
                Rect delRect = new(rect.xMax - 18f, rect.y + 3f, 16f, rowHeight - 6f);
                if (Widgets.ButtonText(delRect, "×"))
                    RemoveMixEntryAt(index);
            }
            
            Text.Anchor = TextAnchor.UpperLeft;
        }
        
        private void ShowSlotAmmoMenu(int slotIndex)
        {
            var ammoSet = isMixMode ? linkedAmmoSet : GetCurrentAmmoSet();
            if (ammoSet == null) return;
            
            var options = new List<FloatMenuOption>();
            
            foreach (var link in ammoSet.ammoTypes)
            {
                if (!IsAmmoCompatible(link.ammo)) continue;
                
                var ammo = link.ammo;
                // 菜单中只显示类型
                string label = GetAmmoShortLabel(ammo, false);
                options.Add(new FloatMenuOption(label, () =>
                {
                    SetMixEntryAmmo(slotIndex, ammo);
                }));
            }
            
            if (options.Count == 0)
                options.Add(new FloatMenuOption("WG_AmmoBackpack_NoMoreAmmo".Translate(), null));
            
            Find.WindowStack.Add(new FloatMenu(options));
        }
        
        #endregion
        
        #region 预设 UI
        
        private void DrawPresetButtons(ref float curY, Rect rect)
        {
            float buttonWidth = (rect.width - 4f) / 2f;
            
            // 保存预设按钮
            Rect saveRect = new(rect.x, curY, buttonWidth, 22f);
            bool canSave = mixEntries.Any(e => e.AmmoDef != null || e.IsWildcard);
            
            if (!canSave) GUI.color = Color.gray;
            if (Widgets.ButtonText(saveRect, "WG_AmmoBackpack_SavePreset".Translate()) && canSave)
                ShowSavePresetDialog();
            GUI.color = Color.white;
            
            // 加载预设按钮
            Rect loadRect = new(rect.x + buttonWidth + 4f, curY, buttonWidth, 22f);
            var manager = AmmoMixPresetManager.Instance;
            bool hasPresets = manager != null && manager.Presets.Count > 0;
            
            if (!hasPresets) GUI.color = Color.gray;
            if (Widgets.ButtonText(loadRect, "WG_AmmoBackpack_LoadPreset".Translate()) && hasPresets)
                ShowLoadPresetMenu();
            GUI.color = Color.white;
            
            curY += 26f;
        }
        
        private void ShowSavePresetDialog()
        {
            Find.WindowStack.Add(new Dialog_SaveMixPreset(this));
        }
        
        private void ShowLoadPresetMenu()
        {
            var manager = AmmoMixPresetManager.Instance;
            if (manager == null) return;
            
            var options = new List<FloatMenuOption>();
            
            // 获取当前弹药组的预设
            string currentAmmoSet = linkedAmmoSet?.defName ?? "";
            var presets = manager.GetPresetsForAmmoSet(currentAmmoSet).ToList();
            
            if (presets.Count == 0)
            {
                options.Add(new FloatMenuOption("WG_AmmoBackpack_NoPresets".Translate(), null));
            }
            else
            {
                foreach (var preset in presets)
                {
                    string label = $"{preset.Name} ({preset.GetDescription()})";
                    
                    // 检查是否可以应用
                    bool canApply = TotalAmmoCount == 0;
                    
                    if (canApply)
                    {
                        options.Add(new FloatMenuOption(label, () => ApplyPreset(preset)));
                    }
                    else
                    {
                        options.Add(new FloatMenuOption(
                            label + " " + "WG_AmmoBackpack_PresetNeedEmpty".Translate(), 
                            null));
                    }
                }
                
                // 分隔线
                options.Add(new FloatMenuOption("---", null));
                
                // 管理预设选项
                options.Add(new FloatMenuOption(
                    "WG_AmmoBackpack_ManagePresets".Translate(), 
                    () => Find.WindowStack.Add(new Dialog_ManageMixPresets())));
            }
            
            Find.WindowStack.Add(new FloatMenu(options));
        }
        
        private void ApplyPreset(AmmoMixPreset preset)
        {
            if (preset == null) return;
            
            if (preset.ApplyTo(this))
            {
                SoundDefOf.Click.PlayOneShotOnCamera();
                Messages.Message(
                    "WG_AmmoBackpack_PresetApplied".Translate(preset.Name), 
                    MessageTypeDefOf.PositiveEvent, false);
            }
            else
            {
                Messages.Message(
                    "WG_AmmoBackpack_PresetApplyFailed".Translate(), 
                    MessageTypeDefOf.RejectInput, false);
            }
        }
        
        #endregion
    }
}
