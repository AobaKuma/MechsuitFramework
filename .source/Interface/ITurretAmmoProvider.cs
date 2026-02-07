// 当白昼倾坠之时
using RimWorld;
using Verse;

namespace Exosuit
{
    // 炮塔弹药供给接口
    // 用于CE模块检测炮塔是否有可用弹药
    // 主项目定义接口，CE模块实现
    public interface ITurretAmmoProvider
    {
        // 是否有弹药可用
        bool HasAmmoAvailable { get; }
        
        // 当前弹药定义 (使用 ThingDef 以解耦 CE)
        ThingDef CurrentAmmoDef { get; }
        
        // 当前弹药数量
        int CurrentAmmo { get; }
        
        // 最大弹药容量
        int MaxAmmo { get; }
        
        // 消耗弹药
        bool TryConsumeAmmo(int count);
        
        // 是否允许使用弹药箱供弹
        bool AllowBackpackFeed { get; set; }

        // 获取当前应发射的弹丸定义 (CE 专用)
        ThingDef CurAmmoProjectile { get; }
    }
}
