// 当白昼倾坠之时
using HarmonyLib;
using Verse;

namespace Exosuit.CE
{
    // CE兼容模块入口
    [StaticConstructorOnStartup]
    public static class CEMod
    {
        private const bool DebugLog = false; // CE模块的调试日志开关
        private static void DLog(string message)
        {
            if (DebugLog)
                Verse.Log.Message($"[Exosuit.CE] {message}");
        }
        static CEMod()
        {
            DLog("CE兼容模块已加载");
            DLog("正在注册生成钩子与数据回调...");
            PawnGenerator_Patch.GenerationHooks.Add(new CEAmmoBackpackHook());
            CEPatches.RegisterMechDataCallbacks();
            DLog("MechData回调注册完成");
            
            var harmony = new Harmony("Exosuit.CE");
            harmony.PatchAll();
        }
    }
}
