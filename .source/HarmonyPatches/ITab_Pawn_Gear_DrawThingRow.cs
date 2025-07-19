using HarmonyLib;
using RimWorld;
using Verse;

namespace Exosuit
{
    [HarmonyPatch(typeof(ITab_Pawn_Gear), "DrawThingRow")]
    static class ITab_Pawn_Gear_DrawThingRow
    {
        [HarmonyPrefix]
        static bool DrawThingRow(ITab_Pawn_Gear __instance, ref float y, float width, Thing thing, bool inventory = false)
        {
            return thing is not Apparel || !thing.TryGetComp<CompSuitModule>(out _);
        }
    }
}