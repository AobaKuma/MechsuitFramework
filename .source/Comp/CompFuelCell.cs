using RimWorld;
using RimWorld.Utility;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Exosuit
{
    // 燃料电池组件
    // 物品形态：显示百分比 Gizmo + 燃料填充设置
    // 龙门架预览：图标右上角显示百分比
    // 上机形态：消耗燃料，给驾驶员 Hediff
    // 实现 IReloadableComp 以复用整备架装填系统
    public class CompFuelCell : ThingComp, IReloadableComp
    {
        #region 常量

        private const float FuelConsumptionPerDay = 15f;
        private const float FuelConsumptionPerTick = FuelConsumptionPerDay / 60000f;

        #endregion

        #region 字段

        private float fuel;

        #endregion

        #region 属性

        public CompProperties_FuelCell Props => (CompProperties_FuelCell)props;

        public float Fuel
        {
            get => fuel;
            set => fuel = Mathf.Clamp(value, 0f, Props.fuelCapacity);
        }

        public float FuelPercent => fuel / Props.fuelCapacity;

        public float FuelPercentArbitrary => Props.fuelCapacity > 0 ? fuel / Props.fuelCapacity : 0f;

        public bool HasFuel => fuel > 0f;

        public bool IsWorn => parent is Apparel apparel && apparel.Wearer != null;

        public Pawn Wearer => parent is Apparel apparel ? apparel.Wearer : null;

        // 检查是否在外骨骼上（作为模块安装）
        public bool IsOnExosuit
        {
            get
            {
                if (Wearer == null) return false;
                return Wearer.TryGetExosuitCore(out _);
            }
        }

        #endregion

        #region 重写方法

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            // 新创建的燃料电池默认满燃料
            if (!respawningAfterLoad && fuel <= 0f)
            {
                fuel = Props.fuelCapacity;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref fuel, "fuel", Props.fuelCapacity);
        }

        public override void CompTick()
        {
            base.CompTick();
            
            if (!IsOnExosuit) return;
            
            // 消耗燃料
            if (HasFuel)
            {
                fuel -= FuelConsumptionPerTick;
                
                // 确保 Hediff 存在
                EnsureHediff(Wearer);
                
                // 燃料耗尽
                if (!HasFuel)
                {
                    RemoveHediff(Wearer);
                }
            }
        }

        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            
            // 穿上时如果有燃料，添加 Hediff
            if (HasFuel && IsOnExosuit)
            {
                EnsureHediff(pawn);
            }
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            
            // 脱下时移除 Hediff
            RemoveHediff(pawn);
        }

        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            foreach (var gizmo in base.CompGetWornGizmosExtra())
            {
                yield return gizmo;
            }

            // 上机形态显示燃料百分比 Gizmo
            if (IsOnExosuit)
            {
                yield return new Gizmo_FuelCell(this);
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }

            // 物品形态显示燃料百分比 Gizmo
            if (!IsWorn)
            {
                yield return new Gizmo_FuelCell(this);
            }
        }

        public override string CompInspectStringExtra()
        {
            return "WG_FuelCell_Fuel".Translate() + ": " + FuelPercent.ToStringPercent();
        }

        #endregion

        #region IReloadableComp 实现

        public Thing ReloadableThing => parent;

        public ThingDef AmmoDef => Props.fuelDef;

        public int BaseReloadTicks => 60;

        // 将燃料容量视为最大充能次数
        public int MaxCharges => Mathf.RoundToInt(Props.fuelCapacity);

        // 当前燃料值视为剩余充能
        public int RemainingCharges
        {
            get => Mathf.RoundToInt(fuel);
            set => fuel = value;
        }

        public string LabelRemaining => $"{RemainingCharges} / {MaxCharges}";

        public bool NeedsReload(bool allowForceReload)
        {
            if (Props.fuelDef == null) return false;
            // 允许强制装填时，只要不满就返回true
            if (allowForceReload) return RemainingCharges < MaxCharges;
            // 否则只有完全空了才需要装填
            return RemainingCharges == 0;
        }

        public int MinAmmoNeeded(bool allowForcedReload)
        {
            if (!NeedsReload(allowForcedReload)) return 0;
            return 1;
        }

        public int MaxAmmoNeeded(bool allowForcedReload)
        {
            if (!NeedsReload(allowForcedReload)) return 0;
            return MaxCharges - RemainingCharges;
        }

        public int MaxAmmoAmount()
        {
            return MaxCharges;
        }

        public void ReloadFrom(Thing ammo)
        {
            if (!NeedsReload(true)) return;
            if (ammo?.def != Props.fuelDef) return;

            int needed = MaxCharges - RemainingCharges;
            int toConsume = Mathf.Min(ammo.stackCount, needed);
            
            if (toConsume <= 0) return;

            ammo.SplitOff(toConsume).Destroy();
            fuel += toConsume * Props.fuelPerUnit;
        }

        public bool CanBeUsed(out string reason)
        {
            reason = "";
            return false; // 燃料电池不是主动使用的
        }

        public string DisabledReason(int minNeeded, int maxNeeded) => "";

        #endregion

        #region 私有方法

        private void EnsureHediff(Pawn pawn)
        {
            if (pawn == null || Props.hediffDef == null) return;
            if (pawn.health.hediffSet.HasHediff(Props.hediffDef)) return;
            
            pawn.health.AddHediff(Props.hediffDef);
        }

        private void RemoveHediff(Pawn pawn)
        {
            if (pawn == null || Props.hediffDef == null) return;
            
            var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(Props.hediffDef);
            if (hediff != null)
            {
                pawn.health.RemoveHediff(hediff);
            }
        }

        #endregion
    }

    public class CompProperties_FuelCell : CompProperties
    {
        public float fuelCapacity = 150f;
        public ThingDef fuelDef;
        public float fuelPerUnit = 1f; // 每单位燃料物品提供多少燃料
        public HediffDef hediffDef;

        public CompProperties_FuelCell()
        {
            compClass = typeof(CompFuelCell);
        }
    }
}
