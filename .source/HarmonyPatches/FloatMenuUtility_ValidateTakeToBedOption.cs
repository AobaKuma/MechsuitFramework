using HarmonyLib;
using RimWorld;
using Verse;

namespace Exosuit
{
    //防止龍騎兵被帶上床(醫療或者俘虜)
    [HarmonyPatch(typeof(FloatMenuUtility), nameof(FloatMenuUtility.ValidateTakeToBedOption))]
    internal static class FloatMenuUtility_ValidateTakeToBedOption
    {
        [HarmonyPostfix]
        static void Postfix(Pawn target, ref FloatMenuOption option)
        {
            if (MechUtility.PawnWearingExosuitCore(target))
            {
                option.Disabled = true;
                option.Label = "WG_Disabled_VictimInWalkerCore".Translate();
                option.orderInPriority = 0;
            }
        }
    }
}
