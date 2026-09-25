using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace AzWeaponLib.SpecialProjectile
{
    public class Projectile_ExplosiveWithFrag : Projectile_Explosive
    {
        protected override void Explode()
        {
            ModExtension_Cone modext = def.GetModExtension<ModExtension_Cone>();
            if (modext != null)
            {
                modext.DoConeExplosion(Position, Map, ExactRotation, def.projectile.damageDef, launcher, 
                    explosionSound: def.projectile.soundExplode, weapon: equipmentDef, projectile: def,
                    intendedTarget: intendedTarget.Thing, postExplosionSpawnThingDef: def.projectile.postExplosionSpawnThingDef,
                    postExplosionSpawnChance: def.projectile.postExplosionSpawnChance, postExplosionSpawnThingCount: def.projectile.postExplosionSpawnThingCount,
                    postExplosionGasType: def.projectile.postExplosionGasType, postExplosionGasRadiusOverride: null, postExplosionGasAmount: 255,
                    applyDamageToExplosionCellsNeighbors: def.projectile.applyDamageToExplosionCellsNeighbors, preExplosionSpawnThingDef: def.projectile.preExplosionSpawnThingDef,
                    preExplosionSpawnChance: def.projectile.preExplosionSpawnChance, preExplosionSpawnThingCount: def.projectile.preExplosionSpawnThingCount,
                    chanceToStartFire: def.projectile.explosionChanceToStartFire, damageFalloff: def.projectile.explosionDamageFalloff, direction: ExactRotation.eulerAngles.y);
            }
            Destroy();
        }
    }
}
