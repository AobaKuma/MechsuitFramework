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
        
        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t is not Building_MaintenanceBay) return false;

            if (!pawn.CanReserve(t, ignoreOtherReservations: forced))
                return false;

            return JobOnThing(pawn,t,forced)!=null;
        }

        private Job _cacheJob;
        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (_cacheJob!=null && _cacheJob.targetA.Thing==t)
            {
                var _tmp = _cacheJob;
                _cacheJob = null;
                return _tmp;
            }
            _cacheJob=null;
            if (t is not Building_MaintenanceBay bay) return null;
            if (bay.NeedRepair)
            {
                return _cacheJob = JobMaker.MakeJob(JobDefOf.WG_RepairAtGantry, t);
            }
            if (bay.NeedReload)
            {
                var ammos = ReloadableUtility.FindEnoughAmmo(pawn,pawn.Position,bay.GetFirstNeedReload(),false);
                if (!ammos.NullOrEmpty())
                {
                    var reloadJob = JobMaker.MakeJob(JobDefOf.WG_ReloadAtGantry, bay);
                    reloadJob.targetQueueB = ammos.Select(t=>new LocalTargetInfo(t)).ToList();
                    reloadJob.count = Math.Min(ammos.Sum(t => t.stackCount), bay.GetFirstNeedReload().MaxAmmoNeeded(true));
                    return _cacheJob = reloadJob;
                }
                    
            }
            return null;
        }
    }
}
