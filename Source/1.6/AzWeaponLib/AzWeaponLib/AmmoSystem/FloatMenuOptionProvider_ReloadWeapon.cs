using RimWorld;
using RimWorld.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace AzWeaponLib.AmmoSystem
{
    public class FloatMenuOptionProvider_ReloadWeapon : FloatMenuOptionProvider
    {
        protected override bool Drafted => true;

        protected override bool Undrafted => true;

        protected override bool Multiselect => false;

        public override IEnumerable<FloatMenuOption> GetOptionsFor(Thing clickedThing, FloatMenuContext context)
        {
            CompAmmo compAmmo = AmmoUtility.FindCompAmmo(context.FirstSelectedPawn);
            if (compAmmo == null || clickedThing.def != compAmmo.Props.ammunitionDef) yield break;
            string text = "Reload".Translate(compAmmo.parent.Named("GEAR"), NamedArgumentUtility.Named(compAmmo.Props.ammunitionDef, "AMMO"));
            if (!context.FirstSelectedPawn.CanReach(clickedThing, PathEndMode.ClosestTouch, Danger.Deadly))
            {
                yield return new FloatMenuOption(text + ": " + "NoPath".Translate().CapitalizeFirst(), null);
                yield break;
            }
            if (!compAmmo.needReloadBackupAmmo)
            {
                yield return new FloatMenuOption(text + ": " + "ReloadFull".Translate(), null);
                yield break;
            }
            Action action = delegate
            {
                Job job = JobGiver_ReloadWeaponWithAmmo.MakeReloadJob(compAmmo, new List<Thing> { clickedThing });
                context.FirstSelectedPawn.jobs.StartJob(job, lastJobEndCondition: JobCondition.InterruptForced);
            };
            yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text, action), context.FirstSelectedPawn, clickedThing);
        }
    }
}
