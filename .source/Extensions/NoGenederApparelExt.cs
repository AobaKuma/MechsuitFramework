using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Exosuit
{
    internal class NoGenederApparelExt:DefModExtension
    {
        static NoGenederApparelExt()
        {
            ExosuitMod.instance.Patch(typeof(ApparelGraphicRecordGetter).Method(nameof(ApparelGraphicRecordGetter.TryGetGraphicApparel)), transpiler: typeof(NoGenederApparelExt).Method(nameof(TryGetGraphicApparel_NoGenderTranspiler)));
        }
        static IEnumerable<CodeInstruction> TryGetGraphicApparel_NoGenderTranspiler(IEnumerable<CodeInstruction> instructions) => instructions.MethodReplacer(typeof(PawnRenderUtility).Method(nameof(PawnRenderUtility.RenderAsPack)), typeof(NoGenederApparelExt).Method(nameof(NoGenderOrRenderAsPack)));
        static bool NoGenderOrRenderAsPack(Apparel ap) => ap.def.GetModExtension<NoGenederApparelExt>() != null || ap.RenderAsPack();

    }
}
