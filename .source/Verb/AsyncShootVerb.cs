using RimWorld;
using Verse;
using System.Collections.Generic;
using UnityEngine;

namespace Mechsuit
{
    // 异步射击 Verb 包装器
    // 用于在不干扰 Pawn 状态机的情况下驱动远程武器
    public class AsyncShootVerb : Verb_LaunchProjectile, IAsyncShootVerb
    {
        public CompTurretGun turretComp;

        // IAsyncShootVerb 接口实现
        public CompTurretGun TurretComp 
        { 
            get => turretComp; 
            set => turretComp = value; 
        }

        public VerbState State => state;
        public new LocalTargetInfo CurrentTarget 
        { 
            get => currentTarget; 
            set => currentTarget = value; 
        }
        public new Texture2D UIIcon => verbProps.defaultProjectile?.uiIcon;

        // 获取当前是否应使用 CE 逻辑
        public static bool IsCEActive => ModsConfig.IsActive("cpeterson.combatextended") || ModLister.HasActiveModWithName("Combat Extended");

        public new ThingWithComps EquipmentSource => (ThingWithComps)turretComp?.parent;

        public override bool Available()
        {
            if (turretComp == null || !turretComp.CanShoot) return false;
            return true;
        }

        public override float EffectiveRange => verbProps.range;

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.Thing != null && targ.Thing == caster) return false;
            if (OutOfRange(root, targ, CellRect.SingleCell(root))) return false;

            ShootLine shootLine;
            return TryFindShootLineFromTo(root, targ, out shootLine);
        }

        public void AsyncVerbTick()
        {
            if (turretComp != null)
            {
                // 手动驱动 Verb 的逻辑：处理 Burst 时刻、Cooldown 等
                VerbTick();
                
                // 模拟预热逻辑（因为我们不使用 Stance_Warmup）
                if (state == VerbState.Idle && turretComp.WarmingUpInternal)
                {
                    // 状态转换由 CompTurretGun 驱动，这里仅维持 Verb 内部的一致性
                }

                if (IsCEActive)
                {
                    // 当 CE 进入射击后的冷却或空闲状态时，清除组件的瞄准标记
                    if (state != VerbState.Bursting && !WarmingUp)
                    {
                        turretComp.isAiming = false;
                    }
                }
            }
        }

        // 响应 Command_VerbTarget 的点击
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            if (turretComp != null)
            {
                // 让 Comp 处理强制攻击逻辑
                turretComp.OrderAttack(target);
            }
        }

        // 强行启动射击流程，不通过原版的 Stance 系统
        public bool TryStartAsyncCast(LocalTargetInfo targ)
        {
            if (state != VerbState.Idle) return false;
            if (!CanHitTarget(targ)) return false;

            this.currentTarget = targ;
            this.currentDestination = targ;
            
            if (WarmupTime > 0f)
            {
                // 由组件处理预热倒计时
                return true;
            }
            else
            {
                WarmupComplete();
                return true;
            }
        }

        public override void WarmupComplete()
        {
            // 模仿原版逻辑但去除副作用
            burstShotsLeft = BurstShotCount;
            state = VerbState.Bursting;
            TryCastNextBurstShot();
        }

        // 核心射击出口
        public override bool TryCastShot()
        {
            if (IsCEActive) return base.TryCastShot();

            if (currentTarget.HasThing && (currentTarget.Thing.Map != caster.Map)) 
            {
                return false;
            }

            // 动态获取当前的枪口位置（绝对坐标）
            Vector3 muzzlePos = turretComp?.TurretDrawPos ?? caster.DrawPos;
            
            Projectile projectile = (Projectile)GenSpawn.Spawn(verbProps.defaultProjectile, caster.Position, caster.Map);
            
            float missRadius = verbProps.ForcedMissRadius;
            LocalTargetInfo targ = currentTarget;
            if (missRadius > 0.5f)
            {
                int numCells = GenRadial.NumCellsInRadius(missRadius);
                targ = currentTarget.Cell + GenRadial.RadialPattern[Rand.Range(0, numCells)];
            }

            // 发射
            projectile.Launch(caster, muzzlePos, targ, currentTarget, ProjectileHitFlags.All, false, EquipmentSource);
            return true;
        }

        public bool TryDoCastShot()
        {
            if (state == VerbState.Idle)
            {
                return TryCastShot();
            }
            return false;
        }
    }
}
