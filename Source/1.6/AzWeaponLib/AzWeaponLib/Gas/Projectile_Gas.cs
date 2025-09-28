using AzWeaponLib.SpecialProjectile;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace AzWeaponLib.Gas
{
    public class GasExplosionDef : DefModExtension
    {
        public float radius;
        public float density;
        public ThingDef gasDef;
    }
    public class Projectile_Gas : Projectile_Explosive
    {
        public GasExplosionDef GasExplosionDef => def.GetModExtension<GasExplosionDef>();
        protected override void Explode()
        {
            Map map = Map;
            GasExplosionDef gasExplosionDef = GasExplosionDef;
            IEnumerable<IntVec3> cells = DamageDefOf.Bomb.Worker.ExplosionCellsToHit(Position, map, gasExplosionDef.radius);
            foreach (IntVec3 cell in cells)
            {
                AWLGas gas = cell.GetFirstThing(map, gasExplosionDef.gasDef) as AWLGas;
                if (gas != null)
                {
                    gas.SetDensityTo(Mathf.Min(gas.density + gasExplosionDef.density, 1f));
                }
                else 
                {
                    gas = ThingMaker.MakeThing(gasExplosionDef.gasDef) as AWLGas;
                    if (gas != null)
                    {
                        gas.initDensity = gasExplosionDef.density;
                        GenSpawn.Spawn(gas, cell, map);
                    }
                }
            }
            base.Explode();
        }
    }
}
