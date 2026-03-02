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
        public bool SwitchStyle = false;
        public bool CanSwitchVerbAutomatically = false;
        public List<MultiVerbInfo> verbInfos;
        public CompProperties_MultiVerb()
        {
            compClass = typeof(CompMultiVerbByHediff);
        }
        public override void ResolveReferences(ThingDef parentDef)
        {
            base.ResolveReferences(parentDef);
            SwitchStyle = verbInfos.Any(vi => vi.styleDef != null);
            CanSwitchVerbAutomatically = verbInfos.Any(vi => vi.verbSwitchWorker != typeof(VerbSwitchWorker));
        }
        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            if (verbInfos.Count != parentDef.Verbs.Count)
            {
                yield return parentDef.ToString() + "'s CompProperties_MultiVerb has wrong count of verbInfos. It must be consistant with verbs' count";
            }
            if (verbInfos.Any(vi => vi.styleDef != null) && !parentDef.HasComp(typeof(CompStyleable)))
            {
                yield return parentDef.ToString() + " has verbInfos with styleDef but it doesn't have CompStyleable. Please add CompStyleable to this def.";
            }
        }
    }
    public abstract class CompMultiVerb : ThingComp
    {
        public static bool GlobalDisabled = false;
        public CompProperties_MultiVerb Props => (CompProperties_MultiVerb)props;
        private CompStyleable compStyleable;
        public CompStyleable CompStyleable
        {
            get
            {
                if (compStyleable == null)
                {
                    compStyleable = parent.GetComp<CompStyleable>();
                }
                return compStyleable;
            }
        }
        private Pawn pawn;
        public static AWL_Settings AWL_Settings = LoadedModManager.GetMod<AWL_Mod>().GetSettings<AWL_Settings>();
        public int verbIndex = 0;
        public List<VerbSwitchWorker> VerbSwitchWorkers = new List<VerbSwitchWorker>();
        private bool switchVerbAutomatically = true;
        public bool SwitchVerbAutomatically
        {
            get
            {
                if (!Props.CanSwitchVerbAutomatically) return false;
                return switchVerbAutomatically;
            }
            set
            {
                if (!Props.CanSwitchVerbAutomatically)
                {
                    Log.Error("Trying to set SwitchVerbAutomatically to " + value + " for a CompMultiVerb whose Props doesn't allow automatic verb switching. This setting will be ignored.");
                    return;
                }
                switchVerbAutomatically = value;
            }
        }
        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            for (int i = 0; i < Props.verbInfos.Count; i++)
            {
                VerbSwitchWorkers.Add((VerbSwitchWorker)Activator.CreateInstance(Props.verbInfos[i].verbSwitchWorker));
                VerbSwitchWorkers[i].weapon = parent;
            }
        }
        public override void Notify_Equipped(Pawn pawn)
        {
            this.pawn = pawn;
            if (GlobalDisabled) return;
            if (AWL_Settings.randomWeaponModeForNonPlayerPawn && (pawn.Faction == null || !pawn.Faction.IsPlayer))
            {
                verbIndex = Rand.Range(0, parent.def.Verbs.Count);
            }
        }
        public override void Notify_Unequipped(Pawn pawn)
        {
            this.pawn = null;
        }
        public int SetNextVerbIndex()
        {
            verbIndex++;
            if (verbIndex >= parent.def.Verbs.Count)
                verbIndex = 0;
            Notify_VerbChanged();
            return verbIndex;
        }
        public int GetBestVerbIndex()
        {
            float bestPriority = 0f;
            int bestIndex = verbIndex;
            for (int i = 0; i < VerbSwitchWorkers.Count; i++)
            {
                float priority = VerbSwitchWorkers[i].GetPriority(pawn);
                if (priority > bestPriority)
                {
                    bestPriority = priority;
                    bestIndex = i;
                }
            }
            return bestIndex;
        }
        public void SetVerbIndex(int index)
        {
            if (index < 0 || index >= parent.def.Verbs.Count)
            {
                Log.Error("Trying to set verb index with an invalid value: " + index + " for " + parent.ToString());
                return;
            }
            verbIndex = index;
            Notify_VerbChanged();
        }
        public Verb VerbByIndex(int index)
        {
            return parent.GetComp<CompEquippable>().AllVerbs[index];
        }
        public override void PostExposeData()
        {
            Scribe_Values.Look(ref verbIndex, "verbIndex", 0);
            Scribe_References.Look(ref pawn, "pawn_CompMultiVerb");
        }
        public virtual void Notify_PawnUsedVerb(Verb verb, LocalTargetInfo target)
        {
            VerbSwitchWorkers[verbIndex].Notify_PawnUsedVerb(verb, target);
        }
        public virtual void Notify_VerbChanged()
        {
            if (Props.SwitchStyle && CompStyleable != null && Props.verbInfos[verbIndex].styleDef != CompStyleable.styleDef)
            {
                CompStyleable.styleDef = Props.verbInfos[verbIndex].styleDef;
                pawn?.Drawer.renderer.SetAllGraphicsDirty();
            }
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
