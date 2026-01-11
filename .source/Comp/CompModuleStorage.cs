using RimWorld;
using RimWorld.Utility;
using Verse;

namespace Exosuit
{
    public class CompModuleStorage : ThingComp
    {
        public Building_Storage Parent => (Building_Storage)this.parent;
        public Thing maintanenceTar;
        public bool CheckMaintenance()
        {
            maintanenceTar = null;
            foreach (Thing thing in Parent.slotGroup.HeldThings)
            {
                if (thing.Map.reservationManager.IsReserved(thing)) continue;
                
                if (thing is not ThingWithComps twc) continue;
                
                // 检查 CompSuitModule 的维护需求
                if (twc.TryGetComp(out CompSuitModule c) && c.NeedMaintenance)
                {
                    maintanenceTar = thing;
                    return true;
                }
                
                // 检查 CompFuelCell 的装填需求（实现了 IReloadableComp）
                if (twc.TryGetComp(out CompFuelCell fc) && fc.NeedsReload(true))
                {
                    maintanenceTar = thing;
                    return true;
                }
            }
            return false;
        }
    }
}
