using Verse;

namespace WalkerGear
{
    public class PawnRenderNodeWorker_WeaponOnGantry : PawnRenderNodeWorker
    {
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            if (base.CanDrawNow(node, parms))
            {
                return !parms.pawn.Spawned;
            }
            return base.CanDrawNow(node, parms);
        }
    }
    public class PawnRenderNodeWorker_Undrafted : PawnRenderNodeWorker
    {
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            if (base.CanDrawNow(node, parms))
            {
                var pawn = parms.pawn;
                if ((pawn.Drafted))
                {
                    return false;
                }
                if (!parms.flags.HasFlag(PawnRenderFlags.NeverAimWeapon) && pawn.stances?.curStance is Stance_Busy stance_Busy && !stance_Busy.neverAimWeapon && stance_Busy.focusTarg.IsValid)
                {
                    return false;
                }
                return true;
            }
            return base.CanDrawNow(node, parms);
        }
    }
}