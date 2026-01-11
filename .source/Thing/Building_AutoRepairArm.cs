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
        private int ticksUntilRepair;
        private int ticksUntilCacheRefresh;
        private Effecter repairEffecter;
        private int effectTicksRemaining;
        
        private const int CacheRefreshInterval = 250;

        #endregion

        #region 属性

        // 获取指向的龙门架
        public Building_MaintenanceBay TargetBay
        {
            get
            {
                // 检查缓存是否仍然有效
                if (cacheValid && cachedTargetBay != null && !cachedTargetBay.Spawned)
                    cacheValid = false;
                
                // 定期刷新缓存以检测新建的龙门架
                if (cacheValid && cachedTargetBay == null && ticksUntilCacheRefresh <= 0)
                {
                    cacheValid = false;
                    ticksUntilCacheRefresh = CacheRefreshInterval;
                }
                
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
        public override void TickInterval(int delta)
        {
            if (!Spawned || !HasPower) return;
            
            // 更新缓存刷新计时器
            ticksUntilCacheRefresh -= delta;
            
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
            
            // 前方一格
            IntVec3 frontCell = Position + Rotation.FacingCell;
            if (!frontCell.InBounds(Map)) return null;
            
            var bay = frontCell.GetFirstThing<Building_MaintenanceBay>(Map);
            if (bay == null) return null;
            
            // 检查是否对准龙门架边缘中间
            IntVec3 bayCenter = bay.Position;
            bool aligned = (Rotation == Rot4.North || Rotation == Rot4.South) 
                ? Position.x == bayCenter.x 
                : Position.z == bayCenter.z;
            
            return aligned ? bay : null;
        }

        #endregion
    }
}
