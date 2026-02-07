// 当白昼倾坠之时
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Exosuit
{
    // 龙门架 ITab 的关联拓展窗口基类
    public abstract class Window_ExosuitCompanion : Window
    {
        protected ITab_Exosuit parentTab;
        
        public override Vector2 InitialSize => windowRect.size;

        public override float Margin => 0f;

        public Window_ExosuitCompanion(ITab_Exosuit parentTab)
        {
            this.parentTab = parentTab;
            this.doCloseX = false;
            this.doCloseButton = false;
            this.closeOnClickedOutside = false;
            this.draggable = false;
            this.layer = WindowLayer.GameUI;
            this.soundAppear = null;
            this.soundClose = null;
            this.preventCameraMotion = false;
            this.drawShadow = false;
            this.doWindowBackground = true;
        }

        public override void WindowUpdate()
        {
            base.WindowUpdate();
            // 关闭检测
            // 1. ITab 状态失效
            // 2. 选中的 Thing 变化或消失
            // 3. 当前 InspectPane 不再打开 ITab_Exosuit
            bool shouldClose = parentTab == null || parentTab.SelThingPublic == null;
            
            if (!shouldClose)
            {
                // 检查选中的对象是否匹配
                if (Find.Selector.SingleSelectedThing != parentTab.SelThingPublic)
                {
                    shouldClose = true;
                }
                // 检查 Inspect 主 Tab 是否打开
                else if (Find.MainTabsRoot.OpenTab != MainButtonDefOf.Inspect)
                {
                    shouldClose = true;
                }
                // 检查当前打开的 ITab 是否是 ITab_Exosuit
                else
                {
                    var inspectPane = MainButtonDefOf.Inspect.TabWindow as MainTabWindow_Inspect;
                    if (inspectPane?.OpenTabType != typeof(ITab_Exosuit))
                    {
                        shouldClose = true;
                    }
                }
            }

            if (shouldClose)
            {
                this.Close(false);
                return;
            }
            UpdatePosition();
        }

        public override void SetInitialSizeAndPosition()
        {
            // 强制在第一帧就计算好位置，避免从中心点“飘”过来
            UpdatePosition();
        }

        protected abstract void UpdatePosition();
    }

    // 侧边拓展窗口
    public class Window_ExosuitSideExtension : Window_ExosuitCompanion
    {
        private IModuleExtensionUI ui;

        public Window_ExosuitSideExtension(ITab_Exosuit parentTab, IModuleExtensionUI ui) : base(parentTab)
        {
            this.ui = ui;
            this.windowRect.size = new Vector2(ui.ExtensionUIWidth + 12f, parentTab.size.y);
        }

        protected override void UpdatePosition()
        {
            Rect tabRect = parentTab.LastScreenRect;
            if (tabRect == Rect.zero) return;

            // 紧贴主 UI 右侧
            this.windowRect = new Rect(tabRect.xMax, tabRect.y, windowRect.width, tabRect.height);
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (ui == null) return;

            Text.Font = GameFont.Small;
            Rect titleRect = new(inRect.x, inRect.y, inRect.width, 24f);
            Widgets.Label(titleRect, ui.ExtensionUITitle);
            
            // 绘制内容
            Rect contentRect = new(inRect.x, inRect.y + 28f, inRect.width, inRect.height - 28f);
            ui.DrawExtensionUI(contentRect);
        }
    }

    // 顶部拓展窗口
    public class Window_ExosuitTopExtension : Window_ExosuitCompanion
    {
        private List<IModuleTopExtensionUI> uis;

        public Window_ExosuitTopExtension(ITab_Exosuit parentTab, List<IModuleTopExtensionUI> uis) : base(parentTab)
        {
            this.uis = uis;
            float height = uis.Sum(u => u.TopExtensionHeight) + 2f; // 只保留分隔线空间
            this.windowRect.size = new Vector2(parentTab.size.x, height);
        }

        protected override void UpdatePosition()
        {
            Rect tabRect = parentTab.LastScreenRect;
            if (tabRect == Rect.zero) return;

            // 紧贴主 UI 顶部
            this.windowRect = new Rect(tabRect.x, tabRect.y - windowRect.height, tabRect.width, windowRect.height);
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (uis.NullOrEmpty()) return;

            float curY = inRect.y;
            foreach (var ui in uis)
            {
                Rect rect = new(inRect.x, curY, inRect.width, ui.TopExtensionHeight);
                ui.DrawTopExtension(rect);
                curY += ui.TopExtensionHeight;
            }
            
            // 底部画一根线区分主 UI
            Widgets.DrawLineHorizontal(inRect.x, inRect.yMax - 2f, inRect.width);
        }
    }
}
