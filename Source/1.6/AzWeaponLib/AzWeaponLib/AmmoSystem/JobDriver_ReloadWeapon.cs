﻿using RimWorld;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using static RimWorld.PsychicRitualRoleDef;
using static UnityEngine.GraphicsBuffer;
using AzWeaponLib;

namespace AzWeaponLib.AmmoSystem
{
    /// <summary>
    /// pawn装填武器
    /// </summary>
    public class JobDriver_ReloadWeapon : JobDriver
    {
        public bool canMove => compAmmo.Props.canMoveWhenReload;
        private ThingWithComps weapon => TargetB.Thing as ThingWithComps;
        private CompAmmo compAmmoInt;
        private CompAmmo compAmmo
        {
            get
            {
                if (compAmmoInt == null) compAmmoInt = weapon.TryGetComp<CompAmmo>();
                return compAmmoInt;
            }
            set
            {
                compAmmoInt = value;
            }
        }
        private bool useAmmo => job.count > 0;
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (TargetB.Thing == null || TargetB.Thing != pawn.equipment.Primary) return false;
            if (useAmmo) 
            { 
                if (compAmmo.NoBackupAmmo) return false;
            }
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            int reloadTick = Mathf.Min(compAmmo.GetReloadTicks(), compAmmo.MaxReloadTick);
            this.FailOn(() => (useAmmo && compAmmo.NoBackupAmmo) || !compAmmo.NeedReload);
            Toil waitForAvaliable = ReloadWait(int.MaxValue, canMove: true);
            waitForAvaliable.tickAction = delegate
            {
                if (!pawn.stances.curStance.StanceBusy) ReadyForNextToil();
            };
            yield return waitForAvaliable;
            int loop = compAmmo.ReloadLoop(job.playerForced);
            do
            {
                Toil reload = ReloadWait(reloadTick, canMove: canMove);
                reload.WithProgressBarToilDelay(TargetIndex.A);
                yield return reload;
                switch (useAmmo)
                {
                    case false://无需弹药
                        if (compAmmo.Props.singleShotLoading) yield return Toils_General.Do(compAmmo.ReloadByOne);
                        else yield return Toils_General.Do(compAmmo.ReloadToMax);
                        break;
                    case true: //需要弹药
                        if (compAmmo.Props.singleShotLoading) yield return Toils_General.Do(compAmmo.ReloadByBackupAmmoOnce);
                        else yield return Toils_General.Do(compAmmo.ReloadByBackupAmmo);
                        break;
                }
                loop--;
            } while (loop > 0);
            if (canMove)
            {
                Toil toil = ToilMaker.MakeToil("TryGoToDest");
                toil.initAction = delegate {
                    if (pawn.pather.Moving)
                    {
                        Job job = JobMaker.MakeJob(JobDefOf.Goto, pawn.pather.Destination);
                        pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    }
                };
                yield return toil;
            }
            
        }
        public static Toil ReloadWait(int ticks, TargetIndex face = TargetIndex.None, bool canMove = false)
        {
            Toil toil = ToilMaker.MakeToil("Reload");
            if (!canMove)
            {
                toil.initAction = delegate
                {
                    toil.actor.pather.StopDead();
                };
            }
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = ticks;
            if (face != TargetIndex.None)
            {
                toil.handlingFacing = true;
                toil.tickIntervalAction = delegate
                {
                    toil.actor.rotationTracker.FaceTarget(toil.actor.CurJob.GetTarget(face));
                };
            }
            return toil;
        }
    }
}
