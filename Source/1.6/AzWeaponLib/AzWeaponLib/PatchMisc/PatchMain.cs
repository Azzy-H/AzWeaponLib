using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using static UnityEngine.Scripting.GarbageCollector;
using AzWeaponLib.HeavyWeapon;
using AzWeaponLib.MultiVerb;

namespace AzWeaponLib
{
    [StaticConstructorOnStartup]
    public class PatchMain
    {
        public static Harmony instance;
        static PatchMain()
        {
            instance = new Harmony("AWL_Patch");
            instance.PatchAll(Assembly.GetExecutingAssembly());
            if (ModLister.GetActiveModWithIdentifier("OskarPotocki.VanillaFactionsExpanded.Core") != null)
            {
                Log.Message("[AWL]Notify MVCF exsists");
                LongEventHandler.ExecuteWhenFinished(StartUpLib.PatchWhenMVCFEnabled);
            }
            StartUpLib.PatchAllHeavyWeaponApparel();
        }
    }
    public class StartUpLib
    {
        public static void PatchAllHeavyWeaponApparel()
        {
            Log.Message("[AWL]Try patch AWL_PatchAllHeavyWeaponApparel");
            HashSet<ThingDef> heavyWeaponApparel = (from thingDef in DefDatabase<ThingDef>.AllDefs
                                                    let heavyWeaponExt = thingDef.GetModExtension<HeavyWeaponDef>()
                                                    where heavyWeaponExt != null && heavyWeaponExt.apparelGroupDef != null
                                                    from apparel in heavyWeaponExt.apparelGroupDef.availableApparels
                                                    select apparel).ToHashSet();
            StringBuilder log = new StringBuilder();
            foreach (ThingDef item in heavyWeaponApparel)
            {
                try
                {
                        CompProperties compProperties = new CompProperties();
                    compProperties.compClass = typeof(CompApparelWithHeavyWeapon);
                        item.comps.Add(compProperties);
                        log.AppendLine("Success in patching " + item.ToString() + " with CompApparelWithHeavyWeapon");
                }
                catch (Exception ex)
                {
                    Log.Error($"Error adding CompApparelWithHeavyWeapon {item.defName}: {ex.Message}");
                }
            }
            Log.Message(log);
        }
        public static void PatchWhenMVCFEnabled()
        {
            Type Feature_ExtraEquipmentVerbsType = AccessTools.TypeByName("MVCF.Features.Feature_ExtraEquipmentVerbs");
            object Feature_ExtraEquipmentVerbsObj = AccessTools
                .TypeByName("MVCF.MVCF")
                .GetMethod("GetFeature", BindingFlags.Static | BindingFlags.Public)
                .MakeGenericMethod(Feature_ExtraEquipmentVerbsType).Invoke(null, null);
            FieldInfo EnabledField = Feature_ExtraEquipmentVerbsType.GetField("Enabled");
            if (AWL_Mod.MVCF_Feature_ExtraEquipmentVerbs = (bool)EnabledField.GetValue(Feature_ExtraEquipmentVerbsObj))
            {
                
                Log.Message("[AWL]MVCF.Features.Feature_ExtraEquipmentVerbs is enabled, try to remove CompProperties_MultiVerb");
                StringBuilder log = new StringBuilder();
                List<ThingDef> MultiVerbWeapon = DefDatabase<ThingDef>.AllDefs.Where(x => x.HasComp<CompMultiVerb>()).ToList();
                foreach (ThingDef item in MultiVerbWeapon)
                {
                    try
                    {
                        item.comps.Remove(item.GetCompProperties<CompProperties_MultiVerb>());
                        log.AppendLine("Success in removing " + item.ToString() + "'s CompProperties_MultiVerb");
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error removing CompProperties_MultiVerb {item.defName}: {ex.Message}");
                    }
                }
                Log.Message(log);
            }
            else
            {
                Log.Message("[AWL]No need to patch MVCF.");
            }
            
        }
    }
}
