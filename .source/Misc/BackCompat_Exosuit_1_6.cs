using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Verse;

namespace Exosuit
{
    public class BackCompat_Exosuit_1_6 : BackCompatibilityConverter
    {

        public override bool AppliesToVersion(int majorVer, int minorVer)
        {
            return true;
        }

        public override string BackCompatibleDefName(Type defType, string defName, bool forDefInjections = false, XmlNode node = null)
        {

            if(defType==typeof(ThingDef))
            {

            }
            return null;
        }

        public override Type GetBackCompatibleType(Type baseType, string providedClassName, XmlNode node)
        {


                return providedClassName switch
                {
                    "WalkerGear.Building_EjectorBay" => typeof(Building_EjectorBay),
                    "WalkerGear.JobDriver_GetInEjector" => typeof(JobDriver_GetInEjector),
                    "WalkerGear.WalkerGear_Core" => typeof(Exosuit_Core),
                    "WalkerGear.PawnRenderNodeWorker_WeaponOnGantry" => typeof(PawnRenderNodeWorker_WeaponOnGantry),
                    "WalkerGear.CompApparelForcedWeapon" => typeof(CompApparelForcedWeapon),
                    "WalkerGear.Projectile_Parabola" => typeof(Projectile_Parabola),
                    _ => null,
                };

            
        }

        public override void PostExposeData(object obj)
        {
            
        }
    }
}
