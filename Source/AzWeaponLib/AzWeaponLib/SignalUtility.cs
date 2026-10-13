using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace AzWeaponLib
{
    public static class SignalUtility
    {
        public static void BroadcastCompSignalToPawn(this Pawn pawn, string signal)
        {
            pawn.BroadcastCompSignal(signal);
            pawn.apparel?.WornApparel.ForEach(apparel => apparel.BroadcastCompSignal(signal));
            pawn.equipment?.AllEquipmentListForReading.ForEach(eq => eq.BroadcastCompSignal(signal));
        }
    }
}
