using UnityEngine;
using Verse;

namespace Exosuit
{
    // 自动维修臂放置验证
    // 必须贴着龙门架放置，前方一格是龙门架边缘中间
    public class PlaceWorker_AutoRepairArm : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            Map map = Find.CurrentMap;
            if (map == null) return;
            
            // 维修臂前方一格
            IntVec3 frontCell = center + rot.FacingCell;
            if (!frontCell.InBounds(map)) return;
            
            GenDraw.DrawTargetHighlight(frontCell);
            
            var bay = frontCell.GetFirstThing<Building_MaintenanceBay>(map);
            if (bay != null)
            {
                Vector3 armCenter = center.ToVector3ShiftedWithAltitude(def.Altitude);
                GenDraw.DrawLineBetween(armCenter, bay.DrawPos, SimpleColor.Green);
            }
        }
        
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            // 维修臂前方一格必须是龙门架
            IntVec3 frontCell = loc + rot.FacingCell;
            
            if (!frontCell.InBounds(map))
                return "WG_AutoRepair_NoTarget".Translate();
            
            var bay = frontCell.GetFirstThing<Building_MaintenanceBay>(map);
            if (bay == null)
                return "WG_AutoRepair_NoTarget".Translate();
            
            // 检查是否对准龙门架边缘中间
            IntVec3 bayCenter = bay.Position;
            bool aligned = (rot == Rot4.North || rot == Rot4.South) 
                ? loc.x == bayCenter.x 
                : loc.z == bayCenter.z;
            
            if (!aligned)
                return "WG_AutoRepair_NoTarget".Translate();
            
            // 检查该方向是否已有维修臂
            foreach (var t in map.listerThings.ThingsOfDef(ThingDef.Named("MF_Building_AutoRepairArm")))
            {
                if (t == thingToIgnore) continue;
                IntVec3 otherFront = t.Position + t.Rotation.FacingCell;
                if (otherFront.GetFirstThing<Building_MaintenanceBay>(map) == bay && t.Rotation == rot)
                    return "WG_AutoRepair_SideOccupied".Translate();
            }
            
            return true;
        }
    }
}
