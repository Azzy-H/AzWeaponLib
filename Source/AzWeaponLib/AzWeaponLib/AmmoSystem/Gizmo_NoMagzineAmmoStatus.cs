using AzWeaponLib.AmmoSystem;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace AzWeaponLib.AmmoSystem
{
    public class Gizmo_NoMagzineAmmoStatus : Gizmo_AmmoStatus
    {
        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);//Gizmo本体
            Rect rect2 = rect.ContractedBy(6f);//缩小留出边界
            Widgets.DrawWindowBackground(rect);//画出本体
            Rect rect3 = rect2;
            rect3.height = rect.height / 2f;//上方空白占1/2
            Text.Font = GameFont.Tiny;
            Widgets.Label(rect3, gizmoLabel);//上方说明
            Rect rect4 = rect2;
            rect4.yMin = rect2.y + rect2.height / 2f;//下方条占1/2
            Widgets.FillableBar(rect4, FillPercent, FullAmmoBarTex, EmptyAmmoBarTex, doBorder: false);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect4, GetInfoDispString());//数字显示
            Text.Anchor = TextAnchor.UpperLeft;
            TooltipHandler.TipRegion(rect2, gizmoTip);
            return new GizmoResult(GizmoState.Clear);
        }
    }
}
