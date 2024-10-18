using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Steamworks;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using Verse;
using Verse.AI;
using Verse.Noise;



namespace WalkerGear
{
    public partial class Building_MaintenanceBay : Building
    {
        protected CompAffectedByFacilities cacheByFacilities = null;
        public CompAffectedByFacilities compAffectedBy
        {
            get
            {
                if (cacheByFacilities == null && this.TryGetComp<CompAffectedByFacilities>(out cacheByFacilities))
                {
                    return cacheByFacilities;
                }
                return cacheByFacilities;
            }
        }
        public bool CanRepair => this.Faction.IsPlayer && this.GetInspectTabs().Where(tab=>tab is ITab_MechGear).Any() && autoRepair;//臨時停機點不能修。
        public bool NeedRepair //只要有一個需要修，那就能修。
        {
            get
            {
                if (!(Spawned && this.CanRepair && HasGearCore)) return false;
                if (ModuleStorage.NullOrEmpty()) return false;
                foreach (Apparel item in ModuleStorage)
                {
                    var comp = item.GetComp<CompWalkerComponent>();
                    if (comp == null) continue;
                    if (comp.HP < comp.MaxHP)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        protected bool autoRepair = true;

        internal void Repair()
        {
            foreach (Apparel item in ModuleStorage)
            {
                var comp = item.GetComp<CompWalkerComponent>();
                if (comp == null) continue;
                if (comp.HP < comp.MaxHP)
                {
                    comp.HP++;
                    return;
                }
            }
        }
    }
    [StaticConstructorOnStartup]
    public partial class Building_MaintenanceBay : Building
    {
        //cached stuffs

        [Unsaved(false)]
        private CompPowerTrader cachedPowerComp;
        //Properties
        private CompPowerTrader PowerTraderComp
        {
            get
            {
                return cachedPowerComp ??= this.TryGetComp<CompPowerTrader>();
            }
        }
        public bool PowerOn => PowerTraderComp.PowerOn;

        //methods override
        public override IEnumerable<Gizmo> GetGizmos()
        {

            if (HasGearCore && Faction.IsPlayer)
            {
                Command_Target command_GetIn = new()
                {
                    defaultLabel = "WG_GetIn".Translate(),
                    targetingParams = TargetingParameters.ForPawns(),
                    action = (tar) =>
                    {
                        if (tar.Pawn == null || !CanAcceptPawn(tar.Pawn) || tar.Pawn.Downed) return;
                        tar.Pawn.jobs.StartJob(JobMaker.MakeJob(JobDefOf.WG_GetInWalkerCore, this));
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
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
        }
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            foreach (Apparel a in ModuleStorage)
            {
                GenPlace.TryPlaceThing(MechUtility.Conversion(a), Position, Map, ThingPlaceMode.Direct);
            }
            Dummy.Destroy();
            base.Destroy(mode);
        }
        public void RemoveModule(Thing t)
        {
            if (t == null) return;
            if (cachePawn == null) return;
            if (DummyApparels.Contains(t))
            {
                if (!this.TryGetComp(out CompAffectedByFacilities abf))
                {
                    Log.Warning("CompAffectedByFacilities is null");
                    return;
                }
                Thing moduleItem = MechUtility.Conversion(t);
                foreach (Thing b in abf.LinkedFacilitiesListForReading)
                {
                    if (b is not Building_Storage s) continue;
                    if (!s.Accepts(moduleItem)) continue;
                    foreach (IntVec3 cell in GenAdj.CellsOccupiedBy(s))
                    {
                        List<Thing> cellThings = cell.GetThingList(s.Map);
                        if (cellThings.Where(thing => thing != s).EnumerableNullOrEmpty())
                        {
                            GenPlace.TryPlaceThing(moduleItem, cell, Map, ThingPlaceMode.Direct);
                            return;
                        }
                    }
                }
                GenPlace.TryPlaceThing(moduleItem, InteractionCell, Map, ThingPlaceMode.Direct);
            }
        }
        public void Add(Thing t)
        {
            if (cachePawn == null) return;
            Apparel a = MechUtility.Conversion(t) as Apparel;
            foreach (SlotDef b in a.GetComp<CompWalkerComponent>().Slots)
            {
                UnloadAtSlot(b.uiPriority);
            }
            DummyApparels.Wear(a);
        }
        public void UnloadAtSlot(int ui)
        {
            if (!positionWSlot.ContainsKey(ui)) return;
            if (!occupiedSlots.ContainsKey(positionWSlot[ui])) return;

            RemoveModule(occupiedSlots[positionWSlot[ui]]);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref cachePawn, "cachedPawn");
            Scribe_Values.Look(ref autoRepair, "autoRepair");
            SetItabCacheDirty();
        }
    }
    //给Itab提供的功能
    public partial class Building_MaintenanceBay
    {
        private bool isOccupiedSlotDirty = true;

        private float massCapacity;
        private float currentLoad;

        private readonly List<SlotDef> toRemove = new();

        public readonly Dictionary<SlotDef, Thing> occupiedSlots = new();
        public readonly Dictionary<int, SlotDef> positionWSlot = new(7);

        public float MassCapacity => massCapacity;
        public float CurrentLoad => currentLoad;

        public Rot4 direction = Rot4.South;//缓存Itab里pawn的方向
        public bool HasGearCore => GetGearCore != null;

        public Apparel GetGearCore => DummyApparels?.WornApparel?.Find(a => a is WalkerGear_Core);

        public List<CompWalkerComponent> GetwalkerComponents()
        {
            if (ComponentsCache == null)
            {
                TryUpdateOccupiedSlotsCache(true);
            }
            return ComponentsCache;
        }
        private List<CompWalkerComponent> ComponentsCache;
        public void TryUpdateOccupiedSlotsCache(bool force = false)
        {
            if (!force && !isOccupiedSlotDirty)
            {
                return;
            }
            occupiedSlots.Clear();
            positionWSlot.Clear();
            massCapacity = 0;
            currentLoad = 0;
            List<CompWalkerComponent> li = new List<CompWalkerComponent>();
            foreach (Apparel a in DummyApparels?.WornApparel?.Where(t => t.IsModule()))
            {
                CompWalkerComponent comp = a.GetComp<CompWalkerComponent>();
                massCapacity += a.def.equippedStatOffsets.GetStatOffsetFromList(StatDefOf.CarryingCapacity);
                currentLoad += a.GetStatValue(StatDefOf.Mass);

                foreach (SlotDef s in comp.Props.slots)
                {
                    occupiedSlots[s] = a;
                    positionWSlot[s.uiPriority] = s;
                }
                li.Add(comp);
            }

            if (Dummy.GetWalkerCore(out WalkerGear_Core core)) core.RefreshHP(true);
            ComponentsCache = li;
            isOccupiedSlotDirty = false;
        }
        public void RemoveModules(SlotDef slot, bool updateNow = true)
        {
            if (!occupiedSlots.ContainsKey(slot)) return;
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
                TryUpdateOccupiedSlotsCache(true);
            }
        }

        public void AddOrReplaceModule(Thing thing)
        {
            if (!thing.TryGetComp(out CompWalkerComponent comp)) return;
            if (comp.Props.slots.Where(slot => slot.isCoreFrame).Any() && GetGearCore != null)
            {
                //如果這個要裝上的模塊是CoreFrame，清空所有。
                ClearAllModules();
            }
            Add(thing);
            TryUpdateOccupiedSlotsCache(true);
        }
        public void ClearAllModules()
        {
            var comp = GetGearCore.GetComp<CompWalkerComponent>();
            RemoveModules(GetGearCore.GetComp<CompWalkerComponent>().Props.slots.Where(l => l.isCoreFrame).First());
            foreach (var slot in comp.Props.slots)
            {
                foreach (var s in slot.supportedSlots)
                {
                    RemoveModules(s, true);
                }
            }
        }

        public IEnumerable<Thing> GetAvailableModules(SlotDef slotDef, bool IsCore = false)
        {
            if (!this.TryGetComp(out CompAffectedByFacilities abf))
            {
                Log.Warning("CompAffectedByFacilities is null");
                yield break;
            }
            foreach (Thing b in abf.LinkedFacilitiesListForReading)
            {
                if (b is not Building_Storage s) continue;
                if (s.GetSlotGroup().HeldThings.EnumerableNullOrEmpty()) continue;

                foreach (Thing module in s.GetSlotGroup().HeldThings)
                {
                    if (!module.TryGetComp(out CompWalkerComponent c)) continue;
                    if (IsCore)
                    {
                        if (!c.Props.slots.Where(s => s.isCoreFrame).Any()) continue;
                    }
                    else
                    {
                        if (!c.Props.slots.Contains(slotDef)) continue;
                    }
                    //if (DebugSettings.godMode) Log.Message(module.def.defName + " is walker module of " + (slotDef == null ? "any" : slotDef.defName) + " added to list.");
                    yield return module;
                }
            }
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
        private void SetItabCacheDirty()
        {
            isOccupiedSlotDirty = true;
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
            AcceptanceReport acceptanceReport = CanAcceptPawn(selPawn);
            if (acceptanceReport.Accepted)
            {
                yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("EnterBuilding".Translate(this), delegate
                {
                    selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.WG_GetInWalkerCore, this), JobTag.DraftedOrder);
                }), selPawn, this);
            }
            else if (selPawn.CanReach(this, PathEndMode.Touch, Danger.Deadly))
            {
                if (!HasGearCore && selPawn.apparel.WornApparel.Any((a) => a is WalkerGear_Core))
                {
                    yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("EnterBuilding".Translate(this), delegate
                    {
                        selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.WG_GetOffWalkerCore, this), JobTag.DraftedOrder);
                    }), selPawn, this);
                }
            }

            foreach (FloatMenuOption floatMenuOption in base.GetFloatMenuOptions(selPawn))
            {
                yield return floatMenuOption;
            }
        }
        public AcceptanceReport CanReach(Thing thing)
        {
            if (thing == null) return false;
            if (!thing.MapHeld.reachability.CanReach(thing.Position, this.InteractionCell, PathEndMode.OnCell, TraverseMode.ByPawn, Danger.Deadly)) return false;
            return true;
        }
        private AcceptanceReport CanAcceptPawn(Pawn pawn)
        {
            if (!pawn.IsColonist && !pawn.IsSlaveOfColony && !pawn.IsPrisonerOfColony && (!pawn.IsColonyMutant || !pawn.IsGhoul))
            {
                return false;
            }
            if (!pawn.RaceProps.Humanlike || pawn.IsQuestLodger())
            {
                return false;
            }
            if (PowerComp != null && !PowerOn)
            {
                return "NoPower".Translate().CapitalizeFirst();
            }
            if (pawn.apparel.WornApparel.Any((a) => a is WalkerGear_Core)) return "WG_Disabled_AlreadyHasCoreFrame".Translate(pawn.Name.ToString()).CapitalizeFirst();
            if (!HasGearCore) return "WG_Disabled_NoCoreFrame".Translate().CapitalizeFirst();
            return true;
        }
        public bool CanGear(Pawn pawn)
        {
            foreach (Apparel a in ModuleStorage)
            {
                foreach (var item in pawn.apparel.WornApparel)
                {
                    if (!ApparelUtility.CanWearTogether(item.def, a.def, pawn.RaceProps.body) && pawn.apparel.IsLocked(item))
                        return false;
                }
            }
            return true;
        }
        public virtual void GearUp(Pawn pawn)
        {
            if (cachePawn == null || !HasGearCore) return;
            foreach (Apparel a in ModuleStorage)
            {
                foreach (var item in pawn.apparel.WornApparel)
                {
                    if (!ApparelUtility.CanWearTogether(item.def, a.def, pawn.RaceProps.body) && pawn.apparel.IsLocked(item))
                        return;
                }
            }
            for (int i = ModuleStorage.Count - 1; i >= 0; i--)
            {
                Apparel a = ModuleStorage[i];
                DummyApparels.Remove(a);
                pawn.apparel.Wear(a, true, locked: true);
            }
            MechUtility.InitFrameDataCache(pawn); //負重數值更新
            SetItabCacheDirty();
        }
        public void GearDown(Pawn pawn)
        {
            if (cachePawn == null) return;
            if (HasGearCore) return;
            List<Apparel> _temp = MechUtility.WalkerCoreApparelLists(pawn);
            MechUtility.WalkerCoreRemove(pawn);
            foreach (Apparel a in _temp)
            {
                DummyApparels.Wear(a, false, true);
            }
            SetItabCacheDirty();
            MechUtility.WeaponDropCheck(pawn);
        }
    }
    //渲染小人的
    public partial class Building_MaintenanceBay
    {
        private Pawn cachePawn;

        public static bool PawnInBuilding(Pawn pawn)
        {
            pawnsInBuilding.RemoveAll(p => p == null || p.Destroyed);
            return pawnsInBuilding.Contains(pawn);
        }
        public static List<Pawn> pawnsInBuilding = new();

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (respawningAfterLoad)
            {
                pawnsInBuilding.Add(Dummy);
            }
        }
        public Pawn Dummy
        {
            get
            {
                if (cachePawn == null)
                {
                    cachePawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist);//生成
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
        public Pawn_ApparelTracker DummyApparels => Dummy?.apparel;
        public List<Apparel> ModuleStorage
        {
            get
            {
                List<Apparel> tmp = new();
                tmp.AddRange(DummyApparels.WornApparel.Where(a => a.HasComp<CompWalkerComponent>()));
                return tmp;
            }
        }
        public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
        {
            if (HasGearCore)
            {
                Dummy.Drawer.renderer.DynamicDrawPhaseAt(phase, drawLoc.WithYOffset(1f), null, true);
            }
            base.DynamicDrawPhaseAt(phase, drawLoc, flip);
        }
    }
}