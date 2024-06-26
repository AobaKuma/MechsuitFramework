using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace WalkerGear
{
    public class JobDriver_ReturnToBay : JobDriver
    {
        protected Building Target
        {
            get
            {
                return this.job.targetA.Thing as Building;
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return ReservationUtility.Reserve(this.pawn, this.Target, this.job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoCell(this.Target.Position, PathEndMode.OnCell);
            yield return Toils_General.Wait(30, TargetIndex.None);
            yield return new Toil
            {
                initAction = delegate ()
                {
                    pawn.GetWalkerCore(out WalkerGear_Core Core);
                    //Core.GetOut();
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield break;
        }
    }
}
