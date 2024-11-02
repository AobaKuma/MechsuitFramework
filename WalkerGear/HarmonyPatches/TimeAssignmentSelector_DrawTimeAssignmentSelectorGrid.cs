using HarmonyLib;
using RimWorld;
using System.Numerics;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace WalkerGear
{

    [StaticConstructorOnStartup]
    [HarmonyPatch(typeof(TimeAssignmentSelector), "DrawTimeAssignmentSelectorGrid")]
    internal static class TimeAssignmentSelector_DrawTimeAssignmentSelectorGrid
    {
        [HarmonyPostfix]
        static void DrawTimeAssignmentSelectorGrid(Rect rect)
        {
            //rect.yMax -= 2f;
            Rect rect2 = rect;
            rect2.xMax = rect2.center.x;
            rect2.yMax = rect2.center.y;

            if (ModsConfig.RoyaltyActive)
            {
                rect2.x += rect2.width * 5;
            }
            else
            {
                rect2.x += rect2.width * 4;
            }
            DrawTimeAssignmentSelectorFor(rect2, TimeAssignmentDefOf.WG_WorkWithFrame);
        }
        private static void DrawTimeAssignmentSelectorFor(Rect rect, TimeAssignmentDef ta)
        {
            rect = rect.ContractedBy(2f);
            GUI.DrawTexture(rect, ta.ColorTexture);
            if (Widgets.ButtonInvisible(rect))
            {
                TimeAssignmentSelector.selectedAssignment = ta;
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }
            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
            }
            using (new TextBlock(TextAnchor.MiddleCenter))
            {
                Widgets.Label(rect, ta.LabelCap);
            }
            if (TimeAssignmentSelector.selectedAssignment == ta)
            {
                Widgets.DrawBox(rect, 2);
            }
            else
            {
                UIHighlighter.HighlightOpportunity(rect, ta.cachedHighlightNotSelectedTag);
            }
        }
    }
}