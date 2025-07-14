using HarmonyLib;
using System;
using Verse;

namespace AzWeaponLib
{
    [StaticConstructorOnStartup]
    internal static class NonPublicProperties
    {
        // Token: 0x040005A6 RID: 1446
        public static Func<Projectile, float> Projectile_get_StartingTicksToImpact = (Func<Projectile, float>)Delegate.CreateDelegate(typeof(Func<Projectile, float>), null, AccessTools.Property(typeof(Projectile), "StartingTicksToImpact").GetGetMethod(true));
    }
}
