using UnityEngine;
using Verse;

namespace Exosuit
{
    // 自动维修臂放置时显示朝向指示器
    public class PlaceWorker_AutoRepairArm : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            // 计算目标格子（建筑前方2格）
            IntVec3 targetCell = center + rot.FacingCell * 2;
            
            Map map = Find.CurrentMap;
            if (map == null) return;
            if (!targetCell.InBounds(map)) return;
            
            // 绘制目标格子高亮
            GenDraw.DrawTargetHighlight(targetCell);
            
            // 检查目标位置是否有龙门架
            Building_MaintenanceBay bay = null;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    IntVec3 checkCell = targetCell + new IntVec3(dx, 0, dz);
                    if (!checkCell.InBounds(map)) continue;
                    
                    bay = checkCell.GetFirstThing<Building_MaintenanceBay>(map);
                    if (bay != null) break;
                }
                if (bay != null) break;
            }
            
            // 如果找到龙门架，绘制连接线
            if (bay != null)
            {
                Vector3 armCenter = center.ToVector3ShiftedWithAltitude(def.Altitude);
                GenDraw.DrawLineBetween(armCenter, bay.DrawPos, SimpleColor.Green);
            }
        }
    }
}
