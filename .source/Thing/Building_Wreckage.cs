using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Exosuit
{
    public class Building_Wreckage : Building_Casket
    {
        public void SetContained(Thing thing)
        {
            innerContainer.TryAdd(thing);
        }
        public override void Destroy(DestroyMode mode)
        {
            if (mode == DestroyMode.Deconstruct)
            {
                for (int i = moduleContainer.Count - 1; i >= 0; --i)
                {
                    if (Rand.Bool)
                    {
                        Thing slug = ThingMaker.MakeThing(RimWorld.ThingDefOf.ChunkSlagSteel);
                        GenDrop.TryDropSpawn(slug, Position, Map, ThingPlaceMode.Near, out var _);
                    }
                    moduleContainer[i].HitPoints = (int)(MaxHitPoints * Rand.Range(0.15f, 0.5f));
                    GenDrop.TryDropSpawn(moduleContainer[i], Position, Map, ThingPlaceMode.Near, out var _);
                }
            }
            else if (mode ==DestroyMode.KillFinalize)
            {
                for (int i = moduleContainer.Count - 1; i >= 0; i--)
                {
                    Thing slug = ThingMaker.MakeThing(RimWorld.ThingDefOf.ChunkSlagSteel);
                    GenDrop.TryDropSpawn(slug, Position, Map, ThingPlaceMode.Direct, out var _);
                }
            }
            base.Destroy(mode);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref moduleContainer, "moduleContainer", LookMode.Deep);
            if (Scribe.mode==LoadSaveMode.PostLoadInit && moduleContainer == null)
            {
                moduleContainer = [];
            }
        }

        public List<Thing> moduleContainer = [];
    }
}