using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Exosuit
{
    [HarmonyPatch(typeof(JobGiver_GetRest), "GetPriority")]
    static class JobGiver_GetRest_GetPriority_Patch
    {
        static bool Prefix(JobGiver_GetRest __instance, Pawn pawn,ref float __result)
        {
            if (MechUtility.IsWorkWithFrame(pawn))
            {
                if (!MechUtility.PawnWearingExosuitCore(pawn) && MechUtility.GetClosestCoreForPawn(pawn) != null)
                {
                    MechUtility.TryMakeJob_GearOn(pawn);
                }
                __result = 0f;
                return false;
            }
            else if (MechUtility.IsNotAnything(pawn) && MechUtility.PawnWearingExosuitCore(pawn)) //沒有設置的狀況會自己下機。
            {
                MechUtility.TryMakeJob_GearOff(pawn);
                __result = 0f;
                return false;
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(JobGiver_Work), "GetPriority")]
    static class JobGiver_Work_GetPriority_Patch
    {
        static bool Prefix(JobGiver_Work __instance, Pawn pawn, ref float __result)
        {
            if (MechUtility.IsWorkWithFrame(pawn))
            {
                if (!MechUtility.PawnWearingExosuitCore(pawn) && MechUtility.GetClosestCoreForPawn(pawn) != null)
                {
                    MechUtility.TryMakeJob_GearOn(pawn);
                }
                __result = 9f;
                return false;
            }
            else if (MechUtility.IsNotAnything(pawn) && MechUtility.PawnWearingExosuitCore(pawn)) //沒有設置的狀況會自己下機。
            {
                MechUtility.TryMakeJob_GearOff(pawn);
                __result = 9f;
                return false;
            }
            return true;
        }
    }
}