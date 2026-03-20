using RimWorld;
using Verse;

namespace Exosuit
{
    [DefOf, StaticConstructorOnStartup]
    public static class WG_StatCategoryDefOf
    {
        static WG_StatCategoryDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(WG_StatCategoryDefOf));
        }
        public static StatCategoryDef MF_ModuleStats;
    }
}