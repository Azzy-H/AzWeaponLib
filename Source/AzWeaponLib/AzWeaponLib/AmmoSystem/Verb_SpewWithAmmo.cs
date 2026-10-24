using System.Collections.Generic;
using System.Linq;
using AzWeaponLib.AmmoSystem;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AzWeaponLib.AmmoSystem
{
    public class VerbProperties_SpewWithAmmo : VerbProperties_ShootWithAmmo
    {
        /// <summary>单发喷射的最大偏转角（度）。每发都会在此范围内随机偏转一次，连续射击即形成扇面。</summary>
        public float affectedAngle = 5f;

        public VerbProperties_SpewWithAmmo()
        {
            verbClass = typeof(Verb_SpewWithAmmo);
        }
    }

    /// <summary>
    /// 喷射类 verb：每次 TryCastShot 在 ±affectedAngle 内随机偏转一次，生成一颗投射物并 Launch，
    /// 由投射物自己飞行、穿透、撞墙滑走 —— 于是自动获得飞行时间、护盾拦截、掩体判定与伤害归属。
    ///
    /// 不经过 ShotReport / 命中率判定：火焰喷射是范围压制，不是精确射击；
    /// 墙体阻挡与命中概率由 SEA.JetProjectile 自己的碰撞与筛选负责。
    /// </summary>
    public class Verb_SpewWithAmmo : Verb_ShootWithAmmo
    {
        public new VerbProperties_SpewWithAmmo VerbProps => (VerbProperties_SpewWithAmmo)verbProps;

        protected override bool Verb_LaunchProjectile_TryCastShot()
        {
            if (currentTarget.HasThing && currentTarget.Thing.Map != caster.Map)
            {
                return false;
            }
            ThingDef projectile = Projectile;
            if (projectile == null)
            {
                return false;
            }
            ShootLine resultingLine;
            bool flag = TryFindShootLineFromTo(caster.Position, currentTarget, out resultingLine);
            if (verbProps.stopBurstWithoutLos && !flag)
            {
                return false;
            }
            if (base.EquipmentSource != null)
            {
                base.EquipmentSource.GetComp<CompChangeableProjectile>()?.Notify_ProjectileLaunched();
                base.EquipmentSource.GetComp<CompApparelVerbOwner_Charged>()?.UsedOnce();
            }
            lastShotTick = Find.TickManager.TicksGame;
            Thing? manningPawn = caster;
            Thing? equipmentSource = base.EquipmentSource;
            CompMannable compMannable = caster.TryGetComp<CompMannable>();
            if (compMannable?.ManningPawn != null)
            {
                manningPawn = compMannable.ManningPawn;
                equipmentSource = caster;
            }


            // ---- 1) 喷射方向：朝向目标的角度 + 扇面随机偏转 ----
            Vector2 aim = new Vector2(resultingLine.Dest.x - resultingLine.Source.x, resultingLine.Dest.z - resultingLine.Source.z);
            if (aim.sqrMagnitude < 0.01f)
            {
                aim = Vector2.up;   // 目标就在脚下：给一个默认朝向，避免零向量
            }
            float spread = VerbProps.affectedAngle > 0f
                ? Rand.Range(-VerbProps.affectedAngle, VerbProps.affectedAngle)
                : 0f;
            Vector2 planarDir = aim.normalized.RotatedBy(spread);
            Vector3 dir = new Vector3(planarDir.x, 0f, planarDir.y);

            // ---- 2) 射程末端（夹进地图，避免把越界格交给 Launch） ----
            Vector3 muzzle = caster.DrawPos;
            Vector3 end = muzzle + dir * verbProps.range;
            end.x = Mathf.Clamp(end.x, 0.5f, caster.Map.Size.x - 0.5f);
            end.z = Mathf.Clamp(end.z, 0.5f, caster.Map.Size.z - 0.5f);
            IntVec3 endCell = end.ToIntVec3();

            // ---- 3) 生成并发射 ----
            // 不走 ShotReport / 命中率判定：范围压制不需要精确命中，
            // 墙体阻挡与命中概率交给投射物自己的碰撞与筛选。
            Projectile projectile2 = (Projectile)GenSpawn.Spawn(projectile, resultingLine.Source, caster.Map);
            projectile2.Launch(
                caster,
                muzzle,
                new LocalTargetInfo(endCell),
                currentTarget,
                ProjectileHitFlags.All,
                preventFriendlyFire,
                equipmentSource,
                null);

            lastShotTick = Find.TickManager.TicksGame;
            return true;
        }

        public override void DrawHighlight(LocalTargetInfo target)
        {
            base.DrawHighlight(target);
            if (target.IsValid)
            {
                IntVec3 position = caster.Position;
                float num = Mathf.Atan2(-(target.Cell.z - position.z), target.Cell.x - position.x) * 57.29578f;
                RenderPredictedAreaOfEffect(caster.Position, VerbProps.range, verbProps.explosionRadiusRingColor, new FloatRange(num - VerbProps.affectedAngle, num + VerbProps.affectedAngle));
            }
        }

        public static void RenderPredictedAreaOfEffect(IntVec3 loc, float radius, Color color, FloatRange affectedAngle)
        {
            List<IntVec3> cells1;
            List<IntVec3> cells2;
            List<IntVec3> cellsSum;
            if (affectedAngle.min < -180f || affectedAngle.max > 180f)
            {
                cells1 = DamageDefOf.Bomb.Worker.ExplosionCellsToHit(loc, Find.CurrentMap, radius, affectedAngle: new FloatRange(AngleWrapped(affectedAngle.min), 180f)).ToList();
                cells2 = DamageDefOf.Bomb.Worker.ExplosionCellsToHit(loc, Find.CurrentMap, radius, affectedAngle: new FloatRange(-180f, AngleWrapped(affectedAngle.max))).ToList();
                cellsSum = cells1.Concat(cells2).ToList();
            }
            else
            {
                cellsSum = DamageDefOf.Bomb.Worker.ExplosionCellsToHit(loc, Find.CurrentMap, radius, affectedAngle: affectedAngle).ToList();
            }
            GenDraw.DrawFieldEdges(cellsSum, color);
        }

        public static float AngleWrapped(float angle)
        {
            while (angle > 180f)
            {
                angle -= 360f;
            }
            while (angle < -180f)
            {
                angle += 360f;
            }
            return (angle == 180f) ? (-180f) : angle;
        }
    }

    public class Verb_SpewWithAmmoConstantly : Verb_SpewWithAmmo
    {
        protected override bool TryCastShot()
        {
            Pawn? casterPawn = CasterPawn;
            if (casterPawn != null && casterPawn.jobs?.curJob != null)
            {
                Job job = casterPawn.jobs.curJob;
                LocalTargetInfo casterTarget = job.targetA;
                bool waiting = job.def == JobDefOf.Wait_Combat;
                bool attacking = job.def == JobDefOf.AttackStatic
                    && (casterTarget.Cell == CurrentTarget.Cell || casterTarget.Thing == CurrentTarget.Thing);
                if (waiting || attacking)
                {
                    if (!(currentTarget.Pawn?.DeadOrDowned ?? currentTarget.Thing?.Destroyed ?? false))
                    {
                        burstShotsLeft++;
                    }
                }
            }
            else
            {
                burstShotsLeft++;
            }
            return base.TryCastShot();
        }
    }
}
