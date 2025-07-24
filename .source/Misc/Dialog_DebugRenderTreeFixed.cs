using HarmonyLib;
using RimWorld;
using System.Diagnostics;
using System.Linq;
using Verse;

namespace Exosuit.Misc
{
    /// <summary>
    /// A DebugRenderTree that don't need the pawn spawned
    /// </summary>
    /// <param name="pawn"></param>
    internal class Dialog_DebugRenderTreeFixed(Pawn pawn) : Dialog_DebugRenderTree(pawn)
    {
        public override void WindowUpdate()
        {
            if (pawn == null)
            {
                pawn = Find.Selector.SelectedPawns.FirstOrDefault();
                if (pawn != null)
                {
                    Init(pawn);
                }
            }
            var drawParmsField = Traverse.Create(this).Field("drawParms");
            PawnDrawParms drawParms = drawParmsField.GetValue<PawnDrawParms>();

            drawParms.facing = pawn.Rotation;
            drawParms.posture = pawn.GetPosture();
            drawParms.bed = pawn.CurrentBed();
            drawParms.coveredInFoam = pawn.Drawer.renderer.FirefoamOverlays.coveredInFoam;
            drawParms.carriedThing = pawn.carryTracker?.CarriedThing;
            drawParms.dead = pawn.Dead;
            drawParms.rotDrawMode = pawn.Drawer.renderer.CurRotDrawMode;
            drawParmsField.SetValue(drawParms);
        }
    }
}
