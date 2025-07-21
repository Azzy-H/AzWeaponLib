using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace AzWeaponLib.MultiVerb
{
    [HarmonyPatch(typeof(VerbTracker))]
    internal class Patch_VerbTracker
    {
        //public static readonly Dictionary<VerbTracker, Verb> VerbDict = new Dictionary<VerbTracker, Verb>();
        public static readonly Dictionary<CompEquippable, CompMultiVerb> MultiVerbDict = new Dictionary<CompEquippable, CompMultiVerb>();
        public static Verb verb = null;
        [HarmonyPatch("get_PrimaryVerb")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> list = instructions.ToList();
            MethodInfo methodInfo = AccessTools.Method(typeof(Patch_VerbTracker), "PrefixMethod_get_PrimaryVerb");
            MethodInfo methodInfo2 = AccessTools.Method(typeof(Patch_VerbTracker), "MakeCache");
            FieldInfo fieldInfo = AccessTools.Field(typeof(Patch_VerbTracker), "verb");
            Label endTag_prefix = generator.DefineLabel();
            list[0].labels.Add(endTag_prefix);
            List<CodeInstruction> prefix = new List<CodeInstruction>
        {
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Call, methodInfo),
            new CodeInstruction(OpCodes.Stsfld, fieldInfo),
            new CodeInstruction(OpCodes.Ldsfld, fieldInfo),
            new CodeInstruction(OpCodes.Brfalse_S, endTag_prefix),//prefix未获取则继续原始方法
            new CodeInstruction(OpCodes.Ldsfld, fieldInfo),
            new CodeInstruction(OpCodes.Ret)
        };
            list.InsertRange(0, prefix);
            return list;
        }
        public static Verb PrefixMethod_get_PrimaryVerb(VerbTracker __instance)
        {
            if (__instance.directOwner is CompEquippable Eq)
            {
                if (!MultiVerbDict.TryGetValue(Eq, out CompMultiVerb comp_MultiVerb))
                {
                    comp_MultiVerb = Eq.parent.GetComp<CompMultiVerb>();
                    MultiVerbDict.Add(Eq, comp_MultiVerb);
                }
                if (comp_MultiVerb != null)
                {
                    return __instance.AllVerbs[comp_MultiVerb.verbIndex];
                }
            }
            return null;
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
}
