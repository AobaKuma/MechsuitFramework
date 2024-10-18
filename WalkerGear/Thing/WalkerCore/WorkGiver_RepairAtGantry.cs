using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace WalkerGear
{
    public class WorkGiver_RepairAtGantry : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);

        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public virtual JobDef Job => JobDefOf.WG_RepairAtGantry;

        public virtual bool CanRepair(Thing t)
        {
            return t is Building_MaintenanceBay bay && bay.CanRepair;
        }
        public virtual bool NeedRepair(Thing t)
        {
            return t is Building_MaintenanceBay bay && bay.NeedRepair;
        }
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
            return pawn.Map.listerBuildings.AllBuildingsColonistOfClass<Building_MaintenanceBay>().EnumerableNullOrEmpty();
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!CanRepair(t))
            {
                return false;
            }
            if (!pawn.CanReserve(t, 2, -1, null, forced))
            {
                return false;
            }
            if (!NeedRepair(t))
            {
                return false;
            }
            if (!pawn.CanReach(t, PathEndMode, MaxPathDanger(pawn)))
            {
                return false;
            }
            return true;
        }
        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(JobDefOf.WG_RepairAtGantry, t);
        }
    }
}
