using HarmonyLib;
using Verse;
using RimWorld;

namespace Mechsuit
{
    // 用于确保异步 Verb 不会干扰 Pawn 的 StanceTracker
    // 这是解决“定身”问题的核心 Patch
    [HarmonyPatch(typeof(Pawn_StanceTracker), nameof(Pawn_StanceTracker.SetStance))]
    public static class Patch_Pawn_StanceTracker_SetStance
    {
        [HarmonyPrefix]
        public static bool Prefix(Stance newStance)
        {
            // 如果新状态是 Stance_Busy 且由 AsyncShootVerb 发起，则取消该状态的设置
            // 这允许肩炮射击时，Pawn 依然可以自由移动或执行其他逻辑
            if (newStance is Stance_Busy busy && busy.verb is AsyncShootVerb)
            {
                return false;
            }
            return true;
        }
    }
}
