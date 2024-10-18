using UnityEngine;
using Verse;

namespace WalkerGear
{
    public class Projectile_Parabola : Projectile_Explosive
    {
        public Vector3 ExactPos => DrawPos + new Vector3(0f, 0f, 1f) * (ArcHeightFactor * GenMath.InverseParabola(DistanceCoveredFraction));
        private float ArcHeightFactor
        {
            get
            {
                float num = def.projectile.arcHeightFactor;
                float num2 = (destination - origin).MagnitudeHorizontalSquared();
                if (Mathf.Pow(num, 2) > num2 * 0.2f * 0.2f)
                {
                    num = Mathf.Sqrt(num2) * 0.2f;
                }
                return num;
            }
        }
        public float Progress => DistanceCoveredFraction;
        private Vector3 LookTowards => new Vector3(destination.x - origin.x, def.Altitude, destination.z - origin.z + ArcHeightFactor * Accelerate);
        public override Quaternion ExactRotation => Quaternion.LookRotation(LookTowards);
        public float Accelerate => 5f - 10f * DistanceCoveredFraction;
        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
        {
            base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);
        }
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);
        }

        public override void Tick()
        {
            base.Tick();
            if (Map != null)
            {
                float num = ArcHeightFactor * GenMath.InverseParabola(DistanceCoveredFraction);
                Vector3 drawPos = DrawPos;
                Vector3 vector = drawPos + new Vector3(0f, 0f, 1f) * num;
            }
        }
    }

}
