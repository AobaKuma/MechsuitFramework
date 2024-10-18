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

        /// <summary>
        /// 維修整備架上的組件
        /// </summary>
        public static JobDef WG_RepairAtGantry;

        /// <summary>
        /// 右鍵選擇後 移動到維護塢並登上Walker
        /// </summary>
        public static JobDef WG_GetInWalkerCore;
        public static JobDef WG_GetInWalkerCore_NonDrafted;
        /// <summary>
        /// 右鍵選擇後 移動到維護塢並離開Walker
        /// </summary>
        public static JobDef WG_GetOffWalkerCore;



        /// <summary>
        /// 右鍵選擇後 移動到彈射器
        /// </summary>
        public static JobDef WG_GetInEjector;

        /// <summary>
        /// 對倒地的自家龍騎兵右鍵後，攜帶對方到維護塢並使其離開Walker。
        /// </summary>
        public static JobDef WG_TakeToMaintenanceBay;



        /// <summary>
        /// 對倒地的自家龍騎兵右鍵後，攜帶對方到維護塢並使其離開Walker。
        /// </summary>
        public static JobDef WG_ReturnToBay;

        /// <summary>
        /// 對倒地的敵對龍騎兵右鍵後，原地在對方位置上讀條並在完成時原地使Walker分解為廢鐵與模塊，並使其離開Walker。
        /// </summary>
        public static JobDef WG_DisassembleWalkerCore;
    }
}
