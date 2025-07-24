using HarmonyLib;
using RimWorld;
using Verse;

namespace Exosuit
{
    [HarmonyPatch(typeof(ITab_Pawn_Gear), "DrawThingRow")]
    static class ITab_Pawn_Gear_DrawThingRow
    {
        [HarmonyPrefix]
        static bool DrawThingRow(Thing thing)
        {
            return thing is not Apparel || !thing.TryGetComp<CompSuitModule>(out _);
        }
    }
}