using AzWeaponLib.SpecialProjectile;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                Thing t = ThingMaker.MakeThing(gasExplosionDef.gasDef);
                if (t is AWLGas gas)
                {
                    gas.initDensity = gasExplosionDef.density;
                }
                GenSpawn.Spawn(t, cell, map);
            }
            base.Explode();
        }
    }
}
