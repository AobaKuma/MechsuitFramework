using HarmonyLib;
using RimWorld;
using System;
using Verse;
using Verse.AI;
using static HarmonyLib.Code;
using static UnityEngine.GraphicsBuffer;

namespace WalkerGear
{
    //防止龍騎兵衣服被脫
    [HarmonyPatch(typeof(StrippableUtility), nameof(StrippableUtility.CanBeStrippedByColony))]
    internal static class StrippableUtility_CanBeStrippedByColony
    {
        [HarmonyPostfix]
        static void Postfix(Thing th, ref bool __result)
        {
            if (!__result) return;
            if (th is Pawn pawn && !pawn.NonHumanlikeOrWildMan())
            {
                __result = !MechUtility.PawnWearingWalkerCore(pawn);
            }
            else if(th is Corpse corpse && !corpse.InnerPawn.NonHumanlikeOrWildMan())
            {
                __result = !MechUtility.PawnWearingWalkerCore(corpse.InnerPawn);
            }
        }
    }
    //防止龍騎兵能睡在床上
    [HarmonyPatch(typeof(RestUtility), nameof(RestUtility.CanUseBedEver))]
    internal static class RestUtility_CanUseBedEver
    {
        [HarmonyPostfix]
        static void Postfix(Pawn p, ThingDef bedDef, ref bool __result)
        {
            if (__result && MechUtility.PawnWearingWalkerCore(p))
            {
                __result = false;
            }
        }
    }

    //防止龍騎兵被帶上床(醫療或者俘虜)
    [HarmonyPatch(typeof(FloatMenuMakerMap), "ValidateTakeToBedOption")]
    internal static class FloatMenuMakerMap_ValidateTakeToBedOption
    {
        [HarmonyPostfix]
        static void Postfix(Pawn target, ref FloatMenuOption option)
        {
            if (target.apparel !=null && !target.NonHumanlikeOrWildMan() && MechUtility.PawnWearingWalkerCore(target))
            {
                option.Disabled = true;
                option.Label = "WG_Disabled_VictimInWalkerCore".Translate();
                option.orderInPriority = 0;
            }
        }
    }
    
    /* 
     [StaticConstructorOnStartup]
     [HarmonyPatch(typeof(RestUtility), nameof(RestUtility.FindBedFor),
         new Type[] { typeof(Pawn), typeof(Pawn), typeof(bool), typeof(bool), typeof(GuestStatus?) },
         new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal })]
     internal static class RestUtility_FindBedFor
     {
         [HarmonyPrefix]
         static bool FindBedFor(Pawn sleeper, GuestStatus? guestStatus, ref Building_Bed __result)
         {
             if (sleeper.RaceProps.IsMechanoid) return true;
             if (Validator(sleeper, guestStatus))
             {
                 __result = null;
                 return false;
             }
             return true;
         }
         static bool Validator(Pawn p, GuestStatus? guestStatus)
         {
             if (guestStatus != GuestStatus.Prisoner) return false;//非囚禁上床的都隨意。
             if (p.IsPlayerControlled) return false; //玩家控制的就不判定了。

             if (MechUtility.PawnWearingWalkerCore(p))
             {
                 Messages.Message("WG_Disabled_VictimInWalkerCore".Translate(), MessageTypeDefOf.RejectInput, false);
                 return true;
             }
             return false;
         }
     }
    */
}
