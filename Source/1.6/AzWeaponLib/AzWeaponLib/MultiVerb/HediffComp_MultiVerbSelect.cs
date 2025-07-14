using RimWorld.BaseGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace AzWeaponLib.MultiVerb
{
    public class HediffCompProperties_MultiVerbSelect : HediffCompProperties
    {
        public HediffCompProperties_MultiVerbSelect()
        {
            compClass = typeof(HediffComp_MultiVerbSelect);
        }
    }
    public class HediffComp_MultiVerbSelect : HediffComp
    {
        private bool compShouldRemove = false;
        public override bool CompShouldRemove => compShouldRemove;
        private int verbIndexCache = 0;
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
            Command_Action Verb_Switch = new Command_Action
            {
                icon = ContentFinder<Texture2D>.Get(Eq_Comp.Props.gizmoInfos[verbIndexCache].iconPath),
                defaultLabel = Eq_Comp.Props.gizmoInfos[verbIndexCache].defaultLabel,
                defaultDesc = Eq_Comp.Props.gizmoInfos[verbIndexCache].defaultDesc,
                onHover = null,
                activateSound = SoundDef.Named("Click"),
                groupKey = 85486589 + verbIndexCache,
                hotKey = null,
                action = SwitchToNextVerb
            };
            yield return Verb_Switch;
        }
        public override void CompPostMake()
        {
            base.CompPostMake();
            if (Eq_Comp == null) 
            { 
                compShouldRemove = true;
                return;
            }
            verbIndexCache = Eq_Comp.verbIndex;
        }
        private void SwitchToNextVerb()
        {
            if (Eq_Comp == null) compShouldRemove = true;
            else
            {
                verbIndexCache = Eq_Comp.SetNextVerbIndex();
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                if (Eq_Comp == null)
                {
                    compShouldRemove = true;
                    return;
                }
                verbIndexCache = Eq_Comp.verbIndex;
            }
        }
    }
}
