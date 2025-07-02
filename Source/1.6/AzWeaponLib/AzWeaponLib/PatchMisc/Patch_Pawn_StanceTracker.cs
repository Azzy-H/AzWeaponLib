using AzWeaponLib.AmmoSystem;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AzWeaponLib
{
    [HarmonyPatch(typeof(Verb))]
    public class Patch_Verb//Building
    {
        [HarmonyPatch("VerbTick")]
        [HarmonyPrefix]
        public static bool Prefix_VerbTick(Verb __instance)
        {
            //Stance_Busy stance = __instance.CasterPawn?.stances.curStance as Stance_Busy;
            if(__instance.CasterIsPawn) return true;
            if (__instance.verbProps is VerbProperties_ShootWithAmmo vpswa && vpswa.retargetRange > 0)
            {
                if (!NeedRetarget(__instance.CurrentTarget) || (!(__instance.CasterPawn?.drafter?.FireAtWill ?? false))) return true;
                if (__instance.caster is IAttackTargetSearcher attackTargetSearcher)
                {
                    List<IAttackTarget> attackTargets = attackTargetSearcher.Thing.Map.attackTargetsCache.GetPotentialTargetsFor(attackTargetSearcher);
                    attackTargets.RemoveAll(x => Vector3.Dot((__instance.CurrentTarget.Cell - attackTargetSearcher.Thing.Position).ToVector3(), (x.Thing.Position - attackTargetSearcher.Thing.Position).ToVector3()) < 0f);
                    if (attackTargets.Count == 0) return true;
                    Thing closestTarget = attackTargets.MinBy(x => x.Thing.Position.DistanceToSquared(__instance.CurrentTarget.Cell)).Thing;
                    if (closestTarget.Position.DistanceToSquared(__instance.CurrentTarget.Cell) < (vpswa.retargetRange * vpswa.retargetRange))
                    {
                        NonPublicFields.Verb_currentTarget.SetValue(__instance, new LocalTargetInfo(closestTarget));
                    }
                }
            }
            return true;
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
    //[HarmonyPatch(typeof(Pawn))]
    //public class Patch_Pawn
    //{
    //    [HarmonyPatch("Tick")]
    //    [HarmonyPrefix]
    //    public static bool Prefix_StanceTick(Pawn __instance)
    //    {
    //        Verb verb = __instance?.equipment?.PrimaryEq?.PrimaryVerb;
    //        if (verb != null && verb.verbProps is VerbProperties_ShootWithAmmo vpswa && vpswa.retargetRange > 0)
    //        {
    //            Log.Warning("TickPre" + RuntimeHelpers.GetHashCode(__instance.stances.curStance));
    //        }
    //        return true;

    //    }
    //    [HarmonyPatch("Tick")]
    //    [HarmonyPostfix]
    //    public static void Postfix_StanceTick(Pawn __instance)
    //    {
    //        Verb verb = __instance?.equipment?.PrimaryEq?.PrimaryVerb;
    //        if (verb != null && verb.verbProps is VerbProperties_ShootWithAmmo vpswa && vpswa.retargetRange > 0)
    //        {
    //            Log.Warning("TickPost" + RuntimeHelpers.GetHashCode(__instance.stances.curStance));
    //        }
    //    }
    //}
    [HarmonyPatch(typeof(Pawn_StanceTracker))]
    public class Patch_Pawn_StanceTracker
    {
        [HarmonyPatch("StanceTrackerTick")]
        [HarmonyPrefix]
        public static bool Prefix_StanceTick(Pawn_StanceTracker __instance)
        {
            Stance_Busy stance = __instance.curStance as Stance_Busy;
            if (stance == null) return true;
            Verb verb = stance.verb;
            if (verb != null && verb.verbProps is VerbProperties_ShootWithAmmo vpswa && vpswa.retargetRange > 0)
            {
                if (!Patch_Verb.NeedRetarget(verb.CurrentTarget) || (!(verb.CasterPawn?.drafter?.FireAtWill ?? false))) return true;
                if (verb.caster is IAttackTargetSearcher attackTargetSearcher)
                {
                    List<IAttackTarget> attackTargets = attackTargetSearcher.Thing.Map.attackTargetsCache.GetPotentialTargetsFor(attackTargetSearcher);
                    attackTargets.RemoveAll(x => Vector3.Dot((verb.CurrentTarget.Cell - attackTargetSearcher.Thing.Position).ToVector3(), (x.Thing.Position - attackTargetSearcher.Thing.Position).ToVector3()) < 0f);
                    if (attackTargets.Count == 0) return true;
                    Thing closestTarget = attackTargets.MinBy(x => x.Thing.Position.DistanceToSquared(verb.CurrentTarget.Cell)).Thing;
                    if (closestTarget.Position.DistanceToSquared(verb.CurrentTarget.Cell) < (vpswa.retargetRange * vpswa.retargetRange))
                    {
                        stance.focusTarg = closestTarget;
                        NonPublicFields.Verb_currentTarget.SetValue(verb, new LocalTargetInfo(closestTarget));
                    }
                }
            }
            return true;
        }
    }
    //[HarmonyPatch(typeof(Stance_Warmup))]
    //public class Patch_Stance_Warmup//Pawn
    //{
    //    [HarmonyPatch("StanceTick")]
    //    [HarmonyPrefix]
    //    public static bool Prefix_StanceTick(Stance_Warmup __instance)
    //    {
    //        Verb verb = __instance.verb;
    //        if (verb.verbProps is VerbProperties_ShootWithAmmo vpswa && vpswa.retargetRange > 0)
    //        {
    //            if (!Patch_Verb.NeedRetarget(verb.CurrentTarget) || (verb.CasterPawn?.drafter.FireAtWill ?? false)) return true;
    //            if (verb.caster is IAttackTargetSearcher attackTargetSearcher)
    //            {
    //                List<IAttackTarget> attackTargets = attackTargetSearcher.Thing.Map.attackTargetsCache.GetPotentialTargetsFor(attackTargetSearcher);
    //                attackTargets.RemoveAll(x => Vector3.Dot((verb.CurrentTarget.Cell - attackTargetSearcher.Thing.Position).ToVector3(), (x.Thing.Position - attackTargetSearcher.Thing.Position).ToVector3()) < 0f);
    //                if (attackTargets.Count == 0) return true;
    //                Thing closestTarget = attackTargets.MinBy(x => x.Thing.Position.DistanceToSquared(verb.CurrentTarget.Cell)).Thing;
    //                if (closestTarget.Position.DistanceToSquared(verb.CurrentTarget.Cell) < (vpswa.retargetRange * vpswa.retargetRange))
    //                {
    //                    __instance.focusTarg = closestTarget;
    //                    NonPublicFields.Verb_currentTarget.SetValue(verb, new LocalTargetInfo(closestTarget));
    //                }
    //            }
    //        }
    //        return true;
    //    }
    //}
}
