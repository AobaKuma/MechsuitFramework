
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Exosuit
{
    public class StatPart_CarryingCapacity : StatPart
    {
        private static readonly Dictionary<Thing, float> TakenMass = [];
        private static readonly Dictionary<Thing, float> AddonMass = [];
        public override string ExplanationPart(StatRequest req)
        {
            if (!IsWorking(req, out Exosuit_Core core))
            {
                return null;
            }

            TakenMass.Clear();
            AddonMass.Clear();
            foreach (Apparel item in core.modules.Cast<Apparel>())
            {
                //額外攜帶量
                float v = item.def.equippedStatOffsets.GetStatOffsetFromList(StatDefOf.CarryingCapacity);
                if (v != 0)
                {
                    AddonMass.Add(item, v);
                }

                Thing module = item.GetComp<CompSuitModule>().PeakConverted();
                //質量(會在佔用質量的同時增加同等攜帶量)
                v = module != null ? module.GetStatValue(StatDefOf.Mass) : 0;
                if (v != 0)
                {
                    TakenMass.Add(item, v);
                }
            }
            string line = "\n\n" + "WG_PilotingFrame".Translate();

            line += "\n\n" + "WG_ArmorDeadWeight".Translate() + " " + core.DeadWeight.ToStringMass();
            if (TakenMass.NullOrEmpty()) return line;
            foreach (var pair in TakenMass)
            {
                line += "\n    " + pair.Key.LabelShort + ": " + pair.Value.ToStringMass();
            }

            line += "\n\n" + "WG_ArmorCapacity".Translate() + " {0}".Formatted(core.Capacity.ToStringMass());
            if (AddonMass.NullOrEmpty()) return line;
            foreach (var p in AddonMass)
            {
                line += "\n    " + p.Key.LabelShort + ": +" + p.Value.ToStringMass();
            }
            return line;
        }

        public override void TransformValue(StatRequest req, ref float val)
        {
            if (IsWorking(req, out Exosuit_Core core))
            {
                val += core.Capacity;
            }
        }
        private bool IsWorking(StatRequest req, out Exosuit_Core core)
        {
            core = null;
            if (req.HasThing && req.Thing is Pawn pawn)
            {
                return pawn.TryGetExosuitCore(out core);
            }
            return false;
        }
    }
}