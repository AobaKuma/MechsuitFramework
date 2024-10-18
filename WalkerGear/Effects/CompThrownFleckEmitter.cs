using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace WalkerGear
{
    public class CompProjectileFleckEmitter : ThingComp
    {
        public bool emittedBefore;

        public int ticksSinceLastEmitted;

        private CompProperties_ProjectileFleckEmitter Props => (CompProperties_ProjectileFleckEmitter)props;

        private Vector3 EmissionOffset => new Vector3(Rand.Range(Props.offsetMin.x, Props.offsetMax.x), Rand.Range(Props.offsetMin.y, Props.offsetMax.y), Rand.Range(Props.offsetMin.z, Props.offsetMax.z));

        private Color EmissionColor => Color.Lerp(Props.colorA, Props.colorB, Rand.Value);

        private bool IsOn
        {
            get
            {
                if (!parent.Spawned)
                {
                    return false;
                }
                return true;
            }
        }
        public override void CompTick()
        {
            if (!IsOn)
            {
                return;
            }
            if (Props.emissionInterval != -1)
            {
                if (ticksSinceLastEmitted >= Props.emissionInterval)
                {
                    Emit();
                    ticksSinceLastEmitted = 0;
                }
                else
                {
                    ticksSinceLastEmitted++;
                }
            }
            else if (!emittedBefore)
            {
                Emit();
                emittedBefore = true;
            }
        }

        private void Emit()
        {
            if (this.parent is Projectile_Parabola p)
            {
                if (p.Progress < 0.5)
                {
                    for (int i = 0; i < Props.burstCount; i++)
                    {
                        FleckCreationData dataStatic = FleckMaker.GetDataStatic(p.ExactPos, parent.Map, Props.fleck, Props.scale.RandomInRange * (1 - 2f * p.Progress));
                        dataStatic.velocityAngle = parent.Rotation.AsAngle + Props.rotationRate.RandomInRange;
                        dataStatic.targetSize = (1 - 2f * p.Progress);
                        dataStatic.solidTimeOverride = (1 - 2f * p.Progress);
                        dataStatic.instanceColor = EmissionColor;
                        dataStatic.rotationRate = Props.rotationRate.RandomInRange * (dataStatic.velocityAngle > parent.Rotation.AsAngle ? 1 : -1);
                        dataStatic.velocitySpeed = 4 - 2 * p.Progress;
                        parent.Map.flecks.CreateFleck(dataStatic);
                    }
                }
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref ticksSinceLastEmitted, "ticksSinceLastEmitted", 0);
            Scribe_Values.Look(ref emittedBefore, "emittedBefore", defaultValue: false);
        }
    }
    public class CompProperties_ProjectileFleckEmitter : CompProperties
    {
        public FleckDef fleck;

        public Vector3 offsetMin;

        public Vector3 offsetMax;

        public int emissionInterval = -1;

        public int burstCount = 1;

        public Color colorA = Color.white;

        public Color colorB = Color.white;

        public FloatRange scale;

        public FloatRange rotationRate;

        public CompProperties_ProjectileFleckEmitter()
        {
            compClass = typeof(CompProjectileFleckEmitter);
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            if (fleck == null)
            {
                yield return "CompThrownFleckEmitter must have a fleck assigned.";
            }
        }
    }
}
