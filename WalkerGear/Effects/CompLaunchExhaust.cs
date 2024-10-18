using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Noise;
using VFECore;

namespace WalkerGear
{
    public class CompLaunchExhaust : ThingComp //給子彈發射時用的
    {
        public CompProperties_LaunchExhaust Props => (CompProperties_LaunchExhaust)props;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            if (!parent.Spawned || respawningAfterLoad || this.parent.Map == null) return;
            for (int i = 0; i < 5; i++)
            {
                LaunchExhaust();
            }
        }
        public override void CompTick()
        {
            base.CompTick();
        }
        private void LaunchExhaust()
        {
            FleckCreationData dataStatic = FleckMaker.GetDataStatic(parent.DrawPos, parent.Map, FleckDefOf.AirPuff, Rand.Range(2, 4));
            dataStatic.velocityAngle = parent.Rotation.AsAngle+ Rand.Range(-36, 36);
            dataStatic.solidTimeOverride = 0.5f;
            dataStatic.rotationRate = Rand.Range(60, 60) * (dataStatic.velocityAngle > parent.Rotation.AsAngle ? 1 : -1);
            dataStatic.velocitySpeed = Rand.Range(0.6f, 0.75f);
            parent.Map.flecks.CreateFleck(dataStatic);
        }
    }

    public class CompProperties_LaunchExhaust : CompProperties
    {
        public CompProperties_LaunchExhaust()
        {
            compClass = typeof(CompLaunchExhaust);
        }
    }
}
