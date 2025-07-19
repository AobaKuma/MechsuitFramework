using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Exosuit
{
    public class FloatMenuOptionProvider_ExosuitDown : FloatMenuOptionProvider
    {
        
        protected override bool Drafted => false;

        protected override bool Undrafted => true;

        protected override bool Multiselect => false;
        protected override bool RequiresManipulation => true;
        public override IEnumerable<FloatMenuOption> GetOptionsFor(Pawn clickedPawn, FloatMenuContext context)
        {

            var selectedPawn = context.FirstSelectedPawn;
            if (clickedPawn.IsPlayerControlled)
            {
                Thing bay = MechUtility.GetClosestBay(clickedPawn);
                if (bay != null)
                {
                    yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("WG_Job_TakeToMaintenanceBay".Translate(), delegate
                    {
                        selectedPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.WG_TakeToMaintenanceBay, bay, clickedPawn), JobTag.DraftedOrder);
                        selectedPawn.jobs.curJob.count = 1;//不確定這裡為什麼會出問題，所以要寫這個來防止跳紅字。
                    }
                        ), selectedPawn, null);
                }
                else
                {
                    yield return new ("WG_Disabled_NoMaintenanceBay".Translate(), null)
                    {
                        Disabled = true
                    };
                }
            }
            LessonAutoActivator.TeachOpportunity(ConceptDef.Named("WG_Frame_Capture"), OpportunityType.Important);
            yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("WG_Job_DisassembleFrame".Translate(), delegate
                {
                    selectedPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.WG_DisassembleWalkerCore, clickedPawn), JobTag.DraftedOrder);
                }
            ), selectedPawn, clickedPawn);
            

        }

        public override IEnumerable<FloatMenuOption> GetOptionsFor(Thing clickedThing, FloatMenuContext context)
        {
            foreach (var opt in base.GetOptionsFor(clickedThing, context))
            {
                yield return opt;
            }

            Pawn selectedPawn = context.FirstSelectedPawn;
            FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("WG_Job_DisassembleFrame".Translate(), delegate
            {
                selectedPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.WG_DisassembleWalkerCore, clickedThing), JobTag.DraftedOrder);
            }
                ), selectedPawn, clickedThing);
            
        }
        public override bool TargetPawnValid(Pawn pawn, FloatMenuContext context)
        {
            return base.TargetPawnValid(pawn, context)&& pawn.Downed&&MechUtility.PawnWearingExosuitCore(pawn);
        }
        public override bool TargetThingValid(Thing thing, FloatMenuContext context)
        {
            return base.TargetThingValid(thing, context)&&(thing is Corpse corpse &&MechUtility.PawnWearingExosuitCore(corpse.InnerPawn));
        }
    }
}