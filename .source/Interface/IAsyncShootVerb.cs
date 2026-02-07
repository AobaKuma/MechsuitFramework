// 当白昼倾坠之时
using UnityEngine;
using Verse;

namespace Mechsuit
{
    // 异步射击 Verb 接口
    // 用于统一处理普通模式和 CE 模式的异步射击
    public interface IAsyncShootVerb
    {
        // 关联的炮塔组件
        CompTurretGun TurretComp { get; set; }
        
        // Verb 是否可用
        bool Available();
        
        // 能否从指定位置命中目标
        bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ);
        
        // 能否命中目标
        bool CanHitTarget(LocalTargetInfo targ);
        
        // 异步驱动 Tick
        void AsyncVerbTick();
        
        // 响应强制目标选择
        void OrderForceTarget(LocalTargetInfo target);
        
        // 尝试开始异步射击
        bool TryStartAsyncCast(LocalTargetInfo targ);
        
        // 预热完成后开始射击
        void WarmupComplete();
        
        // 获取 Verb 状态
        VerbState State { get; }
        
        // 是否正在预热
        bool WarmingUp { get; }
        
        // 预热时间
        float WarmupTime { get; }
        
        // 当前目标
        LocalTargetInfo CurrentTarget { get; set; }
        
        // UI 图标
        Texture2D UIIcon { get; }
    }
}
