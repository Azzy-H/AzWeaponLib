using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static RimWorld.IdeoFoundation_Deity;

namespace AzWeaponLib.SpecialProjectile
{
    [HarmonyPatch(typeof(Projectile))]
    public class Patch_Projectile
    {
        private static List<Thing> thingsInCell = new List<Thing>();
        public static AWL_Settings AWL_Settings = LoadedModManager.GetMod<AWL_Mod>().GetSettings<AWL_Settings>();
        [HarmonyPatch("CheckForFreeIntercept")]
        [HarmonyPostfix]
        public static void Postfix_CheckForFreeIntercept(Projectile __instance, ref bool __result, IntVec3 c)
        {
            PiercingProjectileMethod(__instance, ref __result, c);
        }
        public static void PiercingProjectileMethod(Projectile __instance, ref bool __result, IntVec3 c)
        {
            if (__instance is PiercingProjectile pp && __instance.Map != null)
            {
                if (DebugViewSettings.drawShooting)
                {
                    __instance.Map.debugDrawer.FlashCell(c, 0.5f);
                }
                List<Thing> list = c.GetThingList(__instance.Map);
                int penetratingPowerLeft = pp.PenetratingPowerLeft;
                for (int num = list.Count - 1; num >= 0; num--)
                {
                    if (__instance.Destroyed) break;
                    Impact(__instance, list[num]);
                }
                if (penetratingPowerLeft != pp.PenetratingPowerLeft) __result = true;
            }
        }
        public static void Impact(Projectile projectile, Thing hitThing = null, bool blockedByShield = false)
        {
            MethodInfo method = projectile.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                            .Where(m => m.Name == "Impact" && m.GetParameters().Length == 2 && m.GetParameters()[0].ParameterType == typeof(Thing) && m.GetParameters()[1].ParameterType == typeof(bool))
                            .FirstOrDefault();
            method.Invoke(projectile, new object[2] { hitThing, blockedByShield });
        }
    }
}
