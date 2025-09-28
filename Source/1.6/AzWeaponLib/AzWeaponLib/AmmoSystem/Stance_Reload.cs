using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace AzWeaponLib.AmmoSystem
{
    public class Stance_Reload : Stance_Busy
    {
        public override bool StanceBusy => false;
        public Stance_Reload(int ticks, LocalTargetInfo focusTarg)
        {
            ticksLeft = ticks;
            startedTick = Find.TickManager.TicksGame;
            this.focusTarg = focusTarg;
        }
    }
}
