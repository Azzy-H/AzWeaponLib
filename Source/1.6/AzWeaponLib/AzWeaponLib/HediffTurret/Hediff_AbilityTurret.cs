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

        // 缓存由 AbilityTargetFinder 预处理过的技能列表
        private List<Ability> cachedPreprocessedAbilities;
        private bool abilityCacheDirty = true;


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
            abilityCacheDirty = true; // 主动技能列表变化，缓存也需要刷新
        }

        /// <summary>
        /// 调用 AbilityTargetFinder 来刷新预处理过的技能缓存。
        /// </summary>
        private void RecacheAbilities()
        {
            var enabledAbilities = ActiveAbilities.Where(IsEnabled).ToList();
            cachedPreprocessedAbilities = AbilityTargetFinder.PreprocessAndSortCandidates(enabledAbilities);
            abilityCacheDirty = false;
        }


        public void Notify_AbilitiesChanged()
        {
            activeAbilitiesDirty = true;
            abilityCacheDirty = true;
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
            // 强制目标具有最高优先级
            if (forced && currentTargetInt.IsValid && !currentTargetInt.ThingDestroyed)
            {
                foreach (var ability in ActiveAbilities
                    .Where(IsEnabled)
                    .OrderByDescending(a => a.def.GetModExtension<TurretAbility>()?.priority ?? 0f))
                {
                    if (!ability.CanCast || !AbilityTargetFinder.AbilityCanTargetNowIgnoringAiCanUse(ability, currentTargetInt))
                    {
                        continue;
                    }

                    if (ability.verb.TryStartCastOn(currentTargetInt))
                    {
                        break;
                    }
                }
                return;
            }

            // 如果缓存脏了，重新计算
            if (abilityCacheDirty)
            {
                RecacheAbilities();
            }

            // 使用 AbilityTargetFinder 来寻找最佳目标
            if (AbilityTargetFinder.TryFindBestTarget(cachedPreprocessedAbilities, pawn, out var finalCandidate))
            {
                if (finalCandidate.ability.verb.TryStartCastOn(finalCandidate.target))
                {
                    if (!forced) currentTargetInt = finalCandidate.target;
                    return;
                }
            }
            
            // 如果没有任何技能能找到目标，清除当前目标
            if (!forced)
            {
                currentTargetInt = LocalTargetInfo.Invalid;
            }
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
                abilityCacheDirty = true; // 读档后需要刷新缓存
            }
        }

        public void ToggleAuto(AbilityDef def)
        {
            if (disabledAbilities.Contains(def))
                disabledAbilities.Remove(def);
            else
                disabledAbilities.Add(def);
            
            abilityCacheDirty = true; // 技能启用状态改变，刷新缓存
        }
    }

    public static class AbilityTargetFinder
    {
        /// <summary>
        /// 从给定的技能列表中，通过迭代剪枝算法找到最佳的目标和技能。
        /// </summary>
        /// <param name="preprocessedAbilities">已经过 PreprocessAndSortCandidates 处理的技能列表。</param>
        /// <param name="pawn">执行技能的Pawn。</param>
        /// <param name="result">包含最佳目标和技能的元组。</param>
        /// <returns>如果找到有效目标则为 true。</returns>
        public static bool TryFindBestTarget(List<Ability> preprocessedAbilities, Pawn pawn, out (LocalTargetInfo target, Ability ability) result)
        {
            result = FindBestCandidateGlobal(preprocessedAbilities, pawn);
            return result.ability != null;
        }

        /// <summary>
        /// 对原始技能列表进行分组、预处理和排序，生成用于高效索敌的候选列表。
        /// </summary>
        /// <param name="abilities">原始的可用技能列表。</param>
        /// <returns>一个优化过的、按半径降序排列的技能列表。</returns>
        public static List<Ability> PreprocessAndSortCandidates(List<Ability> abilities) 
        {
            var groupedAbilities = GroupAbilities(abilities);
            var finalCandidates = new List<Ability>();

            foreach (var group in groupedAbilities.Values)
            {
                var listA = group
                    .OrderByDescending(a => a.verb.EffectiveRange)
                    .ThenByDescending(a => a.def.GetModExtension<TurretAbility>()?.priority ?? 0f)
                    .ToList();
                if (listA.Count == 0) continue;

                var listB = new List<Ability>();
                foreach (var ability in listA)
                {
                    float currentPriority = ability.def.GetModExtension<TurretAbility>()?.priority ?? 0f;
                    bool isShadowed = listB.Any(prev =>
                        (prev.def.GetModExtension<TurretAbility>()?.priority ?? 0f) > currentPriority);

                    if (!isShadowed)
                    {
                        listB.Add(ability);
                    }
                }
                finalCandidates.AddRange(listB);
            }

            return finalCandidates
                .OrderByDescending(a => a.verb.EffectiveRange)
                .ThenByDescending(a => a.def.GetModExtension<TurretAbility>()?.priority ?? 0f)
                .ToList();
        }

        // 负责分组
        private static Dictionary<(bool Los, System.Type VerbType), List<Ability>> GroupAbilities(List<Ability> abilities) 
        {
            return abilities.GroupBy(a => (a.verb.verbProps?.requireLineOfSight ?? false, a.verb.GetType()))
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        // 核心的迭代剪枝索敌算法
        private static (LocalTargetInfo, Ability) FindBestCandidateGlobal(List<Ability> candidates, Pawn pawn)
        {
            if (candidates.NullOrEmpty()) return (LocalTargetInfo.Invalid, null);

            var availableCandidates = new List<Ability>(candidates);

            int currentIndex = 0;
            int safety = 0;
            int safetyMax = availableCandidates.Count * 4 + 8; // 保险阈值，防止极端情况下长循环

            while (currentIndex < availableCandidates.Count && safety++ < safetyMax)
            {
                var currentAbility = availableCandidates[currentIndex];
                var target = TryFindNewTargetFor(currentAbility, pawn);

                if (target.IsValid)
                {
                    float currentPriority = currentAbility.def.GetModExtension<TurretAbility>()?.priority ?? 0f;

                    int beforeCount = availableCandidates.Count;
                    availableCandidates.RemoveAll(c => (c.def.GetModExtension<TurretAbility>()?.priority ?? 0f) < currentPriority);

                    if (availableCandidates.Count < beforeCount)
                    {
                        // 发生了有效剪枝：若只剩一个候选，直接返回当前命中的结果
                        if (availableCandidates.Count < 2)
                        {
                            return (target, currentAbility);
                        }
                        // 否则从索引1继续评估
                        currentIndex = 1;
                    }
                    else
                    {
                        // 没有任何元素被剪掉，必须前进，否则会卡在同一个 index
                        currentIndex++;
                    }
                }
                else
                {
                    var groupKey = (Los: currentAbility.verb.verbProps?.requireLineOfSight ?? false, VerbType: currentAbility.verb.GetType());

                    int beforeCount = availableCandidates.Count;
                    availableCandidates.RemoveAll(c =>
                        c.verb.EffectiveRange < currentAbility.verb.EffectiveRange &&
                        (c.verb.verbProps?.requireLineOfSight ?? false, c.verb.GetType()).Equals(groupKey)
                    );

                    if (availableCandidates.Count == beforeCount)
                    {
                        currentIndex++;
                    }
                    // 若删除了元素，不自增，让 currentIndex 指向删除后“顶上来”的下一个元素
                }
            }

            foreach (var ability in availableCandidates)
            {
                var target = TryFindNewTargetFor(ability, pawn);
                if (target.IsValid)
                {
                    return (target, ability);
                }
            }

            return (LocalTargetInfo.Invalid, null);
        }

        // 辅助方法
        private static LocalTargetInfo TryFindNewTargetFor(Ability ability, Pawn pawn) 
        {
            if (!ability.CanCast) return LocalTargetInfo.Invalid;

            var searcher = new AbilitySearcher
            {
                pawn = pawn,
                verb = ability.verb
            };
            TargetScanFlags flags = TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable;
            if(ability.def.verbProperties.requireLineOfSight)
            {
                flags |= TargetScanFlags.NeedLOSToAll;
            }
            return (Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(
                searcher,
                flags,
                validator: t => AbilityCanTargetNowIgnoringAiCanUse(ability, t)
            );
        }
        
        public static bool AbilityCanTargetNowIgnoringAiCanUse(Ability ability, LocalTargetInfo target) 
        {
            if (!ability.CanApplyOn(target))
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
    }
}
