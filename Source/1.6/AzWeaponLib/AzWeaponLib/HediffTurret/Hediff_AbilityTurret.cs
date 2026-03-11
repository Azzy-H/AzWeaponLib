using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AzWeaponLib.HediffTurret
{
    public class TurretAbility : DefModExtension
    {
        public float priority = 0f;
        public HediffDef hediffDef;

        public HediffDef ResolvedHediffDef => hediffDef ?? AWL_DefOf.AWL_AbilityTurret;
    }
    // HediffDef 的扩展，现在主要用于配置
    class AbilitySearcher : IAttackTargetSearcher
    {
        public Pawn pawn;
        public Verb verb;
        public Thing Thing => pawn;
        public Verb CurrentEffectiveVerb => verb;
        public LocalTargetInfo LastAttackedTarget => pawn.LastAttackedTarget;
        public int LastAttackTargetTick => pawn.LastAttackTargetTick;
    }

    [StaticConstructorOnStartup]
    public class Hediff_AbilityTurret : HediffWithComps
    {
        protected static readonly CachedTexture CancelCommandTex = new CachedTexture("UI/Designators/Cancel");

        private bool forced = false;
        protected LocalTargetInfo currentTargetInt = LocalTargetInfo.Invalid;
        private List<AbilityDef> disabledAbilities = new List<AbilityDef>();
        private List<Ability> cachedActiveAbilities;
        private bool activeAbilitiesDirty = true;

        // 获取当前 Hediff 负责的 Ability
        public IEnumerable<Ability> ActiveAbilities
        {
            get
            {
                if (activeAbilitiesDirty || cachedActiveAbilities == null)
                {
                    RecacheActiveAbilities();
                }
                return cachedActiveAbilities;
            }
        }

        private void RecacheActiveAbilities()
        {
            cachedActiveAbilities = pawn.abilities.AllAbilitiesForReading.Where(a => ControlsAbility(a.def)).ToList();
            activeAbilitiesDirty = false;
        }

        public void Notify_AbilitiesChanged()
        {
            activeAbilitiesDirty = true;
        }

        private bool ControlsAbility(AbilityDef abilityDef)
        {
            return abilityDef.GetModExtension<TurretAbility>()?.ResolvedHediffDef == def;
        }

        // 检查某个技能是否被启用
        public bool IsEnabled(Ability ability) => !disabledAbilities.Contains(ability.def);

        public override void Tick()
        {
            base.Tick();
            if (!pawn.Spawned || pawn.Downed || pawn.CurJobDef != JobDefOf.Wait_Combat || !(pawn.stances.curStance is Stance_Mobile))
            {
                return;
            }
            if (pawn.IsHashIntervalTick(10))
            {
                UpdateTargetAndCast();
            }
        }

        private void UpdateTargetAndCast()
        {
            var abilities = ActiveAbilities.Where(IsEnabled).ToList();
            if (abilities.Count == 0)
            {
                currentTargetInt = LocalTargetInfo.Invalid;
                return;
            }

            abilities.SortByDescending(a => a.def.GetModExtension<TurretAbility>()?.priority ?? 0f);

            foreach (var ability in abilities)
            {
                if (!ability.CanCast) continue;

                LocalTargetInfo target = forced && currentTargetInt.IsValid && !currentTargetInt.ThingDestroyed
                    ? currentTargetInt
                    : TryFindNewTargetFor(ability);

                if (!target.IsValid || !ability.CanApplyOn(target)) continue;

                if (ability.verb.TryStartCastOn(target))
                {
                    if (!forced) currentTargetInt = target;
                    return;
                }
            }

            if (!forced)
            {
                currentTargetInt = LocalTargetInfo.Invalid;
            }
        }

        private LocalTargetInfo TryFindNewTargetFor(Ability ability)
        {
            var searcher = new AbilitySearcher
            {
                pawn = pawn,
                verb = ability.verb
            };

            return (Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(
                searcher,
                TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable,
                validator: t => AbilityCanTargetNowIgnoringAiCanUse(ability, t)
            );
        }

        private bool AbilityCanTargetNowIgnoringAiCanUse(Ability ability, LocalTargetInfo target)
        {
            if (!(bool)ability.CanCast || !ability.CanApplyOn(target))
            {
                return false;
            }
            if (ability.EffectComps != null)
            {
                foreach (CompAbilityEffect effectComp in ability.EffectComps)
                {
                    if (!effectComp.AICanTargetNow(target))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos()) yield return gizmo;

            if (pawn.Drafted)
            {
                yield return new Command_Target
                {
                    defaultLabel = "CommandSquadAttack".Translate(),
                    icon = TexCommand.SquadAttack,
                    targetingParams = TargetingParameters.ForAttackAny(),
                    action = (LocalTargetInfo target) =>
                    {
                        forced = true;
                        currentTargetInt = target;
                    }
                };

                if (forced)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "AWL_StopForceTarget".Translate(),
                        icon = CancelCommandTex.Texture,
                        action = () => forced = false
                    };
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref forced, "forced", false);
            Scribe_TargetInfo.Look(ref currentTargetInt, "currentTargetInt");
            Scribe_Collections.Look(ref disabledAbilities, "disabledAbilities", LookMode.Def);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (disabledAbilities == null)
                {
                    disabledAbilities = new List<AbilityDef>();
                }
                activeAbilitiesDirty = true;
            }
        }

        public void ToggleAuto(AbilityDef def)
        {
            if (disabledAbilities.Contains(def))
                disabledAbilities.Remove(def);
            else
                disabledAbilities.Add(def);
        }
    }
}
