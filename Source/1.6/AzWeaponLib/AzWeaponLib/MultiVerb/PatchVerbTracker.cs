using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace AzWeaponLib.MultiVerb
{
    [HarmonyPatch(typeof(VerbTracker))]
    public class Patch_VerbTracker
    {
        [HarmonyPatch("get_PrimaryVerb")]
        [HarmonyPrefix]
        public static bool Prefix_get_PrimaryVerb(VerbTracker __instance, ref Verb __result)
        {
            if (__instance.directOwner is CompEquippable Eq)
            {
                CompMultiVerb comp_MultiVerb = Eq.parent.TryGetComp<CompMultiVerb>();
                if (comp_MultiVerb != null)
                {
                    __result = __instance.AllVerbs[comp_MultiVerb.verbIndex];
                    return false;
                }
            }
            return true;
        }
        [HarmonyPatch("CreateVerbTargetCommand")]
        [HarmonyPostfix]
        public static void Postfix_CreateVerbTargetCommand(Thing ownerThing, Verb verb, VerbTracker __instance, ref Command_VerbTarget __result)
        {
            if (__instance.directOwner is CompEquippable Eq)
            {
                CompMultiVerb compMultiVerb = Eq.parent.TryGetComp<CompMultiVerb>();
                if (compMultiVerb != null)
                {
                    if (verb != __instance.AllVerbs[compMultiVerb.verbIndex])
                    {
                        __result = new Command_VerbTargetInvisible();
                    }
                }
            }
        }
    }
    public class Command_VerbTargetInvisible : Command_VerbTarget
    {
        public override bool Visible => false;
    }
}
