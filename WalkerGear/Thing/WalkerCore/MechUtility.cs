using RimWorld;
using RimWorld.BaseGen;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using Verse;
using Verse.AI;
using static Unity.Burst.Intrinsics.X86.Avx;

namespace WalkerGear
{
    [StaticConstructorOnStartup]
    public static class MechUtility
    {
        public static List<ThingDef> bayDefs;
        public static List<ThingDef> coreDefs;
        static MechUtility()
        {
            bayDefs = DefDatabase<ThingDef>.AllDefs.Where((ThingDef def) => def.thingClass.IsSubclassOf(typeof(Building_MaintenanceBay)) || def.thingClass == typeof(WalkerGear_Core)).ToList();
            coreDefs = DefDatabase<ThingDef>.AllDefs.Where((ThingDef def) => def.thingClass.IsSubclassOf(typeof(WalkerGear_Core)) || def.thingClass == typeof(WalkerGear_Core)).ToList();
        }

        public static List<Building_MaintenanceBay> GetBaysFromMap(Map map) { return map.listerBuildings.AllBuildingsColonistOfClass<Building_MaintenanceBay>().ToList(); }
        public static Thing GetClosestEmptyBay(Pawn pawn, bool AssignedPriority = true)
        {
            IEnumerable<Building_MaintenanceBay> bays = GetBaysFromMap(pawn.Map);
            if (!bays.Any()) return null;

            if (AssignedPriority)
            {
                var AssignedBays = bays.Where(b => b.TryGetComp<CompAssignableToPawn_Parking>(out var comp) && comp.AssignedPawns.Contains(pawn) && !b.HasGearCore);
                if (AssignedBays.Any())
                {
                    return GenClosest.ClosestThing_Global_Reachable(pawn.PositionHeld, pawn.MapHeld, AssignedBays, PathEndMode.InteractionCell, TraverseParms.For(pawn), 9999f);
                }
            }
            return GenClosest.ClosestThing_Global_Reachable(pawn.PositionHeld, pawn.MapHeld, bays, PathEndMode.InteractionCell, TraverseParms.For(pawn), 9999f, validator: c => !(c as Building_MaintenanceBay).HasGearCore && pawn.CanReserveAndReach(c, PathEndMode.InteractionCell, Danger.Deadly));
        }

        public static Thing GetClosestCoreForPawn(Pawn pawn)
        {
            IEnumerable<Building_MaintenanceBay> bays = GetBaysFromMap(pawn.Map);
            if (!bays.Any()) return null;

            var AssignedBays = bays.Where(b => b.TryGetComp<CompAssignableToPawn_Parking>(out var comp) && comp.AssignedPawns.Contains(pawn) && b.HasGearCore);
            if (!AssignedBays.Any()) return null;

            return GenClosest.ClosestThing_Global_Reachable(pawn.PositionHeld, pawn.MapHeld, bays, PathEndMode.InteractionCell, TraverseParms.For(pawn), 9999f, validator: c => (c as Building_MaintenanceBay).CanGear(pawn) && pawn.CanReserveAndReach(c, PathEndMode.InteractionCell, Danger.Deadly));
        }

        public static void WeaponDropCheck(Pawn pawn)
        {
            if (pawn == null) return;
            if (pawn.equipment.Primary != null && !EquipmentUtility.CanEquip(pawn.equipment.Primary, pawn))
            {
                Messages.Message("WG_WeaponDropped".Translate(pawn.Name.ToString()), pawn, MessageTypeDefOf.NeutralEvent, false);
                if (pawn.Map == null)
                {
                    pawn.equipment.DestroyEquipment(pawn.equipment.Primary);
                }
                else
                {
                    pawn.equipment.TryDropEquipment(pawn.equipment.Primary, out var weapon, pawn.Position, false);
                }
            }
        }
        static MechData mechData = new();
        public static readonly Dictionary<QualityCategory, float> qualityToHPFactor = new() {
            {QualityCategory.Awful, 0.5f},
            {QualityCategory.Poor,0.75f },
            {QualityCategory.Normal,1f},
            {QualityCategory.Good,1.25f},
            {QualityCategory.Excellent,1.5f},
            {QualityCategory.Masterwork,1.75f},
            {QualityCategory.Legendary,2f }
        };
        public static bool PawnWearingWalkerCore(Pawn pawn)
        {
            if (pawn == null) return false;
            if (pawn.NonHumanlikeOrWildMan()) return false;

            if (pawn.apparel.WornApparel.ContainsAny(c => coreDefs.Contains(c.def))) return true;
            return false;
        }
        public static bool GetWalkerCore(this Pawn pawn, out WalkerGear_Core core)
        {
            core = null;
            if (!PawnWearingWalkerCore(pawn)) return false;

            IEnumerable<Apparel> apparel = pawn.apparel?.WornApparel?.Where(c => coreDefs.Contains(c.def));
            if (!apparel.EnumerableNullOrEmpty())
            {
                core = apparel.First() as WalkerGear_Core;
                return true;
            }
            return false;
        }
        public static void WalkerCoreRemove(Pawn pawn)
        {
            for (int i = pawn.apparel.WornApparelCount - 1; i >= 0; i--)
            {
                var a = pawn.apparel.WornApparel[i];
                if (a.HasComp<CompWalkerComponent>())
                {
                    pawn.apparel.Unlock(a);
                    pawn.apparel.Remove(a);
                    pawn.apparel.Notify_ApparelRemoved(a);
                }
            }
        }
        public static List<Apparel> WalkerCoreApparelLists(Pawn pawn)
        {
            List<Apparel> tmpApparelList = new List<Apparel>();
            for (int i = pawn.apparel.WornApparelCount - 1; i >= 0; i--)
            {
                var a = pawn.apparel.WornApparel[i];
                if (a.HasComp<CompWalkerComponent>())
                {
                    tmpApparelList.Add(a);
                }
            }
            WalkerGear_Core core = (WalkerGear_Core)tmpApparelList.Find((a) => a is WalkerGear_Core);
            if (core == null) return null;
            List<float> values = new();

            //Log.Message(core.HealthDamaged);
            if (core.HealthDamaged > 0)
            {
                Rand.SplitRandomly(core.HealthDamaged, tmpApparelList.Count, values);
            }
            List<Apparel> finalList = new List<Apparel>();

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
                finalList.Add(a);
            }
            return finalList;
        }
        /// <summary>
        /// 給屍體或倒地龍騎兵脫下模塊(並機率損壞的)方法
        /// </summary>
        /// <param name="pawnOrCorpse"></param>
        public static void DissambleFrom(Thing pawnOrCorpse)
        {
            if (!pawnOrCorpse.Spawned) return;
            Pawn pawn = null;
            IntVec3 pos = IntVec3.Invalid;
            Map map = null;

            if (pawnOrCorpse is Pawn p)
            {
                pawn = p;
                pos = p.Position;
                map = pawn.Map;
            }
            else if (pawnOrCorpse is Corpse c)
            {
                pawn = c.InnerPawn;
                pos = c.Position;
                map = c.Map;
            }

            //檢查可用性
            if (pawn == null || pos == IntVec3.Invalid || map == null)
            {
                Log.Error("Error on MechUltility, Data is Null");
                return;
            }
            if (!PawnWearingWalkerCore(pawn)) return;

            //生成拆除物。
            var a = MechUtility.WalkerCoreApparelLists(pawn);
            MechUtility.WalkerCoreRemove(pawn);
            foreach (Apparel item in a)
            {
                if (Rand.Chance(0.5f))
                {
                    GenPlace.TryPlaceThing(ThingMaker.MakeThing(RimWorld.ThingDefOf.ChunkSlagSteel), pos, map, ThingPlaceMode.Near);
                }
                else if (Rand.Chance(0.5f))
                {
                    GenSpawn.Refund(GenSpawn.Spawn(item.Conversion(), pos, map, WipeMode.VanishOrMoveAside), map, new CellRect(pos.x, pos.y, 1, 1), false);
                }
                else
                {
                    GenPlace.TryPlaceThing(item.Conversion(), pos, map, ThingPlaceMode.Near);
                }
            }
        }
        public static bool IsModule(this Thing source) => source.TryGetComp(out CompWalkerComponent _t);
        public static bool IsModule(this Thing source, out CompWalkerComponent comp) => source.TryGetComp(out comp);
        //添加的
        public static Thing PeakConverted(this CompWalkerComponent source)
        {
            return source == null ? null : ThingMaker.MakeThing(source.Props.ItemDef, source.parent.Stuff);
        }
        public static Thing Conversion(this Thing source) => source.IsModule(out CompWalkerComponent m) ? m.Conversion() : null;
        public static Thing Conversion(this CompWalkerComponent source)
        {
            if (source == null) return null;
            mechData.Init(source.parent);
            Thing outcome;

            if (source.parent.def.IsApparel)
            {
                Thing item = ThingMaker.MakeThing(source.Props.ItemDef);
                mechData.GetDataFromMech(item);
                outcome = item;
            }
            else
            {
                Thing mech = ThingMaker.MakeThing(source.Props.EquipedThingDef);
                mechData.SetDataToMech(mech);
                outcome = mech;
            }
            source.parent.Destroy();
            return outcome;
        }

        public static void InitFrameDataCache(Pawn pawn)
        {
            float massCapacity = 0;
            float currentLoad = 0;
            pawn.apparel.WornApparel.Find((a) => a is WalkerGear_Core c && c.RefreshHP(true)); //這行很重要，否則模塊血量不會刷新

            List<CompWalkerComponent> li = new List<CompWalkerComponent>();
            foreach (Apparel a in pawn.apparel.WornApparel.Where(t => t.IsModule()))
            {
                var comp = a.GetComp<CompWalkerComponent>();
                massCapacity += a.def.equippedStatOffsets.GetStatOffsetFromList(StatDefOf.CarryingCapacity);
                currentLoad += a.GetStatValue(StatDefOf.Mass);

                li.Add(comp);
            }
            if (pawn.GetWalkerCore(out var core))
            {
                core.ResetStats(massCapacity, massCapacity, currentLoad);
            }
        }


        public static void TryMakeJob_GearOn(Pawn pawn)
        {
            Thing bay = GetClosestCoreForPawn(pawn);
            if (bay != null)
            {
                pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.WG_GetInWalkerCore_NonDrafted, bay), JobTag.ChangingApparel);
            }
        }
        public static void TryMakeJob_GearOff(Pawn pawn)
        {
            Thing bay = GetClosestEmptyBay(pawn);
            if (bay != null)
            {
                pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.WG_GetOffWalkerCore, bay), JobTag.ChangingApparel);
            }
        }
    }

    public class MechData
    {
        private int remainingCharges;
        private QualityCategory quality;
        private Color color;
        private int hp;
        public MechData()
        {

        }

        public void Init(Thing thing)
        {
            quality =default;
            color = default;
            remainingCharges = default;
            hp = default;

            thing.TryGetQuality(out quality);
            if (thing.TryGetComp(out CompColorable colorable)) color = colorable.Color;
            if (thing.TryGetComp(out CompWalkerComponent comp))
            {
                hp = comp.HP;
                if (comp.hasReloadableProps)
                {
                    remainingCharges = comp.remainingCharges;
                }
                else if (thing.TryGetComp<CompApparelReloadable>(out var reloadable))
                {
                    remainingCharges = reloadable.RemainingCharges;
                }
                if (remainingCharges < 0) remainingCharges = 0;
            }
        }
        public void GetDataFromMech( Thing item) {
            if (item.TryGetComp<CompQuality>(out CompQuality compQuality)) compQuality.SetQuality(quality, null);
            item.SetColor(color);
            if (item.TryGetComp<CompWalkerComponent>(out var comp))
            {
                comp.remainingCharges = remainingCharges;
                comp.HP = Mathf.FloorToInt((hp / MechUtility.qualityToHPFactor[quality]));
            }
        }
        public void SetDataToMech( Thing mech) {
            
            if (mech.TryGetComp<CompQuality>(out CompQuality compQuality)) compQuality.SetQuality(quality, null);

            mech.SetColor(color);

            if (mech.TryGetComp<CompApparelReloadable>(out var comp))
            {
                comp.remainingCharges = remainingCharges;
            }
            if (mech.TryGetComp<CompWalkerComponent>(out var c))
            {

                c.HP = Mathf.FloorToInt(hp * MechUtility.qualityToHPFactor[quality]);
            }
        }
    }
}
