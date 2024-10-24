﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WalkerGear
{
    [DefOf]
    internal static class MiscDefOf
    {
        static MiscDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(MiscDefOf));
        }
        public static RenderSkipFlagDef Head;
        public static RenderSkipFlagDef Body;
    }
}
