using RimWorld;
using Verse;

namespace Exosuit
{
    [DefOf, StaticConstructorOnStartup]
    public static class StatDefof
    {
        static StatDefof()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(StatDefof));
        }
        public static StatDef MF_Stat_Slot;
        public static StatDef MF_FlightRange;
    }
}
