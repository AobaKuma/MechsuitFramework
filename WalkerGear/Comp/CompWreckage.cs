using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace WalkerGear
{
    public class CompWreckage : ThingComp
    {
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            position = parent.Position;
        }
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            if(mode == DestroyMode.Deconstruct)
            {
                for (int i = moduleContainer.Count - 1; i >= 0; i--)
                {
                    GenDrop.TryDropSpawn(moduleContainer[i],position, previousMap,ThingPlaceMode.Near,out var _);
                }
                GenDrop.TryDropSpawn(pawnContainer.FirstOrDefault(), position, previousMap, ThingPlaceMode.Direct, out var _);
                
            }
            else
            {
                for (int i = moduleContainer.Count - 1; i >= 0; i--)
                {
                    Thing slug = ThingMaker.MakeThing(RimWorld.ThingDefOf.ChunkSlagSteel);
                    GenDrop.TryDropSpawn(slug, position, previousMap, ThingPlaceMode.Direct, out var _);  
                }
                GenDrop.TryDropSpawn(pawnContainer.FirstOrDefault(), position, previousMap, ThingPlaceMode.Direct, out var _);
            }
            base.PostDestroy(mode, previousMap);
        }
        public override void CompTick()
        {
            base.CompTick();
            pawnContainer.FirstOrDefault().Tick();
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref moduleContainer, "moduleContainer",LookMode.Deep);
            Scribe_Collections.Look(ref pawnContainer, "pawnContainer", LookMode.Deep);
        }

        public CompWreckage()
        {
            pawnContainer = new();
            moduleContainer = new();
        }
        public List<Pawn> pawnContainer;
        public List<Thing> moduleContainer;
        public IntVec3 position;
        public override void PostPostMake()
        {
            base.PostPostMake();
            position = parent.Position;
        }
    }
}
