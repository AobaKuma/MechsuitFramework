using RimWorld;
using UnityEngine;
using Verse;

namespace Exosuit
{
    // 燃料电池的 Gizmo 显示
    // 结构与原版护盾 Gizmo 相同
    [StaticConstructorOnStartup]
    public class Gizmo_FuelCell : Gizmo
    {
        #region 常量

        private static readonly Texture2D FullBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.6f, 0.2f));
        private static readonly Texture2D EmptyBarTex = SolidColorMaterials.NewSolidColorTexture(Color.clear);

        #endregion

        #region 字段

        public CompFuelCell comp;

        #endregion

        #region 构造函数

        public Gizmo_FuelCell(CompFuelCell comp)
        {
            this.comp = comp;
            Order = -100f;
        }

        #endregion

        #region 重写方法

        public override float GetWidth(float maxWidth)
        {
            return 140f;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect rect = new(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Rect inner = rect.ContractedBy(6f);
            Widgets.DrawWindowBackground(rect);
            
            // 标题
            Rect titleRect = inner;
            titleRect.height = rect.height / 2f;
            Text.Font = GameFont.Tiny;
            Widgets.Label(titleRect, comp.parent.LabelCap);
            
            // 燃料条
            Rect barRect = inner;
            barRect.yMin = inner.y + inner.height / 2f;
            float fillPercent = comp.FuelPercentArbitrary;
            Widgets.FillableBar(barRect, fillPercent, FullBarTex, EmptyBarTex, doBorder: false);
            
            // 燃料数值
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            string fuelText = $"{comp.Fuel:F0} / {comp.Props.fuelCapacity:F0}";
            Widgets.Label(barRect, fuelText);
            Text.Anchor = TextAnchor.UpperLeft;
            
            // 提示
            TooltipHandler.TipRegion(inner, "WG_FuelCell_Tip".Translate());
            
            return new GizmoResult(GizmoState.Clear);
        }

        #endregion
    }
}
