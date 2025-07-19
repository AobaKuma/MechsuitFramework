using RimWorld;
using Verse;

namespace Exosuit
{
    [DefOf, StaticConstructorOnStartup]
    public static class ThingDefOf
	{
		public static ThingDef MF_Building_MaintenanceBay;
		public static ThingDef MF_Building_ComponentStorage;
		public static ThingDef MF_Building_Wreckage;
        public static ThingDef WG_PawnFlyer;

        //新的race，但是没做
        public static ThingDef Dummy;
    }
}