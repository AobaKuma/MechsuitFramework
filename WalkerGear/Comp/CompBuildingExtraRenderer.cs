using System.Collections.Generic;
using RimWorld;
using Verse;

namespace WalkerGear
{
    public class CompBuildingExtraRenderer:ThingComp
    {
        public CompProperties_BuildingExtraRenderer Props => (CompProperties_BuildingExtraRenderer)props;
        public override void PostPrintOnto(SectionLayer layer)
        {
            if (layerDebug == null) layerDebug = layer;
            base.PostPrintOnto(layer);
            foreach(var g in ExtraGraphic)
            {
                g.Print(layer, parent, 0f);
            }
        }
        private SectionLayer layerDebug;
        public override void Notify_DefsHotReloaded()
        {
            base.Notify_DefsHotReloaded();
            extraGraphic = null;
            PostPrintOnto(layerDebug);
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
