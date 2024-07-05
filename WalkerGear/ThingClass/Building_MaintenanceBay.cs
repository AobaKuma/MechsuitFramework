using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;


namespace WalkerGear
{

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

            if (HasGearCore&&false)
            {
                Command_Target command_GetIn = new()
                {
                    defaultLabel = "Get In".Translate(),
                    targetingParams = TargetingParameters.ForPawns(),
                    action = (tar) =>
                    {
                        if (tar.Pawn == null || !CanAcceptPawn(tar.Pawn)) return;
                        tar.Pawn.jobs.StartJob(JobMaker.MakeJob(JobDefOf.WG_GetInWalkerCore, this));
                    }
                };
                yield return command_GetIn;
            }
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref cachePawn, "cachedPawn");
        }
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            foreach(Apparel a in ModuleStorage)
            {
                GenPlace.TryPlaceThing(MechUtility.Conversion(a),Position,Map,ThingPlaceMode.Direct);
            }
            Dummy.Destroy();
            base.Destroy(mode);
        }
        public void RemoveModule(Thing t)
        {
            if (cachePawn == null) return;
            if (DummyApparels.Contains(t))
            {
                GenPlace.TryPlaceThing(MechUtility.Conversion(t), Position, Map, ThingPlaceMode.Direct);
            }
        }
        public void Add(Thing t)
        {
            if (cachePawn == null) return;
            Apparel a = MechUtility.Conversion(t) as Apparel;
            DummyApparels.Wear(a);
        }
    }
    //给Itab提供的功能
    public partial class Building_MaintenanceBay
    {
        public Rot4 direction = Rot4.South;//缓存Itab里pawn的方向
        public bool HasGearCore => GetGearCore !=null;

        public Apparel GetGearCore => DummyApparels.WornApparel.Find(a=>a is WalkerGear_Core);
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
            if (pawn.apparel.WornApparel.Any((a) => a is WalkerGear_Core)) return "AlreadyHasArmor".Translate().CapitalizeFirst();
            if (!HasGearCore) return "NoArmor".Translate().CapitalizeFirst();
            return true;
        }
        public void GearUp(Pawn pawn)
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
            pawn.apparel.WornApparel.Find((a) => a is WalkerGear_Core c && c.RefreshHP(true));
        }
        public void GearDown(Pawn pawn)
        {
            if (cachePawn == null) return;
            if (HasGearCore) return;
            tmpApparelList.Clear();

            for (int i = pawn.apparel.WornApparelCount - 1; i >= 0; i--)
            {
                var a = pawn.apparel.WornApparel[i];
                if (a.HasComp<CompWalkerComponent>())
                {
                    tmpApparelList.Add(a);
                    pawn.apparel.Unlock(a);
                    pawn.apparel.Remove(a);
                }
            }
            WalkerGear_Core core = (WalkerGear_Core)tmpApparelList.Find((a) => a is WalkerGear_Core);
            if (core == null) return;
            List<float> values = new();
            //Log.Message(core.HealthDamaged);
            if (core.HealthDamaged>0)
            {
                Rand.SplitRandomly(core.HealthDamaged, tmpApparelList.Count, values);
            }
            

            for (int j = 0; j < tmpApparelList.Count; j++)
            {
                var a = tmpApparelList[j];
                var c = a.TryGetComp<CompWalkerComponent>();
                if (!values.Empty())
                {
                    if (values[j] >= c.HP)
                    {
                        if (j < tmpApparelList.Count - 1)
                        {
                            values[j + 1] += values[j] - c.HP;
                        }
                        c.HP = 1;
                    }
                    else
                    {
                        c.HP -= Mathf.FloorToInt(values[j]);
                    }
                }
                
                DummyApparels.Wear(a);
            }
            ITab_MechGear.needUpdateCache=true;
            tmpApparelList.Clear();
        }

        static readonly List<Apparel> tmpApparelList = new();
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
        public Pawn Dummy{
            get
            {
                if (cachePawn==null)
                {
                    cachePawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist);
                    pawnsInBuilding.Add(cachePawn);
                    cachePawn.apparel.DestroyAll();
                    cachePawn.rotationInt = Rotation.Opposite;
                    //cachePawn.apparel.Wear((Apparel)ThingMaker.MakeThing(ThingDefOf.Apparel_Dummy));
                    cachePawn.drafter = new(cachePawn)
                    {
                        Drafted = true
                    };
                   
                }
                return cachePawn;
            }
        }
        public Pawn_ApparelTracker DummyApparels => Dummy?.apparel;
        public List<Apparel> ModuleStorage {
            get
            {
                List<Apparel> tmp = new();
                foreach(Apparel a in DummyApparels.WornApparel)
                {
                    if (a.HasComp<CompWalkerComponent>())
                        tmp.Add(a);
                }
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
