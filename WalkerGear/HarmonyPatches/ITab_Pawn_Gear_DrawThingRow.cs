using HarmonyLib;
using RimWorld;
using Verse;

namespace WalkerGear
{
    [HarmonyPatch(typeof(ITab_Pawn_Gear), "DrawThingRow")]
    static class ITab_Pawn_Gear_DrawThingRow
    {
        [HarmonyPrefix]
        static bool DrawThingRow(ITab_Pawn_Gear __instance, ref float y, float width, Thing thing, bool inventory = false)
        {
            if (thing is Apparel) return !thing.TryGetComp<CompWalkerComponent>(out var comp);
            return true;
        }
    }
}