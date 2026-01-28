// 当白昼倾坠之时
using RimWorld;
using System.Collections.Generic;
using Verse.AI;
using Verse;
using System.Linq;
using UnityEngine;
using Verse.Sound;

namespace Mechsuit
{
    public class CompProperties_TurretGun : CompProperties
    {
        public ThingDef turretDef;

        public float angleOffset;

        public bool autoAttack = true;

        public bool attackUndrafted = true;

        public List<PawnRenderNodeProperties> renderNodeProperties;
        public float cooldownTimeOverride = -1;

        public float turnSpeed = 2f;
        public float minAngle = 0f;
        public float maxAngle = 0f;

        public CompProperties_TurretGun()
        {
            compClass = typeof(CompTurretGun);
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            if (renderNodeProperties.NullOrEmpty())
            {
                yield break;
            }
            foreach (PawnRenderNodeProperties renderNodeProperty in renderNodeProperties)
            {
                if (!typeof(PawnRenderNode_TurretGun).IsAssignableFrom(renderNodeProperty.nodeClass))
                {
                    yield return "contains nodeClass which is not PawnRenderNode_TurretGun or subclass thereof.";
                }
            }
        }
    }

    public class CompTurretGun : ThingComp, IAttackTargetSearcher, IVerbOwner
    {
        private const int StartShootIntervalTicks = 10;

        private static readonly CachedTexture ToggleTurretIcon = new("UI/Gizmos/ToggleTurret");

        public static Dictionary<Thing, CompTurretGun> subGunRegistry = new();

        public Thing gun;

        protected int burstCooldownTicksLeft;

        protected int burstWarmupTicksLeft;

        public LocalTargetInfo currentTarget = LocalTargetInfo.Invalid;

        private bool fireAtWill = true;

        private LocalTargetInfo forcedTarget = LocalTargetInfo.Invalid;

        public bool isAiming;

        private LocalTargetInfo lastAttackedTarget = LocalTargetInfo.Invalid;

        private int lastAttackTargetTick;

        public float curRotation;

        public Thing Thing => PawnOwner;

        public CompProperties_TurretGun Props => (CompProperties_TurretGun)props;

        public Verb CurrentEffectiveVerb => AttackVerb;

        public LocalTargetInfo LastAttackedTarget => lastAttackedTarget;

        public int LastAttackTargetTick => lastAttackTargetTick;

        // 探测起点
        public IntVec3 SearchRoot => PawnOwner?.Position ?? parent.Position;

        public CompEquippable GunCompEq => gun?.TryGetComp<CompEquippable>();

        public Verb AttackVerb
        {
            get
            {
                var eq = GunCompEq;
                if (eq != null)
                {
                    var verb = eq.PrimaryVerb as IAsyncShootVerb;
                    return verb as Verb;
                }

                // 从追踪器查找可用 Verb
                var asyncVerb = VerbTracker.AllVerbs.OfType<IAsyncShootVerb>().FirstOrDefault();
                return asyncVerb as Verb;
            }
        }

        #region IVerbOwner Implementation
        private VerbTracker verbTracker;
        public VerbTracker VerbTracker
        {
            get
            {
                if (verbTracker == null)
                {
                    verbTracker = new VerbTracker(this);
                }
                return verbTracker;
            }
        }

        public List<VerbProperties> VerbProperties => gun?.def?.verbs ?? new List<VerbProperties>();

        public List<Tool> Tools => gun?.def?.tools ?? new List<Tool>();

        public ImplementOwnerTypeDef ImplementOwnerTypeDef => ImplementOwnerTypeDefOf.NativeVerb;

        public Thing ConstantCaster => PawnOwner;

        public string UniqueVerbOwnerID() => "MF_Turret_" + parent.thingIDNumber + "_" + (gun != null ? gun.thingIDNumber.ToString() : "none");

        public bool VerbsStillUsableBy(Pawn p) => p != null && !p.Dead;

        #endregion

        public bool WarmingUpInternal => burstWarmupTicksLeft > 0;

        private bool IsApparel => parent is Apparel;

        public Pawn PawnOwner
        {
            get
            {
                if (!(parent is Apparel { Wearer: var wearer }))
                {
                    if (parent is Pawn result)
                    {
                        return result;
                    }
                    return null;
                }
                return wearer;
            }
        }
        public bool CanShoot
        {
            get
            {
                if (PawnOwner != null)
                {
                    if (!PawnOwner.Spawned || PawnOwner.Downed || PawnOwner.Dead || !PawnOwner.Awake())
                    {
                        return false;
                    }
                    if (PawnOwner.IsPlayerControlled && PawnOwner.Drafted)
                    {
                        // 检查征召模式射击许可
                        return fireAtWill && PawnOwner.drafter.FireAtWill;
                    }
                    if (!PawnOwner.Drafted && !Props.attackUndrafted)
                    {
                        return false;
                    }
                    if (PawnOwner.stances.stunner.Stunned)
                    {
                        return false;
                    }
                    if (TurretDestroyed)
                    {
                        return false;
                    }
                    if (!fireAtWill)
                    {
                        return false;
                    }
                    CompCanBeDormant compCanBeDormant = PawnOwner.TryGetComp<CompCanBeDormant>();
                    if (compCanBeDormant != null && !compCanBeDormant.Awake)
                    {
                        return false;
                    }
                    var verb = AttackVerb as IAsyncShootVerb;
                    if (verb == null)
                    {
                        Log.WarningOnce($"[MF炮塔] {parent.def.defName} AttackVerb 为空或不是 IAsyncShootVerb", parent.thingIDNumber);
                        return false;
                    }
                    return true;
                }
                return false;
            }
        }

        public bool TurretDestroyed
        {
            get
            {
                var verb = AttackVerb;
                if (!IsApparel && verb != null && verb.verbProps.linkedBodyPartsGroup != null && verb.verbProps.ensureLinkedBodyPartsGroupAlwaysUsable && PawnCapacityUtility.CalculateNaturalPartsAverageEfficiency(PawnOwner.health.hediffSet, verb.verbProps.linkedBodyPartsGroup) <= 0f)
                {
                    return true;
                }
                return false;
            }
        }

        public bool AutoAttack => Props.autoAttack;
        
        public bool HasForcedTarget => forcedTarget.IsValid;
        
        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            MakeGun();
            ResetRotation();
        }

        public void ResetRotation()
        {
            if (PawnOwner == null) return;
            curRotation = PawnOwner.Rotation.AsAngle + Props.angleOffset - 90f;
        }

        private void MakeGun()
        {
            if (gun != null && subGunRegistry.ContainsKey(gun)) subGunRegistry.Remove(gun);
            if (Props.turretDef != null)
            {
                gun = ThingMaker.MakeThing(Props.turretDef);
            }
            else
            {
                // 默认自身为武器来源
                gun = parent;
            }
            subGunRegistry[gun] = this;
            UpdateGunVerbs();
        }

        private void UpdateGunVerbs()
        {
            if (gun == null) return;
            
            foreach (var v in VerbTracker.AllVerbs)
            {
                if (v is IAsyncShootVerb asyncVerb)
                {
                    if (PawnOwner != null) v.caster = PawnOwner;
                    asyncVerb.TurretComp = this;
                    
                    v.castCompleteCallback = delegate
                    {
                        if (Props.cooldownTimeOverride != -1)
                        {
                            burstCooldownTicksLeft = Props.cooldownTimeOverride.SecondsToTicks();
                        }
                        else
                        {
                            burstCooldownTicksLeft = v.verbProps.defaultCooldownTime.SecondsToTicks();
                        }
                    };
                }
            }
        }

        public override void CompTick()
        {
            base.CompTick();

            // 自动重连丢失数据
            if ((VerbTracker.AllVerbs.Count == 0 || AttackVerb == null) && gun != null && (gun.def.verbs?.Count > 0))
            {
                if (parent.IsHashIntervalTick(120)) 
                {
                    Log.Message($"[MF炮塔] Tick中检测到Verb丢失，尝试重连: {parent.def.defName}");
                    UpdateGunVerbs();
                }
            }
            
            if (!CanShoot)
            {
                return;
            }
            if (currentTarget.IsValid)
            {
                float targetAngle = (currentTarget.Cell.ToVector3Shifted() - TurretLocation).AngleFlat() + Props.angleOffset - 90f;
                float finalAngle = targetAngle;

                // 检查旋转角度限制
                if (Props.minAngle != 0 || Props.maxAngle != 0)
                {
                    float pawnBaseAngle = PawnOwner.Rotation.AsAngle + Props.angleOffset - 90f;
                    float relativeTarget = Mathf.DeltaAngle(pawnBaseAngle, targetAngle);
                    float clampedRelative = Mathf.Clamp(relativeTarget, Props.minAngle, Props.maxAngle);
                    finalAngle = pawnBaseAngle + clampedRelative;
                }

                curRotation = Mathf.MoveTowardsAngle(curRotation, finalAngle, Props.turnSpeed);
            }
            else
            {
                // 扫描射程内潜在威胁
                IAttackTarget potentialTarget = null;
                if (PawnOwner.Spawned && PawnOwner.IsHashIntervalTick(60)) // 每秒扫描一次潜在威胁
                {
                    potentialTarget = AttackTargetFinder.BestShootTargetFromCurrentPosition(this, TargetScanFlags.NeedAutoTargetable | TargetScanFlags.NeedThreat | TargetScanFlags.NeedLOSToAll);
                }

                float pawnCenterAngle = PawnOwner.Rotation.AsAngle + Props.angleOffset - 90f;

                if (potentialTarget != null)
                {
                    float enemyAngle = (potentialTarget.Thing.Position.ToVector3Shifted() - TurretLocation).AngleFlat() + Props.angleOffset - 90f;
                    float finalEnemyAngle = enemyAngle;

                    // 旋转限制检查
                    if (Props.minAngle != 0 || Props.maxAngle != 0)
                    {
                        float relativeEnemy = Mathf.DeltaAngle(pawnCenterAngle, enemyAngle);
                        float clampedRelative = Mathf.Clamp(relativeEnemy, Props.minAngle, Props.maxAngle);
                        finalEnemyAngle = pawnCenterAngle + clampedRelative;
                    }
                    
                    // 独立追踪大偏角目标
                    if (Mathf.Abs(Mathf.DeltaAngle(pawnCenterAngle, finalEnemyAngle)) > 45f)
                    {
                        curRotation = Mathf.MoveTowardsAngle(curRotation, finalEnemyAngle, Props.turnSpeed);
                        return;
                    }
                }

                // 默认跟随驾驶员朝向
                curRotation = pawnCenterAngle;
            }
            if (burstCooldownTicksLeft > 0)
            {
                burstCooldownTicksLeft--;
            }

            var verb = AttackVerb as IAsyncShootVerb;
            if (verb == null)
            {
                if (PawnOwner.IsHashIntervalTick(250))
                {
                    Log.Warning($"[MF炮塔] AttackVerb 为 null! parent={parent.def.defName}, gun={gun?.def.defName}, VerbCount={VerbTracker?.AllVerbs.Count ?? 0}");
                }
                return;
            }

            // 驱动远程动作逻辑
            verb.AsyncVerbTick();

            // 连射期间锁定目标
            if (verb.State == VerbState.Bursting) return;

            // 处理预热
            if (WarmingUpInternal)
            {
                burstWarmupTicksLeft--;
                if (burstWarmupTicksLeft <= 0)
                {
                    // 对准目标后开启火控
                    if (currentTarget.IsValid)
                    {
                        float properAngle = (currentTarget.Cell.ToVector3Shifted() - TurretLocation).AngleFlat() + Props.angleOffset - 90f;
                        if (Mathf.Abs(Mathf.DeltaAngle(curRotation, properAngle)) > 20f)
                        {
                            burstWarmupTicksLeft = 1; // 强制等待对准
                            return;
                        }
                    }

                    // 预热结束开启连射
                    verb.CurrentTarget = currentTarget; // 确保 Verb 知道打谁
                    verb.WarmupComplete();
                    
                    lastAttackTargetTick = Find.TickManager.TicksGame;
                    lastAttackedTarget = currentTarget;
                }
                return;
            }

            // 检查强制目标是否仍然有效
            if (HasForcedTarget)
            {
                if (!ForcedTargetValid())
                {
                    // 强制目标失效，清除
                    forcedTarget = LocalTargetInfo.Invalid;
                    ResetCurrentTarget();
                }
                else if (verb.State == VerbState.Idle)
                {
                    // 强制目标有效，继续攻击
                    currentTarget = forcedTarget;
                    if (burstCooldownTicksLeft <= 0 && burstWarmupTicksLeft <= 0)
                    {
                        burstWarmupTicksLeft = verb.WarmupTime.SecondsToTicks();
                    }
                }
                return;
            }

            // 搜索空闲位新目标
            if (verb.State == VerbState.Idle && PawnOwner.IsHashIntervalTick(StartShootIntervalTicks))
            {
                TargetScanFlags flags = TargetScanFlags.NeedAutoTargetable | TargetScanFlags.NeedLOSToAll;
                if (!PawnOwner.Drafted) flags |= TargetScanFlags.NeedThreat;
                
                var searchResult = AttackTargetFinder.BestShootTargetFromCurrentPosition(this, flags, (Thing t) => IsTargetInAllowedArc(t));
                if (searchResult != null)
                {
                    currentTarget = (Thing)searchResult;
                    burstWarmupTicksLeft = verb.WarmupTime.SecondsToTicks();
                }
                else
                {
                    ResetCurrentTarget();
                }
            }
        }

        public bool IsTargetInAllowedArc(LocalTargetInfo target)
        {
            if (Props.minAngle == 0 && Props.maxAngle == 0) return true;
            if (!target.IsValid) return false;

            float targetAngle = (target.Cell.ToVector3Shifted() - TurretLocation).AngleFlat() + Props.angleOffset - 90f;
            float pawnBaseAngle = PawnOwner.Rotation.AsAngle + Props.angleOffset - 90f;
            float relativeTarget = Mathf.DeltaAngle(pawnBaseAngle, targetAngle);

            return relativeTarget >= Props.minAngle && relativeTarget <= Props.maxAngle;
        }


        public void OrderAttack(LocalTargetInfo target)
        {
            var verb = AttackVerb as IAsyncShootVerb;
            if (verb == null) return;
            
            // 设置强制目标以持续攻击
            forcedTarget = target;
            currentTarget = target;
            
            // 冷却结束后开始预热
            if (burstCooldownTicksLeft <= 0)
            {
                burstWarmupTicksLeft = verb.WarmupTime.SecondsToTicks();
            }
            isAiming = true;
            
            SoundDefOf.Tick_Tiny.PlayOneShot(SoundInfo.OnCamera());
        }
        
        // 检查强制目标是否仍然有效
        private bool ForcedTargetValid()
        {
            if (!forcedTarget.IsValid) return false;
            
            if (forcedTarget.HasThing)
            {
                var thing = forcedTarget.Thing;
                if (thing.Destroyed || !thing.Spawned) return false;
                if (thing.Map != PawnOwner?.Map) return false;
                if (thing is Pawn p && p.Dead) return false;
                
                // 检查是否在射程内
                var verb = AttackVerb;
                if (verb != null && !verb.CanHitTarget(forcedTarget))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        // 清除强制目标
        public void ClearForcedTarget()
        {
            forcedTarget = LocalTargetInfo.Invalid;
        }

        public override void PostPostMake()
        {
            base.PostPostMake();
            MakeGun();
            ResetRotation();
        }

        private void ResetCurrentTarget()
        {
            currentTarget = LocalTargetInfo.Invalid;
            burstWarmupTicksLeft = 0;
        }

        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            foreach (Gizmo item in base.CompGetWornGizmosExtra())
            {
                yield return item;
            }
            if (!IsApparel)
            {
                yield break;
            }
            foreach (Gizmo gizmo in GetGizmos())
            {
                yield return gizmo;
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo item in base.CompGetGizmosExtra())
            {
                yield return item;
            }
            if (IsApparel)
            {
                yield break;
            }
            foreach (Gizmo gizmo in GetGizmos())
            {
                yield return gizmo;
            }
        }

        public IEnumerable<Gizmo> GetGizmos()
        {
            var verb = AttackVerb;
            if (PawnOwner != null && PawnOwner.Faction == Faction.OfPlayer)
            {
                yield return new Command_Toggle
                {
                    defaultLabel = "WG_CommandToggleTurret".Translate(),
                    defaultDesc = "WG_CommandToggleTurretDesc".Translate(),
                    isActive = () => fireAtWill,
                    icon = ToggleTurretIcon.Texture,
                    toggleAction = delegate
                    {
                        fireAtWill = !fireAtWill;
                    }
                };

                if (PawnOwner.Drafted && verb != null)
                {
                    var asyncVerb = verb as IAsyncShootVerb;
                    yield return new Command_VerbTarget
                    {
                    defaultLabel = "WG_CommandManualAttack".Translate(Props.turretDef?.LabelCap ?? parent.LabelCap),
                    defaultDesc = "WG_CommandManualAttackDesc".Translate(),
                        icon = asyncVerb?.UIIcon ?? TexCommand.Attack,
                        verb = verb,
                        hotKey = KeyBindingDefOf.Misc4,
                        drawRadius = true
                    };
                }
                
                if (HasForcedTarget)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "WG_CommandStopAttack".Translate(),
                        defaultDesc = "WG_CommandStopAttackDesc".Translate(),
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/Halt"),
                        action = delegate
                        {
                            ClearForcedTarget();
                            ResetCurrentTarget();
                            SoundDefOf.Tick_Low.PlayOneShot(SoundInfo.OnCamera());
                        }
                    };
                }
            }

            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEBUG: Force Shoot",
                    action = () =>
                    {
                        var debugVerb = AttackVerb as IAsyncShootVerb;
                        if (debugVerb != null && currentTarget.IsValid)
                        {
                            debugVerb.TryStartAsyncCast(currentTarget);
                        }
                    }
                };
            }
        }

        public override List<PawnRenderNode> CompRenderNodes()
        {
            if (!Props.renderNodeProperties.NullOrEmpty() && PawnOwner != null)
            {
                List<PawnRenderNode> nodes = new List<PawnRenderNode>();
                foreach (var p in Props.renderNodeProperties)
                {
                    var node = new PawnRenderNode_TurretGun(PawnOwner, p, PawnOwner.Drawer.renderer.renderTree)
                    {
                        turretComp = this,
                        apparel = parent as Apparel
                    };
                    nodes.Add(node);
                }
                return nodes;
            }
            return base.CompRenderNodes();
        }

        public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            if (Props.turretDef != null)
            {
                yield return new StatDrawEntry(StatCategoryDefOf.PawnCombat, "WG_SubTurret".Translate(), Props.turretDef.LabelCap, "WG_SubTurretDesc".Translate(), 5600, null, Gen.YieldSingle(new Dialog_InfoCard.Hyperlink(Props.turretDef)));
            }
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            if (gun != null) subGunRegistry.Remove(gun);
            base.PostDestroy(mode, previousMap);
        }

        public Vector3 TurretLocation
        {
            get
            {
                if (PawnOwner == null) return parent.DrawPos;
                if (TryGetTurretOffset(out Vector3 offset))
                {
                    offset.y = 0;
                }
                else
                {
                    offset = new Vector3(0, 0, 0.6f);
                }
                return PawnOwner.DrawPos + offset;
            }
        }

        public Vector3 TurretDrawPos
        {
            get
            {
                if (PawnOwner == null) return parent.DrawPos;
                if (!TryGetTurretOffset(out Vector3 offset))
                {
                    offset = new Vector3(0, 0, 0.6f);
                }
                
                offset.y = 0;
                Vector3 barrelDir = Vector3.right.RotatedBy(curRotation);
                return PawnOwner.DrawPos + offset + (barrelDir * 0.5f);
            }
        }

        private bool TryGetTurretOffset(out Vector3 offset)
        {
            offset = Vector3.zero;
            if (parent.def.apparel != null && !parent.def.apparel.renderNodeProperties.NullOrEmpty())
            {
                var props = parent.def.apparel.renderNodeProperties.Find(x => x is PawnRenderNodeProperties_TurretGun)
                            ?? parent.def.apparel.renderNodeProperties.Find(x => x.workerClass == typeof(PawnRenderNodeWorker_TurretGun));

                if (props?.drawData != null)
                {
                    offset = props.drawData.OffsetForRot(PawnOwner.Rotation);
                    return true;
                }
            }
            return false;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref burstCooldownTicksLeft, "burstCooldownTicksLeft", 0);
            Scribe_Values.Look(ref burstWarmupTicksLeft, "burstWarmupTicksLeft", 0);
            Scribe_Values.Look(ref isAiming, "isAiming", defaultValue: false);
            Scribe_TargetInfo.Look(ref currentTarget, "currentTarget");
            if (Props.turretDef != null)
            {
                Scribe_Deep.Look(ref gun, "gun");
            }
            Scribe_Deep.Look(ref verbTracker, "verbTracker", this);
            Scribe_Values.Look(ref fireAtWill, "fireAtWill", defaultValue: true);
            Scribe_TargetInfo.Look(ref forcedTarget, "forcedTarget");
            Scribe_Values.Look(ref curRotation, "curRotation", 0);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (gun == null)
                {
                    MakeGun();
                }
                else
                {
                    UpdateGunVerbs();
                }
            }
        }
    }
}
