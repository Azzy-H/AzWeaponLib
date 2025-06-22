using RimWorld;

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
            int loop = compAmmo.ReloadLoop(job.playerForced);
            do
            {
                Toil wait = Toils_General.Wait(reloadTick);
                wait.WithProgressBarToilDelay(TargetIndex.A);
                yield return wait;
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
            
        }
    }
}
