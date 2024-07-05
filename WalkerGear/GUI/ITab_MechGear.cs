using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using static Verse.Text;

namespace WalkerGear
{
    [StaticConstructorOnStartup]
    public partial class ITab_MechGear : ITab
    {
        public ITab_MechGear()
        {
            size = new Vector2(512f, 368f);
            labelKey = "TabMechGear".Translate();
        }
        
        private const float side = 80f;
        private Vector2 GizmoSize => new(side, side);
        private readonly Color grey = new ColorInt(72, 82, 92).ToColor;

        private static readonly Texture2D EmptySlotIcon = Command.BGTex;

        protected override void FillTab()
        {
            if (Parent!=lastBay) {
                lastBay = Parent;
                UpdateOccupiedSlotsCache();
            }
            if (needUpdateCache)
            {
                UpdateOccupiedSlotsCache();
            }   
            Text.Font = GameFont.Small;
            Rect rect = new(Vector2.zero, size);
            Rect inner = rect.ContractedBy(3f);
            //Draw Title
            {
                Anchor = TextAnchor.UpperRight;
                string title = "MechSolution".Translate();
                var titleSize = CalcSize(title);
                Widgets.LabelFit(new(new(inner.xMax - titleSize.x - 20f, inner.y), titleSize), title);
                Anchor = TextAnchor.UpperLeft;
            }

            //Draw S/L solution
            if(false)
            {
                Vector2 slPosition = new(14f, inner.y);

                string text = "Save".Translate();
                Vector2 size = CalcSize(text);
                size.Scale(new(1.2f,1.2f));
                Rect slgizmoRect = new(slPosition, size);
                Widgets.ButtonText(slgizmoRect,text);
                text = "Load".Translate();
                slgizmoRect.x = slgizmoRect.xMax + 10f;
                size=CalcSize(text);
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
            if (!Parent.HasGearCore)
            {
                return;
            }
            for (int i = 1; i < 7; ++i)
            {
                Draw_GizmoSlot(i);
            }

            Vector2 position = positions[0];
            //stats
            {
                position.x =170f - (side / 5f);
                position.y = 56f+(side * 2 + 5f);
                Vector2 box = GizmoSize * 2f;
                box.x *= 1.1f;
                box.y = size.y - position.y - 10f;
                Rect statBlock = new(position, box);
                DrawStatEntries(statBlock, occupiedSlots[positionWSlot[0]]);
            }

            //rotate&color
            Vector2 rotateGizmosBotLeft = new(340f, 216f);
            {
                Vector2 smallGizmoSize = new(30f,30f);
                    
                for (int i = 1; i < 3; i++)
                {
                    rotateGizmosBotLeft.y -= 30f;
                    Rect gizmoRect = new(rotateGizmosBotLeft,smallGizmoSize);
                    switch (i)
                    {
                        case 0://染色
                                
                            break;
                        case 1:
                            if (Widgets.ButtonImage(gizmoRect, Resources.rotateButton))
                                Parent.direction.Rotate(RotationDirection.Clockwise);
                            break;
                        case 2:

                            if (Widgets.ButtonImage(gizmoRect, Resources.rotateOppoButton))
                                Parent.direction.Rotate(RotationDirection.Counterclockwise);
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
            var slot = positionWSlot.TryGetValue(Order);
            if (Order > 0)
            {
                slot ??= positionWSlot[0].supportedSlots.Find(s => s.uiPriority == Order);
            }
            using (new TextBlock(TextAnchor.MiddleCenter)) {
                Vector2 position = positions[Order];
                Rect gizmoRect = new(position, GizmoSize* (Order>0?1f:2f));

                bool disabled= Order > 0 && (occupiedSlots[positionWSlot[0]]
                        ?.TryGetComp<CompWalkerComponent>().Props.ItemDef?.GetCompProperties<CompProperties_WalkerComponent>()?.disabledSlots?.Contains(slot) ?? false);
                Thing thing = null;
                bool hasThing = slot != null&&occupiedSlots.TryGetValue(slot, out thing);
                //标签
                if(slot != null)
                {
                    string label = "";
                    if (hasThing)
                    {
                        var c = thing.TryGetComp<CompWalkerComponent>();
                        //血条
                        GizmoHealthBar(gizmoRect, slot, c.HP / (float)c.MaxHP);
                    }
                    label += $"{slot.label.Translate()}";
                    if (disabled)
                    {
                        label += "("+"Disabled".Translate()+")";
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
                    GenUI.DrawTextureWithMaterial(gizmoRect,Command.BGTex,material);
                }
                if(disabled) return;
                Texture2D icon = hasThing && slot!=null ? new CachedTexture(occupiedSlots[slot].def.graphicData.texPath).Texture : EmptySlotIcon;

                GizmoInteraction(gizmoRect,icon,slot);
                Widgets.DrawHighlightIfMouseover(gizmoRect);

                if(slot != null) return;
                //部件名字
                if (hasThing)
                {
                    Rect nameBlock = gizmoRect.BottomPart(1f/8f);
                    nameBlock.y+=nameBlock.height-26f;
                    nameBlock.height = 26f;
                    GUI.DrawTexture(nameBlock, TexUI.TextBGBlack);
                    Widgets.LabelFit(nameBlock, occupiedSlots[slot].LabelCap);
                }
            }
        }
        private void GizmoHealthBar(Rect gizmoRect,SlotDef slot,float healthPerc)
        {
            bool leftBar = slot.uiPriority == 0 || slot.uiPriority > 3;
            Rect bar = gizmoRect.LeftPart(1f/15f);
            if (leftBar)
            {
                bar.x = gizmoRect.x - 1.5f*bar.width;
            }
            else
            {
                bar.x = gizmoRect.xMax + 0.5f * bar.width;
            }
            Widgets.DrawBoxSolid(bar,Color.black);
            bar.yMin+=bar.height*(1f-healthPerc);
            //bar.height*=healthPerc;          
            var hColor = healthPerc < 0.3f?Color.red:healthPerc<0.7f?Color.yellow:Color.green;
            Widgets.DrawBoxSolid(bar, hColor);
        }
        private void GizmoInteraction(Rect rect, Texture2D icon,SlotDef slot)
        {
            if (slot!=null&&slot.isCoreFrame && Parent.HasGearCore)
            {
                RenderTexture portrait = PortraitsCache.Get(Parent.Dummy, rect.size, Parent.direction,cameraZoom:0.75f);
                Widgets.DrawTextureFitted(rect,portrait,1f);
            }
            else Widgets.DrawTextureFitted(rect, icon, 1f);

            if (!Widgets.ButtonInvisible(rect, icon)) return;//没做完
            
            switch (Event.current.button)
            {
                case 0://左键
                    {
                        if (slot==null) break;
                        if (occupiedSlots.ContainsKey(slot))
                        {
                            Find.WindowStack.Add(new Dialog_InfoCard(occupiedSlots[slot]));
                        }
                        else Find.WindowStack.Add(new Dialog_InfoCard(slot));
                        break;
                    }
                case 1://右键
                    {
                        List<FloatMenuOption> options = GizmoFloatMenu(slot);

                        Find.WindowStack.Add(new FloatMenu(options));
                        break;
                    }
            }
            
        }
        private List<FloatMenuOption> GizmoFloatMenu(SlotDef slot)
        {
            List<FloatMenuOption> options = new();
            if (slot != null&&occupiedSlots.TryGetValue(slot,out Thing t))
            {
                options.Add( new("Remove".Translate(t.LabelCap), () => RemoveModules(slot), MenuOptionPriority.High));
            }
            var modules = GetAvailableModules(slot,slot==null||slot.isCoreFrame);
            if (modules.EnumerableNullOrEmpty())
            {
                options.Add(new("NoModuleForSlot", null));
                return options;
            }
            foreach (var thing in modules)
            {
                string label = thing.LabelCap;
                Action action = () => AddOrReplaceModule(thing);
                options.Add(new(label,action));
            }
            return options;
        }
        //Stats Components
        private void DrawStatEntries(Rect rect,Thing thing)
        {
            WidgetRow row = new(rect.x,rect.y,UIDirection.RightThenDown,rect.width,gap:-8);
            row.Label("Performance".Translate());
            row.Gap(int.MaxValue);
            float loadPercent = Mathf.Max(1f,currentLoad / massCapacity);
            row.FillableBar(rect.width, 16f, loadPercent,$"{currentLoad.ToStringDecimalIfSmall() }/{massCapacity.ToStringDecimalIfSmall()}",WalkerGear_Core.fBarTex);
            //replace
            row.Label("OverallArmor".Translate());     
            foreach (StatDef statDef in toDraw)
            {
                float statValue = thing.GetStatValue(statDef);
                if (statDef.showOnDefaultValue || statValue != statDef.defaultBaseValue)
                {
                    row.Gap(int.MaxValue);
                    var t=statDef.LabelCap;
                    var v =statValue>4?statValue.ToStringDecimalIfSmall():statValue.ToStringPercent();
                    
                    row.Label(t);
                    row.Gap(rect.width-CalcSize(t + v).x-8f);
                    row.Label(v);
                }
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
            StatDefOf.ArmorRating_Sharp,
            StatDefOf.ArmorRating_Blunt,
            StatDefOf.ArmorRating_Heat
        };
    }
    //和维护坞连接
    public partial class ITab_MechGear
    {
        public static bool needUpdateCache=false;
        private Building_MaintenanceBay Parent
        {
            get => SelThing as Building_MaintenanceBay;
        }
        private Building_MaintenanceBay lastBay;
        private static readonly Dictionary<SlotDef, Thing> occupiedSlots = new();
        private static readonly Dictionary<int, SlotDef> positionWSlot = new(7);
        private static readonly List<SlotDef> toRemove=new();
        private static float massCapacity;
        private static float currentLoad;

        public override void OnOpen()
        {
            base.OnOpen();
            UpdateOccupiedSlotsCache();
        }
        public void UpdateOccupiedSlotsCache()
        {
            occupiedSlots.Clear();
            positionWSlot.Clear();
            massCapacity = 0;
            currentLoad = 0;
            foreach (var a in Parent.DummyApparels.WornApparel)
            {
                if (a.TryGetComp<CompWalkerComponent>(out var c))
                {
                    massCapacity += a.GetStatValue(MiscDefOf.VEF_MassCarryCapacity);
                   currentLoad+=a.GetStatValue(StatDefOf.Mass);
                    foreach (var s in c.Props.slots)
                    {
                        occupiedSlots[s] = a;
                        positionWSlot[s.uiPriority] = s;
                    }
                }
            }
            
            needUpdateCache = false;
        }
        private IEnumerable<Thing> GetAvailableModules(SlotDef slotDef, bool IsCore=false)
        {
            if (!Parent.TryGetComp(out CompAffectedByFacilities abf))
            {
                yield break;
            }

            foreach (var b in abf.LinkedFacilitiesListForReading)
            {
                if (b is not Building_Storage s)
                {
                    continue;
                }
                foreach (var t in s.GetSlotGroup()?.HeldThings)
                {
                    if (!t.TryGetComp(out CompWalkerComponent c)) continue;

                    if ((IsCore && c.Props.slots.Any(s => s.isCoreFrame)) || c.Props.slots.Contains(slotDef))
                    {
                        yield return t;
                    }
                }
            }

        }
        private void RemoveModules(SlotDef slot,bool updateNow=true)
        {
            if (!occupiedSlots.ContainsKey(slot)) return;
            toRemove.Clear();
            GetSupportedSlotRecur(slot);
            foreach (var s in toRemove)
            {
                if (occupiedSlots.TryGetValue(s,out var t))
                {
                    Parent.RemoveModule(t);
                }
                
            }
            if (updateNow)
            {
                UpdateOccupiedSlotsCache();
            }
        }
        private void AddOrReplaceModule(Thing thing)
        {
            if (!thing.TryGetComp(out CompWalkerComponent c)) return;
            foreach (var s in c.Props.slots)
            {
                RemoveModules(s,false);
            }
            Parent.Add(thing);
            UpdateOccupiedSlotsCache();
        }
        private static void GetSupportedSlotRecur(SlotDef slotDef)
        {
            if (!slotDef.supportedSlots.NullOrEmpty())
            {
                foreach (var s in slotDef.supportedSlots)
                {
                    GetSupportedSlotRecur(s);
                }
            }
            toRemove.Add(slotDef);
        }
    }
}
