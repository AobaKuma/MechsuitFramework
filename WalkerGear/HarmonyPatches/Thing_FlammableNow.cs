using HarmonyLib;
using Verse;

namespace WalkerGear
{
    [HarmonyPatch(typeof(Thing), "get_FlammableNow")]
    public static class Thing_FlammableNow
    {
        [HarmonyPostfix]
        static void Postfix(ref bool __result, Thing __instance)
        {
            if (__result && __instance is Pawn pawn && pawn.GetWalkerCore(out _))
            {
                __result = false;
            }
        }
    }
}
