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
        // 防止替换逻辑递归重入
        [ThreadStatic]
        static bool replacing;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Pawn_JobTracker),nameof(Pawn_JobTracker.StartJob))]
        static bool JobReplacerPatch(Job newJob, Pawn ___pawn)
        {
            // 重入时放行原始逻辑
            if (replacing) return true;

            if (!___pawn.PawnWearingExosuitCore()) return true;
            if (newJob.def != RimWorld.JobDefOf.LayDown && newJob.def != RimWorld.JobDefOf.Wait_Asleep) return true;

            replacing = true;
            try
            {
                return !___pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.WG_SleepInWalkerCore));
            }
            finally
            {
                replacing = false;
            }
        }

    }
}
