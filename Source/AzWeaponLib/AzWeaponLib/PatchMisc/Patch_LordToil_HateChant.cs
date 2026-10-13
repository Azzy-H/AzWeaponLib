using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using static HarmonyLib.Code;

namespace AzWeaponLib.PatchMisc
{
    //[HarmonyPatch(typeof(Pawn_HealthTracker))]
    //internal class Patch_Pawn_HealthTracker
    //{
    //    [HarmonyPatch("CheckForStateChange")]
    //    [HarmonyPostfix]
    //    public static void Postfix()
    //    {
    //        string stackTrace = Environment.StackTrace;

    //        Log.Message("=== Call Stack ===");

    //        Log.Message(stackTrace);
    //    }
    //}

    [HarmonyPatch(typeof(LordToil_HateChant))]
    public class Patch_LordToil_HateChant
    {
        [HarmonyPatch(nameof(LordToil_HateChant.UpdateAllDuties))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            int index1 = -1;
            int index2 = -1;
            for (int i = 0; i < codes.Count(); i++)
            {
                if (codes[i].operand is FieldInfo field && field.Name == nameof(Pawn_MindState.duty)) 
                {
                    index1 = i;
                    break; 
                }
            }
            for (int j = 0; j < codes.Count(); j++)
            {
                if (codes[j].operand is MethodInfo method && method.Name == nameof(Pawn_HealthTracker.AddHediff))
                {
                    index2 = j;
                    break;
                }
            }
            if (index1 >= 0 && index2 >= 0)
            {
                var label = new Label();
                codes[index2 + 2].labels.Add(label);
                var addList = new List<CodeInstruction>();
                addList.Add(new CodeInstruction(OpCodes.Ldloc_1));
                addList.Add(new CodeInstruction(OpCodes.Call, MethodInfo_ShouldAddHediff));
                addList.Add(new CodeInstruction(OpCodes.Brfalse, label));
                codes.InsertRange(index1+1, addList);
            }
            return codes;
        }
        private static MethodInfo MethodInfo_ShouldAddHediff = AccessTools.Method(typeof(Patch_LordToil_HateChant), nameof(Patch_LordToil_HateChant.ShouldAddHediff));
        private static bool ShouldAddHediff(Pawn pawn)
        {
            return pawn != null && !pawn.health.hediffSet.HasHediff(HediffDefOf.PsychicTrance);
        }
    }
}
