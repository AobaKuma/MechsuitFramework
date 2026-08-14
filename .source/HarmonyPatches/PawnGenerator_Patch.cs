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
        
        private const bool DebugLog = false;
        private static void DLog(string message)
        {
            if (DebugLog)
                Verse.Log.Message($"[PawnGenerator] {message}");
        }

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

                // 执行兼容性检查钩子
                HandleCompatibilityHooks(pawn, modExt);

                // 钩子可能替换过模块，重建模块缓存后再写入耐久
                core.ModuleRecache();

                // 按 StructurePointRange 生成初始损伤。
                // 必须落到各模块的 HP 上：只写 core.Health 的话数值仅存在于
                // healthInt，之后任何一次 RefreshHP（读档 / 维修 / 换装）都会
                // 用模块耐久把它覆盖掉。
                ApplyGeneratedStructurePoints(core, modExt.StructurePointRange);

                // NPC自动装备模块武器
                AutoEquipModuleWeapon(pawn);

                OnPostGenerate?.Invoke(pawn, core);
            }
        }

        // 按比例写入各模块耐久，作为 NPC 机体的初始损伤
        private static void ApplyGeneratedStructurePoints(Exosuit_Core core, FloatRange range)
        {
            float ratio = Mathf.Clamp01(range.RandomInRange);
            if (ratio >= 1f) return; // 满耐久，模块保持默认值即可

            foreach (Thing module in core.modules)
            {
                if (!module.TryGetComp<CompSuitModule>(out var comp)) continue;
                int max = comp.MaxHP;
                comp.HP = Mathf.Clamp(Mathf.CeilToInt(max * ratio), 1, max);
            }
            core.RefreshHP();
        }

        // 新-扫描已穿戴模块，将CompModuleWeapon中的武器自动装备至Primary
        private static void AutoEquipModuleWeapon(Pawn pawn)
        {
            if (pawn.equipment?.Primary != null) return;

            var worn = pawn.apparel?.WornApparel;
            if (worn == null) return;

            for (int i = 0; i < worn.Count; i++)
            {
                var mw = worn[i].GetComp<CompModuleWeapon>();
                if (mw?.Weapon == null) continue;

                var weapon = mw.Weapon;

                // 复制Gizmo切换流程
                if (weapon.holdingOwner == null)
                {
                    weapon.DeSpawnOrDeselect();
                }
                weapon.holdingOwner?.Remove(weapon);
                ThingOwner_TryTransferToContainer.thingListening = null;
                pawn.equipment.AddEquipment(weapon);
                break;
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
                                if (DebugSettings.godMode) DLog($"[Mechsuit] {pawn.LabelShort}: {apparel.def.defName} 不兼容，已替换为 {finalFallback.defName}");
                            }
                        }
                        break; // 处理完当前的 apparel
                    }
                }
            }
        }
    }
}
