using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using AzWeaponLib;

namespace AzWeaponLib.AmmoSystem
{
    public class VerbProperties_ShootWithAmmo : VerbProperties
    {
        public int bulletsPerShot = 1;
        public int ammoCostPerShot = 1;
        public float retargetRange = 0f;
        public float shotgunRetargetRange = 0f;
        public SimpleCurve shotgunRetargetChanceFromRange;
    }
    public class Verb_ShootWithAmmo : Verb_Shoot
    {
        public VerbProperties_ShootWithAmmo VerbProps => (VerbProperties_ShootWithAmmo)verbProps;
        private CompAmmo compAmmoInt;
        public CompAmmo compAmmo
        {
            get
            {
                if (compAmmoInt == null) compAmmoInt = CasterPawn.equipment.Primary.GetComp<CompAmmo>();
                return compAmmoInt;
            }
        }
        public virtual bool canShootNow
        {
            get
            {
                if (compAmmo == null) return true;
                return VerbProps.ammoCostPerShot <= compAmmo.Ammo;
            }
        }
        public bool useAmmoSystem
        { 
            get 
            { 
                if (compAmmo == null) return false;
                return compAmmo.useAmmoSystem;
            }
        }
        public float retargetChance;
        protected static Dictionary<Thing, ShootLine> victims = new Dictionary<Thing, ShootLine>();
        protected override bool TryCastShot()
        {
            if (VerbProps.shotgunRetargetRange > 1)
            {
                GetVicitm();
            }

            bool flag = false;
            if (!useAmmoSystem) 
            { 
                flag = TryCastMultiBulletShot(); 
            }
            else if (canShootNow && TryCastMultiBulletShot())
            {
                compAmmo.UsedByNum(VerbProps.ammoCostPerShot);
                if (!canShootNow) 
                {
                    compAmmo.TryMakeReloadJob();
                    compAmmo.AmmoExhausted(); 
                }
                flag = true;
            }

            if (flag && CasterIsPawn)
            {
                CasterPawn.records.Increment(RecordDefOf.ShotsFired);
            }
            return flag;
        }//尝试射出子弹
        protected virtual bool TryCastMultiBulletShot()
        {
            bool flag;
            int leftBullet = VerbProps.bulletsPerShot;
            do
            {
                flag = Verb_LaunchProjectile_TryCastShot();//base.TryCastShot();
                leftBullet--;
            } while (leftBullet > 0);
            return flag;
        }
        public override bool TryStartCastOn(LocalTargetInfo castTarg, LocalTargetInfo destTarg, bool surpriseAttack = false, bool canHitNonTargetPawns = true, bool preventFriendlyFire = false, bool nonInterruptingSelfCast = false)
        {
            if (useAmmoSystem && !canShootNow)
            {
                if(compAmmo.autoReload) compAmmo.TryMakeReloadJob();
                return false;
            }
            return base.TryStartCastOn(castTarg, destTarg, surpriseAttack, canHitNonTargetPawns, preventFriendlyFire, nonInterruptingSelfCast);
        }
        protected virtual bool Verb_LaunchProjectile_TryCastShot()
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
            Thing manningPawn = caster;
            Thing equipmentSource = base.EquipmentSource;
            CompMannable compMannable = caster.TryGetComp<CompMannable>();
            if (compMannable?.ManningPawn != null)
            {
                manningPawn = compMannable.ManningPawn;
                equipmentSource = caster;
            }
            Vector3 drawPos = caster.DrawPos;
            Projectile projectile2 = (Projectile)GenSpawn.Spawn(projectile, resultingLine.Source, caster.Map);
            ProjectileHitFlags hitFlags = ProjectileHitFlags.None;
            LocalTargetInfo usedTarget = null;
            ThingDef targetCoverDef = null;
            //尝试重定位
            //Log.Message("try shoot");
            if (VerbProps.shotgunRetargetRange > 1 && victims.Count > 0)
            {
                //Log.Message("new shoot");
                foreach (var kv in victims)
                {
                    KeyValuePair<LocalTargetInfo, ShootLine> keyValuePair = new KeyValuePair<LocalTargetInfo, ShootLine> (kv.Key, kv.Value);
                    TryGetLaunchProjectileInfo(projectile2, manningPawn, keyValuePair, equipmentSource, drawPos, out usedTarget, out hitFlags, out targetCoverDef);
                    if ((hitFlags & ProjectileHitFlags.IntendedTarget) == ProjectileHitFlags.IntendedTarget) break;
                }
                if ((hitFlags & ProjectileHitFlags.IntendedTarget) == ProjectileHitFlags.IntendedTarget)
                {
                    projectile2.Launch(manningPawn, drawPos, usedTarget, currentTarget, hitFlags, preventFriendlyFire, equipmentSource, targetCoverDef);
                }
                else
                {
                    ShotReport shotReport = ShotReport.HitReportFor(caster, this, currentTarget);
                    resultingLine.ChangeDestToMissWild(shotReport.AimOnTargetChance_StandardTarget, projectile.projectile.flyOverhead, caster.Map);
                    projectile2.Launch(manningPawn, drawPos, resultingLine.Dest, currentTarget, hitFlags, preventFriendlyFire, equipmentSource, targetCoverDef);
                }
            }
            //原版逻辑
            else
            {
                //Log.Message("vanilla shoot");
                KeyValuePair<LocalTargetInfo, ShootLine> keyValuePair = new KeyValuePair<LocalTargetInfo, ShootLine>(currentTarget, resultingLine);
                TryGetLaunchProjectileInfo(projectile2, manningPawn, keyValuePair, equipmentSource, drawPos, out usedTarget, out hitFlags, out targetCoverDef);
                projectile2.Launch(manningPawn, drawPos, usedTarget, currentTarget, hitFlags, preventFriendlyFire, equipmentSource, targetCoverDef);
            }
            return true;
        }
        protected void TryGetLaunchProjectileInfo(Projectile projectile, Thing manningPawn, KeyValuePair<LocalTargetInfo, ShootLine> kv, Thing equipmentSource, Vector3 drawPos, out LocalTargetInfo usedTarget, out ProjectileHitFlags hitFlags, out ThingDef targetCoverDef)
        {
            LocalTargetInfo currentTarget = kv.Key;
            ShootLine resultingLine = kv.Value;
            if (equipmentSource.TryGetComp(out CompUniqueWeapon comp))
            {
                foreach (WeaponTraitDef item in comp.TraitsListForReading)
                {
                    if (item.damageDefOverride != null)
                    {
                        projectile.damageDefOverride = item.damageDefOverride;
                    }
                    if (!item.extraDamages.NullOrEmpty())
                    {
                        if (projectile.extraDamages == null)
                        {
                            projectile.extraDamages = new List<ExtraDamage>();
                        }
                        projectile.extraDamages.AddRange(item.extraDamages);
                    }
                }
            }
            if (verbProps.ForcedMissRadius > 0.5f)
            {
                float num = verbProps.ForcedMissRadius;
                if (manningPawn is Pawn pawn)
                {
                    num *= verbProps.GetForceMissFactorFor(equipmentSource, pawn);
                }
                float num2 = VerbUtility.CalculateAdjustedForcedMiss(num, base.currentTarget.Cell - caster.Position);
                if (num2 > 0.5f)
                {
                    IntVec3 forcedMissTarget = GetForcedMissTarget(num2);
                    if (forcedMissTarget != base.currentTarget.Cell)
                    {
                        ThrowDebugText("ToRadius");
                        ThrowDebugText("Rad\nDest", forcedMissTarget);
                        ProjectileHitFlags projectileHitFlags = ProjectileHitFlags.NonTargetWorld;
                        if (Rand.Chance(0.5f))
                        {
                            projectileHitFlags = ProjectileHitFlags.All;
                        }
                        if (!canHitNonTargetPawnsNow)
                        {
                            projectileHitFlags &= ~ProjectileHitFlags.NonTargetPawns;
                        }
                        usedTarget = forcedMissTarget;
                        hitFlags = projectileHitFlags;
                        targetCoverDef = null;
                        return;
                    }
                }
            }
            ShotReport shotReport = ShotReport.HitReportFor(caster, this, currentTarget);
            Thing randomCoverToMissInto = shotReport.GetRandomCoverToMissInto();
            targetCoverDef = randomCoverToMissInto?.def;
            if (verbProps.canGoWild && !Rand.Chance(shotReport.AimOnTargetChance_IgnoringPosture))
            {
                bool flyOverhead = projectile?.def?.projectile != null && projectile.def.projectile.flyOverhead;
                resultingLine.ChangeDestToMissWild(shotReport.AimOnTargetChance_StandardTarget, flyOverhead, caster.Map);
                ThrowDebugText("ToWild" + (canHitNonTargetPawnsNow ? "\nchntp" : ""));
                ThrowDebugText("Wild\nDest", resultingLine.Dest);
                ProjectileHitFlags projectileHitFlags2 = ProjectileHitFlags.NonTargetWorld;
                if (Rand.Chance(0.5f) && canHitNonTargetPawnsNow)
                {
                    projectileHitFlags2 |= ProjectileHitFlags.NonTargetPawns;
                }
                usedTarget = resultingLine.Dest;
                hitFlags = projectileHitFlags2;
                return;
            }
            if (currentTarget.Thing != null && currentTarget.Thing.def.CanBenefitFromCover && !Rand.Chance(shotReport.PassCoverChance))
            {
                ThrowDebugText("ToCover" + (canHitNonTargetPawnsNow ? "\nchntp" : ""));
                ThrowDebugText("Cover\nDest", randomCoverToMissInto.Position);
                ProjectileHitFlags projectileHitFlags3 = ProjectileHitFlags.NonTargetWorld;
                if (canHitNonTargetPawnsNow)
                {
                    projectileHitFlags3 |= ProjectileHitFlags.NonTargetPawns;
                }
                usedTarget = randomCoverToMissInto;
                hitFlags = projectileHitFlags3;
                return;
            }
            ProjectileHitFlags projectileHitFlags4 = ProjectileHitFlags.IntendedTarget;
            if (canHitNonTargetPawnsNow)
            {
                projectileHitFlags4 |= ProjectileHitFlags.NonTargetPawns;
            }
            if (!currentTarget.HasThing || currentTarget.Thing.def.Fillage == FillCategory.Full)
            {
                projectileHitFlags4 |= ProjectileHitFlags.NonTargetWorld;
            }
            ThrowDebugText("ToHit" + (canHitNonTargetPawnsNow ? "\nchntp" : ""));
            if (currentTarget.Thing != null)
            {
                ThrowDebugText("Hit\nDest", currentTarget.Cell);
                usedTarget = currentTarget;
                hitFlags = projectileHitFlags4;
                return;
            }
            else
            {
                ThrowDebugText("Hit\nDest", resultingLine.Dest);
                usedTarget = resultingLine.Dest;
                hitFlags = projectileHitFlags4;
                return;
            }
        }
        private void ThrowDebugText(string text)
        {
            if (DebugViewSettings.drawShooting)
            {
                MoteMaker.ThrowText(caster.DrawPos, caster.Map, text);
            }
        }
        private void ThrowDebugText(string text, IntVec3 c)
        {
            if (DebugViewSettings.drawShooting)
            {
                MoteMaker.ThrowText(c.ToVector3Shifted(), caster.Map, text);
            }
        }
        protected void GetVicitm()
        {
            //Log.Message("GetVicitm");
            victims.Clear();
            int num = GenRadial.NumCellsInRadius(VerbProps.shotgunRetargetRange);
            IntVec3 targetCell = currentTarget.Cell;
            for (int i = 0; i < num; i++)
            {
                IntVec3 cell = targetCell + GenRadial.RadialPattern[i];
                if (Vector3.Dot((cell - caster.Position).ToVector3(), (currentTarget.Cell - caster.Position).ToVector3()) < 0f) continue;
                if (!cell.InBounds(caster.Map)) continue;
                foreach (Thing victim in caster.Map.thingGrid.ThingsListAtFast(cell))
                {
                    ShootLine shootline;
                    if (!TryFindShootLineFromTo(caster.Position, victim, out shootline))
                    {
                        continue;
                    }
                    if ((victim is Pawn || victim.def.Fillage != FillCategory.None) && (victim.HostileTo(caster) || victim.Faction == null) && VerbProps.shotgunRetargetChanceFromRange.Evaluate(GenRadial.RadialPattern[i].Magnitude) > Rand.Value && !victims.ContainsKey(victim))
                    {
                        //Log.Message(victim.ToString());
                        victims.Add(victim, shootline);
                    }
                }
            }
            //Log.Message("GetVicitmEnd");
        }
    }
    public class Verb_ShootWithAmmoConstantly : Verb_ShootWithAmmo
    {
        public virtual bool isConstantly
        { get { return true; } }
        public LocalTargetInfo jobTarget => CasterPawn.jobs.curJob.targetA;
        protected override bool TryCastShot()
        {
            if (isConstantly)
            {
                //if((!currentTarget.HasThing) || !(currentTarget.Pawn?.DeadOrDowned ?? currentTarget.Thing.Destroyed)) burstShotsLeft++;
                if (CasterPawn.jobs.curJob.def == JobDefOf.Wait_Combat || (CasterPawn.jobs.curJob.def == JobDefOf.AttackStatic && (jobTarget.Cell == CurrentTarget.Cell || jobTarget.Thing == CurrentTarget.Thing)))
                {
                    if (currentTarget.Pawn?.DeadOrDowned ?? currentTarget.Thing.Destroyed) goto end;
                    burstShotsLeft++;
                }
            }
            end:
            return base.TryCastShot();
        }
    }
}
