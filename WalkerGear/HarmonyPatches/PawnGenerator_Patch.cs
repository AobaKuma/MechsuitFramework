using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace WalkerGear
{

    [HarmonyPatch(typeof(PawnGenerator), "GenerateGearFor")]
    static class PawnGenerator_Patch
    {
        static void Postfix(Pawn pawn, PawnGenerationRequest request)
        {
            PawnKindDef def = request.KindDef;
            if (def == null) return;
            ModExtForceApparelGen modExt = def.GetModExtension<ModExtForceApparelGen>();
            if (modExt == null) return;
            if (DebugSettings.godMode) Log.Message("ModExtForceApparelGen loaded, Force Apparel Gen");
            WalkerGear_Core core = null;

            Color? color = null;
            if (modExt.colorGenerator != null) color = modExt.colorGenerator?.NewRandomizedColor();

            foreach (ThingDef apparelDef in modExt.apparels)
            {
                Apparel apparel = (Apparel)ThingMaker.MakeThing(apparelDef);
                if (color != null) apparel.SetColor((Color)color);

                pawn.apparel.Wear(apparel, false, true);
                if (apparel is WalkerGear_Core core2) core = core2;
            }
            foreach (ApparelChance item in modExt.chanceApparels)
            {
                if (!Rand.Chance(item.chance)) continue;
                Apparel apparel = (Apparel)ThingMaker.MakeThing(item.apparel);
                if (color != null) apparel.SetColor((Color)color);
                pawn.apparel.Wear(apparel, false, true);
                if (apparel is WalkerGear_Core core2) core = core2;
            }

            core?.RefreshHP(true);
            core.Health = core.HealthMax * modExt.StructurePointRange.RandomInRange;
        }
    }
}