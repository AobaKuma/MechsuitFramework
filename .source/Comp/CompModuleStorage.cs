using RimWorld;
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
                if (thing is ThingWithComps twc && twc.TryGetComp(out CompSuitModule c) && c.NeedMaintenance && !thing.Map.reservationManager.IsReserved(thing))
                {
                    maintanenceTar = thing;
                    return true;
                }
            }
            return false;
        }
    }
}
