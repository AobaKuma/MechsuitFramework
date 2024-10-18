using HarmonyLib;
using Verse;

namespace WalkerGear
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
    public class ModExtension_NoIdeoApparel : DefModExtension { };
}

