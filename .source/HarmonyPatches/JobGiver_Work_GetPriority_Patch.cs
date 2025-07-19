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
            TimeAssignmentDef timeAssignmentDef = (pawn.timetable == null) ? WG_TimeAssignmentDefOf.Anything : pawn.timetable.CurrentAssignment;
            if (timeAssignmentDef == WG_TimeAssignmentDefOf.WG_WorkWithFrame)
            {
                if (!MechUtility.PawnWearingExosuitCore(pawn) && MechUtility.GetClosestCoreForPawn(pawn) != null)
                {
                    MechUtility.TryMakeJob_GearOn(pawn);
                }
                __result = 0f;
                return false;
            }
            else if (timeAssignmentDef != WG_TimeAssignmentDefOf.Anything && MechUtility.PawnWearingExosuitCore(pawn)) //沒有設置的狀況會自己下機。
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
            TimeAssignmentDef timeAssignmentDef = (pawn.timetable == null) ? WG_TimeAssignmentDefOf.Anything : pawn.timetable.CurrentAssignment;
            if (timeAssignmentDef == WG_TimeAssignmentDefOf.WG_WorkWithFrame)
            {
                if (!MechUtility.PawnWearingExosuitCore(pawn) && MechUtility.GetClosestCoreForPawn(pawn) != null)
                {
                    MechUtility.TryMakeJob_GearOn(pawn);
                }
                __result = 9f;
                return false;
            }
            else if (timeAssignmentDef != WG_TimeAssignmentDefOf.Anything && MechUtility.PawnWearingExosuitCore(pawn)) //沒有設置的狀況會自己下機。
            {
                MechUtility.TryMakeJob_GearOff(pawn);
                __result = 9f;
                return false;
            }
            return true;
        }
    }
}