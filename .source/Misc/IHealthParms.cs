﻿using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace Exosuit
{
    public interface IHealthParms
    {
        float HPPercent { get;}
        string PanelName {  get; }
        string LabelHPPart { get; }
        string LabelMaxHPPart { get; }
        string Tooltips {  get; }
        Texture2D FullShieldBarTex { get; }
        Texture2D EmptyShieldBarTex { get; }
    }
    public class Gizmo_HealthPanel(IHealthParms healthParms) : Gizmo
    {
        public IHealthParms healthParms = healthParms ?? throw new ArgumentNullException(nameof(healthParms));

        public override float GetWidth(float maxWidth) => 140f;
        public override float Order => -120f;
        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            if (healthParms == null) return new GizmoResult(GizmoState.Clear);
            Rect outer = new(topLeft,new(GetWidth(maxWidth),75f));
            Rect inner = outer.ContractedBy(6f);
            Widgets.DrawWindowBackground(outer);
            Rect label = new(inner)
            {
                height = outer.height / 2f
            };
            Widgets.Label(label, healthParms.PanelName);
            Rect bar = new(inner)
            {
                yMin = inner.y + inner.height / 2f
            };
            float fillPercent = Mathf.Min(1f, healthParms.HPPercent);
            Widgets.FillableBar(bar, fillPercent, healthParms.FullShieldBarTex, healthParms.EmptyShieldBarTex, doBorder: false);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(bar, healthParms.LabelHPPart + " / " + healthParms.LabelMaxHPPart);
            Text.Anchor = TextAnchor.UpperLeft;
            TooltipHandler.TipRegion(inner, healthParms.Tooltips);
            return new GizmoResult(GizmoState.Clear);
        }
    }

}
