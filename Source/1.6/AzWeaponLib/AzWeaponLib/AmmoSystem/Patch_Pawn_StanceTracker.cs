using AzWeaponLib.AmmoSystem;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AzWeaponLib.AmmoSystem
{
    //太卡，删除对建筑的兼容
    //[HarmonyPatch(typeof(Verb))]
    //internal class Patch_Verb//Building
    //{
    //    [HarmonyPatch("VerbTick")]
    //    [HarmonyTranspiler]
    //    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    //    {
    //        List<CodeInstruction> list = instructions.ToList();
    //        MethodInfo methodInfo = AccessTools.Method(typeof(Patch_Verb), "PrefixMethod_VerbTick");
    //        List<CodeInstruction> prefix = new List<CodeInstruction>
    //    {
    //        new CodeInstruction(OpCodes.Ldarg_0),
    //        new CodeInstruction(OpCodes.Call, methodInfo)
    //    };
    //        list.InsertRange(0, prefix);
    //        return list;
    //    }
    //    public static void PrefixMethod_VerbTick(Verb __instance)
    //    {
    //        //Stance_Busy stance = __instance.CasterPawn?.stances.curStance as Stance_Busy;
    //        return;
    //        if(__instance.CasterIsPawn) return;
    //        if (__instance.verbProps is VerbProperties_ShootWithAmmo vpswa && vpswa.retargetRange > 0)
    //        {
    //            if (!NeedRetarget(__instance.CurrentTarget) || (!(__instance.CasterPawn?.drafter?.FireAtWill ?? false))) return;
    //            if (__instance.caster is IAttackTargetSearcher attackTargetSearcher)
    //            {
    //                List<IAttackTarget> attackTargets = attackTargetSearcher.Thing.Map.attackTargetsCache.GetPotentialTargetsFor(attackTargetSearcher);
    //                attackTargets.RemoveAll(x => Vector3.Dot((__instance.CurrentTarget.Cell - attackTargetSearcher.Thing.Position).ToVector3(), (x.Thing.Position - attackTargetSearcher.Thing.Position).ToVector3()) < 0f);
    //                if (attackTargets.Count == 0) return;
    //                Thing closestTarget = attackTargets.MinBy(x => x.Thing.Position.DistanceToSquared(__instance.CurrentTarget.Cell)).Thing;
    //                if (closestTarget.Position.DistanceToSquared(__instance.CurrentTarget.Cell) < (vpswa.retargetRange * vpswa.retargetRange))
    //                {
    //                    NonPublicFields.Verb_currentTarget.SetValue(__instance, new LocalTargetInfo(closestTarget));
    //                }
    //            }
    //        }
    //        return;
    //    }
    //}
    [HarmonyPatch(typeof(Pawn_StanceTracker))]
    internal class Patch_Pawn_StanceTracker
    {
        [HarmonyPatch("StanceTrackerTick")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> list = instructions.ToList();
            MethodInfo methodInfo = AccessTools.Method(typeof(Patch_Pawn_StanceTracker), "PrefixMethod_StanceTick");
            List<CodeInstruction> prefix = new List<CodeInstruction>
        {
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Call, methodInfo)
        };
            list.InsertRange(0, prefix);
            return list;
        }
        public static void PrefixMethod_StanceTick(Pawn_StanceTracker __instance)
        {
            Stance_Busy stance = __instance.curStance as Stance_Busy;
            if (stance == null) return;
            Verb verb = stance.verb;
            if (verb != null && verb.verbProps is VerbProperties_ShootWithAmmo vpswa && vpswa.retargetRange > 0)
            {
                if (!NeedRetarget(verb.CurrentTarget) || (!(verb.CasterPawn?.drafter?.FireAtWill ?? false))) return;
                if (verb.caster is IAttackTargetSearcher attackTargetSearcher)
                {
                    List<IAttackTarget> attackTargets = attackTargetSearcher.Thing.Map.attackTargetsCache.GetPotentialTargetsFor(attackTargetSearcher);
                    attackTargets.RemoveAll(x => Vector3.Dot((verb.CurrentTarget.Cell - attackTargetSearcher.Thing.Position).ToVector3(), (x.Thing.Position - attackTargetSearcher.Thing.Position).ToVector3()) < 0f);
                    if (attackTargets.Count == 0) return;
                    Thing closestTarget = attackTargets.MinBy(x => x.Thing.Position.DistanceToSquared(verb.CurrentTarget.Cell)).Thing;
                    if (closestTarget.Position.DistanceToSquared(verb.CurrentTarget.Cell) < (vpswa.retargetRange * vpswa.retargetRange))
                    {
                        stance.focusTarg = closestTarget;
                        NonPublicFields.Verb_currentTarget.SetValue(verb, new LocalTargetInfo(closestTarget));
                    }
                }
            }
            return;
        }
        public static bool NeedRetarget(LocalTargetInfo localTargetInfo)
        {
            if (!localTargetInfo.HasThing) return false;
            if (localTargetInfo.Thing is IAttackTarget)
            {
                if (localTargetInfo.Pawn != null && localTargetInfo.Pawn.DeadOrDowned) return true;
                if (localTargetInfo.Thing.Destroyed) return true;
            }
            return false;
        }
    }

}
