using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using static HarmonyLib.Code;
using static UnityEngine.GraphicsBuffer;

namespace AzWeaponLib.SpecialProjectile
{
    public class Projectile_Homing : Bullet
    {
        private HomingProjectileDef homingDefInt;
        public HomingProjectileDef HomingDef
        {
            get
            {
                if(homingDefInt == null ) homingDefInt = def.GetModExtension<HomingProjectileDef>();
                return homingDefInt;
            }
        }

        private Sustainer ambientSustainer;
        private List<ThingComp> comps;

        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
        {
            bool flag = false;
            if (usedTarget.HasThing && usedTarget.Thing is IAttackTarget)
            {
                if (Rand.Chance(GetHitChance(usedTarget.Thing)))
                {
                    hitFlags |= ProjectileHitFlags.IntendedTarget;
                    intendedTarget = usedTarget;
                    //Log.Message("应该击中");
                    flag = true;
                }
            }
            else if (Rand.Chance(GetHitChance(intendedTarget.Thing)))
            {
                hitFlags |= ProjectileHitFlags.IntendedTarget;
                usedTarget = intendedTarget;
                //Log.Message("应该击中");
                flag = true;
            }
            if(flag)
            {
                hitFlags &= ~ProjectileHitFlags.IntendedTarget;
                //Log.Message("应该脱靶");
            }
            base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);
            exactPositionInt = origin.Yto0() + Vector3.up * def.Altitude;
            Vector3 aimBase = (destination - origin).Yto0().normalized;
            float rotateAngle = Rand.Range(-HomingDef.initRotateAngle, HomingDef.initRotateAngle);
            //初始旋转
            Vector2 initSpeedBase2 = new Vector2(aimBase.x, aimBase.z);
            initSpeedBase2 = initSpeedBase2.RotatedBy(rotateAngle);
            Vector3 initSpeedBase3 = new Vector3(initSpeedBase2.x, 0, initSpeedBase2.y);
            if (HomingDef.speedRangeOverride == null)
            {
                curSpeed = initSpeedBase3 * def.projectile.SpeedTilesPerTick;
            }
            else 
            {
                curSpeed = initSpeedBase3 * HomingDef.SpeedRangeTilesPerTickOverride.RandomInRange;
            }
            ticksToImpact = int.MaxValue;
            lifetime = int.MaxValue;
            ReflectInit();
        }
        protected void ReflectInit()
        {
            if (!def.projectile.soundAmbient.NullOrUndefined())
            {
                ambientSustainer = (Sustainer)(NonPublicFields.Projectile_AmbientSustainer.GetValue(this));
            }
            comps = (List<ThingComp>)(NonPublicFields.ThingWithComps_comps.GetValue(this));
        }
        public float GetHitChance(Thing thing)
        {
            float chance = HomingDef.hitChance;

            // 目标为空时直接返回基础命中率
            if (thing == null) return chance;

            if (thing is Pawn pawn)
            {
                chance *= Mathf.Clamp(pawn.BodySize, 0.5f, 1.5f);
                // 目标姿势影响命中率，站立时更容易命中
                if (pawn.GetPosture() != PawnPosture.Standing)
                {
                    chance *= 0.5f;
                }
                // 武器质量对命中率的影响
                float qualityFactor = 1f;
                switch (equipmentQuality)
                {
                    case QualityCategory.Awful:
                        qualityFactor = 0.5f;
                        break;
                    case QualityCategory.Poor:
                        qualityFactor = 0.75f;
                        break;
                    case QualityCategory.Normal:
                        qualityFactor = 1f;
                        break;
                    case QualityCategory.Excellent:
                        qualityFactor = 1.1f;
                        break;
                    case QualityCategory.Masterwork:
                        qualityFactor = 1.2f;
                        break;
                    case QualityCategory.Legendary:
                        qualityFactor = 1.3f;
                        break;
                    default:
                        Log.Message("Unknown QualityCategory, returning default qualityFactor = 1");
                        break;
                }
                chance *= qualityFactor;
            }
            else
            {
                // 对于非Pawn目标根据其体积调整命中概率
                chance *= 1.5f * thing.def.fillPercent;
            }
            //Log.Message(chance);
            return Mathf.Clamp(chance, 0f, 1f);
        }


        protected Vector3 exactPositionInt;
        public override Vector3 ExactPosition
        { 
            get { return exactPositionInt; }
        }

        public Vector3 curSpeed;
        public override Quaternion ExactRotation => Quaternion.LookRotation(curSpeed);
        public bool homing = true;
        public virtual void MovementTick()
        {
            Vector3 expectPosition = ExactPosition + curSpeed;
            ShootLine shootLine = new ShootLine(ExactPosition.ToIntVec3(), expectPosition.ToIntVec3());
            Vector3 distance = (intendedTarget.Cell.ToVector3() - ExactPosition).Yto0();//距离目标的向量
            if (homing)
            {
                Vector3 vecDiffNorm = distance.normalized - curSpeed.normalized;
                if (vecDiffNorm.sqrMagnitude >= 1.414f)//distance和当前速度夹角大于90度
                {
                    homing = false;
                    lifetime = HomingDef.destroyTicksAfterLosingTrack.RandomInRange;
                    ticksToImpact = lifetime;
                    //Log.Message("角度过大，脱靶");
                    HitFlags &= ~ProjectileHitFlags.IntendedTarget;
                    HitFlags |= ProjectileHitFlags.NonTargetPawns;
                    HitFlags |= ProjectileHitFlags.NonTargetWorld;
                }
                else
                {
                    curSpeed += vecDiffNorm * HomingDef.homingSpeed * curSpeed.magnitude;
                }
            }
            foreach (IntVec3 cell in shootLine.Points())
            {
                IntVec3 distanceTmp = (intendedTarget.Cell - cell);//距离目标的向量
                if (distanceTmp.SqrMagnitude <= HomingDef.proximityFuseRange * HomingDef.proximityFuseRange)//distanceTmp距离小于近炸引信距离或者在同一格子时
                {
                    homing = false;
                    lifetime = HomingDef.destroyTicksAfterLosingTrack.RandomInRange;
                    if ((HitFlags & ProjectileHitFlags.IntendedTarget) == ProjectileHitFlags.IntendedTarget || HomingDef.proximityFuseRange > 0)//原计划是击中目标或是近炸
                    {
                        //Log.Message("击中");
                        lifetime = 0;
                        ticksToImpact = 0;
                        expectPosition = cell.ToVector3();
                        if (Find.TickManager.CurTimeSpeed == TimeSpeed.Normal && def.projectile.soundImpactAnticipate != null)
                        {
                            def.projectile.soundImpactAnticipate.PlayOneShot(this);
                        }
                    }
                }
            }
            //if (curSpeed.sqrMagnitude < 25f)//速度低于5则落地
            //{
            //    //Log.Message("失速过多，着陆");
            //    lifetime = 1;
            //    ticksToImpact = 1;
            //    HitFlags &= ~ProjectileHitFlags.IntendedTarget;
            //}
            exactPositionInt = expectPosition;
            curSpeed *= (curSpeed.magnitude + HomingDef.SpeedChangeTilesPerTickOverride) / curSpeed.magnitude;
        }
        protected override void Tick()
        {
            ThingWithCompsTick();
            lifetime--;
            if (landed)
            {
                return;
            }
            Vector3 exactPositionPast = ExactPosition;
            ticksToImpact--;

            MovementTick();
            if (!ExactPosition.InBounds(base.Map))
            {
                //ticksToImpact++;
                base.Position = exactPositionPast.ToIntVec3();
                Destroy();
                return;
            }
            Vector3 exactPositionNow = ExactPosition;
            object[] parameters = new object[] { exactPositionPast, exactPositionNow };
            if ((bool)ProjectileCheckForFreeInterceptBetween.Invoke(this, parameters))
            {
                return;
            }
            base.Position = ExactPosition.ToIntVec3();
            if (ticksToImpact == 60 && Find.TickManager.CurTimeSpeed == TimeSpeed.Normal && def.projectile.soundImpactAnticipate != null)
            {
                def.projectile.soundImpactAnticipate.PlayOneShot(this);
            }
            if (ticksToImpact <= 0)
            {
                //if (DestinationCell.InBounds(base.Map))
                //{
                //    base.Position = DestinationCell;
                //}
                ImpactSomething();
            }
            else if (ambientSustainer != null)
            {
                ambientSustainer.Maintain();
            }
        }
        private void ThingWithCompsTick()
        {
            if (comps != null)
            {
                int i = 0;
                for (int count = comps.Count; i < count; i++)
                {
                    comps[i].CompTick();
                }
            }
        }
       
        private static MethodInfo ProjectileCheckForFreeInterceptBetween = typeof(Projectile).GetMethod("CheckForFreeInterceptBetween", BindingFlags.NonPublic | BindingFlags.Instance);
        protected override void Impact(Thing hitThing, bool blockedByShield = false) 
        {
            Map map = Map;
            IntVec3 cell = Position;
            base.Impact(hitThing, blockedByShield);
            if (HomingDef.extraProjectile != null)
            {
                if (hitThing != null && hitThing.Spawned)
                {
                    ((Projectile)GenSpawn.Spawn(HomingDef.extraProjectile, Position, map)).Launch(launcher, ExactPosition, hitThing, hitThing, ProjectileHitFlags.All);
                }
                else
                {
                    ((Projectile)GenSpawn.Spawn(HomingDef.extraProjectile, Position, map)).Launch(launcher, ExactPosition, cell, cell, ProjectileHitFlags.All);
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref exactPositionInt, "exactPosition");
            Scribe_Values.Look(ref curSpeed, "curSpeed");
            Scribe_Values.Look(ref homing, "homing");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                ReflectInit();
            }
        }
    }
}
