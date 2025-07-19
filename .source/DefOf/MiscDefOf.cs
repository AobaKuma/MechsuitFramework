using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace Exosuit
{
    [DefOf, StaticConstructorOnStartup]
    public static class MiscDefOf
    {
        static MiscDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(MiscDefOf));
        }
        public static RenderSkipFlagDef Head;
        public static RenderSkipFlagDef Body;
        public static RenderSkipFlagDef WGRoot;
        public static SlotDef Core;
    }
}
