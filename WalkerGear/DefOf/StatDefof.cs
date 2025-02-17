using RimWorld;
using Verse;

namespace WalkerGear
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
