using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Exosuit
{
    public class StatPart_MoveSpeed : StatPart
    {
        private static readonly StatDef ExosuitMoveSpeedDef = DefDatabase<StatDef>.GetNamedSilentFail("ExosuitMoveSpeed");
        private Dictionary<Thing, float> AffectedModules = new Dictionary<Thing, float>();
        private float GetBaseValue(Exosuit_Core core) => core.GetStatValue(StatDefOf.MoveSpeed);

        public override string ExplanationPart(StatRequest req)
        {
            if (IsWorking(req, out Exosuit_Core core))
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

                if (ExosuitMoveSpeedDef != null)
                {
                    float totalExosuitMoveSpeed = GetExosuitMoveSpeedOffset(req, core, out float coreExosuitMoveSpeed);
                    if (totalExosuitMoveSpeed != 0f)
                    {
                        line += "\n\n" + ExosuitMoveSpeedDef.LabelCap + ": " + " {0} c/s".Formatted(totalExosuitMoveSpeed.ToString("0.##"));

                        if (coreExosuitMoveSpeed != 0f)
                        {
                            line += "\n    " + core.LabelCap + ": " + " {0} c/s".Formatted(coreExosuitMoveSpeed.ToString("0.##"));
                        }
                    }
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

        private float GetValue(StatRequest req, Exosuit_Core core)
        {
            AffectedModules.Clear();
            float baseStat = core.GetStatValue(StatDefOf.MoveSpeed);

            foreach (Apparel item in core.modules.Cast<Apparel>())
            {
                float v = item.def.equippedStatOffsets.GetStatOffsetFromList(StatDefOf.MoveSpeed);
                if (v != 0)
                {
                    AffectedModules.Add(item, v);
                    baseStat += v;
                }
            }
            var fuelcells = core.modules.Cast<Apparel>().Where(m => m.TryGetComp<CompFuelCell>()!=null);
            foreach (Apparel item in fuelcells)
            {
                var v = item.GetComp<CompFuelCell>().MoveSpeedOffset;
                if (v != 0)
                {
                    AffectedModules.Add(item, v);
                    baseStat += v;
                } 
            }

            baseStat += GetExosuitMoveSpeedOffset(req, core, out _);

            float result = core.Overload ? baseStat * 0.5f : baseStat;

            return result;
        }

        // 統計核心自身與駕駛員(含穿戴模組 equippedStatOffsets)提供的 ExosuitMoveSpeed 總和
        private float GetExosuitMoveSpeedOffset(StatRequest req, Exosuit_Core core, out float coreOffset)
        {
            coreOffset = 0f;
            if (ExosuitMoveSpeedDef == null)
            {
                return 0f;
            }
            coreOffset = core.GetStatValue(ExosuitMoveSpeedDef);
            float total = coreOffset;
            if (req.HasThing && req.Thing is Pawn pawn)
            {
                // 駕駛員的 GetStatValue 會彙總所有穿戴中裝備(模組)的 equippedStatOffsets
                total += pawn.GetStatValue(ExosuitMoveSpeedDef);
            }
            return total;
        }

        private bool IsWorking(StatRequest req, out Exosuit_Core core)
        {
            core = null;
            if (req.HasThing && req.Thing is Pawn pawn)
            {
                return MechUtility.TryGetExosuitCore(pawn, out core);
            }
            return false;
        }
    }
}