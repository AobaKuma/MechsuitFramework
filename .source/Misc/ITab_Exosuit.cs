using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using static Verse.Text;

namespace Exosuit
{
    [StaticConstructorOnStartup]
    public partial class ITab_Exosuit : ITab
    {
        public ITab_Exosuit()
        {
            size = new Vector2(512f, 368f);
            labelKey = "WG_TabMechGear".Translate();
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
        protected override void FillTab()
        {
            Parent.TryUpdateCache();

            Text.Font = GameFont.Small;
            Rect rect = new(Vector2.zero, size);
            Rect inner = rect.ContractedBy(3f);
            //Draw Title
            {
                Anchor = TextAnchor.UpperRight;
                string title = "WG_MechSolution".Translate();
                var titleSize = CalcSize(title);
                Widgets.LabelFit(new(new(inner.xMax - titleSize.x - 20f, inner.y), titleSize), title);
                Anchor = TextAnchor.UpperLeft;
                LessonAutoActivator.TeachOpportunity(ConceptDef.Named("WG_Gantry_LoadModule"), this.Parent, OpportunityType.GoodToKnow);
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

            Draw_GizmoSlot(0);
            for (int i = 1; i <= 6; i++)
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
            Vector2 position = positions[0];
            //stats
            {
                position.x = 170f - (gizmoSide / 5f);
                position.y = 56f + (gizmoSide * 2 + 5f);
                Vector2 box = GizmoSize * 2f;
                box.x *= 1.2f;
                box.y = size.y - position.y - 10f;
                Rect statBlock = new(position, box);
                DrawStatEntries(statBlock, OccupiedSlots[PositionWSlot[0]]);
            }

            //rotate&color
            Vector2 rotateGizmosBotLeft = new(340f, 216f);
            {
                Vector2 smallGizmoSize = new(30f, 30f);

                for (int i = 1; i < 3; i++)
                {
                    rotateGizmosBotLeft.y -= 30f;
                    Rect gizmoRect = new(rotateGizmosBotLeft, smallGizmoSize);
                    switch (i)
                    {
                        case 0://染色

                            break;
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
                slot = MiscDefOf.Core;
            }
            using (new TextBlock(TextAnchor.MiddleCenter))
            {
                Vector2 position = positions[Order];
                Rect gizmoRect = new(position, GizmoSize * (Order > 0 ? 1f : 2f));

                bool disabled = Order > 0 &&Parent.HasGearCore && (OccupiedSlots[PositionWSlot[0]]
                        ?.TryGetComp<CompSuitModule>().Props.ItemDef?.GetCompProperties<CompProperties_ExosuitModule>()?.disabledSlots?.Contains(slot) ?? false);
                Thing thing = null;
                bool hasThing = slot != null && OccupiedSlots.TryGetValue(slot, out thing);
                //标签
                if (slot != null)
                {
                    string label = "";
                    if (hasThing)
                    {
                        var c = thing.TryGetComp<CompSuitModule>();
                        //血条
                        GizmoHealthBar(gizmoRect, slot, ((float)c.HP / (float)c.MaxHP));
                    }
                    label += $"{slot.label.Translate()}";
                    if (disabled)
                    {
                        label += "(" + "Disabled".Translate() + ")";
                    }
                    Text.Font = GameFont.Small;
                    Vector2 labelSize = CalcSize(label);
                    Rect labelBlock = new(gizmoRect.x, gizmoRect.y - labelSize.y, gizmoRect.width, labelSize.y);
                    Widgets.LabelFit(labelBlock, label);
                }


                //灰边
                {
                    Widgets.DrawBoxSolid(gizmoRect, grey);
                    gizmoRect = gizmoRect.ContractedBy(3f);
                }

                //底色
                {
                    Material material = disabled ? TexUI.GrayscaleGUI : null;
                    GenUI.DrawTextureWithMaterial(gizmoRect, Command.BGTex, material);
                }
                if (disabled) return;
                Texture2D icon = hasThing && slot != null ? new CachedTexture(OccupiedSlots[slot].def.graphicData.texPath).Texture : EmptySlotIcon;

                GizmoInteraction(gizmoRect, icon, slot);
                if (slot == null) return;
                Widgets.DrawHighlightIfMouseover(gizmoRect);
                //部件名字
                if (hasThing)
                {
                    Rect nameBlock = gizmoRect.BottomPart(1f / 8f);
                    nameBlock.y += nameBlock.height - 26f;
                    nameBlock.height = 26f;
                    GUI.DrawTexture(nameBlock, TexUI.TextBGBlack);
                    Widgets.LabelFit(nameBlock, OccupiedSlots[slot].LabelCap);
                }
            }
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
            if (slot != null && slot.isCoreFrame && Parent.HasGearCore)
            {
                RenderTexture portrait = PortraitsCache.Get(Parent.Dummy, rect.size, direction, cameraOffset: new Vector3(0, 0, 0.6f), cameraZoom: 0.75f);
                Widgets.DrawTextureFitted(rect, portrait, 1f);
            }
            else if (slot != null && OccupiedSlots.TryGetValue(slot,out var t))
            {
                GUI.color = t.DrawColor;
                GUI.DrawTexture(rect, Widgets.GetIconFor(t.def, t.Stuff, t.StyleDef));
                GUI.color = Color.white;
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
            if (slot != null && OccupiedSlots.TryGetValue(slot, out Thing t)) //如果slot有填模塊，額外顯示移除選項
            {
                options.Add(new("ChangeModuleColor".TranslateSimple(), delegate {
                    Find.WindowStack.Add(new Dialog_ChooseColor(
                        "ChangeModuleColor".TranslateSimple(), Find.FactionManager.OfPlayer.AllegianceColor, CachedColors, (color)=>
                        {
                            t.SetColor(color);
                            Parent.SetCacheDirty();
                        })
                        );
                },MenuOptionPriority.High));
                options.Add(new("Remove".Translate(t.LabelCap), () =>
                {
                    RemoveModules(slot);
                }, MenuOptionPriority.High));
            }

            IEnumerable<Thing> modules = GetAvailableModules(slot, slot == null || slot.isCoreFrame);

            if (modules.EnumerableNullOrEmpty())
            {
                options.Add(new("WG_NoModuleForSlot".Translate(), null));
                return options;
            }
            foreach (var thing in modules)
            {
                Action action = () =>
                {
                    AddOrReplaceModule(thing);
                };
                options.Add(new(thing.LabelCap, action));
            }
            return options;
        }
        //Stats Components
        private void DrawStatEntries(Rect rect, Thing thing)
        {
            WidgetRow row = new(rect.x, rect.y, UIDirection.RightThenDown, rect.width, gap: -4);
            row.Label("WG_Performance".Translate().CapitalizeFirst());
            row.Gap(int.MaxValue);
            float loadPercent = CurrentLoad / MassCapacity;
            if (loadPercent > 1f)
            {
                LessonAutoActivator.TeachOpportunity(ConceptDef.Named("WG_Gantry_Overloaded"), OpportunityType.Important);
                row.FillableBar(rect.width, 16f, 1, $"{CurrentLoad} / {MassCapacity}" + " " + "WG_Overload".Translate(), Resources.BarOL, Resources.BarBG);
            }
            else
            {
                row.FillableBar(rect.width, 16f, loadPercent, $"{CurrentLoad} / {MassCapacity}", Exosuit_Core.fBarTex, Resources.BarBG);
            }

            row.Label("WG_OverallArmor".Translate());
            string structrueInt = (Parent.Core as Exosuit_Core)?.LabelHPPart.ToString();
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
                row.Label(v);
            }
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
        private static readonly List<StatDef> toDraw = new()
        {
            StatDefOf.MoveSpeed,
            StatDefOf.ArmorRating_Sharp,
            StatDefOf.ArmorRating_Blunt,
            StatDefOf.ArmorRating_Heat
        };
    }
    //和维护坞连接
    public partial class ITab_Exosuit
    {
        private Building_MaintenanceBay Parent => SelThing as Building_MaintenanceBay;

        private Dictionary<SlotDef, Thing> OccupiedSlots => Parent.occupiedSlots;

        private Dictionary<int, SlotDef> PositionWSlot => Parent.positionWSlot;

        private float CurrentLoad => Parent.Core.DeadWeight;

        private float MassCapacity => Parent.Core.Capacity;

        private IEnumerable<Thing> GetAvailableModules(SlotDef slot, bool isCoreFrame) => Parent.GetAvailableModules(slot, isCoreFrame);

        private void AddOrReplaceModule(Thing thing) => Parent.AddOrReplaceModule(thing);

        private void RemoveModules(SlotDef slot) => Parent.RemoveModules(slot);

        public override void OnOpen()
        {
            base.OnOpen();
            Parent.TryUpdateCache();
        }

    }
}