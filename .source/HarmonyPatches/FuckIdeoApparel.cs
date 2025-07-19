using HarmonyLib;
using Verse;

namespace Exosuit
{
    [StaticConstructorOnStartup]
    [HarmonyPatch(typeof(PawnGenerator), "GenerateOrRedressPawnInternal")]
    static class FuckIdeoApparel
    {
        [HarmonyPrefix]
        static void FuckingIdeoApparel(ref PawnGenerationRequest request)
        {
            if (request.KindDef.HasModExtension<ModExtension_NoIdeoApparel>())
            {
                request.ForceNoIdeoGear = true;
            }
        }
    }
    
}

