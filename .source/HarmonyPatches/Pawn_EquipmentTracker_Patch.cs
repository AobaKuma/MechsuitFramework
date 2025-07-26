using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace Exosuit
{
    /*    [HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.AddEquipment))]
        static class Pawn_EquipmentTracker_AddEquipment
        {
            [HarmonyPrefix]
            static bool AddEquipment(Pawn_EquipmentTracker __instance, ThingWithComps newEq)
            {

                return newEq.def.equipmentType != EquipmentType.Primary
                       || __instance.Primary == null
                       || !__instance.Primary.HasComp<CompApparelForcedWeapon>();
            }
        }*/

    [HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.TryDropEquipment))]
    static class Pawn_EquipmentTracker_TryDropEquipment
    {
        [HarmonyPrefix]
        static bool TryDropEquipment(ThingWithComps eq, ref bool __result)
        {
            return eq.def.equipmentType != EquipmentType.Primary || !eq.HasComp<CompApparelForcedWeapon>() || (__result = false);
        }
    }
    /*
        [HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.Remove))]
        static class Pawn_EquipmentTracker_Remove
        {
            [HarmonyPrefix]
            static bool Remove(ThingWithComps eq)
            {
                return eq.def.equipmentType != EquipmentType.Primary || !eq.TryGetComp<CompApparelForcedWeapon>(out var c) || c.NeedRemove;
            }
        }*/

    [HarmonyPatch(typeof(ThingOwner), nameof(ThingOwner.TryTransferToContainer), [typeof(Thing), typeof(ThingOwner), typeof(int), typeof(Thing), typeof(bool)], [ArgumentType.Normal,ArgumentType.Normal,ArgumentType.Normal,ArgumentType.Out,ArgumentType.Normal])]
    public static class ThingOwner_TryTransferToContainer
    {
        public static ThingWithComps thingListening;
        [HarmonyPostfix]
        static void TryTransferToContainer(Thing item)
        {
            if(thingListening == item)
            {
                item.holdingOwner.Remove(item);
                thingListening = null;
            }
        }
    }

    [HarmonyPatch(
        typeof(EquipmentUtility), 
        nameof(EquipmentUtility.CanEquip), 
        new Type[] { typeof(Thing), typeof(Pawn), typeof(string), typeof(bool) }, 
        new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Normal })]
    static class EquipmentUtility_CanEquip
    {
        static bool Prefix(Pawn pawn, out string cantReason)
        {
            cantReason = null;
            if (pawn.equipment.Primary == null) return true;
            if (pawn.equipment.Primary.HasComp<CompApparelForcedWeapon>())
            {
                cantReason += "WG_HasMechWeapon".Translate();
                return false;
            }
            return true;
        }
    }
}
