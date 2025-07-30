using AzWeaponLib.AmmoSystem;
using AzWeaponLib.HeavyWeapon;
using AzWeaponLib.MultiVerb;
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
            if (ModLister.GetActiveModWithIdentifier("Owlchemist.Tacticowl") != null)
            {
                Log.Message("[AWL]Notify Tacticowl exsists");
                StartUpLib.PatchWhenTacticowlEnabled();
            }
            StartUpLib.PatchAllHeavyWeaponApparel();
        }
    }
    public class StartUpLib
    {
        public static void PatchAllHeavyWeaponApparel()
        {
            StringBuilder log = new StringBuilder("[AWL]Try patch AWL_PatchAllHeavyWeaponApparel");
            HashSet<ThingDef> heavyWeaponApparel = (from thingDef in DefDatabase<ThingDef>.AllDefs
                                                    let heavyWeaponExt = thingDef.GetModExtension<HeavyWeaponDef>()
                                                    where heavyWeaponExt != null && heavyWeaponExt.apparelGroupDef != null
                                                    from apparel in heavyWeaponExt.apparelGroupDef.availableApparels
                                                    select apparel).ToHashSet();
            foreach (ThingDef item in heavyWeaponApparel)
            {
                try
                {
                        CompProperties compProperties = new CompProperties();
                    compProperties.compClass = typeof(CompApparelWithHeavyWeapon);
                        item.comps.Add(compProperties);
                        log.AppendInNewLine("Success in patching " + item.ToString() + " with CompApparelWithHeavyWeapon");
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
            Type MVCF = AccessTools.TypeByName("MVCF.MVCF");
            MethodInfo MVCF_EnableFeature = AccessTools.Method(MVCF, "EnableFeature");


            Type Feature_VerbCompsType = AccessTools.TypeByName("MVCF.Features.Feature_VerbComps");
            object Feature_VerbCompsObj = MVCF
                .GetMethod("GetFeature", BindingFlags.Static | BindingFlags.Public)
                .MakeGenericMethod(Feature_VerbCompsType).Invoke(null, null);
            FieldInfo VerbComps_EnabledField = Feature_VerbCompsType.GetField("Enabled");


            Type Feature_ExtraEquipmentVerbsType = AccessTools.TypeByName("MVCF.Features.Feature_ExtraEquipmentVerbs");
            object Feature_ExtraEquipmentVerbsObj = MVCF
                .GetMethod("GetFeature", BindingFlags.Static | BindingFlags.Public)
                .MakeGenericMethod(Feature_ExtraEquipmentVerbsType).Invoke(null, null);
            FieldInfo ExtraEquipmentVerbs_EnabledField = Feature_ExtraEquipmentVerbsType.GetField("Enabled");

            AWL_Mod.MVCF_Feature_VerbComps = (bool)VerbComps_EnabledField.GetValue(Feature_VerbCompsObj);
            AWL_Mod.MVCF_Feature_ExtraEquipmentVerbs = (bool)ExtraEquipmentVerbs_EnabledField.GetValue(Feature_ExtraEquipmentVerbsObj);

            if (AWL_Mod.MVCF_Feature_VerbComps && !AWL_Mod.MVCF_Feature_ExtraEquipmentVerbs)
            {
                Log.Message("[AWL]MVCF.Features.Feature_VerbComp is enabled, try to enable MVCF.Features.Feature_ExtraEquipmentVerbs");
                MVCF_EnableFeature.Invoke(null, new object[] { Feature_ExtraEquipmentVerbsObj });
                AWL_Mod.MVCF_Feature_ExtraEquipmentVerbs = true;
            }
            if (AWL_Mod.MVCF_Feature_ExtraEquipmentVerbs)
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
        public static void PatchWhenTacticowlEnabled()
        {
            HashSet<CompProperties_Ammo> compProperties = (from thingDef in DefDatabase<ThingDef>.AllDefs
                                                           let prop = thingDef.GetCompProperties<CompProperties_Ammo>()
                                                           where prop != null
                                                           select prop).ToHashSet();
            foreach (var p in compProperties)
            { 
                p.canMoveWhenReload = true;
            }
        }
    }
}
