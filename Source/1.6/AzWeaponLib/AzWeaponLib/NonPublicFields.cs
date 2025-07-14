using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace AzWeaponLib
{
    [StaticConstructorOnStartup]
    internal static class NonPublicFields
    {
        public static FieldInfo SiegeBlueprintPlacer_center = AccessTools.Field(typeof(SiegeBlueprintPlacer), "center");

        public static FieldInfo SiegeBlueprintPlacer_faction = AccessTools.Field(typeof(SiegeBlueprintPlacer), "faction");

        public static FieldInfo SiegeBlueprintPlacer_NumCoverRange = AccessTools.Field(typeof(SiegeBlueprintPlacer), "NumCoverRange");

        public static FieldInfo SiegeBlueprintPlacer_placedCoverLocs = AccessTools.Field(typeof(SiegeBlueprintPlacer), "placedCoverLocs");

        public static FieldInfo SiegeBlueprintPlacer_CoverLengthRange = AccessTools.Field(typeof(SiegeBlueprintPlacer), "CoverLengthRange");

        public static FieldInfo Projectile_ticksToImpact = AccessTools.Field(typeof(Projectile), "ticksToImpact");

        public static FieldInfo Projectile_origin = AccessTools.Field(typeof(Projectile), "origin");

        public static FieldInfo Projectile_destination = AccessTools.Field(typeof(Projectile), "destination");

        public static FieldInfo Projectile_usedTarget = AccessTools.Field(typeof(Projectile), "usedTarget");

        public static FieldInfo StunHandler_adaptationTicksLeft = AccessTools.Field(typeof(StunHandler), "adaptationTicksLeft");

        public static FieldInfo Projectile_AmbientSustainer = typeof(Projectile).GetField("ambientSustainer", BindingFlags.Instance | BindingFlags.NonPublic);

        public static FieldInfo ThingWithComps_comps = typeof(ThingWithComps).GetField("comps", BindingFlags.Instance | BindingFlags.NonPublic);

        public static FieldInfo StatDrawEntry_labelInt = AccessTools.Field(typeof(StatDrawEntry), "labelInt");

        public static FieldInfo StatDrawEntry_displayOrderWithinCategory = AccessTools.Field(typeof(StatDrawEntry), "displayOrderWithinCategory");

        public static FieldInfo Verb_currentTarget = AccessTools.Field(typeof(Verb), "currentTarget");
    }
}
