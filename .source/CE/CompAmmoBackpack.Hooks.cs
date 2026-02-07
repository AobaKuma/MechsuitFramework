// 当白昼倾坠之时
using RimWorld;
using UnityEngine;
using Verse;
using CombatExtended;

namespace Exosuit.CE
{
    [StaticConstructorOnStartup]
    public partial class CompAmmoBackpack
    {
        static CompAmmoBackpack()
        {
            // 注册机兵生成后的回调
            PawnGenerator_Patch.OnPostGenerate += HostileInitializationHook;
        }

        private static void HostileInitializationHook(Pawn pawn, Exosuit_Core core)
        {
            // 自动填充敌方弹药
            if (pawn.Faction != null && pawn.Faction.IsPlayer) return;

            foreach (var apparel in pawn.apparel.WornApparel)
            {
                apparel.TryGetComp<CompAmmoBackpack>()?.InitializeHostileAmmo();
                apparel.TryGetComp<CompTurretAmmo>()?.InitializeHostileAmmo();
            }
        }

        // 实现回收残骸弹药接口
        public void OnExosuitDestroyed(Building_Wreckage wreckage)
        {
            if (!HasAmmo) return;

            // 计算随机回收弹药量
            int recoverAmount = Mathf.RoundToInt(TotalAmmoCount * Rand.Range(0.1f, 0.3f));
            if (recoverAmount <= 0) return;

            AmmoDef ammoToRecover = LastConsumedAmmo ?? CurrentFireAmmo ?? SelectedAmmo;
            if (ammoToRecover == null) return;

            var ammoThing = ThingMaker.MakeThing(ammoToRecover);
            ammoThing.stackCount = recoverAmount;

            // 放入残骸容器
            wreckage.moduleContainer.Add(ammoThing);
            
            Log.Message($"[AmmoBackpack] 机兵残骸回收弹药: {ammoToRecover.LabelCap} x{recoverAmount}");

            // 清理背包弹药数据
            currentAmmoCount = 0;
            if (isMixMode && mixEntries != null)
            {
                foreach (var entry in mixEntries) entry.CurrentCount = 0;
            }
        }
    }
}
