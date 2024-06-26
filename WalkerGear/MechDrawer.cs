using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace WalkerGear
{
    public class MechDrawer
    {
        public void DrawAt(Vector3 drawLoc)
        {
            DrawApparel(drawLoc);
        }
        public void DrawApparel(Vector3 drawLoc)
        {
            if(bay == null) return;

        }
        public static bool TryGetGraphicApparel(Apparel apparel,out ApparelGraphicRecord rec)
        {
            if (apparel.WornGraphicPath.NullOrEmpty())
            {
                rec = new(null, null);
                return false;
            }
            string path =apparel.WornGraphicPath;
            Shader shader = ShaderDatabase.Cutout;
            if (apparel.StyleDef?.graphicData.shaderType != null)
            {
                shader = apparel.StyleDef.graphicData.shaderType.Shader;
            }
            else if ((apparel.StyleDef == null && apparel.def.apparel.useWornGraphicMask) || (apparel.StyleDef != null && apparel.StyleDef.UseWornGraphicMask))
            {
                shader = ShaderDatabase.CutoutComplex;
            }
            Graphic graphic = GraphicDatabase.Get<Graphic_Multi>(path, shader, apparel.def.graphicData.drawSize, apparel.DrawColor);
            rec = new(graphic, apparel);
            return true;
        }

        private readonly Building_MaintenanceBay bay;
    }
}
