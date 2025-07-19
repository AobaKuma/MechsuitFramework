using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Exosuit
{

    [HarmonyPatch(typeof(PawnGenerator), "GenerateGearFor")]
    static class PawnGenerator_Patch
    {
        static void Postfix(Pawn pawn, PawnGenerationRequest request)
        {
            ModExtForceApparelGen modExt = request.KindDef?.GetModExtension<ModExtForceApparelGen>();
            if (modExt == null) return;
            if (DebugSettings.godMode) Log.Message("ModExtForceApparelGen loaded, Force Apparel Gen");
            Exosuit_Core core = null;

            Color? color = null;
            if (modExt.colorGenerator != null) color = modExt.colorGenerator?.NewRandomizedColor();

            void setupApparel(Apparel a)
            {
                if (color != null) a.SetColor((Color)color);
                pawn.apparel.Wear(a, false, true);
                if (a is Exosuit_Core core2) core = core2;
            }

            foreach (ThingDef apparelDef in modExt.apparels)
            {
                setupApparel((Apparel)ThingMaker.MakeThing(apparelDef));
            }
            foreach (var item in modExt.chanceApparels)
            {
                if (!Rand.Chance(item.chance)) continue;
                setupApparel((Apparel)ThingMaker.MakeThing(item.apparel));
            }

            if (core != null)
            {
                core.ModuleRecache();
                core.Health = core.HealthMax * modExt.StructurePointRange.RandomInRange;
            }
            
        }
        
    }
}