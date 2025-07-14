using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace AzWeaponLib
{
    [HarmonyPatch(typeof(Pawn_DraftController))]
    internal class Patch_Pawn_DraftController
    {
        [HarmonyPatch("set_Drafted")]
        [HarmonyPostfix]
        public static void Postfix_set_Drafted(Pawn_DraftController __instance, bool value)
        {
            if (value)
            {
                __instance.pawn.BroadcastCompSignalToPawn("AWL_Drafted");
                //__instance.pawn.BroadcastCompSignal("AWL_Drafted");
                //__instance.pawn.apparel.WornApparel.ForEach(apparel => apparel.BroadcastCompSignal("AWL_Drafted"));
                //__instance.pawn.equipment.AllEquipmentListForReading.ForEach(eq => eq.BroadcastCompSignal("AWL_Drafted"));
            }
            else 
            {
                __instance.pawn.BroadcastCompSignalToPawn("AWL_Undrafted");
                //__instance.pawn.BroadcastCompSignal("AWL_Undrafted");
                //__instance.pawn.apparel.WornApparel.ForEach(apparel => apparel.BroadcastCompSignal("AWL_Undrafted"));
                //__instance.pawn.equipment.AllEquipmentListForReading.ForEach(eq => eq.BroadcastCompSignal("AWL_Undrafted"));
            }
        }
    }
}
