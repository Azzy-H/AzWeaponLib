using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace AzWeaponLib
{
    [HarmonyPatch(typeof(JobDriver_Hunt))]
    internal static class Patch_JobDriver_Hunt
    {
        [HarmonyPatch("StartCollectCorpseToil")]
        [HarmonyPostfix]
        public static void Postfix_StartCollectCorpseToil(JobDriver_Lovin __instance, Toil __result)
        {
            Action action = () =>
            {
                __instance.pawn.BroadcastCompSignalToPawn("AWL_HuntFinished");
                //__instance.pawn.BroadcastCompSignal("AWL_HuntFinished");
                //__instance.pawn.apparel.WornApparel.ForEach(apparel => apparel.BroadcastCompSignal("AWL_HuntFinished"));
                //__instance.pawn.equipment.AllEquipmentListForReading.ForEach(eq => eq.BroadcastCompSignal("AWL_HuntFinished"));
            };
            __result.AddPreInitAction(action);
        }
    }
}
