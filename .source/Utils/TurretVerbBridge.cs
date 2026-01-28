// 当白昼倾坠之时
using System;
using Verse;

namespace Mechsuit
{
    // 炮塔 Verb 桥接器
    // 允许 CE 模块注入自己的 Verb 处理逻辑
    public static class TurretVerbBridge
    {
        // CE 模块注册的回调函数
        public static Func<Verb, CompTurretGun, bool> TryInitCEVerb;
        public static Action<Verb> CEVerbTick;
        public static Func<Verb, LocalTargetInfo, bool> CEVerbTryStartCast;
        public static Action<Verb> CEVerbWarmupComplete;
        
        // 检查是否是 CE 模式的 Verb
        public static bool IsCEVerb(Verb verb)
        {
            if (verb == null) return false;
            // 通过类型名检查
            return verb.GetType().FullName?.Contains("CombatExtended") ?? false;
        }
        
        // 尝试初始化 Verb
        public static bool TryInitVerb(Verb verb, CompTurretGun turretComp)
        {
            if (TryInitCEVerb != null && IsCEVerb(verb))
            {
                return TryInitCEVerb(verb, turretComp);
            }
            return false;
        }
        
        // Verb Tick
        public static void VerbTick(Verb verb)
        {
            if (CEVerbTick != null && IsCEVerb(verb))
            {
                CEVerbTick(verb);
            }
        }
        
        // 尝试开始射击
        public static bool TryStartCast(Verb verb, LocalTargetInfo target)
        {
            if (CEVerbTryStartCast != null && IsCEVerb(verb))
            {
                return CEVerbTryStartCast(verb, target);
            }
            return false;
        }
        
        // 预热完成
        public static void WarmupComplete(Verb verb)
        {
            if (CEVerbWarmupComplete != null && IsCEVerb(verb))
            {
                CEVerbWarmupComplete(verb);
            }
        }
    }
}
