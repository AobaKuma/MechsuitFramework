using RimWorld;
using Verse;

namespace WalkerGear
{
    /// <summary>
    /// 給衣服用的Comp，主要是做模塊拓展用的
    /// </summary>
    public abstract class ModuleComp : ThingComp
    {
        protected Pawn Wearer => (this.parent as Apparel).Wearer;
        protected WalkerGear_Core Core
        {
            get
            {
                if (Wearer == null) return null;
                if (Wearer.GetWalkerCore(out var core))
                {
                    return core;
                }
                return null;
            }
        }
        protected bool Active => Core != null;
    }
}