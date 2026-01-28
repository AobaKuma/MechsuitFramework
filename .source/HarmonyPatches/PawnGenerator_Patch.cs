// 当白昼倾坠之时
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
    public static class PawnGenerator_Patch
    {
        // 机兵装备生成后回调
        public static event Action<Pawn, Exosuit_Core> OnPostGenerate;

        // 处理外部模块生成钩子
        public static List<IExosuitGenerationHook> GenerationHooks = new();

        static void Postfix(Pawn pawn, PawnGenerationRequest request)
        {
            ModExtForceApparelGen modExt = request.KindDef?.GetModExtension<ModExtForceApparelGen>();
            if (modExt == null) return;
            
            Exosuit_Core core = null;
            Color? color = modExt.colorGenerator?.NewRandomizedColor();

            void setupApparel(Apparel a)
            {
                if (color != null) a.SetColor((Color)color);
                pawn.apparel.Wear(a, false, true);
                if (a is Exosuit_Core core2) core = core2;
            }

            // 处理强制装备生成
            if (modExt.apparels != null)
            {
                foreach (ThingDef apparelDef in modExt.apparels)
                {
                    if (apparelDef == null) continue;
                    if (ThingMaker.MakeThing(apparelDef) is Apparel a) setupApparel(a);
                }
            }

            // 处理概率装备生成
            if (modExt.chanceApparels != null)
            {
                foreach (var item in modExt.chanceApparels)
                {
                    if (item?.apparel == null) continue;
                    if (!Rand.Chance(item.chance)) continue;
                    if (ThingMaker.MakeThing(item.apparel) is Apparel a) setupApparel(a);
                }
            }

            // 处理核心逻辑与模组替换
            if (core != null)
            {
                core.ModuleRecache();
                core.Health = core.HealthMax * modExt.StructurePointRange.RandomInRange;
                
                // 执行兼容性检查钩子
                HandleCompatibilityHooks(pawn, modExt);

                OnPostGenerate?.Invoke(pawn, core);
            }
        }

        private static void HandleCompatibilityHooks(Pawn pawn, ModExtForceApparelGen modExt)
        {
            if (GenerationHooks.Count == 0 || pawn.apparel == null) return;

            // 缓存列表防止遍历修改
            var wornApparel = pawn.apparel.WornApparel.ToList();
            
            foreach (var apparel in wornApparel)
            {
                foreach (var hook in GenerationHooks)
                {
                    if (hook.CheckAndHandleIncompatibility(pawn, apparel, out ThingDef suggestedFallback))
                    {
                        // 执行不兼容项替换
                        ThingDef finalFallback = null;

                        // 优先查找配置回退项
                        if (modExt.fallbackApparels != null)
                        {
                            finalFallback = modExt.fallbackApparels.FirstOrDefault(f => f.target == apparel.def)?.fallback;
                        }

                        // 使用钩子默认回退项
                        finalFallback ??= suggestedFallback;

                        if (finalFallback != null)
                        {
                            pawn.apparel.Remove(apparel);
                            if (ThingMaker.MakeThing(finalFallback) is Apparel replacement)
                            {
                                pawn.apparel.Wear(replacement, false, true);
                                if (DebugSettings.godMode) Log.Message($"[Mechsuit] {pawn.LabelShort}: {apparel.def.defName} 不兼容，已替换为 {finalFallback.defName}");
                            }
                        }
                        break; // 处理完当前的 apparel
                    }
                }
            }
        }
    }
}