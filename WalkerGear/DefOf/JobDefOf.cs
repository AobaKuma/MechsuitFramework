using RimWorld;
using Verse;

namespace WalkerGear
{
    [DefOf]
    public static class JobDefOf
    {
        /// <summary>
        /// 將組件從儲存位置搬運到維護港
        /// </summary>
        //public static JobDef WG_LoadComponent;
        /// <summary>
        /// 從維護塢插槽上移除並搬運組件
        /// </summary>
        //public static JobDef WG_RemoveComponent;
        /// <summary>
        /// 維修物品形式的組件
        /// </summary>
        public static JobDef WG_RepairComponent;

        //public static JobDef WG_ModuleMaintenance;
        /// <summary>
        /// 右鍵選擇後 移動到維護塢並登上Walker
        /// </summary>
        public static JobDef WG_GetInWalkerCore;
        /// <summary>
        /// 右鍵選擇後 移動到維護塢並離開Walker
        /// </summary>
        public static JobDef WG_GetOffWalkerCore;
        /// <summary>
        /// 
        /// </summary>
        //public static JobDef WG_BackToMaintenanceBay;
    }
}
