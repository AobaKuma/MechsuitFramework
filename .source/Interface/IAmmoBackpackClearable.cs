namespace Exosuit
{
    // 可清空的弹药背包接口
    // 用于龙门架整备时检测是否需要清空弹药
    // 主项目定义接口，CE 模块实现
    public interface IAmmoBackpackClearable
    {
        // 是否需要清空（切换弹种后需要先清空旧弹药）
        bool NeedsClear { get; }
        
        // 执行清空操作（无参数版本，尝试自动获取位置）
        void EjectCurrentAmmo();
        
        // 执行清空操作（指定龙门架，用于整备工作）
        void EjectCurrentAmmoAt(Building_MaintenanceBay gantry);
    }
}
