using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace WalkerGear
{
    public class StatPart_MoveSpeed : StatPart
    {
        private Dictionary<Thing, float> AffectedModules = new Dictionary<Thing, float>();
        private float GetBaseValue(WalkerGear_Core core) => core.GetStatValue(StatDefOf.MoveSpeed);
        public override string ExplanationPart(StatRequest req)
        {
            if (IsWorking(req, out WalkerGear_Core core))
            {
                string line = "\n\n" + "WG_PilotingFrame".Translate();
                line += "\n" + "WG_ArmorSpeed".Translate() + " {0} c/s".Formatted(GetBaseValue(core).ToString("0.##"));
                if (!AffectedModules.NullOrEmpty())
                {
                    foreach (var pair in AffectedModules)
                    {
                        line += "\n    " + pair.Key.LabelCap + ": " + " {0} c/s".Formatted(pair.Value.ToString("0.##"));
                    }
                }
                if (core.Overload)
                {
                    line += "\n\n" + "WG_Overload".Translate() + "\n    " + StatDefOf.MoveSpeed.LabelCap + " x50%";
                }
                return line;
            }
            return null;
        }

        public override void TransformValue(StatRequest req, ref float val)
        {
            if (IsWorking(req, out var def))
            {
                val = GetValue(req, def);
            }
        }
        private float GetValue(StatRequest req, WalkerGear_Core core)
        {
            AffectedModules.Clear();
            float baseStat = core.GetStatValue(StatDefOf.MoveSpeed);
            foreach (Apparel item in core.modules.Cast<Apparel>())
            {
                //額外攜帶量
                float v = item.def.equippedStatOffsets.GetStatOffsetFromList(StatDefOf.MoveSpeed);
                if (v != 0)
                {
                    AffectedModules.Add(item, v);
                    baseStat += v;
                }
            }
            if (core.Overload) return baseStat * 0.5f;

            return baseStat;
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

