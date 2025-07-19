using System.Collections.Generic;
using RimWorld;
using Verse;
using System.Linq;

namespace Exosuit
{
    public class CompBuildingExtraRenderer:ThingComp
    {
        public CompProperties_BuildingExtraRenderer Props => (CompProperties_BuildingExtraRenderer)props;
        public override void PostPrintOnto(SectionLayer layer)
        {
            base.PostPrintOnto(layer);
            ExtraGraphic.ForEach(g => g.Print(layer, parent, 0f));
        }
        public override void Notify_DefsHotReloaded()
        {
            base.Notify_DefsHotReloaded();
            extraGraphic = null;
        }
        public List<Graphic> ExtraGraphic => extraGraphic ??= [.. Props.extraGraphic.Select(gd => gd.GraphicColoredFor(parent))];
        private List<Graphic> extraGraphic;
    }
    public class CompProperties_BuildingExtraRenderer : CompProperties
    {
        public List<GraphicData> extraGraphic;
        public CompProperties_BuildingExtraRenderer()
        {
            compClass = typeof(CompBuildingExtraRenderer);
        }
    }
}
