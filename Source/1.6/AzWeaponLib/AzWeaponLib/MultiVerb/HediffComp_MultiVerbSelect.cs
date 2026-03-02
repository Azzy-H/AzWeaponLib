using RimWorld.BaseGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AzWeaponLib.MultiVerb
{
    public class HediffCompProperties_MultiVerbSelect : HediffCompProperties
    {
        public HediffCompProperties_MultiVerbSelect()
        {
            compClass = typeof(HediffComp_MultiVerbSelect);
        }
    }
    [StaticConstructorOnStartup]
    public class HediffComp_MultiVerbSelect : HediffComp
    {
        public static readonly Texture autoSwitchTex = ContentFinder<Texture2D>.Get("MultiVerb/modeSwitch");
        private const int TickInterval = 600;
        private const int TickIntervalInCombat = 15;
        private bool compShouldRemove = false;
        public override bool CompShouldRemove => compShouldRemove;
        private CompMultiVerb Eq_CompInt;
        public CompMultiVerb Eq_Comp
        { 
            get 
            { 
                if(Pawn == null || Pawn.equipment == null || Pawn.equipment.Primary == null) return null;
                if (Eq_CompInt == null)
                {
                    Eq_CompInt = Pawn.equipment.Primary.TryGetComp<CompMultiVerb>();
                }
                return Eq_CompInt;
            }
        }
        public override IEnumerable<Gizmo> CompGetGizmos()
        {
            if (CompMultiVerb.GlobalDisabled) yield break;
            Command_Action Verb_Switch = new Command_Action
            {
                icon = ContentFinder<Texture2D>.Get(Eq_Comp.Props.verbInfos[Eq_Comp.verbIndex].iconPath),
                defaultLabel = Eq_Comp.Props.verbInfos[Eq_Comp.verbIndex].defaultLabel,
                defaultDesc = Eq_Comp.Props.verbInfos[Eq_Comp.verbIndex].defaultDesc,
                onHover = null,
                activateSound = SoundDef.Named("Click"),
                groupKey = 85486589 + Eq_Comp.verbIndex,
                hotKey = null,
                action = SwitchToNextVerb
            };
            yield return Verb_Switch;
            if(!Eq_Comp.Props.CanSwitchVerbAutomatically) yield break;
            Command_Toggle Verb_Switch_Toggle = new Command_Toggle
            {
                icon = autoSwitchTex,
                defaultLabel = "AWL_SwitchToggle".Translate(),
                defaultDesc = "AWL_SwitchToggleDesc".Translate(),
                activateSound = SoundDef.Named("Click"),
                groupKey = 85486289 + (Eq_Comp.SwitchVerbAutomatically ? 0 : 1),
                hotKey = null,
                isActive = () => Eq_Comp.SwitchVerbAutomatically,
                toggleAction = () => Eq_Comp.SwitchVerbAutomatically = !Eq_Comp.SwitchVerbAutomatically
            };
            yield return Verb_Switch_Toggle;
        }
        public override void CompPostMake()
        {
            base.CompPostMake();
            if (Eq_Comp == null) 
            { 
                compShouldRemove = true;
                return;
            }
        }
        private void SwitchToNextVerb()
        {
            if (Eq_Comp == null) compShouldRemove = true;
            else
            {

                if (parent.pawn.CurJob.verbToUse == Eq_Comp.VerbByIndex(Eq_Comp.verbIndex))
                {
                    Eq_Comp.SetNextVerbIndex();
                    parent.pawn.CurJob.verbToUse = Eq_Comp.VerbByIndex(Eq_Comp.verbIndex);
                }
                else 
                {
                    Eq_Comp.SetNextVerbIndex();
                } 
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (Eq_Comp == null)
                {
                    compShouldRemove = true;
                    return;
                }
            }
        }
        public override void Notify_PawnUsedVerb(Verb verb, LocalTargetInfo target)
        {
            if (CompMultiVerb.GlobalDisabled) return;
            Eq_Comp.Notify_PawnUsedVerb(verb, target);
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            if (CompMultiVerb.GlobalDisabled) return;
            if (!Eq_Comp.SwitchVerbAutomatically) return;
            if (Pawn.mindState.WasRecentlyCombatantTicks(TickInterval) ? Pawn.IsHashIntervalTick(TickIntervalInCombat) : Pawn.IsHashIntervalTick(TickInterval))
            {
                Eq_Comp.SetVerbIndex(Eq_Comp.GetBestVerbIndex());
            }
        }
    }
}
