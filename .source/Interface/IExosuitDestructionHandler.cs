// 当白昼倾坠之时
using Verse;

namespace Exosuit
{
    // 用于在机兵被击毁生成残骸时执行自定义逻辑
    public interface IExosuitDestructionHandler
    {
        // 当机兵核心被击毁并生成残骸时调用
        void OnExosuitDestroyed(Building_Wreckage wreckage);
    }
}
