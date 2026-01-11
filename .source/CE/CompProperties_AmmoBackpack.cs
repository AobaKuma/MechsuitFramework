using Verse;

namespace Exosuit.CE
{
    // 弹药背包组件属性
    public class CompProperties_AmmoBackpack : CompProperties
    {
        // 背包总容量，重量单位kg
        // 7.62mm NATO Mass=0.025，20/0.025=800发
        public float totalMassCapacity = 20f;
        
        // 最小Mass值，防止除零或极端值
        public float minMass = 0.01f;
        
        // 基础装填时间
        public int baseReloadTicks = 180;
        
        public CompProperties_AmmoBackpack()
        {
            compClass = typeof(CompAmmoBackpack);
        }
    }
}
