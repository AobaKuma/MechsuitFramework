using System;
using UnityEngine;
using Verse;

namespace Exosuit
{
    public class ExosuitExt : DefModExtension
    {
        public float BodySizeCap = 1.25f;
        public string RequiredApparelTag = null;
        public HediffDef RequiredHediff = null;
        public bool RequireAdult = true;
        public bool CanGearOff = true;//false for things like 40k dreadnought.
        public float minArmorBreakdownThreshold = 0.25f;
        public ThingDef wreckageOverride = ThingDefOf.MF_Building_Wreckage;
        public Vector3 bayRenderOffset = Vector3.zero;
        public float bayRenderScale = 1f;
    }
}