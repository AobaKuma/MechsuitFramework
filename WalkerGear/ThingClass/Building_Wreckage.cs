using Mono.Unix.Native;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace WalkerGear
{
    public class Building_Wreckage : Building, IOpenable
    {
        public bool CanOpen => pawnContainer.Count>0;

        public int OpenTicks => 100;

        public void Open()
        {
            if (CanOpen)
                GenDrop.TryDropSpawn(pawnContainer.FirstOrDefault(), Position, Map, ThingPlaceMode.Direct, out var _);
        }
        public override void Destroy(DestroyMode mode)
        {
            if (mode == DestroyMode.Deconstruct)
            {
                for (int i = moduleContainer.Count - 1; i >= 0; --i)
                {
                    GenDrop.TryDropSpawn(moduleContainer[i], Position, Map, ThingPlaceMode.Near, out var _);
                }
                if(CanOpen)
                    GenDrop.TryDropSpawn(pawnContainer.FirstOrDefault(), Position, Map, ThingPlaceMode.Direct, out var _);

            }
            else
            {
                for (int i = moduleContainer.Count - 1; i >= 0; i--)
                {
                    Thing slug = ThingMaker.MakeThing(RimWorld.ThingDefOf.ChunkSlagSteel);
                    GenDrop.TryDropSpawn(slug, Position, Map, ThingPlaceMode.Direct, out var _);
                }
                if (CanOpen)
                    GenDrop.TryDropSpawn(pawnContainer.FirstOrDefault(), Position, Map, ThingPlaceMode.Direct, out var _);
            }
            base.Destroy(mode);
        }
        public override void Tick()
        {
            base.Tick();
            pawnContainer.FirstOrDefault().Tick();
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref moduleContainer, "moduleContainer", LookMode.Deep);
            Scribe_Collections.Look(ref pawnContainer, "pawnContainer", LookMode.Deep);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                pawnContainer = new();
                moduleContainer = new();
            }
           
        }
        public List<Pawn> pawnContainer;
        public List<Thing> moduleContainer;
    }
}
