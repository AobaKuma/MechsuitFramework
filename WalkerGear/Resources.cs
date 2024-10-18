using UnityEngine;
using Verse;

namespace WalkerGear
{
    [StaticConstructorOnStartup]
    public static class Resources
    {
        public static readonly Texture2D BarBG = SolidColorMaterials.NewSolidColorTexture(new Color(0.1f, 0.1f, 0.1f));
        public static readonly Texture2D BarOL = SolidColorMaterials.NewSolidColorTexture(new Color(0.8f, 0.1f, 0.1f));

        //安全裝置的Icon
        public static readonly Texture2D GetSafetyIcon_Disabled = ContentFinder<Texture2D>.Get("UI/Safety_Disabled");
        public static readonly Texture2D GetSafetyIcon = ContentFinder<Texture2D>.Get("UI/Safety");
        public static readonly Texture2D GetOutIcon = ContentFinder<Texture2D>.Get("UI/GetOffWalker");

        //裝配介面的Icon
        public static readonly Texture2D rotateButton = ContentFinder<Texture2D>.Get("UI/Rotate");
        public static readonly Texture2D rotateOppoButton = ContentFinder<Texture2D>.Get("UI/RotateOppo");

        //彈射裝置的Icon
        public static readonly Texture2D catapultLaunch = ContentFinder<Texture2D>.Get("UI/CatapultLaunch");
        public static readonly Texture2D catapultThrow = ContentFinder<Texture2D>.Get("UI/CatapultThrow");
        public static readonly Texture2D catapultEject = ContentFinder<Texture2D>.Get("UI/CatapultEject");

        //整備架的Icon
        public static readonly Texture2D WG_GetInWalker = ContentFinder<Texture2D>.Get("UI/GetInWalker");
        public static readonly Texture2D WG_AutoRepair = ContentFinder<Texture2D>.Get("UI/AutoRepair");
    }
}
