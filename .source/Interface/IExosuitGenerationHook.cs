// 当白昼倾坠之时
using RimWorld;
using Verse;

namespace Exosuit
{
    // 提供给外部模块（如 CE）进行生成后检查的接口
    public interface IExosuitGenerationHook
    {
        // 检查生成的装备是否合理，返回是否需要替换
        // apparel: 当前检查的模块
        // pawn: 宿主
        // fallbackDef: 如果不兼容，建议替换的次级模块
        bool CheckAndHandleIncompatibility(Pawn pawn, Apparel apparel, out ThingDef fallbackDef);
    }
}
