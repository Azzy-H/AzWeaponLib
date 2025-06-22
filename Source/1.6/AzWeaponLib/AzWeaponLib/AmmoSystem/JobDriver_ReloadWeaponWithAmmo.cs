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
    public class JobDriver_ReloadWeaponWithAmmo : JobDriver
    {
        private const TargetIndex GearInd = TargetIndex.A;

        private const TargetIndex AmmoInd = TargetIndex.B;

        private Thing Gear => job.GetTarget(TargetIndex.A).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            pawn.ReserveAsManyAsPossible(job.GetTargetQueue(TargetIndex.B), job);
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            CompAmmo compAmmo = Gear?.TryGetComp<CompAmmo>();
            this.FailOn(() => compAmmo == null);
            this.FailOn(() => compAmmo.pawn != pawn);
            this.FailOn(() => !compAmmo.needReloadBackupAmmo);
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnIncapable(PawnCapacityDefOf.Manipulation);
            Toil getNextIngredient = Toils_General.Label();
            yield return getNextIngredient;
            foreach (Toil item in ReloadAsMuchAsPossible(compAmmo))
            {
                yield return item;
            }
            yield return Toils_JobTransforms.ExtractNextTargetFromQueue(TargetIndex.B);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, putRemainderInQueue: false, subtractNumTakenFromJobCount: true).FailOnDestroyedNullOrForbidden(TargetIndex.B);
            yield return Toils_Jump.JumpIf(getNextIngredient, () => !job.GetTargetQueue(TargetIndex.B).NullOrEmpty());
            foreach (Toil item2 in ReloadAsMuchAsPossible(compAmmo))
            {
                yield return item2;
            }
            Toil toil = ToilMaker.MakeToil("MakeNewToils");
            toil.initAction = delegate
            {
                Thing carriedThing = pawn.carryTracker.CarriedThing;
                if (carriedThing != null && !carriedThing.Destroyed)
                {
                    pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out var _);
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return toil;
        }

        private IEnumerable<Toil> ReloadAsMuchAsPossible(CompAmmo compAmmo)
        {
            Toil done = Toils_General.Label();
            yield return Toils_Jump.JumpIf(done, () => pawn.carryTracker.CarriedThing == null);
            yield return Toils_General.Wait(compAmmo.GetReloadTicks()).WithProgressBarToilDelay(TargetIndex.A);
            Toil toil = ToilMaker.MakeToil("ReloadAsMuchAsPossible");
            toil.initAction = delegate
            {
                Thing carriedThing = pawn.carryTracker.CarriedThing;
                compAmmo.ReloadByAmmoBox(carriedThing);
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return toil;
            yield return done;
        }
    }
}
