using System;
using System.Collections.Generic;
using System.Linq;
using Exosuit.Misc;
using RimWorld;
using RimWorld.Utility;
using UnityEngine;
using Verse;
using Verse.AI;



namespace Exosuit
{
    //维修 TODO: 统一为一个整备的工作，先装弹，再维修
    public partial class Building_MaintenanceBay : Building
    {
        public CompAffectedByFacilities CompAffectedBy => GetComp<CompAffectedByFacilities>();
        public List<Building_Storage> LinkedStorages { get
            {
                List<Building_Storage> storages = [];
                CompAffectedBy?.LinkedFacilitiesListForReading.ForEach(t => {
                    if (t is Building_Storage bs) storages.Add(bs); 
                });
                return storages;
            } 
        }
        public BayExtension Ext=>extension??=def.GetModExtension<BayExtension>();
        private BayExtension extension;
        public bool CanRepair =>Ext.canRepair&& Faction.IsPlayer && autoRepair ;//臨時停機點不能修 TODO: 给我去xml里写能不能修呀。
        public bool CanReload => Ext.canLoad&&Faction.IsPlayer && autoReload   ;
        public bool NeedRepair //只要有一個需要修，那就能修。
        {
            get
            {
                if (!Spawned || !CanRepair || !HasGearCore) return false;
                if (Core.Damaged)
                {
                    LessonAutoActivator.TeachOpportunity(ConceptDef.Named("WG_Gantry_Repair"), OpportunityType.Important);
                    return true;
                }
                return false;
            }
        }
        public bool NeedReload => CanReload && GetFirstNeedReload() != null;
        
        // 是否需要清空弹药背包（CE 模块实现 IAmmoBackpackClearable 接口）
        public bool NeedClearAmmoBackpack => CanReload && GetFirstNeedClearAmmoBackpack() != null;

        protected bool autoRepair = true;
        protected bool autoReload = true;
        
        public IReloadableComp GetFirstNeedReload()
        {
            // 先检查原版的 CompApparelReloadable
            var result = ReloadableUtility.FindSomeReloadableComponent(Dummy, false);
            if (result != null) return result;
            
            // 检查所有实现 IReloadableComp 的组件
            if (Dummy?.apparel != null)
            {
                foreach (Apparel item in Dummy.apparel.WornApparel)
                {
                    if (item is not ThingWithComps twc) continue;
                    
                    foreach (var comp in twc.AllComps)
                    {
                        if (comp is IReloadableComp reloadable && reloadable.NeedsReload(true))
                        {
                            // 跳过需要清空的弹药背包（它们需要先清空再装填）
                            if (comp is IAmmoBackpackClearable clearable && clearable.NeedsClear)
                                continue;
                            
                            return reloadable;
                        }
                    }
                }
            }
            return null;
        }
        
        // 获取第一个需要清空的弹药背包
        public IAmmoBackpackClearable GetFirstNeedClearAmmoBackpack()
        {
            if (Dummy?.apparel == null) return null;
            
            foreach (Apparel item in Dummy.apparel.WornApparel)
            {
                if (item is not ThingWithComps twc) continue;
                
                foreach (var comp in twc.AllComps)
                {
                    if (comp is IAmmoBackpackClearable clearable && clearable.NeedsClear)
                    {
                        return clearable;
                    }
                }
            }
            return null;
        }
        public void Repair(int amount = 1)
        {
            foreach (Apparel item in DummyModules)
            {
                var comp = item.GetComp<CompSuitModule>();
                if (comp == null) continue;
                if (comp.HP < comp.MaxHP)
                {
                    comp.HP = Math.Min(comp.HP + amount, comp.MaxHP);
                    Core.SetHPDirty();
                    return;
                }
            }
        }
    }

    public partial class Building_MaintenanceBay : Building
    {
        //methods override
        public override IEnumerable<Gizmo> GetGizmos()
        {
            if (HasGearCore && Faction.IsPlayer)
            {
                var assignable = GetComp<CompAssignableToPawn_Parking>();
                if (assignable!=null && assignable.AssignedPawnsForReading.Any())
                {
                    Command_Action command_assignedGetIn = new()
                    {
                        defaultLabel = "WG_GetInAssigned".Translate(),
                        icon = Resources.WG_GetInWalker,
                        action = delegate {
                            var cand =
                            assignable.AssignedPawnsForReading.Where(
                                p => p.MapHeld == MapHeld && p.Spawned 
                                && !p.DeadOrDowned && CanAcceptPawn(p));
                            if (cand.Count()>1)
                            {
                                Find.WindowStack.Add(new FloatMenu(
                                    cand.Select(p=>new FloatMenuOption(p.NameShortColored,                      delegate
                                        {
                                            p.jobs.StartJob(
                                                JobMaker.MakeJob(
                                                    JobDefOf.WG_GetInWalkerCore, this),JobCondition.InterruptForced);
                                        })).ToList()
                                    ));
                            }
                            else if (cand.Any())
                            {
                                cand.First().jobs.StartJob(JobMaker.MakeJob(JobDefOf.WG_GetInWalkerCore, this), JobCondition.InterruptForced);
                            }
                        },
                        
                        
                    };
                    if (!assignable.AssignedPawnsForReading.Any(p=>p.MapHeld==MapHeld&&p.Spawned&&!p.DeadOrDowned && CanAcceptPawn(p)))
                    {
                        command_assignedGetIn.Disable("WG_NoAvailablePilot".TranslateSimple());
                    }
                    
                    yield return command_assignedGetIn;
                }

                Command_Target command_GetIn = new()
                {
                    defaultLabel = "WG_GetIn".Translate(),
                    icon = Resources.WG_GetInWalker,
                    targetingParams = TargetingParameters.ForPawns(),
                    action = (tar) =>
                    {
                        if (tar.Pawn == null || !CanAcceptPawn(tar.Pawn) || tar.Pawn.Downed) return;
                        tar.Pawn.jobs.StartJob(JobMaker.MakeJob(JobDefOf.WG_GetInWalkerCore, this), JobCondition.InterruptForced);
                    }
                };
                yield return command_GetIn;
                
                

                Command_Toggle toggle_autoRepair = new()
                {
                    icon = Resources.WG_AutoRepair,
                    defaultLabel = "WG_AutoRepair".Translate(),
                    defaultDesc = "WG_AutoRepair_Desc".Translate(),
                    isActive = () => autoRepair,
                    toggleAction = delegate
                    {
                        autoRepair = !autoRepair;
                    }
                };
                yield return toggle_autoRepair;
            }

            if (Prefs.DevMode == true && DebugSettings.godMode==true)
            {
                Command_Action command_renderTree = new()
                {
                    defaultLabel = "Show Render Tree",
                    action = () => {                        
                        Find.WindowStack.Add(new Dialog_DebugRenderTreeFixed(Dummy));
                    }
                    
                };
                yield return command_renderTree;
            }

            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
        }
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            foreach (Apparel a in DummyModules)
            {
                GenPlace.TryPlaceThing(MechUtility.Conversion(a), Position, Map, ThingPlaceMode.Direct);
            }
            Dummy.Destroy();
            base.Destroy(mode);
        }
        
        static Building_MaintenanceBay()
        {
            GameComp_Tool.RegisterStaticCacheCleaner(pawnsInBuilding.Clear);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref autoRepair, "autoRepair",true);

            Scribe_Deep.Look(ref cachePawn, "cachedPawn");
            
            // 序列化待处理的模块工作队列
            Scribe_Collections.Look(ref pendingModuleWork, "pendingModuleWork", LookMode.Deep);
            
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                pendingModuleWork ??= [];
            }
            
            if (Scribe.mode==LoadSaveMode.PostLoadInit)
            {
                if (Dummy.story.headType != null)
                {
                    var apparel = cachePawn.apparel;
                    cachePawn = null;                    
                    apparel.pawn = Dummy;
                    Dummy.apparel = apparel;
                }
                pawnsInBuilding.Add(Dummy);
                
                LongEventHandler.ExecuteWhenFinished(()=>TryUpdateCache(true));
            }
        }
    }
    //给Itab提供的功能
    public partial class Building_MaintenanceBay
    {
        protected bool isCacheDirty = true;
        public void SetCacheDirty()
        {
            isCacheDirty = true;
            LongEventHandler.ExecuteWhenFinished(() => TryUpdateCache(true));
        }


        private static readonly List<SlotDef> toRemove = [];

        public readonly Dictionary<SlotDef, Thing> occupiedSlots = [];
        public readonly Dictionary<int, SlotDef> positionWSlot = new(7);
        public List<CompSuitModule> ComponentsCache { get; protected set; } = [];
        public bool HasGearCore => Core != null;

        public Exosuit_Core Core { get; protected set; }
        public void TryUpdateCache(bool force = false)
        {
            if (!force && !isCacheDirty)
            {
                return;
            }
            Core = null;

            occupiedSlots.Clear();
            positionWSlot.Clear();

            ComponentsCache.Clear();

            foreach (Apparel a in Dummy.apparel.WornApparel)
            {
                if (!(a.GetComp<CompSuitModule>() is CompSuitModule comp and not null))
                {
                    return;
                }

                foreach (SlotDef s in comp.Props.occupiedSlots)
                {
                    occupiedSlots[s] = a;
                    positionWSlot[s.uiPriority] = s;
                }
                ComponentsCache.Add(comp);
                if (a is Exosuit_Core c)
                {
                    Core = c;
                }
            }

            Core?.ModuleRecache();
            //GraphicRecache(); 衣服改变会自动更新贴图cache
            isCacheDirty = false;
        }
/*        private void GraphicRecache()
        {
            cachePawn.Drawer.renderer.EnsureGraphicsInitialized();
            cachePawn.Drawer.renderer.SetAllGraphicsDirty();
        }*/
        protected void RemoveModule(Thing t)
        {
            if (t == null || cachePawn == null) return;
            if (!DummyApparels.Contains(t)) return;
            Thing moduleItem = MechUtility.Conversion(t);
            foreach (Building_Storage b in LinkedStorages)
            {
                if (!b.Accepts(moduleItem)) continue;
                foreach (IntVec3 cell in b.slotGroup)
                {
                    if (StoreUtility.IsGoodStoreCell(cell, Map, moduleItem, null, Faction))
                    {
                        GenPlace.TryPlaceThing(moduleItem, cell, Map, ThingPlaceMode.Direct);
                        return;
                    }
                }
            }
            GenPlace.TryPlaceThing(moduleItem, InteractionCell, Map, ThingPlaceMode.Direct);
        }
        public void RemoveModules(SlotDef slot, bool updateNow = true)
        {
            if (!occupiedSlots.ContainsKey(slot)) return;
            SetCacheDirty();
            toRemove.Clear();
            GetSupportedSlotRecur(slot);
            foreach (var s in toRemove)
            {
                if (occupiedSlots.TryGetValue(s, out var t))
                {
                    RemoveModule(t);
                }

            }

            if (updateNow)
            {
                TryUpdateCache(true);
            }
        }

        public void AddOrReplaceModule(Thing thing)
        {
            if (!thing.TryGetComp(out CompSuitModule comp)) return;
            Apparel a = MechUtility.Conversion(thing) as Apparel;
            if (comp.Props.occupiedSlots.Any(slot => slot.isCoreFrame))
            {
                //如果這個要裝上的模塊是CoreFrame，清空所有。
                ClearAllModules();
            }
            else foreach (SlotDef s in a.GetComp<CompSuitModule>().Slots)
                {
                    RemoveModules(s, false);
                }
            DummyApparels.GetDirectlyHeldThings().TryAdd(a, false);
            TryUpdateCache(true);
        }
        private void GetSupportedSlotRecur(SlotDef slotDef)
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
        public void ClearAllModules()
        {
            DummyModules.ForEach(RemoveModule);
            TryUpdateCache(true);
        }

        public IEnumerable<Thing> GetAvailableModules(SlotDef slotDef, bool IsCore = false)
        {
            foreach (Building_Storage s in LinkedStorages)
            {
                foreach (Thing module in s.slotGroup.HeldThings)
                {
                    if (!module.TryGetComp(out CompSuitModule c)) continue;
                    
                    // 跳过被其他整备架预留的模块
                    if (IsModuleReservedByAnyGantry(module, this)) continue;
                    
                    if (IsCore)
                    {
                        if (!c.Props.occupiedSlots.Any(s => s.isCoreFrame)) continue;
                    }
                    else
                    {
                        if (!c.Props.occupiedSlots.Contains(slotDef)) continue;
                    }
                    yield return module;
                }
            }
        }

        //在上機時把連接架子的物品放到駕駛員身上
        public void PlaceShelfItemToPilot(Pawn pilotPawn)
        {
            if (pilotPawn.inventory == null) return;
            if (MassUtility.FreeSpace(pilotPawn) <= 0) return; //如果满了就不放了
            if (LinkedStorages.NullOrEmpty()) return; //如果没有连接架子就不放了

            List<Building_Storage> _cache = LinkedStorages.Where(s => s.GetComp<CompModuleStorage>() == null).ToList();
            foreach (var storage in _cache)
            {
                foreach (Thing item in storage.slotGroup.HeldThings)
                {
                    if (MassUtility.FreeSpace(pilotPawn) < item.GetStatValue(StatDefOf.Mass)) continue;

                    item.DeSpawnOrDeselect(); //先取消选择和去除地图上的物品
                    pilotPawn.inventory.innerContainer.TryAddOrTransfer(item);
                }
            }
        }
        //在下机时把驾驶员身上(能被架子收起的)的物品放到连接架子上
        public void PlaceInventoryItemToShelf(Pawn pilotPawn)
        {
            if (pilotPawn.inventory == null) return;
            if (!pilotPawn.inventory.innerContainer.Any) return;
            if (LinkedStorages.NullOrEmpty()) return; //如果没有连接架子就不放了

            List<Thing> _cacheThings = new List<Thing>();
            foreach (var item in pilotPawn.inventory.innerContainer)
            {
                foreach (var shelf in LinkedStorages)
                {
                    if (shelf.Accepts(item) && shelf.SpaceRemainingFor(item.def) != 0)
                    {
                        _cacheThings.Add(item);
                        break; //如果这个架子能放这个物品，就不再检查其他架子了
                    }
                }
            }
            if (_cacheThings.Any())
            {
                List<Building_Storage> _cache = LinkedStorages.Where(s => s.GetComp<CompModuleStorage>() == null).ToList();
                foreach (var item in _cacheThings)
                {
                    //遍历所有架子，找到能放下这个物品的架子
                    Building_Storage shelf = _cache.Where(s => s.Accepts(item) && s.SpaceRemainingFor(item.def) != 0).First();
                    if (shelf != null)
                    {
                        foreach (IntVec3 cell in shelf.slotGroup)
                        {
                            if (StoreUtility.IsGoodStoreCell(cell, Map, item, null, Faction))
                            {
                                pilotPawn.inventory.innerContainer.Remove(item); //尝试将物品从驾驶员的背包中移除
                                pilotPawn.inventory.Notify_ItemRemoved(item);
                                GenPlace.TryPlaceThing(item, cell, Map, ThingPlaceMode.Direct);
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
    //穿脱龙骑兵
    public partial class Building_MaintenanceBay
    {
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn) //選擇殖民者後右鍵
        {

            if (!selPawn.CanReach(this, PathEndMode.InteractionCell, Danger.Deadly))
            {
                yield return new FloatMenuOption("CannotEnterBuilding".Translate(this) + ": " + "NoPath".Translate().CapitalizeFirst(), null);
                yield break;
            }
            var baseAc = AcceptablePawnKind(selPawn);
            if (!baseAc)
            {
                yield return new(baseAc.Reason, null);
                yield break;
            }
            //p b hasCore
            //T T  F Ocuppied
            //T F  T 
            //F T  T 
            //F F  F NoCore
            bool pawnHasCore = selPawn.PawnWearingExosuitCore();
            bool bayHasCore = HasGearCore;
            if (pawnHasCore != bayHasCore)
            {
                yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("EnterBuilding".Translate(this), delegate
                {
                    selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(bayHasCore? JobDefOf.WG_GetInWalkerCore: JobDefOf.WG_GetOffWalkerCore, this), JobTag.DraftedOrder);
                }), selPawn, this);
            } 
            else
            {
                if (bayHasCore)
                {
                    yield return new("WG_BayOccupied".TranslateSimple(), null);
                }
                else
                {
                    yield return new("WG_BayHasNoCore".TranslateSimple(),null);
                }
            }
        }
        public AcceptanceReport CanReach(Thing thing)
        {
            if (thing == null) return false;
            if (!thing.MapHeld.reachability.CanReach(thing.Position, this.InteractionCell, PathEndMode.OnCell, TraverseMode.ByPawn, Danger.Deadly)) return false;
            return true;
        }
        public virtual AcceptanceReport CanAcceptPawn(Pawn pawn)
        {
            AcceptanceReport baseAc = AcceptablePawnKind(pawn);
            if (!baseAc)
            {
                return baseAc;
            }
            if (pawn.apparel.WornApparel.HasCore())
            {
                return "WG_Disabled_AlreadyHasCoreFrame".Translate(pawn.Name.ToString()).CapitalizeFirst();
            }
            if (!HasGearCore)
            {
                return "WG_Disabled_NoCoreFrame".Translate().CapitalizeFirst();
            }

            return true;
        }
        
        public virtual AcceptanceReport AcceptablePawnKind(Pawn pawn)
        {
            if (GetComp<CompPowerTrader>()?.PowerOn ?? false)
            {
                return "NoPower".Translate().CapitalizeFirst();
            }
            if (!pawn.RaceProps.Humanlike || pawn.IsQuestLodger() || !pawn.IsColonist && !pawn.IsSlaveOfColony && !pawn.IsPrisonerOfColony && (pawn.IsColonySubhumanPlayerControlled || pawn.IsGhoul))
            {
                return "PawnNotQualified".Translate(pawn.Name.ToString()).CapitalizeFirst();
            }
            return true ;
        }
        public bool CanGear(Pawn pawn,out Tuple<Apparel,Apparel> cant)
        {
            cant=null;
            foreach (Apparel a in DummyModules)
            {
                foreach (var item in pawn.apparel.WornApparel)
                {
                    if (pawn.apparel.IsLocked(item) && !ApparelUtility.CanWearTogether(item.def, a.def, pawn.RaceProps.body))
                    {
                        cant = new(a,item);
                        return false;
                    }
                        
                }
            }
            return true;
        }
        public virtual void GearUp(Pawn pawn)
        {
            if (cachePawn == null || !HasGearCore) return;
            if (!CanGear(pawn,out var c))
            {
                Log.Error($"Conflict Apparels {c.Item1} => {c.Item2}");
            }
            DummyModules.ForEach(a => {
                DummyApparels.Remove(a);
                pawn.apparel.Wear(a, true, locked: true); 
            });
            Core.Notify_Equipped(pawn);
            Core.ModuleRecache();
            Core = null;
            SetCacheDirty();
            PlaceShelfItemToPilot(pawn);
        }
        public virtual void GearDown(Pawn pawn)
        {
            if (HasGearCore) return;
            bool equippedModuleWeapon = pawn.equipment?.Primary?.TryGetComp<CompApparelForcedWeapon>(out _) ?? false;
            foreach (Apparel a in pawn.RemoveExosuit()) // 在维护龙门架卸甲时不应用额外损坏
            {
                if (equippedModuleWeapon && a.TryGetComp<CompModuleWeapon>(out var moduleWeapon)&&pawn.equipment.Primary== moduleWeapon.Weapon)
                {
                    pawn.equipment.Remove(moduleWeapon.Weapon);
                }
                DummyApparels.Wear(a, false, true);
                if (a is Exosuit_Core core)Core = core;
            }

            SetCacheDirty();
            MechUtility.WeaponDropCheck(pawn);
            PlaceInventoryItemToShelf(pawn);
        }
    }
    //渲染小人的
    public partial class Building_MaintenanceBay
    {
        private Pawn cachePawn;
        public virtual Pawn Dummy
        {
             get
            {
                if (cachePawn == null)
                {
                    cachePawn = (Pawn)ThingMaker.MakeThing(ThingDefOf.Dummy);
                    PawnComponentsUtility.CreateInitialComponents(cachePawn);
                    cachePawn.story.bodyType = BodyTypeDefOf.Male;
                    cachePawn.story.headType = HeadTypeDefOf.Stump;
                    cachePawn.story.hairDef = HairDefOf.Bald;
                    cachePawn.ageTracker.LockCurrentLifeStageIndex(cachePawn.def.race.lifeStageAges.Count-1);
                    cachePawn.Name = new NameSingle(" ",false);
                    cachePawn.gender = Gender.None;
                    cachePawn.kindDef = PawnKindDefOf.Colonist;
                    cachePawn.genes?.SetXenotypeDirect(XenotypeDefOf.Baseliner);
                    cachePawn.ideo?.SetIdeo(Faction.OfPlayer.ideos?.PrimaryIdeo);
                    pawnsInBuilding.Add(cachePawn);
                    cachePawn.apparel.DestroyAll();
                    cachePawn.rotationInt = Rotation.Opposite;
                    cachePawn.drafter = new(cachePawn)
                    {
                        Drafted = true
                    };
                }
                return cachePawn;
            }
        }
        public static bool PawnInBuilding(Pawn pawn)
        {
            pawnsInBuilding.RemoveWhere(p => p == null || p.Destroyed);
            return pawnsInBuilding.Contains(pawn);
        }
        public static HashSet<Pawn> pawnsInBuilding = [];

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            LessonAutoActivator.TeachOpportunity(ConceptDef.Named("WG_Gantry_LinkBuilding"), OpportunityType.GoodToKnow);
        }
        public Pawn_ApparelTracker DummyApparels
        {
            get => Dummy.apparel;
            private set {
                value.pawn = Dummy;
                Dummy.apparel = value;
            }
        }
        public List<Apparel> DummyModules => [.. ComponentsCache.ConvertAll(c => c.parent as Apparel)];

        public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
        {
            base.DynamicDrawPhaseAt(phase, drawLoc, flip);
            if (!HasGearCore) return;
            if (!Dummy.Drawer.renderer.renderTree.Resolved)
            {
                Dummy.Drawer.renderer.renderTree.EnsureInitialized(PawnRenderFlags.DrawNow);
            }
            //
            
            Dummy.Drawer.renderer.DynamicDrawPhaseAt(phase,drawLoc.WithYOffset(1f), Rotation.Opposite, true);
        }
    }

    // 模块装卸工作队列
    public partial class Building_MaintenanceBay
    {
        #region 内部类

        // 模块操作请求
        public class ModuleWorkRequest : IExposable
        {
            public SlotDef targetSlot;           // 目标槽位
            public Thing moduleToInstall;        // 要安装的模块（null 表示纯卸载）
            public ThingDef moduleDefToReinstall; // 要重新安装的模块定义（用于保留模块模式）
            public bool needRemoveFirst;         // 是否需要先卸载现有模块
            public bool removeCompleted;         // 卸载是否已完成

            public void ExposeData()
            {
                Scribe_Defs.Look(ref targetSlot, "targetSlot");
                Scribe_References.Look(ref moduleToInstall, "moduleToInstall");
                Scribe_Defs.Look(ref moduleDefToReinstall, "moduleDefToReinstall");
                Scribe_Values.Look(ref needRemoveFirst, "needRemoveFirst");
                Scribe_Values.Look(ref removeCompleted, "removeCompleted");
            }
        }

        #endregion

        #region 字段

        // 模块操作请求队列
        private List<ModuleWorkRequest> pendingModuleWork = [];

        #endregion

        #region 公共方法

        // 是否有待处理的模块工作
        public bool HasPendingModuleWork()
        {
            CleanupInvalidPendingWork();
            return pendingModuleWork.Count > 0;
        }

        // 是否有待卸载的工作（优先处理）
        public bool HasPendingRemove()
        {
            return pendingModuleWork.Any(w => w.needRemoveFirst && !w.removeCompleted);
        }

        // 是否有待安装的工作
        public bool HasPendingInstall()
        {
            return pendingModuleWork.Any(w => 
                (w.moduleToInstall != null || w.moduleDefToReinstall != null) && 
                (!w.needRemoveFirst || w.removeCompleted));
        }

        // 获取下一个待卸载的槽位
        public SlotDef GetPendingRemoveSlot()
        {
            var work = pendingModuleWork.FirstOrDefault(w => w.needRemoveFirst && !w.removeCompleted);
            return work?.targetSlot;
        }

        // 获取下一个待安装的模块
        public Thing GetPendingInstall()
        {
            CleanupInvalidPendingWork();
            var work = pendingModuleWork.FirstOrDefault(w => 
                (w.moduleToInstall != null || w.moduleDefToReinstall != null) && 
                (!w.needRemoveFirst || w.removeCompleted));
            
            if (work == null) return null;
            
            // 如果有直接的模块引用，返回它
            if (work.moduleToInstall != null) return work.moduleToInstall;
            
            // 否则从储物架中查找对应 ThingDef 的模块
            if (work.moduleDefToReinstall != null)
            {
                return FindModuleInStorage(work.moduleDefToReinstall);
            }
            
            return null;
        }
        
        // 从连接的储物架中查找指定 ThingDef 的模块
        private Thing FindModuleInStorage(ThingDef moduleDef)
        {
            if (moduleDef == null) return null;
            
            foreach (Building_Storage storage in LinkedStorages)
            {
                foreach (Thing thing in storage.slotGroup.HeldThings)
                {
                    if (thing.def == moduleDef && !IsModuleReservedByAnyGantry(thing, this))
                    {
                        return thing;
                    }
                }
            }
            return null;
        }

        // 获取指定槽位的待安装模块（用于 UI 预览）
        public Thing GetPendingInstallForSlot(SlotDef slot)
        {
            if (slot == null) return null;
            var work = pendingModuleWork.FirstOrDefault(w => 
                w.targetSlot == slot && (w.moduleToInstall != null || w.moduleDefToReinstall != null));
            
            if (work == null) return null;
            
            // 如果有直接的模块引用，返回它
            if (work.moduleToInstall != null) return work.moduleToInstall;
            
            // 否则从储物架中查找对应 ThingDef 的模块
            if (work.moduleDefToReinstall != null)
            {
                return FindModuleInStorage(work.moduleDefToReinstall);
            }
            
            return null;
        }

        // 请求安装/替换模块（由 ITab 调用）
        public void RequestInstallModule(Thing module, bool keepModulesOnCoreReplace = false)
        {
            if (module == null || !module.Spawned) return;
            if (!module.TryGetComp(out CompSuitModule comp)) return;
            
            // 检查是否已经有这个模块的请求
            if (pendingModuleWork.Any(w => w.moduleToInstall == module)) return;
            
            // 检查是否是核心框架
            bool isCoreFrame = comp.Props.occupiedSlots.Any(slot => slot.isCoreFrame);
            
            // 如果正在进行核心替换工作，不允许安装非核心模块
            if (!isCoreFrame && IsCoreWorkInProgress())
            {
                Log.Warning("[Exosuit] 核心替换工作进行中，无法安装其他模块");
                return;
            }
            
            if (isCoreFrame)
            {
                // 核心框架：先清除所有现有请求
                pendingModuleWork.Clear();
                
                // 获取新核心支持的槽位
                var newCoreSlot = comp.Props.occupiedSlots.First(s => s.isCoreFrame);
                HashSet<SlotDef> newCoreSupportedSlots = [];
                if (newCoreSlot.supportedSlots != null)
                {
                    foreach (var s in newCoreSlot.supportedSlots)
                    {
                        newCoreSupportedSlots.Add(s);
                    }
                }
                
                // 收集当前安装的非核心模块的 ThingDef（用于保留模块模式）
                List<(SlotDef slot, ThingDef moduleDef)> modulesToReinstall = [];
                if (keepModulesOnCoreReplace)
                {
                    foreach (var kvp in occupiedSlots)
                    {
                        if (!kvp.Key.isCoreFrame && kvp.Value != null)
                        {
                            // 检查新核心是否支持这个槽位
                            if (!newCoreSupportedSlots.Contains(kvp.Key))
                            {
                                continue; // 新核心不支持这个槽位，跳过
                            }
                            
                            // 获取物品形态的 ThingDef
                            if (kvp.Value.TryGetComp(out CompSuitModule moduleComp))
                            {
                                modulesToReinstall.Add((kvp.Key, moduleComp.Props.ItemDef));
                            }
                        }
                    }
                }
                
                // 为每个已安装的槽位添加卸载请求
                foreach (var slot in occupiedSlots.Keys.ToList())
                {
                    pendingModuleWork.Add(new ModuleWorkRequest
                    {
                        targetSlot = slot,
                        moduleToInstall = null,
                        moduleDefToReinstall = null,
                        needRemoveFirst = true,
                        removeCompleted = false
                    });
                }
                
                // 添加安装核心的请求
                pendingModuleWork.Add(new ModuleWorkRequest
                {
                    targetSlot = newCoreSlot,
                    moduleToInstall = module,
                    moduleDefToReinstall = null,
                    needRemoveFirst = false,
                    removeCompleted = true
                });
                
                // 如果保留模块，添加重新安装请求（使用 ThingDef）
                if (keepModulesOnCoreReplace)
                {
                    foreach (var (slot, moduleDef) in modulesToReinstall)
                    {
                        pendingModuleWork.Add(new ModuleWorkRequest
                        {
                            targetSlot = slot,
                            moduleToInstall = null,
                            moduleDefToReinstall = moduleDef,
                            needRemoveFirst = false,
                            removeCompleted = true
                        });
                    }
                }
            }
            else
            {
                // 普通模块：为每个槽位创建请求
                foreach (SlotDef slot in comp.Props.occupiedSlots)
                {
                    // 移除该槽位的现有请求
                    pendingModuleWork.RemoveAll(w => w.targetSlot == slot);
                    
                    bool hasExisting = occupiedSlots.ContainsKey(slot);
                    pendingModuleWork.Add(new ModuleWorkRequest
                    {
                        targetSlot = slot,
                        moduleToInstall = module,
                        moduleDefToReinstall = null,
                        needRemoveFirst = hasExisting,
                        removeCompleted = !hasExisting
                    });
                }
            }
            
            Log.Message($"[Exosuit] 请求安装模块: {module.LabelCap}, 保留模块: {keepModulesOnCoreReplace}");
        }

        // 请求卸载模块（由 ITab 调用）
        public void RequestRemoveModule(SlotDef slot)
        {
            if (slot == null) return;
            if (!occupiedSlots.ContainsKey(slot)) return;
            
            // 移除该槽位的现有请求
            pendingModuleWork.RemoveAll(w => w.targetSlot == slot);
            
            // 如果是核心槽位，需要先卸载所有非核心模块
            if (slot.isCoreFrame)
            {
                // 清除所有现有请求
                pendingModuleWork.Clear();
                
                // 先添加所有非核心模块的卸载请求
                foreach (var kvp in occupiedSlots)
                {
                    if (kvp.Key.isCoreFrame) continue;
                    if (kvp.Value == null) continue;
                    
                    pendingModuleWork.Add(new ModuleWorkRequest
                    {
                        targetSlot = kvp.Key,
                        moduleToInstall = null,
                        needRemoveFirst = true,
                        removeCompleted = false
                    });
                }
                
                // 最后添加核心的卸载请求
                pendingModuleWork.Add(new ModuleWorkRequest
                {
                    targetSlot = slot,
                    moduleToInstall = null,
                    needRemoveFirst = true,
                    removeCompleted = false
                });
                
                Log.Message($"[Exosuit] 请求卸载核心，将先卸载 {pendingModuleWork.Count - 1} 个模块");
            }
            else
            {
                pendingModuleWork.Add(new ModuleWorkRequest
                {
                    targetSlot = slot,
                    moduleToInstall = null,
                    needRemoveFirst = true,
                    removeCompleted = false
                });
                
                Log.Message($"[Exosuit] 请求卸载槽位: {slot.label}");
            }
        }

        // 取消指定槽位的所有请求
        public void CancelPendingWork(SlotDef slot)
        {
            pendingModuleWork.RemoveAll(w => w.targetSlot == slot);
        }

        // 取消安装请求（通过模块）
        public void CancelPendingInstall(Thing module)
        {
            pendingModuleWork.RemoveAll(w => w.moduleToInstall == module);
        }

        // 取消所有核心相关的工作
        public void CancelAllCoreWork()
        {
            pendingModuleWork.Clear();
            Log.Message("[Exosuit] 已取消所有核心工作");
        }

        // 取消卸载请求（通过槽位）- 只取消纯卸载请求
        public void CancelPendingRemove(SlotDef slot)
        {
            pendingModuleWork.RemoveAll(w => w.targetSlot == slot && w.moduleToInstall == null);
        }

        // 完成模块安装（由 JobDriver 调用）
        public void CompleteInstallModule(Thing module)
        {
            if (module == null) return;
            
            // 移除相关的工作请求（包括直接引用和 ThingDef 匹配）
            pendingModuleWork.RemoveAll(w => 
                w.moduleToInstall == module || 
                (w.moduleDefToReinstall != null && w.moduleDefToReinstall == module.def));
            
            if (!module.TryGetComp(out CompSuitModule comp)) return;
            
            Apparel a = MechUtility.Conversion(module) as Apparel;
            if (a == null) return;
            
            DummyApparels.GetDirectlyHeldThings().TryAdd(a, false);
            TryUpdateCache(true);
            
            Log.Message($"[Exosuit] 完成安装模块: {module.LabelCap}");
        }

        // 完成模块卸载（由 JobDriver 调用）
        public void CompleteRemoveModule()
        {
            var work = pendingModuleWork.FirstOrDefault(w => w.needRemoveFirst && !w.removeCompleted);
            if (work == null) return;
            
            // 执行卸载
            RemoveModules(work.targetSlot);
            
            // 标记卸载完成
            work.removeCompleted = true;
            
            // 如果是纯卸载请求（没有要安装的模块），移除这个请求
            if (work.moduleToInstall == null)
            {
                pendingModuleWork.Remove(work);
            }
            
            Log.Message($"[Exosuit] 完成卸载槽位: {work.targetSlot.label}");
        }

        // 检查模块是否在待安装队列中
        public bool IsModulePendingInstall(Thing module)
        {
            return pendingModuleWork.Any(w => w.moduleToInstall == module);
        }

        // 检查槽位是否在待卸载队列中
        public bool IsSlotPendingRemove(SlotDef slot)
        {
            return pendingModuleWork.Any(w => w.targetSlot == slot && w.needRemoveFirst && !w.removeCompleted);
        }
        
        // 检查模块是否在待卸载队列中（用于多槽位模块的显示）
        public bool IsModulePendingRemove(Thing module)
        {
            if (module == null) return false;
            // 遍历所有待卸载工作，检查目标槽位的模块是否与当前模块相同
            foreach (var work in pendingModuleWork)
            {
                if (!work.needRemoveFirst || work.removeCompleted) continue;
                if (work.targetSlot == null) continue;
                // 获取待卸载槽位的模块，检查是否与当前模块是同一个
                if (occupiedSlots.TryGetValue(work.targetSlot, out var targetModule) && targetModule == module)
                {
                    return true;
                }
            }
            return false;
        }

        // 检查槽位是否有待处理的工作（安装或卸载）
        public bool IsSlotPendingWork(SlotDef slot)
        {
            return pendingModuleWork.Any(w => w.targetSlot == slot);
        }

        // 检查是否正在进行核心替换工作
        public bool IsCoreWorkInProgress()
        {
            // 如果有针对核心槽位的待处理工作，说明正在进行核心替换
            return pendingModuleWork.Any(w => w.targetSlot != null && w.targetSlot.isCoreFrame);
        }

        // 检查待安装模块是否有冲突的待卸载槽位
        public bool HasConflictingPendingRemove(Thing module)
        {
            if (module == null) return false;
            
            var work = pendingModuleWork.FirstOrDefault(w => w.moduleToInstall == module);
            if (work == null) return false;
            
            // 检查是否还有未完成的卸载
            return work.needRemoveFirst && !work.removeCompleted;
        }

        // 检查模块是否被任何整备架预留（全局检查）
        public static bool IsModuleReservedByAnyGantry(Thing module, Building_MaintenanceBay excludeBay = null)
        {
            if (module == null || module.Map == null) return false;
            
            foreach (var bay in module.Map.listerBuildings.AllBuildingsColonistOfClass<Building_MaintenanceBay>())
            {
                if (bay == excludeBay) continue;
                if (bay.IsModulePendingInstall(module)) return true;
            }
            return false;
        }

        // 计算安装指定模块后的预计载荷
        // 包括：当前载荷 + 所有待安装模块载荷 - 被替换模块载荷 + 新模块载荷
        public float CalculateProjectedLoad(Thing newModule)
        {
            if (Core == null) return 0f;
            
            float projected = Core.DeadWeight;
            
            // 用于避免重复计算同一个模块
            HashSet<Thing> countedInstalls = [];
            HashSet<Thing> countedRemoves = [];
            
            // 加上所有待安装模块的载荷（排除当前模块，避免重复计算）
            foreach (var work in pendingModuleWork)
            {
                if (work.moduleToInstall != null && work.moduleToInstall != newModule && !countedInstalls.Contains(work.moduleToInstall))
                {
                    projected += work.moduleToInstall.GetStatValue(StatDefOf.Mass);
                    countedInstalls.Add(work.moduleToInstall);
                }
                // 如果是替换操作，减去被替换模块的载荷
                if (work.needRemoveFirst && !work.removeCompleted && occupiedSlots.TryGetValue(work.targetSlot, out var existing))
                {
                    if (!countedRemoves.Contains(existing))
                    {
                        projected -= existing.GetStatValue(StatDefOf.Mass);
                        countedRemoves.Add(existing);
                    }
                }
            }
            
            // 检查新模块是否会替换现有模块
            if (newModule.TryGetComp(out CompSuitModule comp))
            {
                var targetSlot = comp.Props.occupiedSlots?.FirstOrDefault();
                if (targetSlot != null && occupiedSlots.TryGetValue(targetSlot, out var toReplace))
                {
                    // 检查是否已经在待卸载队列中
                    if (!countedRemoves.Contains(toReplace))
                    {
                        projected -= toReplace.GetStatValue(StatDefOf.Mass);
                    }
                }
            }
            
            // 加上新模块的载荷
            projected += newModule.GetStatValue(StatDefOf.Mass);
            
            return projected;
        }

        // 获取待安装模块的净载荷变化（用于 UI 载荷条预览）
        // 返回：待安装模块载荷 - 待卸载模块载荷
        public float GetPendingLoadDelta()
        {
            float delta = 0f;
            
            // 用于避免重复计算同一个模块
            HashSet<Thing> countedInstalls = [];
            HashSet<Thing> countedRemoves = [];
            
            foreach (var work in pendingModuleWork)
            {
                // 加上待安装模块的载荷（避免重复计算占据多槽位的模块）
                if (work.moduleToInstall != null && !countedInstalls.Contains(work.moduleToInstall))
                {
                    delta += work.moduleToInstall.GetStatValue(StatDefOf.Mass);
                    countedInstalls.Add(work.moduleToInstall);
                }
                
                // 减去待卸载模块的载荷（如果还未完成卸载）
                if (work.needRemoveFirst && !work.removeCompleted && occupiedSlots.TryGetValue(work.targetSlot, out var existing))
                {
                    // 避免重复计算占据多槽位的模块
                    if (!countedRemoves.Contains(existing))
                    {
                        delta -= existing.GetStatValue(StatDefOf.Mass);
                        countedRemoves.Add(existing);
                    }
                }
            }
            
            return delta;
        }
        
        // 获取待安装模块带来的载荷上限变化（用于 UI 载荷条预览）
        public float GetPendingCapacityDelta()
        {
            float delta = 0f;
            
            HashSet<Thing> countedInstalls = [];
            HashSet<Thing> countedRemoves = [];
            
            foreach (var work in pendingModuleWork)
            {
                // 加上待安装模块的载荷上限加成
                if (work.moduleToInstall != null && !countedInstalls.Contains(work.moduleToInstall))
                {
                    delta += work.moduleToInstall.def.equippedStatOffsets.GetStatOffsetFromList(StatDefOf.CarryingCapacity);
                    countedInstalls.Add(work.moduleToInstall);
                }
                
                // 减去待卸载模块的载荷上限加成（如果还未完成卸载）
                if (work.needRemoveFirst && !work.removeCompleted && occupiedSlots.TryGetValue(work.targetSlot, out var existing))
                {
                    if (!countedRemoves.Contains(existing))
                    {
                        delta -= existing.def.equippedStatOffsets.GetStatOffsetFromList(StatDefOf.CarryingCapacity);
                        countedRemoves.Add(existing);
                    }
                }
            }
            
            return delta;
        }

        #endregion

        #region 私有方法

        // 清理无效的待处理工作
        private void CleanupInvalidPendingWork()
        {
            pendingModuleWork.RemoveAll(w => 
            {
                if (w.moduleToInstall == null) return false;
                if (!w.moduleToInstall.Destroyed && w.moduleToInstall.Spawned) return false;
                
                // 模块已销毁或未生成，需要清理
                if (w.removeCompleted)
                {
                    // 卸载已完成但新模块丢失，记录警告
                    Log.Warning($"[Exosuit] 待安装模块已丢失，槽位 {w.targetSlot?.label} 的替换工作已取消");
                }
                return true;
            });
        }

        #endregion
    }
}