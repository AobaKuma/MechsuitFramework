using HarmonyLib;
using RimWorld;
using Verse;

namespace WalkerGear
{
    //防止龍騎兵被帶上床(醫療或者俘虜)
    [HarmonyPatch(typeof(FloatMenuMakerMap), "ValidateTakeToBedOption")]
    internal static class FloatMenuMakerMap_ValidateTakeToBedOption
    {
        [HarmonyPostfix]
        static void Postfix(Pawn target, ref FloatMenuOption option)
        {
            if (MechUtility.PawnWearingWalkerCore(target))
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
