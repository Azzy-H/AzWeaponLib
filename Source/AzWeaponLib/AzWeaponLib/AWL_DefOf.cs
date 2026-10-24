using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace AzWeaponLib
{
    [DefOf]
    public class AWL_DefOf
    {
        public static StatDef AWL_ReloadingTimeFactor = null!;
        public static StatDef AWL_AmmoCapacity = null!;
        public static StatDef AWL_BackAmmoCapacity = null!;
        public static StatDef AWL_ReloadingTime = null!;
        public static JobDef AWL_ReloadWeaponWithAmmo = null!;
        public static JobDef AWL_ReloadWeapon = null!;
        public static HediffDef AWL_AmmoGizmoDisp = null!;
        public static HediffDef AWL_MultiVerbSelect = null!;
        public static HediffDef AWL_AbilityTurret = null!;
        public static ShaderDef AWL_FlameField = null!;
        static AWL_DefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(AWL_DefOf));
        }
    }
}
