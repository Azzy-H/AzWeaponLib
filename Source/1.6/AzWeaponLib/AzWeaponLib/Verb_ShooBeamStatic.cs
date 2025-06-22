using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AzWeaponLib
{
    public abstract class Verb_ShooBeamStatic : Verb
    {
        protected override int ShotsPerBurst => verbProps.burstShotCount;
        private MoteDualAttached mote;
        private Effecter endEffecter;
        private Sustainer sustainer;
        protected virtual float DamageFactor => 1f;
        protected virtual Vector3 aimPos
        { 
            get 
            {
                if (currentTarget == null) return currentDestination.Cell.ToVector3Shifted();
                if (currentTarget.Pawn != null) return currentTarget.Pawn.DrawPos;
                if (currentTarget.Thing is Projectile p) return p.ExactPosition;
                return currentTarget.Cell.ToVector3Shifted();
            }
        }
        IntVec3 intAimPos => aimPos.ToIntVec3();
        //public override bool TryStartCastOn(LocalTargetInfo castTarg, LocalTargetInfo destTarg, bool surpriseAttack = false, bool canHitNonTargetPawns = true, bool preventFriendlyFire = false, bool nonInterruptingSelfCast = false)
        //{
        //    Log.Message("TryStartCastOn");
        //    bool flag = base.TryStartCastOn(verbProps.beamTargetsGround ? ((LocalTargetInfo)castTarg.Cell) : castTarg, destTarg, surpriseAttack, canHitNonTargetPawns, preventFriendlyFire, nonInterruptingSelfCast);
        //    Log.Message(flag.ToString());
        //    return flag;
        //}
        protected override bool TryCastShot()
        {
            if (currentTarget.HasThing && currentTarget.Thing.Map != caster.Map)
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
                base.EquipmentSource.GetComp<CompApparelReloadable>()?.UsedOnce();
            }
            lastShotTick = Find.TickManager.TicksGame;
            DoEffectToTarget(currentTarget, caster.Position, DamageFactor);
            return true;
        }
        public override void BurstingTick()
        {
            Vector3 vectorDiff = aimPos - caster.Position.ToVector3Shifted();//激光两端的向量差
            Vector3 normalizedVectorDiff = vectorDiff.Yto0().normalized;
            float vectorDiffSqrt = vectorDiff.MagnitudeHorizontal();//激光长度的平方
            Vector3 offsetA = normalizedVectorDiff * verbProps.beamStartOffset;//激光起始点和pawn的偏移量
            Vector3 offsetB = aimPos - intAimPos.ToVector3Shifted();//激光命中目标绘制位置和激光目标所在位置的偏移量
            if (mote != null)
            {
                mote.UpdateTargets(new TargetInfo(caster.Position, caster.Map), new TargetInfo(intAimPos, caster.Map), offsetA, offsetB);
                mote.Maintain();
            }
            if (verbProps.beamGroundFleckDef != null && Rand.Chance(verbProps.beamFleckChancePerTick))
            {
                FleckMaker.Static(intAimPos, caster.Map, verbProps.beamGroundFleckDef);
            }
            if (endEffecter == null && verbProps.beamEndEffecterDef != null)
            {
                endEffecter = verbProps.beamEndEffecterDef.Spawn(intAimPos, caster.Map, offsetB);
            }
            if (endEffecter != null)
            {
                endEffecter.offset = offsetB;
                endEffecter.EffectTick(new TargetInfo(intAimPos, caster.Map), TargetInfo.Invalid);
                endEffecter.ticksLeft--;
            }
            if (verbProps.beamLineFleckDef != null)
            {
                float num2 = 1f * vectorDiffSqrt;
                for (int i = 0; (float)i < num2; i++)
                {
                    if (Rand.Chance(verbProps.beamLineFleckChanceCurve.Evaluate((float)i / num2)))
                    {
                        Vector3 vector4 = i * normalizedVectorDiff - normalizedVectorDiff * Rand.Value + normalizedVectorDiff / 2f;
                        FleckMaker.Static(caster.Position.ToVector3Shifted() + vector4, caster.Map, verbProps.beamLineFleckDef);
                    }
                }
            }
            sustainer?.Maintain();
        }
        public override void WarmupComplete()
        {
            burstShotsLeft = ShotsPerBurst;
            state = VerbState.Bursting;
            if (verbProps.beamMoteDef != null)
            {
                mote = MoteMaker.MakeInteractionOverlay(verbProps.beamMoteDef, caster, new TargetInfo(intAimPos, caster.Map));
            }
            //ticksToNextBurstShot = verbProps.ticksBetweenBurstShots;
            //if (CasterIsPawn && !NonInterruptingSelfCast)
            //{
            //    CasterPawn.stances.SetStance(new Stance_Cooldown(verbProps.ticksBetweenBurstShots + 1, currentTarget, this));
            //}//等价于TryCastNextBurstShot()，但不是一开始就生效
            TryCastNextBurstShot();
            endEffecter?.Cleanup();
            if (verbProps.soundCastBeam != null)
            {
                sustainer = verbProps.soundCastBeam.TrySpawnSustainer(SoundInfo.InMap(caster, MaintenanceType.PerTick));
            }
        }
        public abstract void DoEffectToTarget(LocalTargetInfo target, IntVec3 sourceCell, float damageFactor = 1f);
    }
    public class Verb_ShootLaser : Verb_ShooBeamStatic
    {
        public override void DoEffectToTarget(LocalTargetInfo target, IntVec3 sourceCell, float damageFactor = 1)
        {
            Thing thing = target.Thing;
            float angleFlat = (currentTarget.Cell - caster.Position).AngleFlat;
            BattleLogEntry_RangedImpact log = new BattleLogEntry_RangedImpact(caster, thing, currentTarget.Thing, base.EquipmentSource.def, null, null);
            DamageInfo dinfo;
            if (verbProps.beamTotalDamage > 0f)
            {
                float num = verbProps.beamTotalDamage / verbProps.burstShotCount;
                num *= damageFactor;
                dinfo = new DamageInfo(verbProps.beamDamageDef, num, verbProps.beamDamageDef.defaultArmorPenetration, angleFlat, caster, null, base.EquipmentSource.def, DamageInfo.SourceCategory.ThingOrUnknown, currentTarget.Thing);
            }
            else
            {
                float amount = (float)verbProps.beamDamageDef.defaultDamage * damageFactor;
                dinfo = new DamageInfo(verbProps.beamDamageDef, amount, verbProps.beamDamageDef.defaultArmorPenetration, angleFlat, caster, null, base.EquipmentSource.def, DamageInfo.SourceCategory.ThingOrUnknown, currentTarget.Thing);
            }
            if (thing.CanEverAttachFire())
            {
                float chance = ((verbProps.flammabilityAttachFireChanceCurve == null) ? verbProps.beamChanceToAttachFire : verbProps.flammabilityAttachFireChanceCurve.Evaluate(thing.GetStatValue(StatDefOf.Flammability)));
                if (Rand.Chance(chance))
                {
                    thing.TryAttachFire(verbProps.beamFireSizeRange.RandomInRange, caster);
                }
            }
        }
    }
}
