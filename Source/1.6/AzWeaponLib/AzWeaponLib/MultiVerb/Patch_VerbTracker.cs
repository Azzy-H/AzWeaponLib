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
    //[HarmonyPatch(typeof(VerbTracker))]
    internal class Patch_VerbTracker
    {
        private sealed class PrimaryVerbCacheEntry
        {
            public CompMultiVerb CompMultiVerb;
            public int CachedVerbIndex = -1;
            public Verb CachedVerb;
        }

        private static readonly Dictionary<CompEquippable, PrimaryVerbCacheEntry> PrimaryVerbCache = new Dictionary<CompEquippable, PrimaryVerbCacheEntry>();
        //[HarmonyPatch("get_PrimaryVerb")]
        //[HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> list = instructions.ToList();
            MethodInfo methodInfo = AccessTools.Method(typeof(Patch_VerbTracker), nameof(Patch_VerbTracker.PrefixMethod_get_PrimaryVerb));

            Label continueLabel = generator.DefineLabel();
            Label retLabel = generator.DefineLabel();
            list[0].labels.Add(continueLabel);

            List<CodeInstruction> prefix = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, methodInfo),
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Brtrue_S, retLabel),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Br_S, continueLabel),
                new CodeInstruction(OpCodes.Ret) { labels = new List<Label> { retLabel } }
            };
            list.InsertRange(0, prefix);
            return list;
        }
        private static Verb PrefixMethod_get_PrimaryVerb(VerbTracker __instance)
        {
            CompEquippable eq = __instance.directOwner as CompEquippable;
            if (eq == null)
            {
                return null;
            }

            if (!PrimaryVerbCache.TryGetValue(eq, out PrimaryVerbCacheEntry cacheEntry))
            {
                cacheEntry = new PrimaryVerbCacheEntry
                {
                    CompMultiVerb = eq.parent.GetComp<CompMultiVerb>()
                };
                PrimaryVerbCache.Add(eq, cacheEntry);
            }

            if (cacheEntry.CompMultiVerb == null)
            {
                return null;
            }

            int verbIndex = cacheEntry.CompMultiVerb.verbIndex;
            if (cacheEntry.CachedVerb != null && cacheEntry.CachedVerbIndex == verbIndex)
            {
                return cacheEntry.CachedVerb;
            }

            List<Verb> allVerbs = __instance.AllVerbs;
            if ((uint)verbIndex >= (uint)allVerbs.Count)
            {
                cacheEntry.CachedVerbIndex = -1;
                cacheEntry.CachedVerb = null;
                return null;
            }

            Verb selectedVerb = allVerbs[verbIndex];
            cacheEntry.CachedVerbIndex = verbIndex;
            cacheEntry.CachedVerb = selectedVerb;
            return selectedVerb;
        }
        public static void ClearCaches()
        {
            PrimaryVerbCache.Clear();
        }
        //[HarmonyPatch("CreateVerbTargetCommand")]
        //[HarmonyPostfix]
        internal static void Postfix_CreateVerbTargetCommand(Thing ownerThing, Verb verb, VerbTracker __instance, ref Command_VerbTarget __result)
        {
            if (__instance.directOwner is CompEquippable Eq)
            {
                CompMultiVerb compMultiVerb = Eq.parent.TryGetComp<CompMultiVerb>();
                if (compMultiVerb != null)
                {
                    if (verb != __instance.AllVerbs[compMultiVerb.verbIndex])
                    {
                        __result = new Command_VerbTargetInvisible();
                        __result.defaultDesc = ownerThing.LabelCap + ": " + ownerThing.def.description.CapitalizeFirst();
                        __result.ownerThing = null;//用于取消合并
                        __result.tutorTag = "VerbTarget";
                        __result.verb = verb;
                        __result.drawRadius = false;
                        __result.Disable("SR_DisabledByCompMultiVerb".Translate());
                    }
                }
            }
        }
    }
}
