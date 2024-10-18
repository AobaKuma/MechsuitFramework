using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace WalkerGear
{
    //WIP 總之就是如果一個Pawn註冊有一個停機坪，那麼在Gizmo那邊可以設置甚麼時候自動回去(工作/睡覺/解除徵招等等)
    public class CompAssignableToPawn_Parking : CompAssignableToPawn
    {
        public override IEnumerable<Pawn> AssigningCandidates
        {
            get
            {
                if (!parent.Spawned)
                {
                    Pawn pawn = parent as Pawn;
                    return Enumerable.Empty<Pawn>();
                }
                return parent.Map.mapPawns.FreeColonists.OrderByDescending((Pawn p) => CanAssignTo(p).Accepted);
            }
        }
        protected override string GetAssignmentGizmoDesc()
        {
            return "WG_AssignPilot".Translate();
        }
        public override void TryAssignPawn(Pawn pawn)
        {
            assignedPawns.Clear();
            base.TryAssignPawn(pawn);
        }
    }
}
