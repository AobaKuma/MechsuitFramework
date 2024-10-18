
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace WalkerGear
{
    public class StatPart_CarryingCapacity : StatPart
    {
        private Dictionary<Thing, float> TakenMass = null;
        private Dictionary<Thing, float> AddonMass = null;
        public override string ExplanationPart(StatRequest req)
        {
            if (IsWorking(req, core: out WalkerGear_Core core))
            {
                if (TakenMass == null || AddonMass == null)
                {
                    TakenMass = new Dictionary<Thing, float>();
                    AddonMass = new Dictionary<Thing, float>();
                    foreach (Apparel item in core.modules.Cast<Apparel>())
                    {
                        //額外攜帶量
                        float v = item.def.statBases.GetStatValueFromList(StatDefOf.CarryingCapacity, 0);
                        if (v != 0)
                        {
                            AddonMass.Add(item, v);
                        }

                        Thing module = item.GetComp<CompWalkerComponent>().PeakConverted();
                        //質量(會在佔用質量的同時增加同等攜帶量)
                        v = module != null ? module.GetStatValue(StatDefOf.Mass) : 0;
                        if (v != 0)
                        {
                            TakenMass.Add(item, v);
                        }
                    }
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

                    Thing module = item.GetComp<CompWalkerComponent>().PeakConverted();
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
            return null;
        }

        public override void TransformValue(StatRequest req, ref float val)
        {
            if (IsWorking(req, out WalkerGear_Core def))
            {
                val += GetValue(req, def);
            }
        }
        private float GetValue(StatRequest req, WalkerGear_Core core)
        {
            return core.TotalCapacity;
        }
        private Pawn GetPawn(StatRequest req)
        {
            if (req.Thing is Pawn p)
            {
                return p;
            }
            Log.Warning("Error: pawn not exist");
            return null;
        }
        private bool IsWorking(StatRequest req, out WalkerGear_Core core)
        {
            core = null;
            if (req.HasThing && req.Thing is Pawn pawn)
            {
                return MechUtility.GetWalkerCore(pawn, out core);
            }
            return false;
        }
    }
}