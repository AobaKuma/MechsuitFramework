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
}
