using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace WalkerGear
{
    [StaticConstructorOnStartup]
    public static class ModuleUtil
    {
        static readonly ThingDef[] Modules = new ThingDef[0];
        static readonly SlotDef[] Slots = new SlotDef[0];

        static ModuleUtil()
        {
            Modules = DefDatabase<ThingDef>.AllDefs.Where((ThingDef def) => def.category == ThingCategory.Item && !def.IsApparel && def.HasComp<CompWalkerComponent>()).ToArray();
            Slots = DefDatabase<SlotDef>.AllDefs.ToArray();
        }
        public static List<ThingDef> SlotUsers(SlotDef slot)
        {
            return Modules.Where(m => m.GetCompProperties<CompProperties_WalkerComponent>().slots.Contains(slot)).ToList();
        }
    }
    public class StatWorker_SlotUsers : StatWorker
    {
        public override bool ShouldShowFor(StatRequest req)
        {
            return req.Def is SlotDef;
        }
        public override IEnumerable<Dialog_InfoCard.Hyperlink> GetInfoCardHyperlinks(StatRequest statRequest)
        {
            foreach (ThingDef module in ModuleUtil.SlotUsers(statRequest.Def as SlotDef))
            {
                yield return new Dialog_InfoCard.Hyperlink(module);
            }
        }
        public override string GetExplanationFinalizePart(StatRequest req, ToStringNumberSense numberSense, float finalVal)
        {
            return "";
        }
        public override string GetStatDrawEntryLabel(StatDef stat, float value, ToStringNumberSense numberSense, StatRequest optionalReq, bool finalized = true)
        {
            return "";
        }
    }
}

