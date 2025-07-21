using AzWeaponLib.MultiVerb;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace AzWeaponLib.PatchMisc
{
    [HarmonyPatch(typeof(Find))]
    internal class Patch_Find
    {
        [HarmonyPatch("ClearCache")]
        [HarmonyPostfix]
        private static void Postfix_ClearCache()
        {
            Patch_VerbTracker.MultiVerbDict.Clear();
        }
    }
}
