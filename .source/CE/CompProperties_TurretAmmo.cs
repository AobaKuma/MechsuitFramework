// 当白昼倾坠之时
using System.Collections.Generic;
using CombatExtended;
using Verse;

namespace Exosuit.CE
{
    // 炮塔弹药组件属性
    public class CompProperties_TurretAmmo : CompProperties
    {
        // 内置弹仓容量
        public int magCapacity = 300;
        
        // 设置装填时间周期
        public int reloadTicks = 180;
        
        // 设置可用弹药组
        public AmmoSetDef ammoSet;
        
        // 默认允许弹药箱供弹
        public bool defaultAllowBackpackFeed = true;
        
        public CompProperties_TurretAmmo()
        {
            compClass = typeof(CompTurretAmmo);
        }
        
        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (var error in base.ConfigErrors(parentDef))
                yield return error;
            
            if (magCapacity <= 0)
                yield return "magCapacity must be greater than 0";
        }
    }
}
