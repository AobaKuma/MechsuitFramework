using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace WalkerGear
{
    public class StatWorker_Module : StatWorker
    {
        public override bool ShouldShowFor(StatRequest req)
        {
            return base.ShouldShowFor(req) && req.HasThing && req.Thing.HasComp<CompWalkerComponent>();
        }
        public override IEnumerable<Dialog_InfoCard.Hyperlink> GetInfoCardHyperlinks(StatRequest statRequest)
        {
            CompWalkerComponent ext = statRequest.Thing.TryGetComp<CompWalkerComponent>();
            if (ext != null)
            {
                foreach (SlotDef slot in ext.Props.slots)
                {
                    yield return new Dialog_InfoCard.Hyperlink(slot);
                }
            }
        }
        public override string GetExplanationFinalizePart(StatRequest req, ToStringNumberSense numberSense, float finalVal)
        {
            CompProperties_WalkerComponent comp = req.Thing.TryGetComp<CompWalkerComponent>().Props;
            string s = "WG_Stats_TakingSlotsOf".Translate() + "\n";
            foreach (var item in comp.slots)
            {
                s += "  " + item.LabelCap + "\n";
            }
            if (!comp.disabledSlots.NullOrEmpty())
            {
                s += "WG_Stats_DisableSlotsOf".Translate() + "\n";
                foreach (var item in req.Thing.TryGetComp<CompWalkerComponent>().Props.disabledSlots)
                {
                    s += "  " + item.LabelCap + "\n";
                }
            }
            return s;
        }
        public override string GetStatDrawEntryLabel(StatDef stat, float value, ToStringNumberSense numberSense, StatRequest optionalReq, bool finalized = true)
        {
            return optionalReq.Thing.TryGetComp<CompWalkerComponent>().Props.slots.First().LabelCap;
        }
    }
}

