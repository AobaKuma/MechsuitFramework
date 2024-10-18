using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace WalkerGear
{
    /// <summary>
    /// 將倒地或死去的敵對龍騎兵拆解，需要在旁邊讀條400tick,然後完成時每個Module都將有5%機率可回收，或變成原製造資源的10%
    /// </summary>
    [StaticConstructorOnStartup]
    public class JobDriver_DisassembleWalkerCore : JobDriver
    {
        private const TargetIndex target = TargetIndex.A;
        private const int wait = 400;
        protected Thing Target => job.GetTarget(TargetIndex.A).Thing;
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return this.pawn.Reserve(Target, this.job, errorOnFailed: errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(target);

            yield return Toils_Goto.GotoThing(target, PathEndMode.Touch);
            yield return Toils_General.WaitWith(target, wait, true, true, face: TargetIndex.A).WithEffect(EffecterDefOf.ConstructMetal, target);
            Toil gearDown = new()
            {
                initAction = () =>
                {
                    MechUtility.DissambleFrom(Target);
                }
            };
            yield return gearDown.PlaySoundAtStart(SoundDefOf.MetalHitImportant);
        }
    }
}
