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
    [HarmonyPatch(typeof(TickList))]
    internal class Patch_TickList
    {
        [HarmonyPatch("Tick")]
        [HarmonyPostfix]
        private static void Postfix_Tick() 
        {
            Patch_VerbTracker.VerbDict.Clear();
        }
    }
}
