using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace AzWeaponLib.SpecialProjectile
{
    public class CompAbilityEffect_LaunchProjectileWithPreview : CompAbilityEffect_LaunchProjectile
    {
        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            base.DrawEffectPreview(target);
            if (Props.projectileDef.thingClass == typeof(ElectricArc))
            { 
                ElectricArcDef electricArcDef = Props.projectileDef.GetModExtension<ElectricArcDef>();
                GenDraw.DrawRadiusRing(target.Cell, electricArcDef.ShouldExtra(target, Find.CurrentMap) ? electricArcDef.conductRangeExtra : electricArcDef.conductRange);
            }
        }
    }
}
