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

        public static FieldInfo Projectile_AmbientSustainer = typeof(Projectile).GetField("ambientSustainer", BindingFlags.Instance | BindingFlags.NonPublic);

        public static FieldInfo ThingWithComps_comps = typeof(ThingWithComps).GetField("comps", BindingFlags.Instance | BindingFlags.NonPublic);

        public static FieldInfo StatDrawEntry_labelInt = AccessTools.Field(typeof(StatDrawEntry), "labelInt");

        public static FieldInfo StatDrawEntry_displayOrderWithinCategory = AccessTools.Field(typeof(StatDrawEntry), "displayOrderWithinCategory");

        public static FieldInfo Verb_currentTarget = AccessTools.Field(typeof(Verb), "currentTarget");
    }
}
