
using System;
using System.Collections.Generic;
using Verse;

namespace WalkerGear;


public class ModExtForceApparelGen : DefModExtension
{
    public List<ThingDef> apparels;
    public List<ApparelChance> chanceApparels;
    public ColorGenerator colorGenerator = null;
    public FloatRange StructurePointRange = new FloatRange(1, 1);
}
[Serializable]
public class ApparelChance
{
    public ThingDef apparel;
    public float chance;
}