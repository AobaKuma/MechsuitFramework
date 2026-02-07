// 当白昼倾坠之时
using System.Collections.Generic;
using System.Linq;
using CombatExtended;
using Mechsuit;
using RimWorld;
using RimWorld.Utility;
using UnityEngine;
using Verse;

namespace Exosuit.CE
{
    // 炮塔弹药组件
    // 提供炮塔弹药配套接口
    // 实现顶部扩展UI接口
    public partial class CompTurretAmmo : ThingComp, IReloadableComp, ITurretAmmoProvider, IModuleTopExtensionUI, IAmmoBackpackClearable
    {
        #region 字段
        
        private AmmoDef selectedAmmo;
        private int currentAmmoCount;
        private AmmoSetDef linkedAmmoSet;
        private AmmoDef pendingAmmo;
        private bool allowBackpackFeed;
        
        #endregion
        
        #region 属性
        
        public CompProperties_TurretAmmo Props => props as CompProperties_TurretAmmo;
        
        public Pawn Wearer => (parent as Apparel)?.Wearer;
        
        public Mechsuit.CompTurretGun TurretComp => parent.TryGetComp<Mechsuit.CompTurretGun>();
        
        public AmmoDef SelectedAmmo
        {
            get => selectedAmmo;
            set
            {
                if (selectedAmmo == value) return;
                
                // 如果有弹药需要先清空
                if (currentAmmoCount > 0 && selectedAmmo != null)
                {
                    pendingAmmo = value;
                    return;
                }
                
                selectedAmmo = value;
                pendingAmmo = null;
            }
        }
        
        public int CurrentAmmo
        {
            get => currentAmmoCount;
            set => currentAmmoCount = Mathf.Clamp(value, 0, MaxAmmo);
        }
        
        public ThingDef CurrentAmmoDef
        {
            get
            {
                if (currentAmmoCount > 0) return selectedAmmo;
                if (allowBackpackFeed)
                {
                    var backpack = CEPatches_Turret.GetBackpackForTurret(this);
                    if (backpack != null)
                    {
                        return backpack.CurrentFireAmmo;
                    }
                }
                return selectedAmmo;
            }
        }
        
        public ThingDef CurAmmoProjectile
        {
            get
            {
                ThingDef ammo = CurrentAmmoDef;
                if (ammo != null && LinkedAmmoSet != null)
                {
                    // 在弹药组中寻找对应的弹丸
                    foreach (var link in LinkedAmmoSet.ammoTypes)
                    {
                        if (link.ammo == ammo) return link.projectile;
                    }
                }
                return null;
            }
        }
        
        public int MaxAmmo => Props.magCapacity;
        
        public bool HasAmmoAvailable => currentAmmoCount > 0 && selectedAmmo != null;
        
        public bool AllowBackpackFeed
        {
            get => allowBackpackFeed;
            set => allowBackpackFeed = value;
        }
        
        public AmmoSetDef LinkedAmmoSet
        {
            get
            {
                if (linkedAmmoSet != null) return linkedAmmoSet;
                
                // 从Props获取
                if (Props.ammoSet != null)
                {
                    linkedAmmoSet = Props.ammoSet;
                    return linkedAmmoSet;
                }
                
                // 从verb推断
                linkedAmmoSet = InferAmmoSetFromVerb();
                return linkedAmmoSet;
            }
            set => linkedAmmoSet = value;
        }
        
        public bool NeedsClear => pendingAmmo != null && currentAmmoCount > 0;
        
        public AmmoDef PendingAmmo => pendingAmmo;
        
        #endregion
        
        #region 生命周期
        
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            
            if (!respawningAfterLoad)
            {
                allowBackpackFeed = Props.defaultAllowBackpackFeed;
                
                // 初始化弹药选择
                if (selectedAmmo == null && LinkedAmmoSet != null)
                {
                    var firstAmmo = LinkedAmmoSet.ammoTypes?.FirstOrDefault()?.ammo;
                    if (firstAmmo != null && CompAmmoBackpack.IsAmmoCompatible(firstAmmo))
                        selectedAmmo = firstAmmo;
                }
            }
        }
        
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Defs.Look(ref selectedAmmo, "selectedAmmo");
            Scribe_Values.Look(ref currentAmmoCount, "currentAmmoCount", 0);
            Scribe_Defs.Look(ref linkedAmmoSet, "linkedAmmoSet");
            Scribe_Defs.Look(ref pendingAmmo, "pendingAmmo");
            Scribe_Values.Look(ref allowBackpackFeed, "allowBackpackFeed", true);
        }
        
        #endregion
        
        #region ITurretAmmoProvider
        
        public bool TryConsumeAmmo(int count)
        {
            if (selectedAmmo == null) return false;
            if (currentAmmoCount < count) return false;
            
            currentAmmoCount -= count;
            return true;
        }
        
        #endregion
        
        #region IReloadableComp
        
        public Thing ReloadableThing => parent;
        
        public ThingDef AmmoDef => selectedAmmo;
        
        public int BaseReloadTicks => Props.reloadTicks;
        
        public int MaxCharges => MaxAmmo;
        
        public int RemainingCharges
        {
            get => currentAmmoCount;
            set => currentAmmoCount = Mathf.Clamp(value, 0, MaxAmmo);
        }
        
        public string LabelRemaining => $"{currentAmmoCount} / {MaxAmmo}";
        
        public bool NeedsReload(bool allowForceReload)
        {
            if (NeedsClear) return true;
            if (selectedAmmo == null) return false;
            if (allowForceReload) return currentAmmoCount < MaxAmmo;
            return currentAmmoCount == 0;
        }
        
        public int MinAmmoNeeded(bool allowForcedReload)
        {
            if (NeedsClear) return 0;
            if (!NeedsReload(allowForcedReload)) return 0;
            return 1;
        }
        
        public int MaxAmmoNeeded(bool allowForcedReload)
        {
            if (NeedsClear) return 0;
            if (!NeedsReload(allowForcedReload)) return 0;
            return MaxAmmo - currentAmmoCount;
        }
        
        public int MaxAmmoAmount() => MaxAmmo;
        
        public void ReloadFrom(Thing ammo)
        {
            if (NeedsClear) return;
            if (ammo == null) return;
            if (ammo.def != selectedAmmo) return;
            
            int needed = MaxAmmo - currentAmmoCount;
            int toConsume = Mathf.Min(ammo.stackCount, needed);
            
            if (toConsume <= 0) return;
            
            ammo.SplitOff(toConsume).Destroy();
            currentAmmoCount += toConsume;
        }
        
        public string DisabledReason(int minNeeded, int maxNeeded) => "";
        
        public bool CanBeUsed(out string reason)
        {
            reason = "";
            return false;
        }
        
        #endregion
        
        #region IAmmoBackpackClearable
        
        public void EjectCurrentAmmo()
        {
            EjectCurrentAmmoAt(null);
        }
        
        public void EjectCurrentAmmoAt(Building_MaintenanceBay gantry)
        {
            if (selectedAmmo == null || currentAmmoCount <= 0) return;
            
            Map map = null;
            IntVec3 pos = IntVec3.Invalid;
            
            if (gantry != null)
            {
                map = gantry.Map;
                pos = gantry.InteractionCell;
            }
            else
            {
                var wearer = Wearer;
                if (wearer != null)
                {
                    map = wearer.MapHeld;
                    pos = wearer.PositionHeld;
                }
                else
                {
                    map = parent.MapHeld;
                    pos = parent.PositionHeld;
                }
            }
            
            if (map == null || !pos.IsValid)
            {
                Log.Warning($"[TurretAmmo] 无法退出弹药: map={map}, pos={pos}");
                return;
            }
            
            var ammoThing = ThingMaker.MakeThing(selectedAmmo);
            ammoThing.stackCount = currentAmmoCount;
            GenPlace.TryPlaceThing(ammoThing, pos, map, ThingPlaceMode.Near);
            
            Log.Message($"[TurretAmmo] 退出弹药: {selectedAmmo.LabelCap} x{currentAmmoCount}");
            currentAmmoCount = 0;
            
            // 完成待切换
            if (pendingAmmo != null)
            {
                selectedAmmo = pendingAmmo;
                pendingAmmo = null;
            }
        }
        
        #endregion
        
        #region IModuleTopExtensionUI
        
        public bool ShouldShowTopExtension => true;
        
        public float TopExtensionHeight => 64f;
        
        public void DrawTopExtension(Rect rect)
        {
            // 绘制背景框
            Widgets.DrawBoxSolidWithOutline(rect, new Color(1f, 1f, 1f, 0.05f), new Color(1f, 1f, 1f, 0.1f));
            
            Rect inner = rect.ContractedBy(6f);
            
            // 模块图标
            Rect iconRect = new Rect(inner.x, inner.y, 40f, 40f);
            Widgets.DrawTextureFitted(iconRect, parent.def.uiIcon, 1f);
            TooltipHandler.TipRegion(iconRect, parent.LabelCap);
            
            float x = iconRect.xMax + 10f;
            float availableWidth = inner.width - (x - inner.x);
            
            // 绘制首行标题开关
            Rect topRowRect = new Rect(x, inner.y, availableWidth, 20f);
            Text.Font = GameFont.Tiny;
            Widgets.Label(topRowRect.LeftPart(0.7f), "WG_TurretAmmo_Label".Translate() + ": " + parent.LabelCap);
            
            // 绘制弹链供弹开关
            Rect toggleRect = new Rect(inner.xMax - 140f, inner.y, 140f, 20f);
            bool newValue = allowBackpackFeed;
            Widgets.CheckboxLabeled(toggleRect, "WG_TurretAmmo_AllowBackpack".Translate(), ref newValue);
            allowBackpackFeed = newValue;
            
            // 绘制次行弹药配置
            float bottomY = inner.y + 22f;
            Text.Font = GameFont.Small;
            
            // 弹种选择按钮
            Rect ammoBtnRect = new Rect(x, bottomY, 150f, 24f);
            string ammoLabel = selectedAmmo != null ? selectedAmmo.LabelCap : "WG_TurretAmmo_None".Translate();
            if (pendingAmmo != null) ammoLabel = ">> " + ammoLabel;
            
            if (Widgets.ButtonText(ammoBtnRect, ammoLabel.Truncate(ammoBtnRect.width)))
            {
                ShowAmmoSelector();
            }
            
            // 弹药条
            float barX = ammoBtnRect.xMax + 10f;
            Rect barRect = new Rect(barX, bottomY + 2f, inner.xMax - barX, 20f);
            float fillPct = MaxAmmo > 0 ? (float)currentAmmoCount / MaxAmmo : 0f;
            Widgets.FillableBar(barRect, fillPct);
            
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Tiny;
            Widgets.Label(barRect, $"{currentAmmoCount} / {MaxAmmo}");
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }
        
        #endregion
        
        private void ShowAmmoSelector()
        {
            var ammoSet = LinkedAmmoSet;
            if (ammoSet == null)
            {
                Messages.Message("WG_TurretAmmo_NoAmmoSet".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }
            
            var options = new List<FloatMenuOption>();
            
            foreach (var link in ammoSet.ammoTypes)
            {
                if (!CompAmmoBackpack.IsAmmoCompatible(link.ammo)) continue;
                
                var ammoDef = link.ammo;
                string label = ammoDef.LabelCap;
                
                if (ammoDef == selectedAmmo)
                    label += " ✓";
                if (ammoDef == pendingAmmo)
                    label += " (待切换)";
                
                options.Add(new FloatMenuOption(label, () =>
                {
                    SetSelectedAmmo(ammoDef);
                }));
            }
            
            if (options.Count == 0)
            {
                options.Add(new FloatMenuOption("WG_TurretAmmo_NoCompatible".Translate(), null));
            }
            
            Find.WindowStack.Add(new FloatMenu(options));
        }
        
        public void SetSelectedAmmo(AmmoDef ammo)
        {
            if (selectedAmmo == ammo)
            {
                pendingAmmo = null;
                return;
            }
            
            if (currentAmmoCount > 0)
            {
                pendingAmmo = ammo;
                return;
            }
            
            selectedAmmo = ammo;
            pendingAmmo = null;
        }
        
        #region 辅助方法
        
        private AmmoSetDef InferAmmoSetFromVerb()
        {
            // 推断默认弹药组数据
            if (parent.def.verbs.NullOrEmpty()) return null;
            
            foreach (var verb in parent.def.verbs)
            {
                if (verb is VerbPropertiesCE ceProp && ceProp.defaultProjectile != null)
                {
                    // 从弹丸反推弹药组
                    foreach (var ammoSet in DefDatabase<AmmoSetDef>.AllDefs)
                    {
                        foreach (var link in ammoSet.ammoTypes)
                        {
                            if (link.projectile == ceProp.defaultProjectile)
                                return ammoSet;
                        }
                    }
                }
            }
            
            return null;
        }
        
        public IEnumerable<AmmoDef> GetAvailableAmmoTypes()
        {
            var ammoSet = LinkedAmmoSet;
            if (ammoSet == null) yield break;
            
            foreach (var link in ammoSet.ammoTypes)
            {
                if (CompAmmoBackpack.IsAmmoCompatible(link.ammo))
                    yield return link.ammo;
            }
        }
        
        public override string CompInspectStringExtra()
        {
            if (selectedAmmo == null)
                return "WG_TurretAmmo_NoAmmo".Translate();
            
            string result = "WG_TurretAmmo_Status".Translate(selectedAmmo.LabelCap, currentAmmoCount, MaxAmmo);
            
            if (allowBackpackFeed)
                result += "\n" + "WG_TurretAmmo_BackpackEnabled".Translate();
            
            return result;
        }
        
        
        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            foreach (var gizmo in base.CompGetWornGizmosExtra())
            {
                yield return gizmo;
            }
            
            // 过滤玩家派系显示项
            if (Wearer != null && Wearer.Faction == Faction.OfPlayer)
            {
                var turretGun = TurretComp;
                if (turretGun != null)
                {
                    yield return new Mechsuit.Gizmo_TurretAmmoStatus
                    {
                        comp = turretGun,
                        provider = this
                    };
                }
            }
        }
        
        #endregion
    }
}
