using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Exosuit
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    internal static class Patch_Pawn_GetGizmos
    {
        public static void Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            // Target only player-controlled pawns
            if (__instance.Faction != Faction.OfPlayer)
            {
                return;
            }
            if (!__instance.IsColonistPlayerControlled) return;

            // Check if the pawn is wearing Exosuit_Core
            bool isWearingWalkerGearCore = MechUtility.PawnWearingExosuitCore(__instance);

            // Search for the Maintenance Bay assigned to the pawn // and it need to be available.
            Building_MaintenanceBay maintenanceBay = FindAssignedMaintenanceBay(__instance, !isWearingWalkerGearCore);

            // If there is no assigned Maintenance Bay, do nothing
            if (maintenanceBay == null)
            {
                return;
            }

            // Retrieve the list of Gizmos
            List<Gizmo> gizmos = __result.ToList();

            if (!isWearingWalkerGearCore)
            {
                // AddModule the "Get In" Gizmo
                Command_Action getInGizmo = new Command_Action
                {
                    defaultLabel = "WG_GetIn".Translate(),
                    defaultDesc = "WG_GetInDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/GetInWalker", true),
                    action = () =>
                    {
                        Job job = JobMaker.MakeJob(JobDefOf.WG_GetInWalkerCore, maintenanceBay);
                        __instance.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    }
                };

                gizmos.Add(getInGizmo);
            }
            else
            {
                // AddModule the "Get Off" Gizmo
                Command_Action getOffGizmo = new Command_Action
                {
                    defaultLabel = "WG_GetOff".Translate(),
                    defaultDesc = "WG_GetOffDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/GetOffWalker", true),
                    action = () =>
                    {
                        Job job = JobMaker.MakeJob(JobDefOf.WG_GetOffWalkerCore, maintenanceBay);
                        __instance.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    }
                };

                gizmos.Add(getOffGizmo);
            }

            // Update the Gizmo list
            __result = gizmos;
        }
        private static Building_MaintenanceBay FindAssignedMaintenanceBay(Pawn pawn, bool needCore)
        {
            Map map = pawn.Map;
            if (map == null)
            {
                return null;
            }

            // Retrieve all Maintenance Bays
            foreach (Building_MaintenanceBay bay in map.listerBuildings.AllBuildingsColonistOfClass<Building_MaintenanceBay>())
            {
                // Get the CompAssignableToPawn component
                CompAssignableToPawn assignable = bay.GetComp<CompAssignableToPawn>();
                if (assignable != null && assignable.AssignedPawns.Contains(pawn))
                {
                    if (needCore == bay.HasGearCore) return bay;
                } 
            }

            return null;
        }
    }
}