// 当白昼倾坠之时
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Exosuit
{
    /// <summary>
    /// 可脫離式外骨骼模塊抽象類別。
    /// 耐久「用盡」後自動從機體脫離,回復耐久損耗的一半(recoverFraction 可調),
    /// 並經 Conversion 依品質重新計算耐久度後以物品形態彈出。
    /// 子類必須實作 OnEjected() 定義彈出後的表現。
    /// </summary>
    public abstract class CompSuitModule_Ejectable : CompSuitModule
    {
        public CompProperties_EjectableModule EProps => props as CompProperties_EjectableModule;

        private bool ejectQueued;

        /// <summary>
        /// 「用盡」判定。預設為耐久到達下限
        /// (框架 ApplyDamageToModules 分配傷害時不會低於 1)。
        /// 子類可改寫為彈藥耗盡、燃料耗盡等條件。
        /// </summary>
        protected virtual bool Depleted => HP <= DepletionFloor;

        /// <summary>耐久用盡的判定下限。</summary>
        protected virtual int DepletionFloor => 1;

        /// <summary>僅在穿戴於裝備核心的 Pawn 身上時允許脫離。</summary>
        protected bool CanEject => parent is Apparel { Wearer: not null } a && a.Wearer.HasCore(out _);

        public override int HP
        {
            get => base.HP;
            set
            {
                base.HP = value;
                CheckDepletion();
            }
        }

        /// <summary>
        /// 檢查是否用盡並排程脫離。
        /// 子類在自訂消耗(彈藥、燃料等)後可手動呼叫。
        /// </summary>
        public void CheckDepletion()
        {
            if (ejectQueued || !Depleted || !CanEject) return;
            ejectQueued = true;
            // 傷害分配本身在 ExecuteWhenFinished 中執行,
            // 延遲到當前流程結束再脫離,避免在遍歷穿戴列表時修改它
            LongEventHandler.ExecuteWhenFinished(EjectAndRecover);
        }

        /// <summary>
        /// 執行脫離:回復損耗的一半 → 卸下並重算整機耐久 → 轉換為物品形態丟出。
        /// </summary>
        protected virtual void EjectAndRecover()
        {
            if (!CanEject || !Depleted) // 期間可能已被修理或卸除
            {
                ejectQueued = false;
                return;
            }

            var apparel = (Apparel)parent;
            Pawn pawn = apparel.Wearer;
            pawn.HasCore(out Exosuit_Core core);

            // 回復耐久損耗的一半(或 recoverFraction 指定比例)
            int loss = MaxHP - HP;
            HP = Mathf.Clamp(HP + Mathf.CeilToInt(loss * EProps.recoverFraction), 1, MaxHP);

            PreEject(pawn);

            // 從機體脫離,並重新計算整機耐久度
            pawn.apparel.Remove(apparel);
            core?.ModuleRecache();

            // Conversion 會依品質重新換算耐久、銷毀裝備形態並產生物品形態
            Thing item = this.Conversion();
            if (item != null)
            {
                if (pawn.MapHeld != null)
                {
                    GenPlace.TryPlaceThing(item, pawn.PositionHeld, pawn.MapHeld, ThingPlaceMode.Near);
                }
                else
                {
                    pawn.inventory?.innerContainer.TryAdd(item);
                }
            }

            if (pawn.MapHeld != null)
            {
                EProps.ejectSound?.PlayOneShot(new TargetInfo(pawn.PositionHeld, pawn.MapHeld));
            }
            if (EProps.showEjectMessage && pawn.Faction == Faction.OfPlayer)
            {
                Messages.Message("MF_ModuleEjected".Translate(parent.LabelShort.CapitalizeFirst(), pawn.LabelShort),
                    pawn, MessageTypeDefOf.NegativeEvent);
            }

            OnEjected(item, pawn);
        }

        /// <summary>脫離前呼叫(模塊仍在穿戴狀態),可用於特效。</summary>
        protected virtual void PreEject(Pawn pawn) { }

        /// <summary>脫離後呼叫。item 為彈出的物品形態(可能為 null),pawn 為原穿戴者。</summary>
        protected abstract void OnEjected(Thing item, Pawn pawn);

        public override void PostExposeData()
        {
            base.PostExposeData();
            // 讀檔後若已處於用盡狀態,重新排程脫離
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                CheckDepletion();
            }
        }
    }

    public class CompProperties_EjectableModule : CompProperties_ExosuitModule
    {
        /// <summary>脫離時回復的耐久損耗比例 (0~1),預設回復一半。</summary>
        public float recoverFraction = 0.5f;

        /// <summary>脫離時是否對玩家顯示訊息。</summary>
        public bool showEjectMessage = true;

        /// <summary>脫離音效(可選)。</summary>
        public SoundDef ejectSound;

        public CompProperties_EjectableModule()
        {
            // 抽象組件:XML 必須指定繼承 CompSuitModule_Ejectable 的具體 compClass
            compClass = null;
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string e in base.ConfigErrors(parentDef))
            {
                yield return e;
            }
            if (compClass == null || !typeof(CompSuitModule_Ejectable).IsAssignableFrom(compClass))
            {
                yield return "CompProperties_EjectableModule requires a compClass derived from CompSuitModule_Ejectable";
            }
            if (recoverFraction < 0f || recoverFraction > 1f)
            {
                yield return "recoverFraction must be within [0, 1]";
            }
        }
    }
}
