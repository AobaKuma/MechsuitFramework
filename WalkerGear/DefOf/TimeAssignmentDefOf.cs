using RimWorld;

namespace WalkerGear
{
    [DefOf]
    public static class TimeAssignmentDefOf
    {
        static TimeAssignmentDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(TimeAssignmentDefOf));
        }
        public static TimeAssignmentDef WG_WorkWithFrame;
    }
}
