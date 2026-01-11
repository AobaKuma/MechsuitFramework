using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Exosuit.CE
{
    // 显示弹药类型和剩余百分比的弹药背包状态Gizmo
    public class Gizmo_AmmoBackpack : Gizmo
    {
        #region 静态字段
        
        // 使用静态字典保存每个弹药组的当前索引
        private static readonly Dictionary<string, int> BackpackIndexCache = new();
        
        #endregion
        
        #region 常量
        
        private const float Width = 140f;
        private const float MixWidth = 140f;
        private new const float Height = 75f;
        private const float MixHeight = 75f;
        private const float BarHeight = 16f;
        private const float Padding = 4f;
        private const float ArrowButtonSize = 18f;
        private const float HelpButtonSize = 16f;
        
        #endregion
        
        #region 字段
        
        public CompAmmoBackpack compBackpack;
        
        // 同弹药组的背包列表
        public List<CompAmmoBackpack> sameSetBackpacks;
        
        #endregion
        
        #region 属性
        
        // 获取缓存键
        private string CacheKey
        {
            get
            {
                var wearer = compBackpack?.Wearer;
                var ammoSet = compBackpack?.GetCurrentAmmoSet();
                if (wearer == null) return "default";
                return $"{wearer.ThingID}_{ammoSet?.defName ?? "null"}";
            }
        }
        
        // 当前索引（从缓存读取/写入）
        private int CurrentIndex
        {
            get
            {
                var key = CacheKey;
                return BackpackIndexCache.TryGetValue(key, out int idx) ? idx : 0;
            }
            set
            {
                BackpackIndexCache[CacheKey] = value;
            }
        }
        
        // 当前显示的背包
        private CompAmmoBackpack CurrentBackpack
        {
            get
            {
                if (sameSetBackpacks == null || sameSetBackpacks.Count == 0)
                    return compBackpack;
                
                // 确保索引有效
                int idx = CurrentIndex;
                if (idx >= sameSetBackpacks.Count)
                {
                    idx = 0;
                    CurrentIndex = 0;
                }
                
                return sameSetBackpacks[idx];
            }
        }
        
        // 是否有多个背包可切换
        private bool HasMultipleBackpacks => sameSetBackpacks != null && sameSetBackpacks.Count > 1;
        
        public override float GetWidth(float maxWidth) => 
            CurrentBackpack?.IsMixMode == true ? MixWidth : Width;
        
        public override bool Visible => compBackpack != null;
        
        private float CurrentHeight => CurrentBackpack?.IsMixMode == true ? MixHeight : Height;
        
        #endregion
        
        #region 绘制
        
        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            var backpack = CurrentBackpack;
            var rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), CurrentHeight);
            
            // 绘制背景
            Widgets.DrawWindowBackground(rect);
            
            var innerRect = rect.ContractedBy(Padding);
            float curY = innerRect.y;
            
            // 标题行：弹药组名称 + 翻页指示器
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperLeft;
            
            var titleRect = new Rect(innerRect.x, curY, innerRect.width, 20f);
            
            // 标题文本：混装模式显示弹药组名称，单装模式显示当前弹药名称
            string title;
            if (backpack.IsMixMode)
            {
                var ammoSet = backpack.LinkedAmmoSet;
                title = ammoSet != null ? ammoSet.LabelCap : backpack.BackpackDisplayName;
            }
            else
            {
                // 单装模式：显示当前选择的弹药口径
                var selectedAmmo = backpack.SelectedAmmo;
                if (selectedAmmo != null)
                {
                    title = CompAmmoBackpack.GetAmmoShortLabel(selectedAmmo, true).Replace(
                        selectedAmmo.ammoClass?.LabelCap ?? "", "").Trim();
                    if (string.IsNullOrEmpty(title))
                        title = selectedAmmo.LabelCap;
                }
                else
                {
                    title = backpack.BackpackDisplayName;
                }
            }
            
            // 如果有多个背包，显示翻页箭头和指示器
            if (HasMultipleBackpacks)
            {
                // 左侧显示标题
                float arrowWidth = ArrowButtonSize + 4f;
                float helpWidth = backpack.IsMixMode ? HelpButtonSize + 2f : 0f;
                float indicatorWidth = 30f;
                var titleTextRect = new Rect(innerRect.x, curY, innerRect.width - arrowWidth - helpWidth - indicatorWidth, 20f);
                Widgets.Label(titleTextRect, title);
                
                // 翻页指示器
                var indicatorRect = new Rect(titleTextRect.xMax, curY, indicatorWidth, 20f);
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = Color.gray;
                Widgets.Label(indicatorRect, $"{CurrentIndex + 1}/{sameSetBackpacks.Count}");
                GUI.color = Color.white;
                
                // 混装模式下显示问号按钮
                if (backpack.IsMixMode)
                {
                    var helpRect = new Rect(indicatorRect.xMax + 2f, curY + 1f, HelpButtonSize, HelpButtonSize);
                    DrawAmmoColorHelpButton(helpRect, backpack);
                }
                
                // 右侧切换箭头按钮
                var arrowRect = new Rect(innerRect.xMax - ArrowButtonSize, curY + 1f, ArrowButtonSize, ArrowButtonSize);
                if (Widgets.ButtonText(arrowRect, ">"))
                {
                    int newIndex = (CurrentIndex + 1) % sameSetBackpacks.Count;
                    CurrentIndex = newIndex;
                    
                    // 同时更新激活状态
                    foreach (var bp in sameSetBackpacks)
                        bp.IsActiveBackpack = false;
                    sameSetBackpacks[newIndex].IsActiveBackpack = true;
                    
                    SoundDefOf.Click.PlayOneShotOnCamera();
                }
                
                Text.Anchor = TextAnchor.UpperLeft;
            }
            else
            {
                // 单背包混装模式也显示问号按钮
                if (backpack.IsMixMode)
                {
                    var titleTextRect = new Rect(innerRect.x, curY, innerRect.width - HelpButtonSize - 4f, 20f);
                    Widgets.Label(titleTextRect, title);
                    
                    var helpRect = new Rect(innerRect.xMax - HelpButtonSize, curY + 1f, HelpButtonSize, HelpButtonSize);
                    DrawAmmoColorHelpButton(helpRect, backpack);
                }
                else
                {
                    Widgets.Label(titleRect, title);
                }
            }
            
            curY += 18f;
            
            if (backpack.IsMixMode)
            {
                DrawMixModeContent(backpack, innerRect, ref curY);
            }
            else
            {
                DrawNormalModeContent(backpack, innerRect, ref curY);
            }
            
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            
            return new GizmoResult(GizmoState.Clear);
        }
        
        private void DrawNormalModeContent(CompAmmoBackpack backpack, Rect innerRect, ref float curY)
        {
            // 弹种名称（如 "FMJ"）
            var selectedAmmo = backpack.SelectedAmmo;
            string ammoLabel = selectedAmmo != null 
                ? CompAmmoBackpack.GetAmmoShortLabel(selectedAmmo, false)
                : "WG_AmmoBackpack_NoAmmoSelected".Translate().ToString();
            
            // 文本区域：从当前Y位置开始，高度18
            var labelRect = new Rect(innerRect.x, curY, innerRect.width, 18f);
            Text.Anchor = TextAnchor.MiddleLeft;
            if (selectedAmmo == null) GUI.color = Color.gray;
            Widgets.Label(labelRect, ammoLabel);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            
            curY += 20f;
            
            // 进度条：从文本下方开始
            var barRect = new Rect(innerRect.x, curY, innerRect.width, BarHeight);
            float fillPercent = backpack.MaxCapacity > 0 
                ? (float)backpack.CurrentAmmoCount / backpack.MaxCapacity 
                : 0f;
            
            // 绘制进度条背景和填充
            Widgets.DrawBoxSolid(barRect, new Color(0.1f, 0.1f, 0.1f));
            
            var fillRect = barRect;
            fillRect.width *= fillPercent;
            
            Color fillColor;
            if (fillPercent > 0.5f)
                fillColor = new Color(0.2f, 0.6f, 0.2f);
            else if (fillPercent > 0.2f)
                fillColor = new Color(0.6f, 0.6f, 0.2f);
            else
                fillColor = new Color(0.6f, 0.2f, 0.2f);
            
            Widgets.DrawBoxSolid(fillRect, fillColor);
            Widgets.DrawBox(barRect);
            
            // 文字在条中间
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(barRect, backpack.LabelRemaining);
            Text.Anchor = TextAnchor.UpperLeft;
        }
        
        private void DrawMixModeContent(CompAmmoBackpack backpack, Rect innerRect, ref float curY)
        {
            var entries = backpack.MixEntries;
            if (entries == null || entries.Count == 0)
            {
                GUI.color = Color.gray;
                Widgets.Label(new Rect(innerRect.x, curY, innerRect.width, 20f), 
                    "WG_AmmoBackpack_NoMixConfig".Translate());
                GUI.color = Color.white;
                return;
            }
            
            // 显示射击循环（彩色数字）
            var cycleRect = new Rect(innerRect.x, curY, innerRect.width, 16f);
            DrawFireCycleDisplay(cycleRect, entries);
            curY += 18f;
            
            // 彩色容量条（文本在条内）
            int totalCurrent = entries.Sum(e => e.CurrentCount);
            int totalMax = entries.Sum(e => e.MaxCount);
            
            var barRect = new Rect(innerRect.x, curY, innerRect.width, BarHeight);
            DrawMixAmmoBar(barRect, entries);
            
            // 文本叠加在进度条上，使用黑色
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Color.black;
            Widgets.Label(barRect, $"{totalCurrent} / {totalMax}");
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }
        
        // 绘制射击循环
        private void DrawFireCycleDisplay(Rect rect, System.Collections.Generic.IReadOnlyList<AmmoMixEntry> entries)
        {
            Text.Font = GameFont.Tiny;
            
            // 统计连续相同弹药的数量
            var cycleGroups = new System.Collections.Generic.List<(CombatExtended.AmmoDef ammo, int count)>();
            CombatExtended.AmmoDef lastAmmo = null;
            int count = 0;
            
            foreach (var entry in entries)
            {
                if (entry.AmmoDef == null) continue;
                
                if (entry.AmmoDef == lastAmmo)
                {
                    count++;
                }
                else
                {
                    if (lastAmmo != null && count > 0)
                        cycleGroups.Add((lastAmmo, count));
                    lastAmmo = entry.AmmoDef;
                    count = 1;
                }
            }
            if (lastAmmo != null && count > 0)
                cycleGroups.Add((lastAmmo, count));
            
            if (cycleGroups.Count == 0)
            {
                return;
            }
            
            // 先计算总宽度以便居中
            float totalWidth = 0f;
            for (int i = 0; i < cycleGroups.Count; i++)
            {
                totalWidth += Text.CalcSize(cycleGroups[i].count.ToString()).x;
                if (i < cycleGroups.Count - 1)
                    totalWidth += 8f; // 分隔符宽度
            }
            
            // 居中绘制
            float x = rect.x + (rect.width - totalWidth) / 2f;
            Text.Anchor = TextAnchor.MiddleLeft;
            
            for (int i = 0; i < cycleGroups.Count; i++)
            {
                var group = cycleGroups[i];
                Color ammoColor = GetAmmoColor(group.ammo);
                GUI.color = ammoColor;
                
                string numText = group.count.ToString();
                float textWidth = Text.CalcSize(numText).x;
                Rect numRect = new(x, rect.y, textWidth, rect.height);
                Widgets.Label(numRect, numText);
                x += textWidth;
                
                if (i < cycleGroups.Count - 1)
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
        
        // 绘制混装彩色容量条
        private void DrawMixAmmoBar(Rect rect, System.Collections.Generic.IReadOnlyList<AmmoMixEntry> entries)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.1f, 0.1f));
            
            int totalMax = entries.Sum(e => e.MaxCount);
            if (totalMax <= 0)
            {
                Widgets.DrawBox(rect);
                return;
            }
            
            float x = rect.x;
            foreach (var entry in entries)
            {
                if (entry.AmmoDef == null || entry.CurrentCount <= 0) continue;
                
                float widthRatio = (float)entry.CurrentCount / totalMax;
                float segmentWidth = rect.width * widthRatio;
                
                Rect segmentRect = new(x, rect.y, segmentWidth, rect.height);
                Color ammoColor = GetAmmoColor(entry.AmmoDef);
                Widgets.DrawBoxSolid(segmentRect, ammoColor);
                
                x += segmentWidth;
            }
            
            Widgets.DrawBox(rect);
        }
        
        // 获取弹药颜色
        private Color GetAmmoColor(CombatExtended.AmmoDef ammo)
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
        
        // 绘制弹药颜色帮助按钮
        private void DrawAmmoColorHelpButton(Rect rect, CompAmmoBackpack backpack)
        {
            GUI.color = Color.gray;
            if (Widgets.ButtonText(rect, "?"))
            {
                // 点击时也显示提示
            }
            GUI.color = Color.white;
            
            // 生成颜色图例提示
            string tooltip = BuildAmmoColorLegend(backpack);
            TooltipHandler.TipRegion(rect, tooltip);
        }
        
        // 生成弹药颜色图例
        private string BuildAmmoColorLegend(CompAmmoBackpack backpack)
        {
            var entries = backpack.MixEntries;
            if (entries == null || entries.Count == 0)
                return "WG_AmmoBackpack_NoMixConfig".Translate();
            
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("WG_AmmoBackpack_ColorLegend".Translate());
            
            foreach (var entry in entries)
            {
                if (entry.AmmoDef == null) continue;
                
                Color color = GetAmmoColor(entry.AmmoDef);
                string colorName = GetColorName(color);
                string ammoName = CompAmmoBackpack.GetAmmoShortLabel(entry.AmmoDef, false);
                
                sb.AppendLine($"  {colorName}: {ammoName}");
            }
            
            return sb.ToString().TrimEnd();
        }
        
        // 获取颜色名称
        private string GetColorName(Color color)
        {
            // 黄色系
            if (color.r > 0.8f && color.g > 0.8f && color.b < 0.7f)
                return "WG_AmmoBackpack_ColorYellow".Translate();
            // 蓝色系
            if (color.b > 0.8f && color.r < 0.7f)
                return "WG_AmmoBackpack_ColorBlue".Translate();
            // 绿色系
            if (color.g > 0.8f && color.r < 0.7f && color.b < 0.7f)
                return "WG_AmmoBackpack_ColorGreen".Translate();
            // 橙色系
            if (color.r > 0.8f && color.g > 0.4f && color.g < 0.6f)
                return "WG_AmmoBackpack_ColorOrange".Translate();
            // 红色系
            if (color.r > 0.8f && color.g < 0.4f)
                return "WG_AmmoBackpack_ColorRed".Translate();
            // 灰色
            return "WG_AmmoBackpack_ColorGray".Translate();
        }
        
        private void DrawMixEntryBar(Rect innerRect, ref float curY, AmmoMixEntry entry)
        {
            float rowHeight = 14f;
            
            // 图标
            if (entry.AmmoDef?.uiIcon != null)
            {
                var iconRect = new Rect(innerRect.x, curY, 14f, 14f);
                GUI.DrawTexture(iconRect, entry.AmmoDef.uiIcon);
            }
            
            // 进度条
            var barRect = new Rect(innerRect.x + 16f, curY + 1f, innerRect.width - 50f, rowHeight - 2f);
            float fillPercent = entry.MaxCount > 0 ? (float)entry.CurrentCount / entry.MaxCount : 0f;
            
            Widgets.DrawBoxSolid(barRect, new Color(0.1f, 0.1f, 0.1f));
            
            var fillRect = barRect;
            fillRect.width *= fillPercent;
            
            Color fillColor = fillPercent > 0.5f 
                ? new Color(0.2f, 0.5f, 0.2f) 
                : fillPercent > 0.2f 
                    ? new Color(0.5f, 0.5f, 0.2f) 
                    : new Color(0.5f, 0.2f, 0.2f);
            
            Widgets.DrawBoxSolid(fillRect, fillColor);
            Widgets.DrawBox(barRect);
            
            // 数量
            var countRect = new Rect(innerRect.xMax - 32f, curY, 32f, rowHeight);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(countRect, entry.CurrentCount.ToString());
            Text.Anchor = TextAnchor.UpperLeft;
            
            curY += rowHeight + 2f;
        }
        
        #endregion
    }
}
