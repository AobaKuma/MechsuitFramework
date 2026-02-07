// 当白昼倾坠之时
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Mechsuit;
using Verse;

namespace Exosuit.CE
{
    // CE Verb 初始化 Patch
    // 解决主 DLL 无法加载 CE Verb 类型的问题
    [HarmonyPatch]
    public static class CEPatches_VerbInit
    {
        private static FieldInfo verbTrackerField;
        private static FieldInfo verbsField;
        
        static CEPatches_VerbInit()
        {
            verbTrackerField = AccessTools.Field(typeof(CompTurretGun), "verbTracker");
            verbsField = AccessTools.Field(typeof(VerbTracker), "verbs");
        }
        
        // 在 UpdateGunVerbs 之后检查并修复 CE Verb
        [HarmonyPatch(typeof(CompTurretGun), "UpdateGunVerbs")]
        [HarmonyPostfix]
        public static void UpdateGunVerbs_Postfix(CompTurretGun __instance)
        {
            if (__instance.VerbTracker.AllVerbs.Count > 0) return;
            
            var gun = __instance.gun;
            if (gun == null) return;
            
            var verbDefs = gun.def.verbs;
            if (verbDefs == null || verbDefs.Count == 0) return;
            
            // 检查是否有 CE VerbProperties
            bool hasCEVerb = false;
            foreach (var vp in verbDefs)
            {
                if (vp.verbClass != null && vp.verbClass.FullName == "Exosuit.CE.AsyncShootVerbCE")
                {
                    hasCEVerb = true;
                    break;
                }
            }
            
            if (!hasCEVerb) return;
            
            Log.Message($"[CE炮塔] 检测到 CE Verb 未初始化, 正在手动创建: {__instance.parent.def.defName}");
            
            // 手动创建 CE Verb
            try
            {
                var verbList = new List<Verb>();
                
                for (int i = 0; i < verbDefs.Count; i++)
                {
                    var verbProps = verbDefs[i];
                    if (verbProps.verbClass == null) continue;
                    
                    Verb verb;
                    if (verbProps.verbClass.FullName == "Exosuit.CE.AsyncShootVerbCE")
                    {
                        verb = new AsyncShootVerbCE();
                    }
                    else
                    {
                        verb = (Verb)Activator.CreateInstance(verbProps.verbClass);
                    }
                    
                    // 初始化 Verb
                    string loadID = Verb.CalculateUniqueLoadID(__instance, i);
                    verb.loadID = loadID;
                    verb.verbProps = verbProps;
                    verb.verbTracker = __instance.VerbTracker;
                    verb.caster = __instance.PawnOwner;
                    
                    if (verb is IAsyncShootVerb asyncVerb)
                    {
                        asyncVerb.TurretComp = __instance;
                    }
                    
                    verbList.Add(verb);
                    Log.Message($"[CE炮塔] 已创建 Verb: {verb.GetType().Name}");
                }
                
                // 设置 VerbTracker 的内部列表
                verbsField.SetValue(__instance.VerbTracker, verbList);
                
                Log.Message($"[CE炮塔] Verb 初始化完成, VerbCount={__instance.VerbTracker.AllVerbs.Count}");
            }
            catch (Exception ex)
            {
                Log.Error($"[CE炮塔] 手动创建 Verb 失败: {ex}");
            }
        }
    }
}
