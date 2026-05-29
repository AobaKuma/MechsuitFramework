using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace Exosuit
{
    // 双武器图标切换Gizmo
    [StaticConstructorOnStartup]
    public class Gizmo_DualWeaponToggle : Command_Action
    {
        #region 字段

        // 左侧贴图
        public Texture texA;
        // 右侧贴图
        public Texture texB;
        // true时A为实体B为剪影
        public Func<bool> aIsActiveGetter;
        // 未悬浮时显示
        public Func<string> idleLabelGetter;
        // 悬浮时显示
        public Func<string> hoverLabelGetter;
        // 描述
        public Func<string> descGetter;

        #endregion

        #region 重写

        public override string Label
        {
            get
            {
                Rect curRect = GUIRectHint;
                bool hover = curRect.width > 0f && Mouse.IsOver(curRect);
                if (hover && hoverLabelGetter != null) return hoverLabelGetter();
                if (idleLabelGetter != null) return idleLabelGetter();
                return defaultLabel;
            }
        }

        public override string Desc => descGetter?.Invoke() ?? defaultDesc;

        // 缓存按钮Rect用于悬浮判定
        private Rect GUIRectHint;

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            GUIRectHint = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            return base.GizmoOnGUI(topLeft, maxWidth, parms);
        }

        public override void DrawIcon(Rect rect, Material buttonMat, GizmoRenderParms parms)
        {
            bool aActive = aIsActiveGetter?.Invoke() ?? true;

            // 双枪错开布局
            float side = Mathf.Min(rect.width, rect.height) * 0.72f;
            float offset = rect.width * 0.18f;
            Rect rectA = new Rect(rect.x,          rect.y,          side, side);
            Rect rectB = new Rect(rect.x + offset, rect.y + offset, side, side);

            // 先画剪影再画实体
            bool aFront = aActive;
            DrawSlot(aFront ? rectB : rectA, aFront ? texB : texA, isSilhouette: true,  parms);
            DrawSlot(aFront ? rectA : rectB, aFront ? texA : texB, isSilhouette: false, parms);

            GUI.color = Color.white;
        }

        private void DrawSlot(Rect r, Texture tex, bool isSilhouette, GizmoRenderParms parms)
        {
            if (tex == null) return;
            float a = parms.lowLight ? 0.6f : 1f;
            GUI.color = isSilhouette ? new Color(0f, 0f, 0f, a) : new Color(1f, 1f, 1f, a);
            // 斜指右上方
            Widgets.DrawTextureFitted(r, tex, 1f, Vector2.one, new Rect(0f, 0f, 1f, 1f), -45f);
        }

        #endregion
    }
}
