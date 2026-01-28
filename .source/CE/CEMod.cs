// 当白昼倾坠之时
using HarmonyLib;
using Verse;

namespace Exosuit.CE
{
    // CE兼容模块入口
    [StaticConstructorOnStartup]
    public static class CEMod
    {
        static CEMod()
        {
            Log.Message("[Exosuit] CE兼容模块已加载");
            
            // 注册MechData回调，用于模块转换时保存/恢复弹药背包数据
            Log.Message("[Exosuit] 正在注册生成钩子与数据回调...");
            PawnGenerator_Patch.GenerationHooks.Add(new CEAmmoBackpackHook());
            CEPatches.RegisterMechDataCallbacks();
            Log.Message("[Exosuit] MechData回调注册完成");
            
            var harmony = new Harmony("Exosuit.CE");
            harmony.PatchAll();
        }
    }
}
