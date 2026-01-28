// 当白昼倾坠之时
using RimWorld;
using UnityEngine;
using Verse;

namespace Exosuit.CE
{
    // 弹药存储模块接口
    public interface IAmmoStorage
    {
        string StorageName { get; }
        
        bool IsActive { get; set; }
        
        void DrawUI(Rect rect, ref float curY);
    }
}
