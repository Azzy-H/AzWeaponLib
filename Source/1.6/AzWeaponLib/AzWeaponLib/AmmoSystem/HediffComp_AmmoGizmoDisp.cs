using RimWorld.BaseGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using AzWeaponLib;

namespace AzWeaponLib.AmmoSystem
{
    public class HediffCompProperties_AmmoGizmoDisp : HediffCompProperties
    {
        public HediffCompProperties_AmmoGizmoDisp()
        {
            compClass = typeof(HediffComp_AmmoGizmoDisp);
        }
    }
    public class HediffComp_AmmoGizmoDisp : HediffComp
    {
        private bool compShouldRemove = false;
        public override bool CompShouldRemove => compShouldRemove;
        private CompAmmo compAmmoInt;
        public CompAmmo compAmmo
        { 
            get 
            { 
                if(Pawn == null || Pawn.equipment == null || Pawn.equipment.Primary == null) return null;
                if (compAmmoInt == null)
                {
                    compAmmoInt = Pawn.equipment.Primary.TryGetComp<CompAmmo>();
                }
                return compAmmoInt;
            }
        }
        public override IEnumerable<Gizmo> CompGetGizmos()
        {
            if (compAmmo == null)
            {
                compShouldRemove = true;
                yield break;
            }
            if (compAmmo.useAmmoSystem)
            {
                foreach (Gizmo g in compAmmo.GetAmmoGizmos()) yield return g;
            }
        }
        public override void CompPostMake()
        {
            if (compAmmo == null) 
            { 
                compShouldRemove = true;
                return;
            }
        }
    }
}
