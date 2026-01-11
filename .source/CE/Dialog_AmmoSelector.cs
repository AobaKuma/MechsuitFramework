using System.Collections.Generic;
using System.Linq;
using CombatExtended;
using RimWorld;
using UnityEngine;
using Verse;

namespace Exosuit.CE
{
    // 弹药选择对话框，可滚动、可搜索
    public class Dialog_AmmoSelector : Window
    {
        #region 常量
        
        private const float RowHeight = 32f;
        private const float IconSize = 24f;
        private const float SearchHeight = 30f;
        private const float HeaderHeight = 28f;
        
        #endregion
        
        #region 字段
        
        private readonly CompAmmoBackpack comp;
        private string searchText = "";
        private Vector2 scrollPosition;
        private List<AmmoDef> weaponAmmoList;
        private List<AmmoDef> otherAmmoList;
        private List<AmmoDef> filteredWeaponAmmo;
        private List<AmmoDef> filteredOtherAmmo;
        
        #endregion
        
        #region 属性
        
        public override Vector2 InitialSize => new(400f, 500f);
        
        #endregion
        
        #region 构造函数
        
        public Dialog_AmmoSelector(CompAmmoBackpack comp)
        {
            this.comp = comp;
            
            doCloseButton = false;
            doCloseX = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;
            
            BuildAmmoLists();
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
            Widgets.Label(titleRect, "WG_AmmoBackpack_SelectAmmo".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            curY += HeaderHeight + 4f;
            
            // 搜索框
            Rect searchRect = new(0f, curY, inRect.width, SearchHeight);
            DrawSearchBox(searchRect);
            curY += SearchHeight + 8f;
            
            // 弹药列表
            Rect listRect = new(0f, curY, inRect.width, inRect.height - curY);
            DrawAmmoList(listRect);
        }
        
        #endregion
        
        #region 私有方法
        
        private void BuildAmmoLists()
        {
            var weaponAmmoSet = GetWeaponModuleAmmoSet();
            var priorityAmmo = new HashSet<AmmoDef>();
            
            weaponAmmoList = new List<AmmoDef>();
            otherAmmoList = new List<AmmoDef>();
            
            // 武器模块弹药（当前穿戴者的武器）
            if (weaponAmmoSet != null)
            {
                foreach (var link in weaponAmmoSet.ammoTypes)
                {
                    if (CompAmmoBackpack.IsAmmoCompatible(link.ammo))
                    {
                        priorityAmmo.Add(link.ammo);
                        weaponAmmoList.Add(link.ammo);
                    }
                }
            }
            
            // 收集游戏中所有武器正在使用的弹药组
            var usedAmmoSets = GetAllUsedAmmoSets();
            foreach (var ammoSet in usedAmmoSets)
            {
                if (ammoSet == weaponAmmoSet) continue;
                
                foreach (var link in ammoSet.ammoTypes)
                {
                    if (priorityAmmo.Contains(link.ammo)) continue;
                    if (!CompAmmoBackpack.IsAmmoCompatible(link.ammo)) continue;
                    
                    priorityAmmo.Add(link.ammo);
                    otherAmmoList.Add(link.ammo);
                }
            }
            
            otherAmmoList = otherAmmoList.OrderBy(a => a.label).ToList();
            
            UpdateFilteredLists();
        }
        
        // 获取游戏中所有武器正在使用的弹药组
        private HashSet<AmmoSetDef> GetAllUsedAmmoSets()
        {
            var result = new HashSet<AmmoSetDef>();
            var map = comp.Wearer?.MapHeld ?? Find.CurrentMap;
            if (map == null) return result;
            
            // 扫描地图上所有殖民者的武器
            foreach (var pawn in map.mapPawns.FreeColonists)
            {
                // 主武器
                if (pawn.equipment?.Primary != null)
                {
                    var compAmmo = pawn.equipment.Primary.TryGetComp<CompAmmoUser>();
                    if (compAmmo?.Props?.ammoSet != null && CompAmmoBackpack.IsAmmoSetCompatible(compAmmo.Props.ammoSet))
                        result.Add(compAmmo.Props.ammoSet);
                }
                
                // 穿戴的武器模块
                if (pawn.apparel != null)
                {
                    foreach (var apparel in pawn.apparel.WornApparel)
                    {
                        var compWeapon = apparel.TryGetComp<CompModuleWeapon>();
                        if (compWeapon?.Weapon == null) continue;
                        
                        var compAmmo = compWeapon.Weapon.TryGetComp<CompAmmoUser>();
                        if (compAmmo?.Props?.ammoSet != null && CompAmmoBackpack.IsAmmoSetCompatible(compAmmo.Props.ammoSet))
                            result.Add(compAmmo.Props.ammoSet);
                    }
                }
            }
            
            // 扫描库存中的武器
            foreach (var thing in map.listerThings.ThingsInGroup(ThingRequestGroup.Weapon))
            {
                var compAmmo = thing.TryGetComp<CompAmmoUser>();
                if (compAmmo?.Props?.ammoSet != null && CompAmmoBackpack.IsAmmoSetCompatible(compAmmo.Props.ammoSet))
                    result.Add(compAmmo.Props.ammoSet);
            }
            
            return result;
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
                if (compAmmo?.Props?.ammoSet != null && 
                    CompAmmoBackpack.IsAmmoSetCompatible(compAmmo.Props.ammoSet))
                {
                    return compAmmo.Props.ammoSet;
                }
            }
            
            if (wearer.equipment?.Primary != null)
            {
                var compAmmo = wearer.equipment.Primary.TryGetComp<CompAmmoUser>();
                if (compAmmo?.Props?.ammoSet != null && 
                    CompAmmoBackpack.IsAmmoSetCompatible(compAmmo.Props.ammoSet))
                {
                    return compAmmo.Props.ammoSet;
                }
            }
            
            return null;
        }
        
        private void UpdateFilteredLists()
        {
            if (string.IsNullOrEmpty(searchText))
            {
                filteredWeaponAmmo = weaponAmmoList;
                filteredOtherAmmo = otherAmmoList;
            }
            else
            {
                var lowerSearch = searchText.ToLower();
                filteredWeaponAmmo = weaponAmmoList
                    .Where(a => a.label.ToLower().Contains(lowerSearch))
                    .ToList();
                filteredOtherAmmo = otherAmmoList
                    .Where(a => a.label.ToLower().Contains(lowerSearch))
                    .ToList();
            }
        }
        
        private void DrawSearchBox(Rect rect)
        {
            // 搜索图标区域
            Rect iconRect = new(rect.x + 4f, rect.y + 5f, 20f, 20f);
            GUI.color = Color.gray;
            Widgets.Label(iconRect, "🔍");
            GUI.color = Color.white;
            
            // 搜索输入框
            Rect inputRect = new(rect.x + 28f, rect.y, rect.width - 60f, rect.height);
            string newSearch = Widgets.TextField(inputRect, searchText);
            
            if (newSearch != searchText)
            {
                searchText = newSearch;
                UpdateFilteredLists();
            }
            
            // 清除按钮
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
        
        private void DrawAmmoList(Rect rect)
        {
            // 计算总高度
            float totalHeight = 0f;
            
            if (filteredWeaponAmmo.Count > 0)
                totalHeight += HeaderHeight + filteredWeaponAmmo.Count * RowHeight + 8f;
            
            if (filteredOtherAmmo.Count > 0)
                totalHeight += HeaderHeight + filteredOtherAmmo.Count * RowHeight;
            
            Rect viewRect = new(0f, 0f, rect.width - 16f, totalHeight);
            
            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);
            
            float curY = 0f;
            
            // 武器弹药组
            if (filteredWeaponAmmo.Count > 0)
            {
                DrawSectionHeader(ref curY, viewRect.width, "WG_AmmoBackpack_WeaponAmmo".Translate(), 
                    new Color(0.3f, 0.5f, 0.3f));
                
                foreach (var ammo in filteredWeaponAmmo)
                {
                    DrawAmmoRow(ref curY, viewRect.width, ammo, true);
                }
                
                curY += 8f;
            }
            
            // 其他弹药组
            if (filteredOtherAmmo.Count > 0)
            {
                DrawSectionHeader(ref curY, viewRect.width, "WG_AmmoBackpack_OtherAmmo".Translate(),
                    new Color(0.3f, 0.3f, 0.4f));
                
                foreach (var ammo in filteredOtherAmmo)
                {
                    DrawAmmoRow(ref curY, viewRect.width, ammo, false);
                }
            }
            
            // 无结果提示
            if (filteredWeaponAmmo.Count == 0 && filteredOtherAmmo.Count == 0)
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
        
        private void DrawAmmoRow(ref float curY, float width, AmmoDef ammo, bool isWeaponAmmo)
        {
            Rect rowRect = new(0f, curY, width, RowHeight);
            
            // 高亮当前选中
            if (comp.SelectedAmmo == ammo)
            {
                Widgets.DrawBoxSolid(rowRect, new Color(0.2f, 0.4f, 0.2f, 0.5f));
            }
            
            // 鼠标悬停高亮
            Widgets.DrawHighlightIfMouseover(rowRect);
            
            // 图标
            Rect iconRect = new(4f, curY + (RowHeight - IconSize) / 2f, IconSize, IconSize);
            if (ammo.uiIcon != null)
            {
                GUI.DrawTexture(iconRect, ammo.uiIcon);
            }
            
            // 弹药名称
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect nameRect = new(iconRect.xMax + 4f, curY, width - 140f, RowHeight);
            Widgets.Label(nameRect, ammo.LabelCap);
            
            // 容量信息
            int capacity = comp.CalculateMaxCapacity(ammo);
            
            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            Rect infoRect = new(width - 80f, curY, 76f, RowHeight);
            Widgets.Label(infoRect, $"×{capacity}");
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            
            // 点击选择
            if (Widgets.ButtonInvisible(rowRect))
            {
                comp.SetSelectedAmmo(ammo);
                Close();
            }
            
            // 提示信息
            if (Mouse.IsOver(rowRect))
            {
                float mass = CompAmmoBackpack.GetAmmoMass(ammo);
                string tip = "WG_AmmoBackpack_AmmoTip".Translate(
                    ammo.LabelCap,
                    mass.ToString("F3"),
                    capacity);
                TooltipHandler.TipRegion(rowRect, tip);
            }
            
            curY += RowHeight;
        }
        
        #endregion
    }
}
