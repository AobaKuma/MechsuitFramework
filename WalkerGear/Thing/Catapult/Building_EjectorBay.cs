
using Mono.Unix.Native;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Noise;

namespace WalkerGear
{
    public class Building_EjectorBay : Building, IThingHolder
    {
        [Unsaved(false)]
        private CompPowerTrader cachedPowerComp;
        private CompPowerTrader PowerTraderComp
        {
            get
            {
                return cachedPowerComp ??= this.TryGetComp<CompPowerTrader>();
            }
        }
        public bool PowerOn => PowerTraderComp.PowerOn;
        public bool HasPawn => (bool)!innerContainer?.NullOrEmpty() && innerContainer.First() is Pawn;
        public Pawn StoragedPawn => HasPawn ? innerContainer?.First() as Pawn : null;
        public ThingOwner innerContainer;
        private bool ReadyToEject = false;
        public override string GetInspectString()
        {
            if (this.HasPawn)
            {
                return base.GetInspectString() + "\n" + "WG_PilotInBuilding".Translate() + StoragedPawn.Name;
            }
            else return base.GetInspectString();
        }
        public Building_EjectorBay()
        {
            innerContainer = new ThingOwner<Thing>(this);
        }
        public override void Destroy(DestroyMode mode = DestroyMode.KillFinalize)
        {
            if (this.HasPawn)
            {
                GenSpawn.Spawn(StoragedPawn, this.Position, this.Map);
            }
            base.Destroy(mode);
        }
        public ThingOwner GetDirectlyHeldThings()
        {
            return this.innerContainer;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn) //選擇殖民者後右鍵
        {
            if (!selPawn.CanReach(this, PathEndMode.InteractionCell, Danger.Deadly))
            {
                yield return new FloatMenuOption("CannotEnterBuilding".Translate(this) + ": " + "NoPath".Translate().CapitalizeFirst(), null);
                yield break;
            }
            AcceptanceReport acceptanceReport = CanAcceptPawn(selPawn);
            if (acceptanceReport.Accepted)
            {
                yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("EnterBuilding".Translate(this), delegate
                {
                    selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.WG_GetInEjector, this), JobTag.DraftedOrder);
                }), selPawn, this);
            }
            else yield return new FloatMenuOption(acceptanceReport.Reason, null);
            foreach (FloatMenuOption floatMenuOption in base.GetFloatMenuOptions(selPawn))
            {
                yield return floatMenuOption;
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            if (this.HasPawn)
            {
                Command_Target command_Eject = new()
                {
                    defaultLabel = "Throw".Translate(),
                    hotKey = KeyBindingDefOf.Misc1,
                    icon = Resources.catapultThrow,
                    targetingParams = new TargetingParameters
                    {
                        canTargetLocations = true,
                    },
                    action = delegate (LocalTargetInfo target)
                    {
                        Pawn p = StoragedPawn;
                        innerContainer.Remove(StoragedPawn);
                        Map destMap = Find.CurrentMap;
                        GenDrop.TryDropSpawn(p, Position, Map, ThingPlaceMode.Direct, out var _);
                        ReadyToEject = true;
                        float z = p.Position.z;
                        WG_AbilityVerb_QuickJump.DoJump(p, destMap, target, new LocalTargetInfo(new IntVec3(p.Position.x, p.Position.y, (p.Position.z + 25) < destMap.AllCells.MaxBy(o => o.z).z ? (p.Position.z + 25) : destMap.AllCells.MaxBy(o => o.z).z)), false, false);
                    }
                };
                yield return command_Eject;
            }

            if (this.HasPawn)
            {
                Command_Action command_Eject = new()
                {
                    defaultLabel = "Launch to map".Translate(),
                    hotKey = KeyBindingDefOf.Misc1,
                    icon = Resources.catapultEject,
                    action = () =>
                    {
                        Pawn p = StoragedPawn;
                        innerContainer.Remove(StoragedPawn);
                        Map destMap = Find.CurrentMap;
                        GenDrop.TryDropSpawn(p, Position, Map, ThingPlaceMode.Direct, out var _);
                        ReadyToEject = true;
                        float z = p.Position.z;
                        WG_AbilityVerb_QuickJump.DoJump(p, destMap
                            , new LocalTargetInfo(new IntVec3(p.Position.x, p.Position.y, (p.Position.z + 25) < destMap.AllCells.MaxBy(o => o.z).z ? (p.Position.z + 25) : destMap.AllCells.MaxBy(o => o.z).z))
                            , new LocalTargetInfo(new IntVec3(p.Position.x, p.Position.y, (p.Position.z + 25) < destMap.AllCells.MaxBy(o => o.z).z ? (p.Position.z + 25) : destMap.AllCells.MaxBy(o => o.z).z))
                            , true, true);
                    }
                };
                yield return command_Eject;
            }
            if (this.HasPawn)
            {
                Command_Action command_Release = new()
                {
                    defaultLabel = "Release".Translate(),
                    action = () =>
                    {
                        GenDrop.TryDropSpawn(StoragedPawn, Position, Map, ThingPlaceMode.Direct, out var _);
                        innerContainer.Clear();
                    }
                };
                yield return command_Release;
            }

            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
        }
        public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
        {
            if (this.HasPawn)
            {
                Vector3 drawLoc2 = drawLoc.WithYOffset(1f);
                this.StoragedPawn.Rotation = this.Rotation.Opposite;
                this.StoragedPawn.Drawer.renderer.DynamicDrawPhaseAt(phase, drawLoc2, null, false);
            }
            base.DynamicDrawPhaseAt(phase, drawLoc, flip);
        }
        private AcceptanceReport CanAcceptPawn(Pawn pawn)
        {
            if (!pawn.IsColonist && !pawn.IsSlaveOfColony && !pawn.IsPrisonerOfColony && (!pawn.IsColonyMutant || !pawn.IsGhoul))
            {
                return false;
            }
            if (!pawn.RaceProps.Humanlike || pawn.IsQuestLodger()) return false;
            if (PowerComp != null && !PowerOn) return "CannotEnterBuilding".Translate(this) + ": " + "NoPower".Translate().CapitalizeFirst();
            if (HasPawn) return "CannotEnterBuilding".Translate(this) + ": " + "WG_Occupied".Translate().CapitalizeFirst();
            if (!pawn.apparel.WornApparel.Any((a) => a is WalkerGear_Core)) return "CannotEnterBuilding".Translate(this) + ": " + "WG_RequireMechsuit".Translate().CapitalizeFirst();

            return true;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref innerContainer, "Container", new object[]
            {
                this
            });
            Scribe_Values.Look(ref ReadyToEject, "ReadyToEject", false);
        }
    }
}