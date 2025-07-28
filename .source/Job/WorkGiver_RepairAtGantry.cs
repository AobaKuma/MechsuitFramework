using RimWorld;
using RimWorld.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Exosuit
{
    public class WorkGiver_RepairAtGantry : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);

        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override Danger MaxPathDanger(Pawn pawn)
        {
            return Danger.Deadly;
        }
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return pawn.Map.listerBuildings.AllBuildingsColonistOfClass<Building_MaintenanceBay>();
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            var bays = pawn.Map.listerBuildings.AllBuildingsColonistOfClass<Building_MaintenanceBay>();
            if (bays.EnumerableNullOrEmpty()) return true;            
            return !bays.Any(bay=>(bay.CanReload||bay.CanRepair)&&bay.HasGearCore);
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t is not Building_MaintenanceBay bay)return false;
            if (!pawn.CanReserveAndReach(t, PathEndMode, MaxPathDanger(pawn), 2, ignoreOtherReservations: forced))
                return false;
            
            if (bay.NeedRepair)
            {
                return true;
            }
            if (bay.NeedReload)
            {
                var reloadableComp = bay.GetFirstNeedReload();
                if (pawn.carryTracker.AvailableStackSpace(reloadableComp.AmmoDef) < reloadableComp.MinAmmoNeeded(true))
                {
                    return false;
                }
                if (ReloadableUtility.FindEnoughAmmo(pawn, pawn.Position, reloadableComp, false).NullOrEmpty())
                {
                    return false;
                }
                return true;
            }
            return false;
        }
        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t is not Building_MaintenanceBay bay) return null;
            if (bay.NeedRepair)
            {
                return JobMaker.MakeJob(JobDefOf.WG_RepairAtGantry, t);
            }
            if (bay.NeedReload)
            {
                var ammos = ReloadableUtility.FindEnoughAmmo(pawn,pawn.Position,bay.GetFirstNeedReload(),false);
                if (!ammos.NullOrEmpty())
                {
                    var reloadJob = JobMaker.MakeJob(JobDefOf.WG_ReloadAtGantry, bay);
                    reloadJob.targetQueueB = ammos.Select(t=>new LocalTargetInfo(t)).ToList();
                    reloadJob.count = Math.Min(ammos.Sum(t => t.stackCount), bay.GetFirstNeedReload().MaxAmmoNeeded(true));
                    return reloadJob;
                }
                    
            }
            return null;
        }
    }
}
