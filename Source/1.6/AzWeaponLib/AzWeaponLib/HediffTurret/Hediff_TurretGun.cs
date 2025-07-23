using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using static System.Collections.Specialized.BitVector32;
using static UnityEngine.GraphicsBuffer;

namespace AzWeaponLib.HediffTurret
{
    public class HediffDef_TurretGun : DefModExtension
    {
        public BuildingProperties turret;
    }
    [StaticConstructorOnStartup]
    public class Hediff_TurretGun : HediffWithComps, IAttackTargetSearcher
    {
        protected static readonly CachedTexture ToggleTurretIcon = new CachedTexture("UI/Gizmos/ToggleTurret");
        protected static readonly CachedTexture CancelCommandTex = new CachedTexture("UI/Designators/Cancel");
        protected virtual CachedTexture ToggleIcon => ToggleTurretIcon;
        public HediffDef_TurretGun turretGunDef => def.GetModExtension<HediffDef_TurretGun>();
        private bool targetFindingActive = true;
        public bool TargetFindingActive
        {
            get
            {
                return targetFindingActive;
            }
            set 
            {
                targetFindingActive = value;
            }
        }//是否寻找下一个目标
        private bool forced = false;
        public bool Forced
        {
            get { return forced; }
            set { forced = value; }
        }
        public Thing gun;
        protected int burstCooldownTicksLeft;//内置冷却时间
        private CompEquippable gunCompEq;
        public CompEquippable GunCompEq
        {
            get
            {
                if (gunCompEq == null) gunCompEq = gun.TryGetComp<CompEquippable>();
                return gunCompEq;
            }
        }
        public virtual Verb AttackVerb => GunCompEq.PrimaryVerb;
        protected LocalTargetInfo currentTargetInt = LocalTargetInfo.Invalid;
        public virtual LocalTargetInfo CurrentTarget
        {
            get
            {
                return currentTargetInt;
            }
            set 
            {
                currentTargetInt = value;
                if (currentTargetInt.IsValid && burstCooldownTicksLeft <= 0)
                {
                    float randomInRange = turretGunDef.turret.turretBurstWarmupTime.RandomInRange;
                    if (randomInRange > 0f)
                    {
                        burstWarmupTicksLeft = randomInRange.SecondsToTicks();
                    }
                    else
                    {
                        BeginBurst();
                    }
                }
                else
                {
                    ResetCurrentTarget();
                }
            }
        }

        protected int burstWarmupTicksLeft;
        protected int cooldownTickLeft;
        protected int lastAttackTargetTick;
        private LocalTargetInfo lastAttackedTarget = LocalTargetInfo.Invalid;
        private bool WarmingUp => burstWarmupTicksLeft > 0;

        public Thing Thing => pawn;

        public Verb CurrentEffectiveVerb => AttackVerb;

        public LocalTargetInfo LastAttackedTarget => lastAttackedTarget;

        public int LastAttackTargetTick => lastAttackTargetTick;

        protected virtual bool CanMakeNewBurstNow()//是否能够射击
        {
            return true;
        }
        public override void PostMake()
        {
            base.PostMake();
            MakeGun();
        }
        public override void Tick()
        {
            base.Tick();
            if (!pawn.Spawned || pawn.Downed) return;
            TurretTick();
        }
        public virtual void TurretTick()
        {
            GunCompEq.verbTracker.VerbsTick();
            if (AttackVerb.state == VerbState.Bursting)
            {
                //Log.Message("Bursting");
                return;
            }
            if (WarmingUp)
            {
                //Log.Message("Warming");
                burstWarmupTicksLeft--;
                if (burstWarmupTicksLeft == 0)
                {
                    //Log.Message("Warming completed");
                    BeginBurst();
                    lastAttackedTarget = CurrentTarget;
                    lastAttackTargetTick = Find.TickManager.TicksGame;
                }
            }
            else
            {
                if (burstCooldownTicksLeft > 0)
                {
                    //Log.Message("Internal cooling");
                    burstCooldownTicksLeft--;
                }
                if (burstCooldownTicksLeft <= 0 && pawn.IsHashIntervalTick(10))
                {
                    TryStartShootSomething(canBeginBurstImmediately: true);
                }
            }
            //top.TurretTopTick();
        }
        public void MakeGun()
        {
            gun = ThingMaker.MakeThing(turretGunDef.turret.turretGunDef);
            UpdateGunVerbs();
        }
        private void UpdateGunVerbs()
        {
            List<Verb> allVerbs = gun.TryGetComp<CompEquippable>().AllVerbs;
            for (int i = 0; i < allVerbs.Count; i++)
            {
                Verb verb = allVerbs[i];
                verb.caster = pawn;
                verb.castCompleteCallback = BurstComplete;
            }
        }
        protected virtual void BurstComplete()
        {
            burstCooldownTicksLeft = BurstCooldownTime().SecondsToTicks();//内置冷却时间，用于控制频率
            pawn.stances.SetStance(new Stance_Cooldown(cooldownTickLeft, CurrentTarget, AttackVerb));//如果在后摇时触发了，这段代码能防止取消后摇
        }
        protected float BurstCooldownTime()
        {
            if (turretGunDef.turret.turretBurstCooldownTime >= 0f)
            {
                return turretGunDef.turret.turretBurstCooldownTime;
            }
            return AttackVerb.verbProps.defaultCooldownTime;
        }
        protected void ResetCurrentTarget()
        {
            Forced = false;
            currentTargetInt = LocalTargetInfo.Invalid;
            burstWarmupTicksLeft = 0;
        }
        protected virtual void BeginBurst()
        {
            if (CanMakeNewBurstNow())
            {
                if (pawn.stances.curStance is Stance_Cooldown stance_Cooldown)
                {
                    cooldownTickLeft = stance_Cooldown.ticksLeft;
                }
                else cooldownTickLeft = 0;
                //Log.Message("Verb TryStartCastOn");
                AttackVerb.TryStartCastOn(CurrentTarget);
            }
        }
        public void TryStartShootSomething(bool canBeginBurstImmediately)
        {
            if (!pawn.Spawned || (AttackVerb.ProjectileFliesOverhead() && pawn.Map.roofGrid.Roofed(pawn.Position)) || !AttackVerb.Available())
            {
                ResetCurrentTarget();
                return;
            }
            if (pawn.stances.curStance is Stance_Warmup sw)//防止新的瞄准打断未完成的瞄准
            {
                if (BurstCooldownTime() > 0 || turretGunDef.turret.turretBurstWarmupTime.min > 0 || sw.verb.Equals(AttackVerb)) return;
            }
            if (TargetFindingActive && !Forced) 
            {
                //Log.Message("Try Find Target at tick: " + GenTicks.TicksGame);
                currentTargetInt = TryFindNewTarget(); 
            }
            if (currentTargetInt.IsValid)
            {
                float randomInRange = turretGunDef.turret.turretBurstWarmupTime.RandomInRange;
                if (randomInRange > 0f)
                {
                    burstWarmupTicksLeft = randomInRange.SecondsToTicks();
                }
                else if (canBeginBurstImmediately)
                {
                    BeginBurst();
                }
                else
                {
                    burstWarmupTicksLeft = 1;
                }
            }
            else
            {
                //Log.Message("Failed");
                ResetCurrentTarget();
            }
        }
        public virtual LocalTargetInfo TryFindNewTarget()
        {
            return (Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(this, TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable);
        }
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            if (pawn.Drafted)
            {
                yield return new Command_Toggle
                {
                    icon = ToggleIcon.Texture,
                    defaultLabel = "CommandToggleTurret".Translate(),
                    defaultDesc = "CommandToggleTurretDesc".Translate(),
                    isActive = () => targetFindingActive,
                    toggleAction = delegate
                    {
                        targetFindingActive = !targetFindingActive;
                        burstWarmupTicksLeft = 0;
                        ResetCurrentTarget();
                    },
                    activateSound = SoundDef.Named("Click"),
                    groupKey = 476313,
                    hotKey = null
                };
                Command_Target command_Target = new Command_Target();
                command_Target.defaultLabel = "CommandSquadAttack".Translate();
                command_Target.defaultDesc = "CommandSquadAttackDesc".Translate();
                command_Target.hotKey = null;
                command_Target.icon = TexCommand.SquadAttack;
                command_Target.targetingParams = TargetingParameters.ForAttackAny();
                if (FloatMenuUtility.GetAttackAction(pawn, LocalTargetInfo.Invalid, out var failStr) == null)
                {
                    command_Target.Disable(failStr.CapitalizeFirst() + ".");
                }
                command_Target.action = delegate (LocalTargetInfo target)
                {
                    Forced = true;
                    currentTargetInt = target;
                    if (currentTargetInt.IsValid && burstCooldownTicksLeft <= 0)
                    {
                        float randomInRange = turretGunDef.turret.turretBurstWarmupTime.RandomInRange;
                        if (randomInRange > 0f)
                        {
                            burstWarmupTicksLeft = randomInRange.SecondsToTicks();
                        }
                        else
                        {
                            BeginBurst();
                        }
                    }
                    else
                    {
                        ResetCurrentTarget();
                    }
                };
                command_Target.groupKey = 928765;
                yield return command_Target;
                if (Forced)
                {
                    Command_Action stop_Force = new Command_Action();
                    stop_Force.defaultLabel = "AWL_StopForceTarget".Translate();
                    stop_Force.defaultDesc = "AWL_StopForceTargetDesc".Translate();
                    stop_Force.hotKey = null;
                    stop_Force.icon = CancelCommandTex.Texture;
                    stop_Force.action = delegate ()
                    {
                        Forced = false;
                    };
                    stop_Force.groupKey = 127345;
                    yield return stop_Force;
                }
            }
            //if (DebugSettings.ShowDevGizmos)
            //{
            //}
        }
        public override void PreRemoved()
        {
            targetFindingActive = false;
            gun.Destroy();
            base.PreRemoved();
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref targetFindingActive, "targetFindingActive", true);
            Scribe_Values.Look(ref forced, "forced", false);
            Scribe_Deep.Look(ref gun, "gun", null);
            Scribe_Values.Look(ref burstCooldownTicksLeft, "burstCooldownTicksLeft", 0);
            Scribe_TargetInfo.Look(ref currentTargetInt, "currentTargetInt", null);
            Scribe_Values.Look(ref lastAttackTargetTick, "lastAttackTargetTick", 0);
            Scribe_TargetInfo.Look(ref lastAttackedTarget, "lastAttackedTarget", null);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (gun == null)
                {
                    Log.Error("Turret had null gun after loading. Recreating.");
                    MakeGun();
                }
                else
                {
                    UpdateGunVerbs();
                }
            }
        }
    }
}
