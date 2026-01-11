using Verse;

namespace Exosuit
{
    // 模块数据转移接口
    // 用于模块在装甲形态和物品形态之间转换时保存和恢复数据
    // 主项目定义接口，各功能模块（如CE弹药背包）实现
    public interface IModuleDataTransfer
    {
        // 从源物品保存数据（装甲 -> 物品 或 物品 -> 装甲 之前调用）
        void SaveDataFrom(Thing source);
        
        // 将数据恢复到目标物品（转换完成后调用）
        void RestoreDataTo(Thing target);
    }
}
