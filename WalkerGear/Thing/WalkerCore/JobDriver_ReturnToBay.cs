using System.Collections.Generic;
using Verse;
using Verse.AI;
using static UnityEngine.GridBrushBase;

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
        private Building_MaintenanceBay Building => (Building_MaintenanceBay)job.GetTarget(TargetIndex.A).Thing;
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return ReservationUtility.Reserve(this.pawn, this.Target, this.job, 1, -1, null, errorOnFailed);
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoCell(this.Target.Position, PathEndMode.OnCell);
            yield return Toils_General.Wait(30, TargetIndex.None);
            Toil gearDown = new()
            {
                initAction = () =>
                {
                    Pawn actor = this.GetActor();
                    Building.GearDown(actor);
                    pawn.drafter.Drafted = false;
                }
            };
            yield return gearDown;
            yield break;
        }
    }
}
