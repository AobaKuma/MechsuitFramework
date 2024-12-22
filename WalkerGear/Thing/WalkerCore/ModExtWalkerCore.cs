using System;
using Verse;

namespace WalkerGear
{
    public class ModExtWalkerCore : DefModExtension
    {
        public float BodySizeCap = 1.25f;
        public string RequiredApparelTag = null;
        public HediffDef RequiredHediff = null;
        public bool RequireAdult = false;
        public bool CanGearOff = true;//false for things like 40k dreadnought.
        public float minArmorBreakdownThreshold = 0.25f;
    }
}