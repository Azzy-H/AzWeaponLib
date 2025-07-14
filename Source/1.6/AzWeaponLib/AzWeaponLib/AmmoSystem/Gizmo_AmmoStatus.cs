using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;
using AzWeaponLib;

namespace AzWeaponLib.AmmoSystem
{
    [StaticConstructorOnStartup]
    public class Gizmo_AmmoStatus : Gizmo
    {
        public int ammunitionCapacity;
        public int amunitionRemained;
        public bool autoReload;
        public Func<bool> autoReloadToggle;
        public Action<bool, bool> makeReloadJob;
        public bool canAutoReloadToggleNow = true;
        public bool canReloadNow = true;
        public int backupAmmo;
        public string failedReason;
        private static readonly Texture2D fullAmmoBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.2f, 0.24f));
        private static readonly Texture2D emptyAmmoBarTex = SolidColorMaterials.NewSolidColorTexture(Color.clear);
        private static readonly Color disableColor = new Color(0.8f, 0.8f, 0.7f, 0.5f);
        private static readonly Texture2D autoReloadEnableTex = Widgets.CheckboxOnTex;
        private static readonly Texture2D autoReloadDisableTex = Widgets.CheckboxOffTex;
        private static readonly Texture2D reloadActionTex = ContentFinder<Texture2D>.Get("AmmoSystem/reload") ?? BaseContent.BadTex;
        protected virtual Texture2D FullAmmoBarTex => fullAmmoBarTex;
        protected virtual Texture2D EmptyAmmoBarTex => emptyAmmoBarTex;
        protected virtual Color DisableColor => disableColor;
        protected virtual Texture2D AutoReloadEnableTex => autoReloadEnableTex;
        protected virtual Texture2D AutoReloadDisableTex => autoReloadDisableTex;
        protected virtual Texture2D ReloadActionTex => reloadActionTex;

        public Gizmo_AmmoStatus()
        {
            Order = -100f;
        }

        public override float GetWidth(float maxWidth)
        {
            return 140f;
        }
        protected virtual float FillPercent => amunitionRemained / Mathf.Max(1f, ammunitionCapacity);

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);//Gzimo本体
            Rect rect2 = rect.ContractedBy(6f);//缩小留出边界
            Widgets.DrawWindowBackground(rect);//画出本体
            Rect rect3 = rect2;
            rect3.height = rect.height / 2f;//上方空白占1/2
            Text.Font = GameFont.Tiny;
            Widgets.Label(rect3, "AWL_AmmunitionGizmoLabel".Translate());//上方说明
            Rect rect4 = rect2;
            rect4.yMin = rect2.y + rect2.height / 2f;//下方条占1/2
            Widgets.FillableBar(rect4, FillPercent, FullAmmoBarTex, EmptyAmmoBarTex, doBorder: false);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect4, GetInfoDispString());//数字显示
            Text.Anchor = TextAnchor.UpperLeft;
            TooltipHandler.TipRegion(rect2, "AWL_AmmunitionGizmoTip".Translate());

            Rect rectButtonReload = rect3;
            rectButtonReload.width = rectButtonReload.height;
            rectButtonReload.x = rect3.xMax - rectButtonReload.width;//画在右上角
            Rect rectButtonAutoReload = rectButtonReload;
            rectButtonAutoReload.xMin += rectButtonReload.width / 2;
            rectButtonAutoReload.yMin += rectButtonReload.height / 2;//画在rectButtonReload左下角
            if (Widgets.ButtonImage(rectButtonReload, ReloadActionTex, baseColor: canReloadNow ? Color.white : DisableColor, tooltip: canReloadNow ? (string)"AWL_PawnTooltipReload".Translate() : failedReason))
            {

                if (Event.current.button == 0)
                {
                    if (canReloadNow)
                    {
                        makeReloadJob.Invoke(true, KeyBindingDefOf.QueueOrder.IsDownEvent);
                        if (autoReload)
                        {
                            SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                        }
                        else
                        {
                            SoundDefOf.Tick_High.PlayOneShotOnCamera();
                        }
                    }
                }
                else if (Event.current.button == 1)
                {
                    if (canAutoReloadToggleNow)
                    {
                        autoReload = autoReloadToggle.Invoke();
                        if (autoReload)
                        {
                            SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                        }
                        else
                        {
                            SoundDefOf.Tick_High.PlayOneShotOnCamera();
                        }
                    }
                }
            }//按钮交互
            GUI.DrawTexture(rectButtonAutoReload, autoReload ? AutoReloadEnableTex : AutoReloadDisableTex);
            return new GizmoResult(GizmoState.Clear);
        }
        protected virtual string GetInfoDispString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(amunitionRemained.ToString());
            sb.Append(" / ");
            sb.Append(ammunitionCapacity.ToString());
            if (backupAmmo >= 0)
            {
                sb.Append(" ( ");
                sb.Append(backupAmmo.ToString());
                sb.Append(" )");
            }
            return sb.ToString();
        }
    }
}
