using HarmonyLib;
using RimWorld;
using Verse;

namespace Exosuit
{
    //防止龍騎兵能睡在床上
    [HarmonyPatch(typeof(RestUtility), nameof(RestUtility.CanUseBedNow))]
    internal static class RestUtility_CanUseBedNow
    {
        [HarmonyPrefix]
        static bool Prefix(ref bool __result, Thing bedThing, Pawn sleeper, bool checkSocialProperness, bool allowMedBedEvenIfSetToNoCare = false, GuestStatus? guestStatusOverride = null)
        {
            if (sleeper == null || !MechUtility.PawnWearingExosuitCore(sleeper))
            {
                return true; 
            }


            __result = false;
            return false;
            
        }
    }
}
