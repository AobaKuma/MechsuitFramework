using RimWorld.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace WalkerGear
{
    //没做完！！！！！

    //WG_GetInWalkerCore; 不知道能不能用
    public class JobDriver_GetInWalkerCore : JobDriver
    {
        private const TargetIndex maintenanceBay = TargetIndex.A;
        private const int wait = 200;
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return this.pawn.Reserve(this.job.GetTarget(maintenanceBay), this.job, errorOnFailed: errorOnFailed);
        }

        //还在写
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(maintenanceBay);
            
            yield return Toils_Goto.GotoThing(maintenanceBay, PathEndMode.InteractionCell);
           Toil gearUp = Toils_General.WaitWith(TargetIndex.A, wait, true, true);
            Action action= () => 
            {
                Pawn actor = this.pawn;
                if (actor.CurJob.GetTarget(TargetIndex.A).Thing is Building_MaintenanceBay bay)
                {
                    actor.Position = bay.Position;
                    bay.GearUp(actor);
                    actor.drafter.Drafted=true;
                }

            };
            gearUp.AddFinishAction(action);
            
            yield return gearUp;
        }
    }
}
