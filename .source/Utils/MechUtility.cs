// 当白昼倾坠之时
using RimWorld;
using RimWorld.BaseGen;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst.Intrinsics;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Exosuit
{
    [StaticConstructorOnStartup]
    public static class MechUtility
    {
        public static bool HasCore(this List<Apparel> things)
        {
            return HasCore(things, out _);
        }
        public static bool HasCore(this Pawn pawn)
        {
            return HasCore(pawn, out _);
        }
        public static bool HasCore(this Pawn pawn, out Exosuit_Core core)
        {
            Apparel p = pawn?.apparel?.WornApparel?.FirstOrDefault(a => a is Exosuit_Core c);
            core = p as Exosuit_Core;
            return core != null;
        }
        public static bool HasCore(this List<Apparel> things, out Exosuit_Core core)
        {
            if (things?.Find(a => a is Exosuit_Core) is Exosuit_Core core1 and not null)
            {
                core = core1;
                return true;
            }
            core = null;
            return false;
        }

        public static List<Building_MaintenanceBay> GetMapBays(this Map map) => [.. map.listerBuildings.AllBuildingsColonistOfClass<Building_MaintenanceBay>()];
        public static Thing GetClosestBay(Pawn pawn, bool AssignedPriority = true)
        {
            IEnumerable<Building_MaintenanceBay> bays = pawn.Map.GetMapBays();
            if (!bays.Any()) return null;

            if (AssignedPriority)
            {
                var AssignedBays = bays.Where(b => b.TryGetComp<CompAssignableToPawn_Parking>(out var comp) && comp.AssignedPawns.Contains(pawn) && !b.HasGearCore);
                if (AssignedBays.Any())
                {
                    return GenClosest.ClosestThing_Global_Reachable(pawn.PositionHeld, pawn.MapHeld, AssignedBays, PathEndMode.InteractionCell, TraverseParms.For(pawn), 9999f);
                }

            }
            return GenClosest.ClosestThing_Global_Reachable(pawn.PositionHeld, pawn.MapHeld, bays, PathEndMode.InteractionCell, TraverseParms.For(pawn), 9999f, validator: c => c is Building_MaintenanceBay bay && !bay.HasGearCore && bay.TryGetComp<CompAssignableToPawn_Parking>(out var park) && park.AssignedPawns.EnumerableNullOrEmpty() && pawn.CanReserveAndReach(c, PathEndMode.InteractionCell, Danger.Deadly));

        }
        public static bool IsNotAnything(Pawn pawn)
        {
            return pawn.GetTimeAssignment() != WG_TimeAssignmentDefOf.Anything;
        }
        public static bool IsWorkWithFrame(Pawn pawn)
        {
            return pawn.GetTimeAssignment() == WG_TimeAssignmentDefOf.WG_WorkWithFrame;
        }
        public static Thing GetClosestCoreForPawn(Pawn pawn)
        {
            IEnumerable<Building_MaintenanceBay> bays = GetMapBays(pawn.Map);
            if (!bays.Any()) return null;

            var AssignedBays = bays.Where(b => b.TryGetComp<CompAssignableToPawn_Parking>(out var comp) && comp.AssignedPawns.Contains(pawn) && b.HasGearCore);
            if (!AssignedBays.Any()) return null;

            return GenClosest.ClosestThing_Global_Reachable(pawn.PositionHeld, pawn.MapHeld, AssignedBays, PathEndMode.InteractionCell, TraverseParms.For(pawn), 9999f, validator: c => (c as Building_MaintenanceBay).CanGear(pawn, out _) && pawn.CanReserveAndReach(c, PathEndMode.InteractionCell, Danger.Deadly));
        }

        public static void WeaponDropCheck(Pawn pawn)
        {
            if (pawn == null) return;
            if (pawn.equipment.Primary != null && !EquipmentUtility.CanEquip(pawn.equipment.Primary, pawn))
            {
                if (pawn.Faction?.IsPlayer == true) Messages.Message("WG_WeaponDropped".Translate(pawn.Name.ToString()), pawn, MessageTypeDefOf.NeutralEvent, false);
                if (pawn.Map == null)
                {
                    pawn.equipment.DestroyEquipment(pawn.equipment.Primary);
                }
                else
                {
                    pawn.equipment.TryDropEquipment(pawn.equipment.Primary, out _, pawn.Position, false);
                }
            }
        }
        static readonly MechData mechData = new();
        public static readonly Dictionary<QualityCategory, float> qualityToHPFactor = new() {
            {QualityCategory.Awful, 0.7f},
            {QualityCategory.Poor,0.9f },
            {QualityCategory.Normal,1f},
            {QualityCategory.Good,1.1f},
            {QualityCategory.Excellent,1.3f},
            {QualityCategory.Masterwork,1.6f},
            {QualityCategory.Legendary,2f }
        };
        public static bool PawnWearingExosuitCore(this Pawn pawn)
        {
            return pawn.apparel?.WornApparel.HasCore() ?? false;
        }
        public static bool TryGetExosuitCore(this Pawn pawn, out Exosuit_Core core)
        {
            core = null;
            if (!PawnWearingExosuitCore(pawn)) return false;
            if (pawn.apparel.WornApparel.HasCore(out core))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 移除装甲，返回移除衣物的列表
        /// </summary>
        /// <param name="pawn"></param>
        /// <returns></returns>
        public static List<Apparel> RemoveExosuit(this Pawn pawn)
        {
            void Remove(Apparel a)
            {
                pawn.apparel.Unlock(a);
                pawn.apparel.Remove(a);

                a.Notify_Unequipped(pawn);
                pawn.apparel.Notify_ApparelRemoved(a);
            }
            List<Apparel> suitModules =  [..from a in pawn.apparel.WornApparel
                where a.HasComp<CompSuitModule>()
                select a];
            suitModules.ForEach(Remove);
            return suitModules;
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
            if (!PawnWearingExosuitCore(pawn)) return;

            //生成拆除物。

            foreach (Apparel a in RemoveExosuit(pawn))
            {
                if (Rand.Chance(0.5f))
                {
                    GenPlace.TryPlaceThing(ThingMaker.MakeThing(RimWorld.ThingDefOf.ChunkSlagSteel), pos, map, ThingPlaceMode.Near);
                }
                else if (Rand.Chance(0.5f))
                {
                    GenSpawn.Refund(GenSpawn.Spawn(a.Conversion(), pos, map, WipeMode.VanishOrMoveAside), map, new CellRect(pos.x, pos.y, 1, 1), false);
                }
                else
                {
                    GenPlace.TryPlaceThing(a.Conversion(), pos, map, ThingPlaceMode.Near);
                }
            }
        }
        public static bool IsModule(this Thing source) => source.TryGetComp<CompSuitModule>() != null;
        public static bool IsModule(this Thing source, out CompSuitModule comp) => source.TryGetComp(out comp);
        //添加的
        public static Thing PeakConverted(this CompSuitModule source)
        {
            return source == null ? null : ThingMaker.MakeThing(source.Props.ItemDef, source.parent.Stuff);
        }
        public static Thing Conversion(this Thing source) => source.IsModule(out CompSuitModule m) ? m.Conversion() : null;
        public static Thing Conversion(this CompSuitModule source)
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


        public static void TryMakeJob_GearOn(Pawn pawn)
        {
            Thing bay = GetClosestCoreForPawn(pawn);
            if (bay != null)
            {
                pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.WG_GetInWalkerCore_NonDrafted, bay), JobTag.Misc);
            }
        }
        public static void TryMakeJob_GearOff(Pawn pawn)
        {
            Thing bay = GetClosestBay(pawn);
            if (bay != null)
            {
                pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.WG_GetOffWalkerCore, bay), tag: JobTag.Misc);
            }
        }
    }
    public class MechData
    {
        #region 静态回调注册
        
        // 外部模块可以注册的回调，用于在模块转换时保存/恢复自定义数据
        public static event System.Action<Thing> OnInitData;
        public static event System.Action<Thing> OnRestoreToItem;
        public static event System.Action<Thing> OnRestoreToMech;
        
        #endregion
        
        private int remainingCharges;
        private QualityCategory quality;
        private Color color;
        private int hp;
        private float fuelAmount;
        
        // 存储实现了 IModuleDataTransfer 接口的组件数据
        private List<IModuleDataTransfer> dataTransfers = new();
        
        public MechData()
        {

        }
        public void Init(Thing thing)
        {
            quality = default;
            color = default;
            remainingCharges = default;
            hp = default;
            fuelAmount = default;
            dataTransfers.Clear();

            thing.TryGetQuality(out quality);
            if (thing.TryGetComp(out CompColorable colorable)) color = colorable.Active ? colorable.Color : Color.clear;
            if (thing.TryGetComp(out CompSuitModule comp))
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
            // 燃料电池
            if (thing.TryGetComp<CompFuelCell>(out var fuelCell))
            {
                fuelAmount = fuelCell.Fuel;
            }
            
            // 收集所有实现 IModuleDataTransfer 的组件并保存数据
            if (thing is ThingWithComps twc)
            {
                foreach (var thingComp in twc.AllComps)
                {
                    if (thingComp is IModuleDataTransfer transfer)
                    {
                        transfer.SaveDataFrom(thing);
                        dataTransfers.Add(transfer);
                    }
                }
            }
            
            // 调用外部模块注册的回调
            OnInitData?.Invoke(thing);
        }
        public void GetDataFromMech(Thing item)
        {
            if (item.TryGetComp(out CompQuality compQuality)) compQuality.SetQuality(quality, null);
            if (item.TryGetComp(out CompColorable compColorable)) compColorable.SetColor(color);
            if (item.TryGetComp<CompSuitModule>(out var comp))
            {
                comp.remainingCharges = remainingCharges;
                comp.HP = Mathf.CeilToInt((hp / MechUtility.qualityToHPFactor[quality]));
            }
            // 燃料电池
            if (item.TryGetComp<CompFuelCell>(out var fuelCell))
            {
                fuelCell.Fuel = fuelAmount;
            }
            
            // 恢复 IModuleDataTransfer 组件的数据
            foreach (var transfer in dataTransfers)
            {
                transfer.RestoreDataTo(item);
            }
            
            // 调用外部模块注册的回调
            OnRestoreToItem?.Invoke(item);
        }
        public void SetDataToMech(Thing mech)
        {

            if (mech.TryGetComp(out CompQuality compQuality)) compQuality.SetQuality(quality, null);
            if (mech.TryGetComp(out CompColorable compColorable))
            {
                if (color == Color.clear)
                {
                    compColorable.SetColor(mech.def.colorGenerator?.NewRandomizedColor() ?? Color.white);
                }
                else
                {
                    compColorable.SetColor(color);
                }
            }

            if (mech.TryGetComp<CompApparelReloadable>(out var comp))
            {
                comp.remainingCharges = remainingCharges;
            }
            if (mech.TryGetComp<CompSuitModule>(out var c))
            {
                c.HP = Mathf.CeilToInt(hp * MechUtility.qualityToHPFactor[quality]);
            }
            // 燃料电池
            if (mech.TryGetComp<CompFuelCell>(out var fuelCell))
            {
                fuelCell.Fuel = fuelAmount;
            }
            
            // 恢复 IModuleDataTransfer 组件的数据
            foreach (var transfer in dataTransfers)
            {
                transfer.RestoreDataTo(mech);
            }
            
            // 调用外部模块注册的回调
            OnRestoreToMech?.Invoke(mech);
        }
    }
}