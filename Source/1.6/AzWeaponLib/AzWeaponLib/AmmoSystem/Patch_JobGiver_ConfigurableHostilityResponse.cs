using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace AzWeaponLib.AmmoSystem
{
    [HarmonyPatch(typeof(JobGiver_ConfigurableHostilityResponse))]
    internal class Patch_JobGiver_ConfigurableHostilityResponse
    {
        [HarmonyPatch("TryGiveJob")]
        [HarmonyPrefix]
        public static bool Prefix_TryGiveJob(JobGiver_ConfigurableHostilityResponse __instance, Job __result, Pawn pawn)
        {
            if (pawn.CurJobDef == AWL_DefOf.AWL_ReloadWeapon)
            {
                __result = null;
                return false;
            }
            return true;
        }
    }
}
