using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DMSLib
{
    public class CompBuildingExtraRenderer:ThingComp
    {
        public CompProperties_BuildingExtraRenderer Props => (CompProperties_BuildingExtraRenderer)props;
        public override void PostPrintOnto(SectionLayer layer)
        {
            base.PostPrintOnto(layer);
            foreach(var g in ExtraGraphic)
            {
                g.Print(layer, parent, 0f);
            }
        }
        public List<Graphic> ExtraGraphic
        {
            get
            {
                if (extraGraphic == null)
                {
                    extraGraphic = new();
                    foreach(var gd in Props.extraGraphic)
                    {
                        extraGraphic.Add(gd.GraphicColoredFor(parent));
                    }
                }
                return extraGraphic;
            }
        }
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
