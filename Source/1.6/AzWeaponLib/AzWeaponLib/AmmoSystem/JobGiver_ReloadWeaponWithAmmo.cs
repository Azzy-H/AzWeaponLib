using RimWorld.Utility;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using AzWeaponLib;

namespace AzWeaponLib.AmmoSystem
{
    /// <summary>
    /// pawn自己找物品装填备弹
    /// </summary>
    public class JobGiver_ReloadWeaponWithAmmo : ThinkNode_JobGiver
    {
        public override float GetPriority(Pawn pawn)
        {
            return 5.9f;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                return null;
            }
            CompAmmo compAmmo = AmmoUtility.FindCompAmmo(pawn);
            if (compAmmo == null)
            {
                return null;
            }
            if (!compAmmo.needReloadBackupAmmo)
            {
                return null;
            }
            List<Thing> list = AmmoUtility.FindEnoughAmmo(pawn, pawn.Position, compAmmo, forceReload: false);
            if (list.NullOrEmpty())
            {
                return null;
            }
            return MakeReloadJob(compAmmo, list);
        }

        public static Job MakeReloadJob(CompAmmo compAmmo, List<Thing> chosenAmmo)
        {
            Job job = JobMaker.MakeJob(AWL_DefOf.AWL_ReloadWeaponWithAmmo, compAmmo.parent);
            job.targetQueueB = chosenAmmo.Select((Thing t) => new LocalTargetInfo(t)).ToList();
            job.count = chosenAmmo.Sum((Thing t) => t.stackCount);
            job.count = Math.Min(job.count, compAmmo.maxAmmoNeeded);
            return job;
        }
    }
}
