﻿using RimWorld;
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
    public class JobDriver_GetInWalkerCore_Drafted : JobDriver_GetInWalkerCore
    {
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(maintenanceBay);

            yield return Toils_Goto.GotoThing(maintenanceBay, PathEndMode.InteractionCell);
            yield return Toils_General.WaitWith(TargetIndex.A, Wait, true, true);
            Toil gearUp = new()
            {
                initAction = () =>
                {
                    Pawn actor = this.pawn;
                    if (actor.CurJob.GetTarget(TargetIndex.A).Thing is Building_MaintenanceBay bay)
                    {
                        actor.Position = bay.Position;
                        bay.GearUp(actor);
                        actor.drafter.Drafted = true;
                    }
                }
            };
            yield return gearUp;
        }
    }
    public class JobDriver_GetInWalkerCore : JobDriver
    {
        protected const TargetIndex maintenanceBay = TargetIndex.A;
        protected Building_MaintenanceBay Bay => this.job.GetTarget(maintenanceBay).Thing as Building_MaintenanceBay;
        protected int Wait => (int)(200 + 200 * (1 - Bay.GetStatValue(StatDefOf.WorkTableWorkSpeedFactor, true)));
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {

            if (!Bay.CanGear(pawn))
            {
                Messages.Message("WG_ApparelLayerTaken".Translate(GetActor().Name.ToString()), MessageTypeDefOf.RejectInput, false);
                return false;
            }
            if (!Bay.HasGearCore)
            {
                Log.Error("Bay have no Gear but chosen as target!");
                return false;
            }

            if (Bay.GetGearCore.def.HasModExtension<ModExtWalkerCore>())
            {
                ModExtWalkerCore mod = Bay.GetGearCore.def.GetModExtension<ModExtWalkerCore>();
                if (mod.RequireAdult && !pawn.DevelopmentalStage.Adult())
                {
                    Messages.Message("WG_TooYoungToPilot".Translate(GetActor().Name.ToString()), MessageTypeDefOf.RejectInput, false);
                    return false;
                }
                if (pawn.BodySize > mod.BodySizeCap)
                {
                    Messages.Message("WG_TooBigForPilot".Translate(GetActor().Name.ToString()), MessageTypeDefOf.RejectInput, false);
                    return false;
                }
                if (mod.RequiredApparelTag != null)
                {
                    if (!GetActor().apparel.WornApparel.Where(p => p.def.apparel.tags.Contains(mod.RequiredApparelTag)).Any())
                    {
                        Messages.Message("WG_RequirePilotSuit".Translate(GetActor().Name.ToString()), MessageTypeDefOf.RejectInput, false);
                        return false;
                    }
                }
                if (mod.RequiredHediff != null && !GetActor().health.hediffSet.HasHediff(mod.RequiredHediff))
                {
                    Messages.Message("WG_RequireBionic".Translate(GetActor().Name.ToString()), MessageTypeDefOf.RejectInput, false);
                    return false;
                }
            }
            else
            {
                if (this.pawn.BodySize > 1.25)
                {
                    Messages.Message("WG_TooBigForPilot".Translate(GetActor().Label), MessageTypeDefOf.RejectInput, false);
                    return false;
                }
                if (!pawn.DevelopmentalStage.Adult())
                {
                    Messages.Message("WG_TooYoungToPilot".Translate(GetActor().Name.ToString()), MessageTypeDefOf.RejectInput, false);
                    return false;
                }
            }

            return this.pawn.Reserve(this.job.GetTarget(maintenanceBay), this.job, errorOnFailed: errorOnFailed);
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(maintenanceBay);

            yield return Toils_Goto.GotoThing(maintenanceBay, PathEndMode.InteractionCell);
            yield return Toils_General.WaitWith(TargetIndex.A, Wait, true, true);
            Toil gearUp = new()
            {
                initAction = () =>
                {
                    Pawn actor = this.pawn;
                    if (actor.CurJob.GetTarget(TargetIndex.A).Thing is Building_MaintenanceBay bay)
                    {
                        actor.Position = bay.Position;
                        bay.GearUp(actor);
                        actor.drafter.Drafted = false;
                    }
                }
            };
            yield return gearUp;
        }
    }
}