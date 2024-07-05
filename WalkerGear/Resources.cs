using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace WalkerGear
{
    [StaticConstructorOnStartup]
    public static class Resources
    {
        public static readonly Texture2D rotateButton = ContentFinder<Texture2D>.Get("UI/Rotate");
        public static readonly Texture2D rotateOppoButton = ContentFinder<Texture2D>.Get("UI/RotateOppo");


    }
}
