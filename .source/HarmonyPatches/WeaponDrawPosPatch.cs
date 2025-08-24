using HarmonyLib;
using UnityEngine;
using Verse;

namespace Exosuit
{
    [HarmonyPatch(typeof(PawnRenderUtility), "DrawEquipmentAndApparelExtras")]
    internal class WeaponDrawPosPatch
    {
        private static void Prefix(Pawn pawn, ref Vector3 drawPos, Rot4 facing, PawnRenderFlags flags)
        {
            if (pawn.equipment?.Primary == null)
            {
                return;
            }
            if (!pawn.TryGetExosuitCore(out var core)) return;
            var modExt = core.def.GetModExtension<ApparelRenderOffsets>();
            if (modExt == null || modExt.equipmentOffsetData == null)
            {
                return;
            }
            if (!flags.HasFlag(PawnRenderFlags.NeverAimWeapon) && pawn.stances?.curStance is Stance_Busy stance_Busy && !stance_Busy.neverAimWeapon && stance_Busy.focusTarg.IsValid)
            {
                drawPos += modExt.equipmentOffsetData.OffsetForRot(facing);
            }
            else if (PawnRenderUtility.CarryWeaponOpenly(pawn))
            {
                drawPos += modExt.equipmentOffsetData.OffsetForRot(facing);
            }
        }
    }
}
