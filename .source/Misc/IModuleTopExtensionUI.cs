// 当白昼倾坠之时
using UnityEngine;

namespace Exosuit
{
    // 模块顶部拓展 UI 接口
    // 实现此接口的组件可以在龙门架 ITab 顶部显示额外的配置面板
    public interface IModuleTopExtensionUI
    {
        // 是否显示顶部拓展 UI
        bool ShouldShowTopExtension { get; }
        
        // 顶部拓展 UI 的高度
        float TopExtensionHeight { get; }
        
        // 绘制顶部拓展 UI
        // rect: 可用的绘制区域
        void DrawTopExtension(Rect rect);
    }
}
