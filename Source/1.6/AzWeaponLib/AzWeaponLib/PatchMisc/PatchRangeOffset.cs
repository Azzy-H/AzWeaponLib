using UnityEngine;
using Verse;
using HarmonyLib;
using RimWorld;

namespace AzWeaponLib
{
    [HarmonyPatch(typeof(Pawn_MechanitorTracker))]
    [HarmonyPatch(nameof(Pawn_MechanitorTracker.CanCommandTo))]
    public class RangeOffset_Patch
    {
        [HarmonyPostfix]
        public static void CanCommandToPostfix(LocalTargetInfo target, Pawn_MechanitorTracker __instance, ref bool __result)
        {
            float rangeOffset = __instance.Pawn?.GetStatValue(StatDef.Named("RangeOffset")) ?? 0f;
            if (target.Cell.InBounds(__instance.Pawn.MapHeld) && (float)__instance.Pawn.Position.DistanceToSquared(target.Cell) < (24.9f + rangeOffset) * (24.9f + rangeOffset) || __result)
            {
                __result = true;
            }
            else
            {
                __result = false;
            }
        }
    }
    //绘制机械师控制范围
    [HarmonyPatch(typeof(Pawn_MechanitorTracker))]
    [HarmonyPatch("DrawCommandRadius")]
    internal class Pawn_MechanitorTracker_DrawCommandRadius_Patch
    {
        [HarmonyPrefix]
        private static bool DrawCommandRadiusPrefix()
        {
            return false;
        }
        [HarmonyPostfix]
        public static void DrawCommandRadiusPostfix(Pawn_MechanitorTracker __instance)
        {
            Pawn mech = __instance.Pawn;
            bool flag = mech.Spawned && __instance.AnySelectedDraftedMechs;
            if (flag)
            {
                float rangeOffset = 0.0f, DefaultRange = 24.9f;
                rangeOffset = (mech?.GetStatValue(StatDef.Named("RangeOffset")) ?? 0f);
                IntVec3 position = mech.Position;
                float radius = DefaultRange + rangeOffset;
                GenDraw.DrawRadiusRing(position, radius, Color.white, null);
            }
        }
    }

}