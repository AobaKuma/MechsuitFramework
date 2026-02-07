// 当白昼倾坠之时
using System;
using CombatExtended;
using Mechsuit;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Exosuit.CE
{
    // CE 异步射击 Verb
    // 继承 Verb_ShootCE 以获得完整的 CE 弹药系统支持
    // 覆盖关键方法以支持异步射击
    public class AsyncShootVerbCE : Verb_ShootCE, IAsyncShootVerb
    {
        public Mechsuit.CompTurretGun turretComp;

        // IAsyncShootVerb 接口实现
        public Mechsuit.CompTurretGun TurretComp 
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

        public new ThingWithComps EquipmentSource => turretComp?.parent as ThingWithComps;
        
        // 获取炮塔弹药组件
        public CompTurretAmmo TurretAmmoComp => (turretComp?.parent as Apparel)?.TryGetComp<CompTurretAmmo>();

        public override bool Available()
        {
            if (turretComp == null || !turretComp.CanShoot) return false;
            
            // 跳过base.Available中的Pawn状态检查
            if (Projectile == null) return false;
            if (VerbPropsCE?.disallowedProjectileDefs?.Contains(Projectile) ?? false) return false;
            
            return true;
        }

        public override float EffectiveRange => verbProps.range;

        // 覆盖 Projectile 以支持混装弹药和特种弹药
        public override ThingDef Projectile
        {
            get
            {
                var turretAmmo = TurretAmmoComp;
                if (turretAmmo != null)
                {
                    var proj = turretAmmo.CurAmmoProjectile;
                    if (proj != null) return proj;
                }
                return base.Projectile;
            }
        }
        
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.Thing != null && targ.Thing == caster) return false;
            if (OutOfRange(root, targ, CellRect.SingleCell(root))) return false;

            ShootLine shootLine;
            return TryFindShootLineFromTo(root, targ, out shootLine);
        }

        // 异步 Tick 驱动 - 完全绕过 Pawn 姿态系统
        public void AsyncVerbTick()
        {
            if (turretComp == null) return;
            
            // 自定义 burst 处理 - 避免调用原版 VerbTick 影响 Pawn
            if (state == VerbState.Bursting)
            {
                if (!caster.Spawned)
                {
                    Reset();
                    return;
                }
                
                ticksToNextBurstShot--;
                if (ticksToNextBurstShot <= 0)
                {
                    AsyncTryCastNextBurstShot();
                }
            }
            
            // 当进入冷却或空闲状态时清除瞄准标记
            if (state != VerbState.Bursting && !WarmingUp)
            {
                turretComp.isAiming = false;
            }
        }
        
        // 自定义的 burst shot 逻辑 - 不影响 Pawn 状态
        private void AsyncTryCastNextBurstShot()
        {
            if (Available() && TryCastShot())
            {
                // 播放音效
                if (verbProps.muzzleFlashScale > 0.01f)
                {
                    Vector3 muzzlePos = turretComp?.TurretLocation ?? caster.DrawPos;
                    FleckMaker.Static(muzzlePos.ToIntVec3(), caster.Map, FleckDefOf.ShotFlash, verbProps.muzzleFlashScale);
                }
                if (verbProps.soundCast != null)
                {
                    verbProps.soundCast.PlayOneShot(new TargetInfo(caster.Position, caster.MapHeld));
                }
                if (verbProps.soundCastTail != null)
                {
                    verbProps.soundCastTail.PlayOneShotOnCamera(caster.Map);
                }
                
                // 注意：不调用 CasterPawn.stances.SetStance - 这是关键！
                
                burstShotsLeft--;
            }
            else
            {
                burstShotsLeft = 0;
            }
            
            if (burstShotsLeft > 0)
            {
                ticksToNextBurstShot = TicksBetweenBurstShots;
            }
            else
            {
                state = VerbState.Idle;
                // 不设置 Pawn 的 Stance_Cooldown
            }
        }

        // 响应 Command_VerbTarget 的点击
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            if (turretComp != null)
            {
                turretComp.OrderAttack(target);
            }
        }

        // 强行启动射击流程
        public bool TryStartAsyncCast(LocalTargetInfo targ)
        {
            if (state != VerbState.Idle) return false;
            if (!CanHitTarget(targ)) return false;

            this.currentTarget = targ;
            this.currentDestination = targ;
            
            if (WarmupTime > 0f)
            {
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
            // 跳过CE的瞄准模式处理，直接进入射击
            numShotsFired = 0;
            burstShotsLeft = ShotsPerBurst;
            state = VerbState.Bursting;
            
            // 使用自定义方法发射首发 - 不影响 Pawn 状态
            AsyncTryCastNextBurstShot();
            
            // 记录战斗日志
            var shooter = turretComp?.PawnOwner ?? caster;
            Find.BattleLog.Add(
                new BattleLogEntry_RangedFire(
                    shooter,
                    (!currentTarget.HasThing) ? null : currentTarget.Thing,
                    EquipmentSource?.def,
                    Projectile,
                    VerbPropsCE.burstShotCount > 1)
            );
        }

        public override bool TryCastShot()
        {
            var turretAmmo = TurretAmmoComp;
            
            // 检查弹药
            if (turretAmmo != null)
            {
                if (!turretAmmo.HasAmmoAvailable)
                {
                    // 尝试从弹药背包获取
                    if (!turretAmmo.AllowBackpackFeed)
                    {
                        return false;
                    }
                    var backpack = CEPatches_Turret.GetBackpackForTurret(turretAmmo);
                    if (backpack == null || !backpack.HasAmmo)
                    {
                        return false;
                    }
                }
            }
            
            // 调用内部射击逻辑
            if (TryCastShotInternal())
            {
                return OnCastSuccessfulAsync();
            }
            return false;
        }

        // 内部射击逻辑
        private bool TryCastShotInternal()
        {
            if (currentTarget.HasThing && currentTarget.Thing.Map != caster.Map)
            {
                return false;
            }

            // 使用CE的ShiftTarget计算弹道
            ShiftVecReport report = ShiftVecReportFor(currentTarget);
            if (report == null)
            {
                return false;
            }

            bool instant = Projectile?.projectile is ProjectilePropertiesCE pprop && pprop.isInstant;
            ShiftTarget(report, calculateMechanicalOnly: false, isInstant: instant);

            // 发射弹丸
            if (Projectile == null)
            {
                Log.Warning($"[MF_CE] AsyncShootVerbCE: Projectile is null");
                return false;
            }

            var launcher = turretComp?.PawnOwner ?? caster;
            
            // 动态获取炮塔位置
            Vector3 turretPos = turretComp?.TurretLocation ?? caster.DrawPos;
            IntVec3 spawnCell = turretPos.ToIntVec3();
            
            // 确保生成位置在地图内
            if (!spawnCell.InBounds(caster.Map))
            {
                spawnCell = caster.Position;
            }
            
            float shotHeight = ShotHeight;

            ProjectileCE projectile = (ProjectileCE)ThingMaker.MakeThing(Projectile);
            GenSpawn.Spawn(projectile, spawnCell, caster.Map);
            
            projectile.Launch(
                launcher,
                new Vector2(turretPos.x, turretPos.z),
                shotAngle,
                shotRotation,
                shotHeight,
                ShotSpeed,
                EquipmentSource
            );

            return true;
        }

        // 射击成功后的处理
        protected bool OnCastSuccessfulAsync()
        {
            var pawnOwner = turretComp?.PawnOwner;
            if (pawnOwner != null)
            {
                pawnOwner.records.Increment(RecordDefOf.ShotsFired);
            }

            // 消耗弹药
            var turretAmmo = TurretAmmoComp;
            if (turretAmmo != null)
            {
                int ammoConsumedPerShot = VerbPropsCE?.ammoConsumedPerShotCount ?? 1;
                
                // 优先从炮塔弹仓消耗
                if (turretAmmo.TryConsumeAmmo(ammoConsumedPerShot))
                {
                    return true;
                }
                
                // 如果允许弹药箱供弹，从弹药箱消耗
                if (turretAmmo.AllowBackpackFeed)
                {
                    var backpack = CEPatches_Turret.GetBackpackForTurret(turretAmmo);
                    if (backpack != null && backpack.HasAmmo)
                    {
                        if (backpack.IsMixMode)
                        {
                            backpack.TryConsumeMixAmmoWithType(ammoConsumedPerShot, out _);
                        }
                        else if (backpack.CurrentAmmoCount >= ammoConsumedPerShot)
                        {
                            backpack.CurrentAmmoCount -= ammoConsumedPerShot;
                        }
                    }
                }
            }

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

        // 覆盖ShotHeight以使用炮塔位置
        public override float ShotHeight
        {
            get
            {
                if (turretComp != null)
                {
                    return TurretShotHeight;
                }
                return base.ShotHeight;
            }
        }
        
        // 炮塔发射高度
        private float TurretShotHeight
        {
            get
            {
                if (turretComp == null) return 1.5f;
                // 使用固定高度避免Y轴计算问题
                return 1.5f;
            }
        }
        
        // 炮塔发射位置 (XZ平面)
        private Vector3 TurretSourcePos => turretComp?.TurretLocation ?? caster.TrueCenter();
        
        // 覆盖 ShotAngle 使用炮塔位置
        protected override float ShotAngle(Vector3 source, Vector3 targetPos)
        {
            // 如果有炮塔组件，用炮塔位置替换source
            if (turretComp != null)
            {
                var turretPos = TurretSourcePos;
                source = turretPos.WithY(TurretShotHeight);
            }
            return projectilePropsCE.TrajectoryWorker.ShotAngle(projectilePropsCE, source, targetPos, ShotSpeed);
        }
        
        // 覆盖 ShotRotation 使用炮塔位置
        protected override float ShotRotation(Vector3 source, Vector3 targetPos)
        {
            // 如果有炮塔组件，用炮塔位置替换source
            if (turretComp != null)
            {
                source = TurretSourcePos;
            }
            return projectilePropsCE.TrajectoryWorker.ShotRotation(projectilePropsCE, source, targetPos);
        }
    }
}

