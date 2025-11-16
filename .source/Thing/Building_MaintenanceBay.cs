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
        public bool NeedReload => CanReload && GetFirstNeedReload!=null;


        protected bool autoRepair = true;
        protected bool autoReload = true;
        
        public IReloadableComp GetFirstNeedReload()
        {
            return ReloadableUtility.FindSomeReloadableComponent(Dummy, false);
        }
        public void Repair(int amount = 1)
        {
            foreach (Apparel item in DummyModules)
            {
                var comp = item.GetComp<CompSuitModule>();
                if (comp == null) continue;
                if (comp.HP < comp.MaxHP)
                {
                    comp.HP += amount;
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
}