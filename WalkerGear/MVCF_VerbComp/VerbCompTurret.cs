using RimWorld;
using System;
using System.Linq;
using UnityEngine;
using Verse.AI;
using Verse;
using MVCF.VerbComps;
using MVCF.Utilities;
using MVCF.Comps;

namespace WalkerGear
{
    public class VerbCompTurret : VerbComp
    {
        private int cooldownTicksLeft;
        private LocalTargetInfo currentTarget = LocalTargetInfo.Invalid;
        private bool targetWasForced;
        private int warmUpTicksLeft;
        public float curRotation;
        public PawnRenderNode node;
        public override bool NeedsTicking => true;
        public override bool NeedsDrawing => false;

        public Pawn ParentPawn => parent.Manager.Pawn;
        public LocalTargetInfo Target => currentTarget;
        public VerbCompPropertiesTurret Props => (VerbCompPropertiesTurret)props;
        public override bool Independent => true;
        public override void CompTick()
        {
            base.CompTick();
            if (parent is not { Manager.Pawn.Spawned: true }) return;
            if (parent.Verb.Bursting) return;

            if (currentTarget.IsValid && (currentTarget is { HasThing: true, ThingDestroyed: true }
                                       || (currentTarget is { HasThing: true, Thing: Pawn p } && (p.Downed || p.Dead))
                                       || !parent.Verb.CanHitTarget(currentTarget)))
            {
                currentTarget = LocalTargetInfo.Invalid;
                targetWasForced = false;
            }
            if(node != null)
            {
                PawnDrawParms parms = new()
                {
                    facing = ParentPawn.Rotation,
                    pawn = ParentPawn
                };
                curRotation = (currentTarget.Cell.ToVector3Shifted() - (ParentPawn.DrawPos + node.Worker.OffsetFor(node, parms,out var _))).AngleFlat() + Props.angleOffset;
            }
            

            if (cooldownTicksLeft > 0) cooldownTicksLeft--;
            if (cooldownTicksLeft > 0) return;

            if (!parent.Enabled || !CanFire())
            {
                if (currentTarget.IsValid)
                {
                    currentTarget = LocalTargetInfo.Invalid;
                    targetWasForced = false;
                }

                if (warmUpTicksLeft > 0) warmUpTicksLeft = 0;
                return;
            }

            if (!currentTarget.IsValid) currentTarget = TryFindNewTarget();

            if (warmUpTicksLeft == 0) TryCast();
            else if (warmUpTicksLeft > 0) warmUpTicksLeft--;
            else if (currentTarget.IsValid) TryStartCast();
        }
        public virtual void TryStartCast()
        {
            if (currentTarget == null || !currentTarget.IsValid) return;
            if (parent.Verb.verbProps.warmupTime > 0)
                warmUpTicksLeft = (parent.Verb.verbProps.warmupTime * (parent.Manager?.Pawn?.GetStatValue(StatDefOf.AimingDelayFactor) ?? 1f))
                   .SecondsToTicks();
            else
                TryCast();
        }
        public virtual void TryCast()
        {
            warmUpTicksLeft = -1;
            parent.Verb.castCompleteCallback = () =>
                cooldownTicksLeft = parent.Verb.verbProps.AdjustedCooldownTicks(parent.Verb, parent.Manager.Pawn);
            var success = parent.Verb.TryStartCastOn(currentTarget);
            if (success && parent.Verb.verbProps.warmupTime > 0) parent.Verb.WarmupComplete();
        }
        public LocalTargetInfo PointingTarget(Pawn p) => currentTarget;
        public virtual bool CanFire() =>
            parent is { Manager.Pawn: var pawn } && !pawn.Dead && !pawn.Downed
         && (!parent.Verb.verbProps.onlyManualCast || targetWasForced)
         && !(!parent.Verb.verbProps.violent || pawn.WorkTagIsDisabled(WorkTags.Violent))
         && parent.Verb.IsStillUsableBy(pawn);
        public override void CompDrawOn(Pawn pawn, Vector3 drawPos, Rot4 facing, PawnRenderFlags flags)
        {
            base.CompDrawOn(pawn, drawPos, facing, flags);
            if (Find.Selector.IsSelected(pawn) && Target.IsValid)
            {
                if (warmUpTicksLeft > 0)
                    GenDraw.DrawAimPie(pawn, Target, warmUpTicksLeft, 0.2f);
                if (cooldownTicksLeft > 0)
                    GenDraw.DrawCooldownCircle(drawPos, cooldownTicksLeft * 0.002f);
                GenDraw.DrawLineBetween(drawPos, Target.HasThing ? Target.Thing.DrawPos : Target.Cell.ToVector3());
            }
        }
        public override bool SetTarget(LocalTargetInfo target)
        {
            currentTarget = target;
            targetWasForced = true;
            if (cooldownTicksLeft <= 0 && warmUpTicksLeft <= 0) TryStartCast();
            return false;
        }
        public virtual LocalTargetInfo TryFindNewTarget()
        {
            if (parent is not { Manager: var man }) return LocalTargetInfo.Invalid;
            return TargetFinder.BestAttackTarget(
                           man.Pawn, parent.Verb,
                           TargetScanFlags.NeedActiveThreat | TargetScanFlags.NeedLOSToAll |
                           TargetScanFlags.NeedAutoTargetable,
                           Props.uniqueTargets
                               ? new Predicate<Thing>(thing =>
                                   man.Pawn.mindState.enemyTarget != thing &&
                                   man.ManagedVerbs.All(verb =>
                                       verb.Verb.CurrentTarget.Thing != thing &&
                                   verb.TryGetComp<VerbCompTurret>()?.currentTarget.Thing != thing))
            : null, parent.Verb.verbProps.minRange, parent.Verb.verbProps.range, canTakeTargetsCloserThanEffectiveMinRange: false)
            ?.Thing ??
                   LocalTargetInfo.Invalid;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_TargetInfo.Look(ref currentTarget, nameof(currentTarget));
            Scribe_Values.Look(ref warmUpTicksLeft, nameof(warmUpTicksLeft));
            Scribe_Values.Look(ref cooldownTicksLeft, nameof(cooldownTicksLeft));
            Scribe_Values.Look(ref targetWasForced, nameof(targetWasForced));
        }

    }
    public class VerbCompPropertiesTurret : VerbCompProperties
    {
        public VerbCompPropertiesTurret() { compClass = typeof(VerbCompTurret); }

        
        public bool uniqueTargets;
        public float angleOffset;
    }

    public class PawnRenderNode_VerbCompTurret : PawnRenderNode
    {
        public PawnRenderNode_VerbCompTurret(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree) : base(pawn, props, tree)
        {
          
        }
        public VerbCompTurret VerbComp_Turret
        {
            get{
                if (verbComp_Turret==null)
                {
                    if (apparel.TryGetComp(out Comp_VerbGiver comp))
                    {
                        verbComp_Turret = comp.VerbTracker.AllVerbs.FirstOrDefault(v => v.verbProps.label == props.anchorTag).Managed().TryGetComp<VerbCompTurret>();
                        verbComp_Turret.node = this;
                    }
                }
                return verbComp_Turret;
            }
        }
        public VerbCompTurret verbComp_Turret;
    }
    public class PawnRenderNodeWorker_VerbCompTurret : PawnRenderNodeWorker
    {
        public override Quaternion RotationFor(PawnRenderNode node, PawnDrawParms parms)
        {
            Quaternion quaternion = base.RotationFor(node, parms);

            if (node is PawnRenderNode_VerbCompTurret renderNode_VerbCompTurret && renderNode_VerbCompTurret.VerbComp_Turret!=null)
            {
                quaternion *= renderNode_VerbCompTurret.VerbComp_Turret.curRotation.ToQuat();
            }
            return quaternion;
        }
    }
}
