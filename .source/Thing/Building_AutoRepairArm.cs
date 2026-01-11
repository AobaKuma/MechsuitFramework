using RimWorld;
using Verse;

namespace Exosuit
{
    // 自动维修臂 - 可以自动维修指向的龙门架上的外骨骼
    // 支持 1.6 的 Dynamic Tick Rate 机制
    public class Building_AutoRepairArm : Building
    {
        #region 常量

        private const int RepairIntervalMin = 300; // 最小间隔 5 秒 (60 ticks/秒 * 5)
        private const int RepairIntervalMax = 480; // 最大间隔 8 秒 (60 ticks/秒 * 8)
        private const int RepairAmount = 10; // 每次维修 10 点耐久
        private const float IdlePowerConsumption = 50f;
        private const float WorkingPowerConsumption = 500f;
        private const int EffectDuration = 60; // 特效持续 60 ticks (1 秒)

        #endregion

        #region 字段

        private Building_MaintenanceBay cachedTargetBay;
        private bool cacheValid;
        private int ticksUntilRepair; // 距离下次维修的剩余 ticks
        private Effecter repairEffecter; // 维修特效
        private int effectTicksRemaining; // 特效剩余 ticks

        #endregion

        #region 属性

        // 获取指向的龙门架
        public Building_MaintenanceBay TargetBay
        {
            get
            {
                if (!cacheValid)
                {
                    cachedTargetBay = FindTargetBay();
                    cacheValid = true;
                }
                return cachedTargetBay;
            }
        }

        // 是否有电力（如果有电力组件）
        private new CompPowerTrader PowerComp => GetComp<CompPowerTrader>();
        
        private bool HasPower
        {
            get
            {
                var powerComp = PowerComp;
                return powerComp == null || powerComp.PowerOn;
            }
        }

        // 是否可以工作（自动维修臂不依赖 autoRepair 标志）
        public bool CanWork => Spawned && HasPower && TargetBay != null && TargetBayNeedRepair;
        
        // 检查目标龙门架是否需要维修（绕过 autoRepair 检查）
        private bool TargetBayNeedRepair
        {
            get
            {
                if (TargetBay == null) return false;
                if (!TargetBay.HasGearCore) return false;
                return TargetBay.Core.Damaged;
            }
        }

        #endregion

        #region 重写方法

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            InvalidateCache();
            
            // 如果是新生成的，设置初始维修间隔
            if (!respawningAfterLoad)
            {
                ticksUntilRepair = Rand.RangeInclusive(RepairIntervalMin, RepairIntervalMax);
            }
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            base.DeSpawn(mode);
            InvalidateCache();
            
            // 清理特效
            repairEffecter?.Cleanup();
            repairEffecter = null;
        }

        public override void Tick()
        {
            base.Tick();
            TickInterval(1);
        }
        
        // 支持 1.6 Dynamic Tick Rate
        // 当建筑不在玩家视野内时，delta 会大于 1
        public override void TickInterval(int delta)
        {
            if (!Spawned || !HasPower) return;
            
            // 根据工作状态调整功耗
            UpdatePowerConsumption();
            
            // 处理特效
            if (effectTicksRemaining > 0)
            {
                effectTicksRemaining -= delta;
                if (effectTicksRemaining > 0 && TargetBay != null)
                {
                    repairEffecter?.EffectTick(TargetBay, TargetBay);
                }
                else
                {
                    repairEffecter?.Cleanup();
                    repairEffecter = null;
                }
            }
            
            if (!CanWork) return;
            
            // 累积时间，检查是否到达维修时间
            ticksUntilRepair -= delta;
            if (ticksUntilRepair <= 0)
            {
                DoRepair();
                // 设置下次维修时间（随机 5-8 秒）
                ticksUntilRepair = Rand.RangeInclusive(RepairIntervalMin, RepairIntervalMax);
            }
        }
        
        // 执行维修
        private void DoRepair()
        {
            TargetBay.Repair(RepairAmount);
            
            // 在目标位置播放维修特效
            if (repairEffecter == null)
            {
                repairEffecter = EffecterDefOf.ConstructMetal.Spawn();
            }
            effectTicksRemaining = EffectDuration;
        }
        
        // 更新功耗
        private void UpdatePowerConsumption()
        {
            var powerComp = PowerComp;
            if (powerComp == null) return;
            
            float targetPower = CanWork ? WorkingPowerConsumption : IdlePowerConsumption;
            powerComp.PowerOutput = -targetPower;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksUntilRepair, "ticksUntilRepair", RepairIntervalMin);
        }

        public override string GetInspectString()
        {
            string baseStr = base.GetInspectString();
            
            if (TargetBay == null)
            {
                return baseStr + "\n" + "WG_AutoRepair_NoTarget".Translate();
            }
            
            if (!TargetBay.HasGearCore)
            {
                return baseStr + "\n" + "WG_AutoRepair_NoCore".Translate();
            }
            
            if (!TargetBayNeedRepair)
            {
                return baseStr + "\n" + "WG_AutoRepair_NoDamage".Translate();
            }
            
            return baseStr + "\n" + "WG_AutoRepair_Working".Translate();
        }

        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            
            // 绘制目标格子的高亮圈
            IntVec3 targetCell = GetTargetCell();
            if (targetCell.InBounds(Map))
            {
                GenDraw.DrawTargetHighlight(targetCell);
            }
            
            // 如果检测到目标龙门架，绘制连接线
            if (TargetBay != null)
            {
                GenDraw.DrawLineBetween(this.TrueCenter(), TargetBay.TrueCenter(), SimpleColor.Green);
            }
        }

        #endregion

        #region 公共方法

        public void InvalidateCache()
        {
            cacheValid = false;
            cachedTargetBay = null;
        }

        #endregion

        #region 私有方法

        // 查找指向的龙门架
        private Building_MaintenanceBay FindTargetBay()
        {
            if (!Spawned) return null;
            
            // 获取建筑朝向的目标格子
            IntVec3 targetCell = GetTargetCell();
            
            // 在目标区域查找龙门架（考虑龙门架是 3x3 的）
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    IntVec3 checkCell = targetCell + new IntVec3(dx, 0, dz);
                    if (!checkCell.InBounds(Map)) continue;
                    
                    var bay = checkCell.GetFirstThing<Building_MaintenanceBay>(Map);
                    if (bay != null) return bay;
                }
            }
            
            return null;
        }

        // 获取指向的目标格子（建筑宽面方向）
        private IntVec3 GetTargetCell()
        {
            // 建筑是 3x1，宽面是 3 格的那一边
            // 旋转后，FacingCell 指向建筑的"前方"
            IntVec3 offset = Rotation.FacingCell * 2; // 向前 2 格
            return Position + offset;
        }

        #endregion
    }
}
