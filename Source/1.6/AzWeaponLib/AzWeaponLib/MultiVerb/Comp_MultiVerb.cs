using AzWeaponLib.PatchMisc;
using HarmonyLib;  
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace AzWeaponLib.MultiVerb
{
    public class CompProperties_MultiVerb : CompProperties
    {
        public List<GizmoInfo> gizmoInfos;
        public CompProperties_MultiVerb()
        {
            compClass = typeof(CompMultiVerbByHediff);
        }
        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            if (gizmoInfos.Count != parentDef.Verbs.Count)
            {
                yield return parentDef.ToString() + "'s CompProperties_MultiVerb has wrong count of gizmoInfos. It must be consistant with verbs' count";
            }
        }
    }
    public abstract class CompMultiVerb : ThingComp
    {
        public static bool GlobalDisabled = false;
        public CompProperties_MultiVerb Props => (CompProperties_MultiVerb)props;
        public static AWL_Settings AWL_Settings = LoadedModManager.GetMod<AWL_Mod>().GetSettings<AWL_Settings>();
        public int verbIndex = 0;
        public override void Notify_Equipped(Pawn pawn)
        {
            if (GlobalDisabled) return;
            if (AWL_Settings.randomWeaponModeForNonPlayerPawn && (pawn.Faction == null || !pawn.Faction.IsPlayer))
            {
                verbIndex = Rand.Range(0, parent.def.Verbs.Count);
            }
        }
        public int SetNextVerbIndex()
        {
            verbIndex++;
            if (verbIndex >= parent.def.Verbs.Count)
                verbIndex = 0;
            return verbIndex;
        }
        public Verb VerbByIndex(int index)
        {
            return parent.GetComp<CompEquippable>().AllVerbs[index];
        }
        public override void PostExposeData()
        {
            Scribe_Values.Look(ref verbIndex, "verbIndex", 0);
        }
        public static void DoHarmonyPatch(Harmony instance)
        {
            MethodInfo VerbTracker_get_PrimaryVerb = AccessTools.Method(typeof(VerbTracker), "get_PrimaryVerb");
            MethodInfo VerbTracker_get_PrimaryVerb_Transpiler = AccessTools.Method(typeof(Patch_VerbTracker), nameof(Patch_VerbTracker.Transpiler));
            instance.Patch(VerbTracker_get_PrimaryVerb, transpiler: VerbTracker_get_PrimaryVerb_Transpiler);

            MethodInfo VerbTracker_CreateVerbTargetCommand = AccessTools.Method(typeof(VerbTracker), "CreateVerbTargetCommand");
            MethodInfo VerbTracker_CreateVerbTargetCommand_Postfix = AccessTools.Method(typeof(Patch_VerbTracker), nameof(Patch_VerbTracker.Postfix_CreateVerbTargetCommand));
            instance.Patch(VerbTracker_CreateVerbTargetCommand, postfix: VerbTracker_CreateVerbTargetCommand_Postfix);

            MethodInfo Find_ClearCache = AccessTools.Method(typeof(Find), nameof(Find.ClearCache));
            MethodInfo Find_ClearCache_Postfix = AccessTools.Method(typeof(Patch_Find), nameof(Patch_Find.Postfix_ClearCache));
            instance.Patch(Find_ClearCache, postfix: Find_ClearCache_Postfix);
        }
    }
    public class CompMultiVerbByHediff : CompMultiVerb
    {
        HediffDef hediffDef => AWL_DefOf.AWL_MultiVerbSelect;
        Hediff hediff;
        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            if (GlobalDisabled) return;
            hediff = HediffMaker.MakeHediff(hediffDef, pawn);
            pawn.health.AddHediff(hediff);
        }
        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            if (GlobalDisabled) return;
            if (hediff != null) pawn.health.RemoveHediff(hediff);
            hediff = null;
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref hediff, "hediff_CompMultiVerbByHediff");
        }
    }
}
