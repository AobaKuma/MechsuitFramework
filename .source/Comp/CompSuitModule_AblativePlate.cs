// 当白昼倾坠之时
using RimWorld;
using Verse;

namespace Exosuit
{
    /// <summary>
    /// CompSuitModule_Ejectable 的示範實作:燒蝕裝甲板。
    /// 承受傷害至耐久用盡後自動拋離機體,拋出的裝甲板回復一半損耗,
    /// 可回收修理後重新安裝。
    /// </summary>
    public class CompSuitModule_AblativePlate : CompSuitModule_Ejectable
    {
        protected override void PreEject(Pawn pawn)
        {
            if (pawn.MapHeld == null) return;
            FleckMaker.ThrowMicroSparks(pawn.DrawPos, pawn.MapHeld);
            FleckMaker.ThrowSmoke(pawn.DrawPos, pawn.MapHeld, 1.5f);
        }

        protected override void OnEjected(Thing item, Pawn pawn)
        {
            // 拋出的板材標記為待回收
            if (item != null && item.Spawned && pawn.Faction == Faction.OfPlayer)
            {
                item.SetForbidden(false, warnOnFail: false);
            }
        }
    }
}
