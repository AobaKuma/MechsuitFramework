// TODO: 等待拆分为：
// - ITab_Exosuit.cs (~200行) - 构造函数、FillTab、基础字段
// - ITab_Exosuit.Gizmo.cs (~350行) - Draw_GizmoSlot、交互、菜单
// - ITab_Exosuit.Stats.cs (~200行) - DrawStatEntries、DrawLoadBar
// - 懒得拆了

using RimWorld;
using RimWorld.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using static Verse.Text;

namespace Exosuit
{
    [StaticConstructorOnStartup]
    //最好让tab跟core。。。
    //那样就能给pawn用了

    public partial class ITab_Exosuit : ITab
    {
        #region 常量
        
        private const float BaseWidth = 512f;
        private const float BaseHeight = 368f;
        
        #endregion
        
        #region 字段
        
        // 当前激活的拓展 UI 组件
        private IModuleExtensionUI activeExtensionUI;
        
        #endregion
        
        public ITab_Exosuit()
        {
            size = new Vector2(BaseWidth, BaseHeight);
            labelKey = "WG_TabMechGear".Translate();
        }
        
        // 更新窗口大小以适应拓展 UI
        public override void UpdateSize()
        {
            base.UpdateSize();
            
            // 检查是否有拓展 UI
            activeExtensionUI = GetActiveExtensionUI();
            
            if (activeExtensionUI != null && activeExtensionUI.ShouldShowExtensionUI)
            {
                size = new Vector2(BaseWidth + activeExtensionUI.ExtensionUIWidth, BaseHeight);
            }
            else
            {
                size = new Vector2(BaseWidth, BaseHeight);
            }
        }
        
        // 获取当前激活的拓展 UI 组件
        private IModuleExtensionUI GetActiveExtensionUI()
        {
            if (Parent?.Dummy?.apparel == null) return null;
            
            foreach (Apparel apparel in Parent.Dummy.apparel.WornApparel)
            {
                foreach (ThingComp comp in apparel.AllComps)
                {
                    if (comp is IModuleExtensionUI extensionUI && extensionUI.ShouldShowExtensionUI)
                    {
                        return extensionUI;
                    }
                }
            }
            return null;
        }
        
        private const float gizmoSide = 80f;
        private Vector2 GizmoSize => new(gizmoSide, gizmoSide);
        private readonly Color grey = new ColorInt(72, 82, 92).ToColor;

        private static readonly Texture2D EmptySlotIcon = Command.BGTex;
        
        
        public static List<Color> CachedColors {
            get {
                return cachedColors ??= DefDatabase<ColorDef>.AllDefsListForReading.Select((ColorDef c) => c.color).Concat(Find.FactionManager.AllFactionsVisible.Select((Faction f) => f.Color)).Distinct().ToList();
            } 
        }
        private static List<Color> cachedColors;

        private Rot4 direction = Rot4.South;
        
        // 用于在槽位内绘制状态文本
        private string pendingStatusText;
        private Color pendingStatusColor;
        
        public override void FillTab()
        {
            Parent.TryUpdateCache();

            Text.Font = GameFont.Small;
            Rect rect = new(Vector2.zero, size);
            Rect inner = rect.ContractedBy(3f);
            //Draw Title
            {
                using (TextBlock textBlock = new(TextAnchor.UpperRight))
                {
                    string title = "WG_MechSolution".Translate();
                    var titleSize = CalcSize(title);
                    Widgets.LabelFit(new(new(inner.xMax - titleSize.x - 20f, inner.y), titleSize), title);
                    LessonAutoActivator.TeachOpportunity(ConceptDef.Named("WG_Gantry_LoadModule"), this.Parent, OpportunityType.GoodToKnow);
                }
                
            }

            //Draw S/L solution 由於這個還沒做所以就沒裝了
            if (false)
            {
                Vector2 slPosition = new(14f, inner.y);

                string text = "Save".Translate();
                Vector2 size = CalcSize(text);
                size.Scale(new(1.2f, 1.2f));
                Rect slgizmoRect = new(slPosition, size);
                Widgets.ButtonText(slgizmoRect, text);
                text = "Load".Translate();
                slgizmoRect.x = slgizmoRect.xMax + 10f;
                size = CalcSize(text);
                size.Scale(new(1.2f, 1.2f));
                slgizmoRect.size = size;
                Widgets.ButtonText(slgizmoRect, text);
                text = "UnArm".Translate();
                slgizmoRect.x = slgizmoRect.xMax + 10f;
                size = CalcSize(text);
                size.Scale(new(1.2f, 1.2f));
                slgizmoRect.size = size;
                Widgets.ButtonText(slgizmoRect, text);
            }
            
            //7个格子
            for (int i = 0; i <= 6; i++)
            {
                Draw_GizmoSlot(i);
            }
            //提示信息
            if (!Parent.HasGearCore)
            {
                string text = "WG_BayTip".Translate();
                Rect labelRect = new(new(14f, inner.y), CalcSize(text));
                Widgets.Label(labelRect, text);
                LessonAutoActivator.TeachOpportunity(ConceptDef.Named("WG_Gantry_LoadModule"), this.Parent, OpportunityType.GoodToKnow);
                return;
            }
            else
            {
                string text = Parent.Core.Label.CapitalizeFirst();
                Rect labelRect = new(new(14f, inner.y), CalcSize(text));
                Widgets.Label(labelRect, text);
                LessonAutoActivator.TeachOpportunity(ConceptDef.Named("WG_Gantry_PayloadCapacity"), this.Parent, OpportunityType.GoodToKnow);
            }
            
            //stats
            {
                Vector2 position = new()
                {
                    x = 170f - (gizmoSide / 5f),
                    y = 56f + (gizmoSide * 2 + 5f)
                };
                Vector2 box = new()
                {
                    x = GizmoSize.x * 2f,
                    y = size.y - position.y - 10f
                };
 
                Rect statBlock = new(position, box);
                DrawStatEntries(statBlock, OccupiedSlots[PositionWSlot[0]]);
            }

            //rotate
            Vector2 rotateGizmosBotLeft = new(340f, 216f);

            {
                Vector2 smallGizmoSize = new(30f, 30f);

                for (int i = 1; i < 3; i++)
                {
                    rotateGizmosBotLeft.y -= 30f;
                    Rect gizmoRect = new(rotateGizmosBotLeft, smallGizmoSize);
                    switch (i)
                    {
                        case 1:
                            if (Widgets.ButtonImage(gizmoRect, Resources.rotateButton))
                            {
                                direction.Rotate(RotationDirection.Clockwise);
                                PortraitsCache.SetDirty(Parent.Dummy);
                            }
                            break;
                        case 2:

                            if (Widgets.ButtonImage(gizmoRect, Resources.rotateOppoButton))
                            {
                                direction.Rotate(RotationDirection.Counterclockwise);
                                PortraitsCache.SetDirty(Parent.Dummy);
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            
            // 绘制拓展 UI（如果有）
            DrawExtensionUI();

        }
        
        // 绘制模块拓展 UI
        private void DrawExtensionUI()
        {
            if (activeExtensionUI == null || !activeExtensionUI.ShouldShowExtensionUI) return;
            
            // 拓展 UI 区域在基础窗口右侧
            Rect extensionRect = new(
                BaseWidth,
                0f,
                activeExtensionUI.ExtensionUIWidth,
                BaseHeight
            );
            
            // 绘制分隔线
            Widgets.DrawLineVertical(extensionRect.x, extensionRect.y + 6f, extensionRect.height - 12f);
            
            // 绘制拓展 UI 内容
            Rect innerRect = extensionRect.ContractedBy(6f);
            
            // 标题
            Text.Font = GameFont.Small;
            Rect titleRect = new(innerRect.x, innerRect.y, innerRect.width, 24f);
            Widgets.Label(titleRect, activeExtensionUI.ExtensionUITitle);
            
            // 内容区域
            Rect contentRect = new(
                innerRect.x,
                innerRect.y + 28f,
                innerRect.width,
                innerRect.height - 28f
            );
            
            activeExtensionUI.DrawExtensionUI(contentRect);
        }

        //Gizmo Components
        public void Draw_GizmoSlot(int Order)
        {
            var slot = PositionWSlot.TryGetValue(Order);

            if (Order > 0&&Parent.HasGearCore)
            {
                slot ??= PositionWSlot[0].supportedSlots.Find(s => s.uiPriority == Order);
            }
            else if(Order==0)
            {
                slot ??= MiscDefOf.Core;
            }
            using (new TextBlock(TextAnchor.MiddleCenter))
            {
                Vector2 position = positions[Order];
                Rect gizmoRect = new(position, GizmoSize * (Order > 0 ? 1f : 2f));

                bool disabled = Order > 0 && Parent.HasGearCore && (slot==null ||(OccupiedSlots[PositionWSlot[0]]
                        ?.TryGetComp<CompSuitModule>().Props.ItemDef?.GetCompProperties<CompProperties_ExosuitModule>()?.disabledSlots?.Contains(slot) ?? false));
                Thing thing = null;
                bool hasThing = slot != null && OccupiedSlots.TryGetValue(slot, out thing);
                
                // 检查是否有待安装的模块（用于预览）
                Thing pendingModule = slot != null ? Parent.GetPendingInstallForSlot(slot) : null;
                bool hasPending = pendingModule != null && !hasThing;
                
                // 检查是否待卸载（纯卸载，不是替换）
                // 需要同时检查槽位和模块，因为多槽位模块只有一个 targetSlot
                bool isPendingRemove = slot != null && pendingModule == null && 
                    (Parent.IsSlotPendingRemove(slot) || (hasThing && Parent.IsModulePendingRemove(thing)));
                
                // 检查是否待替换（有待安装模块且当前有模块）
                bool isPendingReplace = slot != null && pendingModule != null && hasThing;
                
                //标签
                if (slot != null)
                {
                    if (hasThing)
                    {
                        var c = thing.TryGetComp<CompSuitModule>();
                        //血条
                        GizmoHealthBar(gizmoRect, slot, (c.HP / (float)c.MaxHP));
                    }
                    
                    // 槽位名称
                    string label = slot.label.Translate();
                    Text.Font = GameFont.Small;
                    Vector2 labelSize = CalcSize(label);
                    float labelWidth = Mathf.Max(labelSize.x, gizmoRect.width);
                    float labelX = gizmoRect.x + (gizmoRect.width - labelWidth) / 2f;
                    Rect labelBlock = new(labelX, gizmoRect.y - labelSize.y, labelWidth, labelSize.y);
                    Widgets.Label(labelBlock, label);
                    
                    // 状态文本显示在槽位图标顶部
                    string statusText = null;
                    Color statusColor = Color.white;
                    if (disabled)
                    {
                        statusText = "Disabled".Translate();
                        statusColor = Color.gray;
                    }
                    else if (isPendingReplace)
                    {
                        statusText = "WG_PendingReplace".Translate();
                        statusColor = Color.yellow;
                    }
                    else if (hasPending)
                    {
                        statusText = "WG_Pending".Translate();
                        statusColor = Color.green;
                    }
                    else if (isPendingRemove)
                    {
                        statusText = "WG_PendingRemove".Translate();
                        statusColor = new Color(1f, 0.5f, 0.5f);
                    }
                    
                    // 记录状态文本，稍后在槽位内绘制
                    if (statusText != null)
                    {
                        pendingStatusText = statusText;
                        pendingStatusColor = statusColor;
                    }
                    else
                    {
                        pendingStatusText = null;
                    }
                }


                //灰边
                {
                    Widgets.DrawBoxSolid(gizmoRect, grey);
                    gizmoRect = gizmoRect.ContractedBy(3f);
                }

                //底色
                {
                    GenUI.DrawTextureWithMaterial(gizmoRect, Command.BGTex, null);
                    if (disabled)
                    {
                        //Widgets.DrawBoxSolid(gizmoRect, Color.yellow);
                        GenUI.DrawTextureWithMaterial(gizmoRect, Resources.WG_SlotUnavailable, null);
                    }
                    
                }
                if (disabled) return;
                Texture2D icon = hasThing && slot != null ? new CachedTexture(OccupiedSlots[slot].def.graphicData.texPath).Texture : EmptySlotIcon;

                GizmoInteraction(gizmoRect, icon, slot);
                if (slot == null) return;
                Widgets.DrawHighlightIfMouseover(gizmoRect);
                
                // 绘制待安装模块的半透明预览
                if (hasPending)
                {
                    DrawPendingModulePreview(gizmoRect, pendingModule);
                }
                
                // 绘制待卸载的标记
                if (isPendingRemove && hasThing)
                {
                    DrawPendingRemoveOverlay(gizmoRect);
                }
                
                //部件名字
                if (hasThing)
                {
                    Rect nameBlock = gizmoRect.BottomPart(1f / 8f);
                    nameBlock.y += nameBlock.height - 26f;
                    nameBlock.height = 26f;
                    GUI.DrawTexture(nameBlock, TexUI.TextBGBlack);
                    Widgets.LabelFit(nameBlock, thing.LabelCap);
                    if (thing is ThingWithComps twc)
                    {
                        foreach (var c in twc.AllComps)
                        {
                            if (c is IReloadableComp reloadable)
                            {
                                // 跳过没有充能的组件，继续检查下一个
                                if (reloadable.MaxCharges <= 0)
                                {
                                    continue;
                                }
                                var labelAmmoRemain =reloadable.LabelRemaining.Colorize(Color.green);
                                var labelSize = CalcSize(labelAmmoRemain);
                                Rect labelRect = new(new(gizmoRect.xMax - labelSize.x, gizmoRect.y), labelSize);
                                Widgets.Label(labelRect, labelAmmoRemain);
                                break;
                            }
                        }
                    }
                }
                // 显示待安装模块的名字
                else if (hasPending)
                {
                    Rect nameBlock = gizmoRect.BottomPart(1f / 8f);
                    nameBlock.y += nameBlock.height - 26f;
                    nameBlock.height = 26f;
                    GUI.DrawTexture(nameBlock, TexUI.TextBGBlack);
                    GUI.color = new Color(1f, 1f, 1f, 0.6f);
                    Widgets.LabelFit(nameBlock, pendingModule.LabelCap);
                    GUI.color = Color.white;
                }
                
                // 在槽位顶部绘制状态文本
                if (pendingStatusText != null)
                {
                    Text.Font = GameFont.Tiny;
                    Rect statusBlock = gizmoRect.TopPart(0.25f);
                    GUI.DrawTexture(statusBlock, TexUI.TextBGBlack);
                    GUI.color = pendingStatusColor;
                    Widgets.Label(statusBlock, pendingStatusText);
                    GUI.color = Color.white;
                    Text.Font = GameFont.Small;
                }
            }
        }
        
        // 绘制待安装模块的半透明预览（使用装甲外观而非物品外观）
        private void DrawPendingModulePreview(Rect rect, Thing module)
        {
            if (module == null) return;
            if (!module.TryGetComp(out CompSuitModule comp)) return;
            
            // 获取装甲的 ThingDef
            ThingDef apparelDef = comp.Props.EquipedThingDef;
            if (apparelDef?.graphicData?.texPath == null) return;
            
            GUI.color = new Color(module.DrawColor.r, module.DrawColor.g, module.DrawColor.b, 0.4f);
            Texture2D tex = new CachedTexture(apparelDef.graphicData.texPath).Texture;
            GUI.DrawTexture(rect, tex);
            GUI.color = Color.white;
        }
        
        // 绘制待卸载的覆盖层
        private void DrawPendingRemoveOverlay(Rect rect)
        {
            // 绘制红色半透明覆盖
            GUI.color = new Color(1f, 0.3f, 0.3f, 0.3f);
            Widgets.DrawBoxSolid(rect, GUI.color);
            GUI.color = Color.white;
        }
        private void GizmoHealthBar(Rect gizmoRect, SlotDef slot, float healthPerc)
        {
            bool leftBar = slot.uiPriority == 0 || slot.uiPriority > 3;
            Rect bar = gizmoRect.LeftPart(1f / 15f);
            if (leftBar)
            {
                bar.x = gizmoRect.x - 1.5f * bar.width;
            }
            else
            {
                bar.x = gizmoRect.xMax + 0.5f * bar.width;
            }
            Widgets.DrawBoxSolid(bar, Color.black);
            bar.yMin += bar.height * Mathf.Min(1, 1f - healthPerc);
            //bar.height*=healthPerc;          
            var hColor = healthPerc < 0.3f ? Color.red : healthPerc < 0.7f ? Color.yellow : Color.green;
            Widgets.DrawBoxSolid(bar, hColor);
        }
        private void GizmoInteraction(Rect rect, Texture2D icon, SlotDef slot)
        {
            if (slot != null &&  OccupiedSlots.TryGetValue(slot, out var t))
            {
                if (slot.isCoreFrame)
                {
                    Vector3 offset = new(0, 0, 0.6f);
                    float scale = 0.75f;
                    if (t is Exosuit_Core core)
                    {
                        if (core.Extesnsion?.bayRenderOffset != null) offset += core.Extesnsion.bayRenderOffset;
                        if (core.Extesnsion?.bayRenderScale != null) scale *= core.Extesnsion.bayRenderScale;
                    }
                    RenderTexture portrait = PortraitsCache.Get(Parent.Dummy, rect.size, direction, cameraOffset: offset, cameraZoom: scale);
                    Widgets.DrawTextureFitted(rect, portrait, 1f);
                }
                else
                {
                    GUI.color = t.DrawColor;
                    GUI.DrawTexture(rect, Widgets.GetIconFor(t.def, t.Stuff, t.StyleDef));
                    GUI.color = Color.white;
                }
                
            }
            else Widgets.DrawTextureFitted(rect, EmptySlotIcon, 1f);
            if (slot == null) return;
            if (!Widgets.ButtonInvisible(rect)) return;

            switch (Event.current.button)
            {
                case 0://左键
                    {
                        
                        if (OccupiedSlots.ContainsKey(slot))
                        {
                            Find.WindowStack.Add(new Dialog_InfoCard(OccupiedSlots[slot]));
                        }
                        else Find.WindowStack.Add(new Dialog_InfoCard(slot));
                        break;
                    }
                case 1://右键
                    {


                        Find.WindowStack.Add(new FloatMenu(GizmoFloatMenu(slot)));
                        break;
                    }
            }

        }
        private List<FloatMenuOption> GizmoFloatMenu(SlotDef slot)
        {
            List<FloatMenuOption> options = [];
            
            // 检查是否是核心槽位
            bool isCoreSlot = slot != null && slot.isCoreFrame;
            
            // 如果核心工作进行中且不是核心槽位，显示提示并只允许取消核心工作
            if (!isCoreSlot && Parent.IsCoreWorkInProgress())
            {
                options.Add(new("WG_CoreWorkInProgress".Translate(), null));
                options.Add(new("WG_CancelCoreWork".Translate(), () =>
                {
                    Parent.CancelAllCoreWork();
                }));
                return options;
            }
            
            if (slot != null && OccupiedSlots.TryGetValue(slot, out Thing t)) //如果slot有填模塊，額外顯示移除選項
            {
                if (Parent.Ext.canStyle)
                {
                    options.Add(new("ChangeModuleColor".TranslateSimple(), delegate {
                        Find.WindowStack.Add(new Dialog_ChooseColor(
                            "ChangeModuleColor".TranslateSimple(), Find.FactionManager.OfPlayer.AllegianceColor, CachedColors, (color) =>
                            {
                                t.SetColor(color);
                                //Parent.SetCacheDirty(); 应该自动就更新了渲染cache
                            })
                            );
                    }, MenuOptionPriority.High));
                }
                
                // 检查是否已在待卸载队列中（包括替换操作的卸载阶段）
                if (Parent.IsSlotPendingWork(slot))
                {
                    Thing pendingModule = Parent.GetPendingInstallForSlot(slot);
                    if (pendingModule != null)
                    {
                        // 这是替换操作，显示取消替换
                        options.Add(new("WG_CancelReplace".Translate(pendingModule.LabelCap), () =>
                        {
                            Parent.CancelPendingWork(slot);
                        }, MenuOptionPriority.High));
                    }
                    else
                    {
                        // 这是纯卸载操作
                        options.Add(new("WG_CancelRemove".Translate(t.LabelCap), () =>
                        {
                            Parent.CancelPendingWork(slot);
                        }, MenuOptionPriority.High));
                    }
                }
                else
                {
                    options.Add(new("WG_RequestRemove".Translate(t.LabelCap), () =>
                    {
                        RequestRemoveModule(slot);
                    }, MenuOptionPriority.High));
                }
            }

            IEnumerable<Thing> modules = GetAvailableModules(slot, slot == null || isCoreSlot);

            if (modules.EnumerableNullOrEmpty())
            {
                options.Add(new("WG_NoModuleForSlot".Translate(), null));
                return options;
            }
            
            // 检查槽位是否已有模块（用于决定显示"安装"还是"替换"）
            bool slotHasModule = slot != null && OccupiedSlots.ContainsKey(slot);
            // 检查是否有非核心模块安装（用于核心替换时的保留选项）
            bool hasNonCoreModules = OccupiedSlots.Any(kvp => !kvp.Key.isCoreFrame);
            
            foreach (var thing in modules)
            {
                // 获取模块冲突信息
                string conflictInfo = GetModuleInfoString(thing);
                
                // 检查是否已在待安装队列中
                if (Parent.IsModulePendingInstall(thing))
                {
                    options.Add(CreateModuleOption(
                        "WG_CancelInstall".Translate(thing.LabelCap) + conflictInfo,
                        () => Parent.CancelPendingInstall(thing),
                        thing
                    ));
                }
                else
                {
                    // 核心框架替换时提供两个选项
                    if (isCoreSlot && slotHasModule)
                    {
                        options.Add(CreateModuleOption(
                            "WG_RequestReplaceCoreRemoveAll".Translate(thing.LabelCap) + conflictInfo,
                            () => RequestInstallModule(thing, false),
                            thing
                        ));
                        
                        if (hasNonCoreModules)
                        {
                            options.Add(CreateModuleOption(
                                "WG_RequestReplaceCoreKeepModules".Translate(thing.LabelCap) + conflictInfo,
                                () => RequestInstallModule(thing, true),
                                thing
                            ));
                        }
                    }
                    else if (slotHasModule)
                    {
                        options.Add(CreateModuleOption(
                            "WG_RequestReplace".Translate(thing.LabelCap) + conflictInfo,
                            () => RequestInstallModule(thing, false),
                            thing
                        ));
                    }
                    else
                    {
                        options.Add(CreateModuleOption(
                            "WG_RequestInstall".Translate(thing.LabelCap) + conflictInfo,
                            () => RequestInstallModule(thing, false),
                            thing
                        ));
                    }
                }
            }
            return options;
        }
        
        // 创建带悬停载荷信息的菜单选项
        private FloatMenuOption CreateModuleOption(string label, Action action, Thing module)
        {
            // 计算载荷信息
            float projected = Parent.CalculateProjectedLoad(module);
            float capacity = MassCapacity;
            float moduleMass = module.GetStatValue(StatDefOf.Mass);
            
            // 构建提示文本
            string tipText = "WG_ModuleInfo_LoadTip".Translate(
                moduleMass.ToString("F1"),
                projected.ToString("F0"),
                capacity.ToString("F0")
            );
            
            if (projected > capacity)
            {
                tipText += "\n" + "WG_ModuleInfo_OverloadWarning".Translate();
            }
            
            return new FloatMenuOption(label, action)
            {
                tooltip = new TipSignal(tipText)
            };
        }
        
        // 获取模块信息字符串（仅在有槽位冲突时显示）
        private string GetModuleInfoString(Thing module)
        {
            if (!module.TryGetComp(out CompSuitModule comp)) return "";
            
            var props = comp.Props;
            
            // 仅当模块占据多个槽位，且有其他槽位被占用时才显示
            if (props.occupiedSlots.NullOrEmpty() || props.occupiedSlots.Count <= 1) return "";
            
            // 检查是否有其他槽位被占用（排除当前右键点击的槽位）
            var conflictSlots = props.occupiedSlots.Where(s => OccupiedSlots.ContainsKey(s)).ToList();
            if (conflictSlots.Count == 0) return "";
            
            var slotNames = conflictSlots.Select(s => s.label.Translate().ToString());
            return "\n" + "WG_ModuleInfo_Slots".Translate(string.Join(", ", slotNames));
        }
        //Stats Components
        private void DrawStatEntries(Rect rect, Thing thing)
        {
            float curY = rect.y;
            
            // 标题
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(rect.x, curY, rect.width, 24f), "WG_Performance".Translate().CapitalizeFirst());
            curY += 24f;
            
            // 绘制载荷条（带待安装预览）
            DrawLoadBar(new Rect(rect.x, curY, rect.width, 16f));
            curY += 20f;

            // 结构值
            WidgetRow row = new(rect.x, curY, UIDirection.RightThenDown, rect.width, gap: -4);
            row.Label("WG_OverallArmor".Translate());
            string structrueInt = Parent.Core?.LabelHPPart.ToString();
            row.Gap(rect.width - CalcSize("WG_OverallArmor".Translate() + structrueInt).x - 8f);
            row.Label(structrueInt);

            foreach (StatDef statDef in toDraw)
            {
                float statValue = thing.GetStatValue(statDef);
                row.Gap(int.MaxValue);
                TaggedString t = statDef.LabelCap;
                string v = "";

                if (ModLister.GetActiveModWithIdentifier("ceteam.combatextended", true) != null)
                {
                    v = statValue.ToStringDecimalIfSmall();
                    if (statDef == StatDefOf.ArmorRating_Sharp)
                    {
                        v += "WG_Sharp_CE".Translate();
                    }
                    else if (statDef == StatDefOf.ArmorRating_Blunt)
                    {
                        v += " " + "WG_Blunt_CE".Translate();
                    }
                }
                else if (statDef == StatDefOf.MoveSpeed)
                {
                    if (!Parent.Dummy.TryGetExosuitCore(out var core)) continue;
                    v += "{0} c/s".Formatted(Parent.GetStatValueForPawn(statDef, Parent.Dummy, true).ToString("0.##"));
                }
                else
                {
                    v = statValue > 4 ? statValue.ToStringDecimalIfSmall() : (statValue.ToStringPercent());
                }
                row.Label(t);
                row.Gap(rect.width - CalcSize(t + v).x - 8f);
                using (TextBlock textBlock = new(TextAnchor.MiddleRight))
                {
                    row.Label(v);
                } ;
                
            }
        }
        
        // 绘制载荷条（带待安装模块预览）
        private static readonly Color BarBGColor = new(0.1f, 0.1f, 0.1f);
        private static readonly Color BarOLColor = new(0.8f, 0.1f, 0.1f);
        private static readonly Color BarNormalColor = new(0.2f, 0.6f, 0.2f);
        
        private void DrawLoadBar(Rect barRect)
        {
            float currentLoad = CurrentLoad;
            float capacity = MassCapacity;
            float pendingLoadDelta = Parent.GetPendingLoadDelta();
            float pendingCapacityDelta = Parent.GetPendingCapacityDelta();
            float projectedLoad = currentLoad + pendingLoadDelta;
            float projectedCapacity = capacity + pendingCapacityDelta;
            
            // 绘制背景
            Widgets.DrawBoxSolid(barRect, BarBGColor);
            
            // 使用预计载荷上限计算百分比
            float displayCapacity = pendingCapacityDelta != 0 ? projectedCapacity : capacity;
            float currentPercent = Mathf.Clamp01(currentLoad / displayCapacity);
            float projectedPercent = Mathf.Clamp01(projectedLoad / displayCapacity);
            
            if (pendingLoadDelta >= 0)
            {
                // 正常情况：绘制当前载荷（实心）
                if (currentPercent > 0)
                {
                    Rect currentRect = barRect.LeftPart(currentPercent);
                    bool currentOverload = currentLoad > displayCapacity;
                    Widgets.DrawBoxSolid(currentRect, currentOverload ? BarOLColor : BarNormalColor);
                }
                
                // 绘制待安装载荷预览（半透明带斜线）
                if (pendingLoadDelta > 0 && projectedPercent > currentPercent)
                {
                    Rect pendingRect = new(
                        barRect.x + barRect.width * currentPercent,
                        barRect.y,
                        barRect.width * (projectedPercent - currentPercent),
                        barRect.height
                    );
                    
                    Color pendingColor = projectedLoad > projectedCapacity 
                        ? new Color(1f, 0.3f, 0.3f, 0.4f)
                        : new Color(0.5f, 0.8f, 0.5f, 0.4f);
                    Widgets.DrawBoxSolid(pendingRect, pendingColor);
                    DrawDiagonalLines(pendingRect, pendingColor);
                }
            }
            else
            {
                // 拆卸情况：先绘制预计载荷（实心），再绘制将被移除的部分（半透明带斜线）
                if (projectedPercent > 0)
                {
                    Rect projectedRect = barRect.LeftPart(projectedPercent);
                    Widgets.DrawBoxSolid(projectedRect, BarNormalColor);
                }
                
                // 绘制将被移除的部分（红色半透明带斜线）
                if (currentPercent > projectedPercent)
                {
                    Rect removeRect = new(
                        barRect.x + barRect.width * projectedPercent,
                        barRect.y,
                        barRect.width * (currentPercent - projectedPercent),
                        barRect.height
                    );
                    
                    Color removeColor = new(1f, 0.3f, 0.3f, 0.4f);
                    Widgets.DrawBoxSolid(removeRect, removeColor);
                    DrawDiagonalLines(removeRect, removeColor);
                }
            }
            
            // 绘制文本
            string label = $"{currentLoad:F0}";
            if (pendingLoadDelta != 0)
            {
                string deltaStr = pendingLoadDelta > 0 ? $"+{pendingLoadDelta:F0}" : $"{pendingLoadDelta:F0}";
                label += $" ({deltaStr})";
            }
            
            // 显示载荷上限（如果有变化则显示预计值）
            if (pendingCapacityDelta != 0)
            {
                string capDeltaStr = pendingCapacityDelta > 0 ? $"+{pendingCapacityDelta:F0}" : $"{pendingCapacityDelta:F0}";
                label += $" / {capacity:F0} ({capDeltaStr})";
            }
            else
            {
                label += $" / {capacity:F0}";
            }
            
            // 超载警告单独处理，不放在载荷条内
            bool isOverload = projectedLoad > projectedCapacity;
            if (isOverload)
            {
                LessonAutoActivator.TeachOpportunity(ConceptDef.Named("WG_Gantry_Overloaded"), OpportunityType.Important);
            }
            
            using (new TextBlock(TextAnchor.MiddleCenter))
            {
                Widgets.Label(barRect, label);
            }
            
            // 超载警告显示在载荷条右侧
            if (isOverload)
            {
                string warning = "WG_Overload".Translate();
                Vector2 warningSize = Text.CalcSize(warning);
                GUI.color = Color.red;
                Rect warningRect = new(barRect.xMax + 4f, barRect.y, warningSize.x + 4f, barRect.height);
                using (new TextBlock(TextAnchor.MiddleLeft))
                {
                    Widgets.Label(warningRect, warning);
                }
                GUI.color = Color.white;
            }
        }
        
        // 绘制斜线纹理
        private void DrawDiagonalLines(Rect rect, Color color)
        {
            GUI.BeginClip(rect);
            float lineSpacing = 6f;
            
            Color lineColor = new(color.r, color.g, color.b, 0.6f);
            for (float x = -rect.height; x < rect.width; x += lineSpacing)
            {
                Widgets.DrawLine(new Vector2(x, rect.height), new Vector2(x + rect.height, 0), lineColor, 1f);
            }
            GUI.EndClip();
        }


        private static readonly Dictionary<int, Vector2> positions = new()
        {
            {0,new(170f,56f)},
            {1,new(14f,56f)},
            {2,new(14f,164f)},
            {3,new(14f,282f)},
            {4,new(412f,56f)},
            {5,new(412f,164f)},
            {6,new(412f,282f)},
        };
        private static readonly List<StatDef> toDraw =
        [
            StatDefOf.MoveSpeed,
            StatDefOf.ArmorRating_Sharp,
            StatDefOf.ArmorRating_Blunt,
            StatDefOf.ArmorRating_Heat
        ];
    }
    //和维护坞连接
    public partial class ITab_Exosuit
    {
        private Building_MaintenanceBay Parent => SelThing as Building_MaintenanceBay;

        private Dictionary<SlotDef, Thing> OccupiedSlots => Parent.occupiedSlots;

        private Dictionary<int, SlotDef> PositionWSlot => Parent.positionWSlot;

        private float CurrentLoad => Parent.Core?.DeadWeight ?? 0f;

        private float MassCapacity => Parent.Core?.Capacity ?? 0f;

        private IEnumerable<Thing> GetAvailableModules(SlotDef slot, bool isCoreFrame) => Parent.GetAvailableModules(slot, isCoreFrame);

        // 请求安装模块（通过工作系统）
        private void RequestInstallModule(Thing thing, bool keepModulesOnCoreReplace = false) => Parent.RequestInstallModule(thing, keepModulesOnCoreReplace);

        // 请求卸载模块（通过工作系统）
        private void RequestRemoveModule(SlotDef slot) => Parent.RequestRemoveModule(slot);

        public override void OnOpen()
        {
            base.OnOpen();
            Parent.TryUpdateCache();
        }

    }
}