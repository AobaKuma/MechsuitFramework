using System.Collections.Generic;
using System.Linq;
using CombatExtended;
using RimWorld;
using UnityEngine;
using Verse;

namespace Exosuit.CE
{
    // 弹药组选择对话框，用于混装模式选择弹药组
    public class Dialog_AmmoSetSelector : Window
    {
        #region 常量
        
        private const float RowHeight = 32f;
        private const float IconSize = 24f;
        private const float SearchHeight = 30f;
        private const float HeaderHeight = 28f;
        
        #endregion
        
        #region 字段
        
        private readonly CompAmmoBackpack comp;
        private readonly System.Action<AmmoSetDef> onSelect;
        private string searchText = "";
        private Vector2 scrollPosition;
        private List<AmmoSetDef> weaponAmmoSets;
        private List<AmmoSetDef> otherAmmoSets;
        private List<AmmoSetDef> filteredWeaponSets;
        private List<AmmoSetDef> filteredOtherSets;
        
        #endregion
        
        #region 属性
        
        public override Vector2 InitialSize => new(400f, 500f);
        
        #endregion
        
        #region 构造函数
        
        public Dialog_AmmoSetSelector(CompAmmoBackpack comp, System.Action<AmmoSetDef> onSelect)
        {
            this.comp = comp;
            this.onSelect = onSelect;
            
            doCloseButton = false;
            doCloseX = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;
            
            BuildAmmoSetLists();
        }
        
        #endregion
        
        #region 公共方法
        
        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            float curY = 0f;
            
            // 标题
            Rect titleRect = new(0f, curY, inRect.width, HeaderHeight);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(titleRect, "WG_AmmoBackpack_SelectAmmoSet".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            curY += HeaderHeight + 4f;
            
            // 搜索框
            Rect searchRect = new(0f, curY, inRect.width, SearchHeight);
            DrawSearchBox(searchRect);
            curY += SearchHeight + 8f;
            
            // 弹药组列表
            Rect listRect = new(0f, curY, inRect.width, inRect.height - curY);
            DrawAmmoSetList(listRect);
        }
        
        #endregion
        
        #region 私有方法
        
        private void BuildAmmoSetLists()
        {
            var weaponAmmoSet = GetWeaponModuleAmmoSet();
            var prioritySets = new HashSet<AmmoSetDef>();
            
            weaponAmmoSets = new List<AmmoSetDef>();
            otherAmmoSets = new List<AmmoSetDef>();
            
            // 武器模块弹药组
            if (weaponAmmoSet != null && CompAmmoBackpack.IsAmmoSetCompatible(weaponAmmoSet))
            {
                prioritySets.Add(weaponAmmoSet);
                weaponAmmoSets.Add(weaponAmmoSet);
            }
            
            // 其他弹药组
            foreach (var ammoSet in DefDatabase<AmmoSetDef>.AllDefs)
            {
                if (prioritySets.Contains(ammoSet)) continue;
                if (!CompAmmoBackpack.IsAmmoSetCompatible(ammoSet)) continue;
                
                otherAmmoSets.Add(ammoSet);
            }
            
            otherAmmoSets = otherAmmoSets.OrderBy(a => a.label).ToList();
            
            UpdateFilteredLists();
        }
        
        private AmmoSetDef GetWeaponModuleAmmoSet()
        {
            var wearer = comp.Wearer;
            if (wearer?.apparel == null) return null;
            
            foreach (var apparel in wearer.apparel.WornApparel)
            {
                var compWeapon = apparel.TryGetComp<CompModuleWeapon>();
                if (compWeapon?.Weapon == null) continue;
                
                var compAmmo = compWeapon.Weapon.TryGetComp<CompAmmoUser>();
                if (compAmmo?.Props?.ammoSet != null)
                    return compAmmo.Props.ammoSet;
            }
            
            if (wearer.equipment?.Primary != null)
            {
                var compAmmo = wearer.equipment.Primary.TryGetComp<CompAmmoUser>();
                if (compAmmo?.Props?.ammoSet != null)
                    return compAmmo.Props.ammoSet;
            }
            
            return null;
        }
        
        private void UpdateFilteredLists()
        {
            if (string.IsNullOrEmpty(searchText))
            {
                filteredWeaponSets = weaponAmmoSets;
                filteredOtherSets = otherAmmoSets;
            }
            else
            {
                var lowerSearch = searchText.ToLower();
                filteredWeaponSets = weaponAmmoSets
                    .Where(a => a.label.ToLower().Contains(lowerSearch))
                    .ToList();
                filteredOtherSets = otherAmmoSets
                    .Where(a => a.label.ToLower().Contains(lowerSearch))
                    .ToList();
            }
        }
        
        private void DrawSearchBox(Rect rect)
        {
            Rect iconRect = new(rect.x + 4f, rect.y + 5f, 20f, 20f);
            GUI.color = Color.gray;
            Widgets.Label(iconRect, "🔍");
            GUI.color = Color.white;
            
            Rect inputRect = new(rect.x + 28f, rect.y, rect.width - 60f, rect.height);
            string newSearch = Widgets.TextField(inputRect, searchText);
            
            if (newSearch != searchText)
            {
                searchText = newSearch;
                UpdateFilteredLists();
            }
            
            if (!string.IsNullOrEmpty(searchText))
            {
                Rect clearRect = new(rect.xMax - 28f, rect.y + 3f, 24f, 24f);
                if (Widgets.ButtonText(clearRect, "×"))
                {
                    searchText = "";
                    UpdateFilteredLists();
                }
            }
        }
        
        private void DrawAmmoSetList(Rect rect)
        {
            float totalHeight = 0f;
            
            if (filteredWeaponSets.Count > 0)
                totalHeight += HeaderHeight + filteredWeaponSets.Count * RowHeight + 8f;
            
            if (filteredOtherSets.Count > 0)
                totalHeight += HeaderHeight + filteredOtherSets.Count * RowHeight;
            
            Rect viewRect = new(0f, 0f, rect.width - 16f, totalHeight);
            
            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);
            
            float curY = 0f;
            
            if (filteredWeaponSets.Count > 0)
            {
                DrawSectionHeader(ref curY, viewRect.width, "WG_AmmoBackpack_WeaponAmmoSet".Translate(), 
                    new Color(0.3f, 0.5f, 0.3f));
                
                foreach (var ammoSet in filteredWeaponSets)
                {
                    DrawAmmoSetRow(ref curY, viewRect.width, ammoSet);
                }
                
                curY += 8f;
            }
            
            if (filteredOtherSets.Count > 0)
            {
                DrawSectionHeader(ref curY, viewRect.width, "WG_AmmoBackpack_OtherAmmoSet".Translate(),
                    new Color(0.3f, 0.3f, 0.4f));
                
                foreach (var ammoSet in filteredOtherSets)
                {
                    DrawAmmoSetRow(ref curY, viewRect.width, ammoSet);
                }
            }
            
            if (filteredWeaponSets.Count == 0 && filteredOtherSets.Count == 0)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = Color.gray;
                Widgets.Label(new Rect(0f, curY, viewRect.width, 40f), 
                    "WG_AmmoBackpack_NoSearchResult".Translate());
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
            }
            
            Widgets.EndScrollView();
        }
        
        private void DrawSectionHeader(ref float curY, float width, string label, Color bgColor)
        {
            Rect headerRect = new(0f, curY, width, HeaderHeight);
            Widgets.DrawBoxSolid(headerRect, bgColor);
            
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect labelRect = new(8f, curY, width - 8f, HeaderHeight);
            Widgets.Label(labelRect, label);
            Text.Anchor = TextAnchor.UpperLeft;
            
            curY += HeaderHeight;
        }
        
        private void DrawAmmoSetRow(ref float curY, float width, AmmoSetDef ammoSet)
        {
            Rect rowRect = new(0f, curY, width, RowHeight);
            
            // 高亮当前选中
            if (comp.LinkedAmmoSet == ammoSet)
            {
                Widgets.DrawBoxSolid(rowRect, new Color(0.2f, 0.4f, 0.2f, 0.5f));
            }
            
            Widgets.DrawHighlightIfMouseover(rowRect);
            
            // 图标（使用第一个弹药的图标）
            var firstAmmo = ammoSet.ammoTypes?.FirstOrDefault()?.ammo;
            Rect iconRect = new(4f, curY + (RowHeight - IconSize) / 2f, IconSize, IconSize);
            if (firstAmmo?.uiIcon != null)
            {
                GUI.DrawTexture(iconRect, firstAmmo.uiIcon);
            }
            
            // 弹药组名称
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect nameRect = new(iconRect.xMax + 4f, curY, width - 100f, RowHeight);
            Widgets.Label(nameRect, ammoSet.LabelCap);
            
            // 弹药种类数
            int typeCount = ammoSet.ammoTypes?.Count ?? 0;
            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            Rect infoRect = new(width - 60f, curY, 56f, RowHeight);
            Widgets.Label(infoRect, $"×{typeCount}");
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            
            // 点击选择
            if (Widgets.ButtonInvisible(rowRect))
            {
                onSelect?.Invoke(ammoSet);
                Close();
            }
            
            curY += RowHeight;
        }
        
        #endregion
    }
}
