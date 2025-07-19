using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace Exosuit.HarmonyPatches
{
    [HarmonyPatch]
    static class Pawn_JobTraceker_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Pawn_JobTracker),nameof(Pawn_JobTracker.StartJob))]
        static bool JobReplacerPatch(Job newJob, Pawn ___pawn)
        {
            return !___pawn.PawnWearingExosuitCore()
                || (newJob.def != RimWorld.JobDefOf.LayDown && newJob.def != RimWorld.JobDefOf.Wait_Asleep) || !___pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.WG_SleepInWalkerCore));
            ;
        }
        
    }
}
