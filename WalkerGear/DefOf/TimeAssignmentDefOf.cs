using RimWorld;
using Verse;

namespace WalkerGear
{
    [DefOf, StaticConstructorOnStartup]
    public static class WG_TimeAssignmentDefOf
    {
        static WG_TimeAssignmentDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(WG_TimeAssignmentDefOf));
        }
        public static TimeAssignmentDef WG_WorkWithFrame;
    }
}
