using HarmonyLib;
using RimWorld;
using System;
using Verse;
using Verse.AI;
using static HarmonyLib.Code;
using static UnityEngine.GraphicsBuffer;

namespace WalkerGear
{
    ////防止龍騎兵能睡在床上
    //[HarmonyPatch(typeof(RestUtility), nameof(RestUtility.CanUseBedEver))]
    //internal static class RestUtility_CanUseBedEver
    //{
    //    [HarmonyPostfix]
    //    static void Postfix(Pawn p, ThingDef bedDef, ref bool __result)
    //    {
    //        if (__result && MechUtility.PawnWearingWalkerCore(p))
    //        {
    //            __result = false;
    //        }
    //    }
    //}
}
