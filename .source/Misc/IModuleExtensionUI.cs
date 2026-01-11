using UnityEngine;
using Verse;

namespace Exosuit
{
    // 模块拓展 UI 接口
    // 实现此接口的组件可以在龙门架 ITab 右侧显示额外的 UI 面板
    public interface IModuleExtensionUI
    {
        // 是否显示拓展 UI
        bool ShouldShowExtensionUI { get; }
        
        // 拓展 UI 的宽度
        float ExtensionUIWidth { get; }
        
        // 拓展 UI 的标题
        string ExtensionUITitle { get; }
        
        // 绘制拓展 UI
        // rect: 可用的绘制区域
        void DrawExtensionUI(Rect rect);
    }
}
