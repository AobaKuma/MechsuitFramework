// 当白昼倾坠之时

using System;
using System.Collections.Generic;
using Verse;

namespace Exosuit;


public class ModExtForceApparelGen : DefModExtension
{
    public List<ThingDef> apparels;
    public List<ApparelChance> chanceApparels;
    public List<ApparelFallback> fallbackApparels;
    public ColorGenerator colorGenerator = null;
    public FloatRange StructurePointRange = new FloatRange(1, 1);
}

[Serializable]
public class ApparelChance
{
    public ThingDef apparel;
    public float chance;
}

[Serializable]
public class ApparelFallback
{
    public ThingDef target;   // 原始计划生成的模块
    public ThingDef fallback; // 如果 target 不可用则替换为此模块
}
