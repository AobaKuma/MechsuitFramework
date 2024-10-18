using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace WalkerGear
{

    [StaticConstructorOnStartup]
    [HarmonyPatch(typeof(FloatMenuMakerMap), "AddHumanlikeOrders")]
    internal static class FloatMenuMakerMap_MakeForFrame
    {
        [HarmonyPostfix]
        static void AddHumanlikeOrders(Vector3 clickPos, Pawn pawn, ref List<FloatMenuOption> opts)
        {
            IntVec3 clickCell = IntVec3.FromVector3(clickPos);
            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)) { return; }
            foreach (Thing thing4 in clickCell.GetThingList(pawn.Map))//如果包含一位龍騎兵駕駛。
            {
                if (thing4 is Pawn _targetPawn && MechUtility.PawnWearingWalkerCore(_targetPawn))
                {
                    if (!_targetPawn.Downed) return;//站著的龍騎兵沒法拆

                    if (_targetPawn.IsPlayerControlled)//自家控制的龍騎兵可以搬回維修塢(如果有的話)
                    {
                        Thing bay = MechUtility.GetClosestEmptyBay(pawn);
                        if (bay != null)
                        {
                            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("WG_Job_TakeToMaintenanceBay".Translate(), delegate
                            {
                                pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.WG_TakeToMaintenanceBay, bay, thing4), JobTag.DraftedOrder);
                                pawn.jobs.curJob.count = 1;//不確定這裡為什麼會出問題，所以要寫這個來防止跳紅字。
                            }
                            ), pawn, null));
                        }
                        else
                        {
                            FloatMenuOption option = new FloatMenuOption("WG_Disabled_NoMaintenanceBay".Translate(), null);
                            option.Disabled = true;
                            opts.Add(option);
                        }
                    }

                    LessonAutoActivator.TeachOpportunity(ConceptDef.Named("WG_Frame_Capture"), OpportunityType.Important);
                    //敵方龍騎兵的話得就地拆，對敵我皆可使用
                    opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("WG_Job_DisassembleFrame".Translate(), delegate
                    {
                        pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.WG_DisassembleWalkerCore, thing4), JobTag.DraftedOrder);
                    }
                    ), pawn, _targetPawn));

                }
                else if (thing4 is Corpse c && MechUtility.PawnWearingWalkerCore(c.InnerPawn))
                {
                    opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("WG_Job_DisassembleFrame".Translate(), delegate
                    {
                        pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.WG_DisassembleWalkerCore, thing4), JobTag.DraftedOrder);
                    }
                    ), pawn, thing4));
                }
            }
        }
    }
}