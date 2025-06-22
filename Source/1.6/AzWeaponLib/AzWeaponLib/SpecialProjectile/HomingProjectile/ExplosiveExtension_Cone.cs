using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace AzWeaponLib.SpecialProjectile
{
    public class ModExtension_Cone : DefModExtension
    {
        public float coneAngle = 10f;
        public float coneRange = 7f;
        public int repeatExplosionCount = 1;
        public ThingDef fragment;
        public int fragmentCount;
        public FloatRange? fragmentRange;
        public bool showConeEffect = true;

        public void DoConeExplosion(IntVec3 center,
            Map map,
            Quaternion rotation,
            //float radius,
            DamageDef damType,
            Thing instigator,
            int damAmount = -1,
            float armorPenetration = -1f,
            SoundDef explosionSound = null,
            ThingDef weapon = null,
            ThingDef projectile = null,
            Thing intendedTarget = null,
            ThingDef postExplosionSpawnThingDef = null,
            float postExplosionSpawnChance = 0f,
            int postExplosionSpawnThingCount = 1,
            GasType? postExplosionGasType = null,
            float? postExplosionGasRadiusOverride =null,
            int postExplosionGasAmount = 255,
            bool applyDamageToExplosionCellsNeighbors = false,
            ThingDef preExplosionSpawnThingDef = null,
            float preExplosionSpawnChance = 0f,
            int preExplosionSpawnThingCount = 1,
            float chanceToStartFire = 0f,
            bool damageFalloff = false,
            float? direction = null,
            List<Thing> ignoredThings = null,
            //FloatRange? affectedAngle = null,
            //bool doVisualEffects = true,
            float propagationSpeed = 1f,
            float excludeRadius = 0f,
            //bool doSoundEffects = true,
            ThingDef postExplosionSpawnThingDefWater = null,
            float screenShakeFactor = 1f,
            SimpleCurve flammabilityChanceCurve = null,
            List<IntVec3> overrideCells = null)
        {
            Vector3 forwardDirection = rotation * Vector3.forward;
            FloatRange angle = new FloatRange(forwardDirection.ToAngleFlat() - coneAngle, forwardDirection.ToAngleFlat() + coneAngle);
            for (int i = 0; i < repeatExplosionCount; i++)
            {
                if (angle.max > 360f)
                {
                    FloatRange angle2 = new FloatRange(0f, angle.max - 360f);
                    GenExplosion.DoExplosion(center, map, coneRange, damType, instigator, damAmount, armorPenetration, explosionSound, weapon, projectile, intendedTarget,
                postExplosionSpawnThingDef, postExplosionSpawnChance, postExplosionSpawnThingCount, postExplosionGasType, postExplosionGasRadiusOverride, postExplosionGasAmount, applyDamageToExplosionCellsNeighbors,
                preExplosionSpawnThingDef, preExplosionSpawnChance, preExplosionSpawnThingCount,
                chanceToStartFire, damageFalloff, direction, ignoredThings,
                angle2, showConeEffect, propagationSpeed, excludeRadius, showConeEffect, postExplosionSpawnThingDefWater, screenShakeFactor, flammabilityChanceCurve, overrideCells);
                }
                if (angle.min < 0f)
                {
                    FloatRange angle2 = new FloatRange(angle.min + 360f, 360f);
                    GenExplosion.DoExplosion(center, map, coneRange, damType, instigator, damAmount, armorPenetration, explosionSound, weapon, projectile, intendedTarget,
                postExplosionSpawnThingDef, postExplosionSpawnChance, postExplosionSpawnThingCount, postExplosionGasType, postExplosionGasRadiusOverride, postExplosionGasAmount, applyDamageToExplosionCellsNeighbors,
                preExplosionSpawnThingDef, preExplosionSpawnChance, preExplosionSpawnThingCount,
                chanceToStartFire, damageFalloff, direction, ignoredThings,
                angle2, showConeEffect, propagationSpeed, excludeRadius, showConeEffect, postExplosionSpawnThingDefWater, screenShakeFactor, flammabilityChanceCurve, overrideCells);
                }
                GenExplosion.DoExplosion(center, map, coneRange, damType, instigator, damAmount, armorPenetration, explosionSound, weapon, projectile, intendedTarget,
                postExplosionSpawnThingDef, postExplosionSpawnChance, postExplosionSpawnThingCount, postExplosionGasType, postExplosionGasRadiusOverride, postExplosionGasAmount, applyDamageToExplosionCellsNeighbors,
                preExplosionSpawnThingDef, preExplosionSpawnChance, preExplosionSpawnThingCount,
                chanceToStartFire, damageFalloff, direction, ignoredThings,
                angle, showConeEffect, propagationSpeed, excludeRadius, showConeEffect, postExplosionSpawnThingDefWater, screenShakeFactor, flammabilityChanceCurve, overrideCells);
            }
            if (fragment != null)
            {
                IEnumerable<IntVec3> cells = FragmentCells(center, angle, fragmentRange.HasValue ? fragmentRange.Value : new FloatRange(0, coneRange));
                for (int j = 0; j < fragmentCount; j++)
                {
                    IntVec3 cell = cells.RandomElement();
                    ((Projectile)GenSpawn.Spawn(fragment, center, map)).Launch(instigator, cell, cell, ProjectileHitFlags.All);
                }
            }
        }
        private IEnumerable<IntVec3> FragmentCells(IntVec3 center, FloatRange? angle, FloatRange range)
        {
            int minRange = GenRadial.NumCellsInRadius(range.min);
            int maxRange = GenRadial.NumCellsInRadius(range.max);
            for (int i = minRange; i < maxRange; i++)
            {
                IntVec3 intVec = center + GenRadial.RadialPattern[i];
                if (angle.HasValue)
                {
                    float angelMin = angle?.min ?? 0f;
                    float angleMax = angle?.max ?? 0f;
                    float lengthHorizontal = (intVec - center).LengthHorizontal;
                    float num4 = lengthHorizontal / angleMax;
                    if (lengthHorizontal <= 0.5f)
                    {
                        continue;
                    }
                    float cellAngle = Mathf.Atan2(-(intVec.z - center.z), intVec.x - center.x) * 57.29578f;
                    if (angelMin < 0f && cellAngle - angelMin > 360f)
                    {
                        cellAngle -= 360f;
                    }
                    if (angleMax > 360f && angleMax - cellAngle < 360f)
                    {
                        cellAngle += 360f;
                    }
                    if (cellAngle - angelMin < -0.5f * num4 || cellAngle - angleMax > 0.5f * num4)
                    {
                        continue;
                    }
                }
                yield return intVec;
            }
        }
    }
}
