// 当白昼倾坠之时
using RimWorld;
using UnityEngine;
using Verse;
using Exosuit;

namespace Mechsuit
{
    // 用于在小人 Gizmo 栏显示肩炮弹药状态
    [StaticConstructorOnStartup]
    public class Gizmo_TurretAmmoStatus : Gizmo
    {
        public CompTurretGun comp;
        public ITurretAmmoProvider provider;

        private static readonly Texture2D FullBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.35f, 0.35f, 0.2f));
        private static readonly Texture2D EmptyBarTex = SolidColorMaterials.NewSolidColorTexture(Color.clear);

        public override float GetWidth(float maxWidth) => 140f;

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect outer = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Rect inner = outer.ContractedBy(6f);
            Widgets.DrawWindowBackground(outer);
            
            // 标题区域占上半
            Rect labelRect = new Rect(inner)
            {
                height = outer.height / 2f
            };
            
            Text.Font = GameFont.Tiny;
            Widgets.Label(labelRect, comp.parent.LabelCap);
            
            // 弹药条占下半
            Rect barRect = new Rect(inner)
            {
                yMin = inner.y + inner.height / 2f
            };
            
            float fillPct = provider.MaxAmmo > 0 ? (float)provider.CurrentAmmo / provider.MaxAmmo : 0f;
            Widgets.FillableBar(barRect, fillPct, FullBarTex, EmptyBarTex, doBorder: false);
            
            // 弹药数量文字
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(barRect, $"{provider.CurrentAmmo} / {provider.MaxAmmo}");
            Text.Anchor = TextAnchor.UpperLeft;
            
            return new GizmoResult(GizmoState.Clear);
        }
    }
}

