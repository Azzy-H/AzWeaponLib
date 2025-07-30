using System;
using UnityEngine;
using Verse;

namespace AzWeaponLib
{
    public class AWL_Mod :Mod
    {
        AWL_Settings settings;
        public static string contentDir;
        public static bool MVCF_Feature_VerbComps = false;
        public static bool MVCF_Feature_ExtraEquipmentVerbs = false;
        public AWL_Mod(ModContentPack content) : base(content)
        {
            this.settings = GetSettings<AWL_Settings>();
            contentDir = base.Content.RootDir;
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            bool resetFlag = listingStandard.ButtonText("Reset".Translate());
            if (resetFlag)
            {
                settings.enableAmmoSystem = !ModsConfig.IsActive("ceteam.combatextended");
                settings.enableBackupAmmoSystem = true;
                settings.onlyShowAmmoGizmoWhenSelectedOneThing = true;
                settings.randomWeaponModeForNonPlayerPawn = true;
            }
            listingStandard.Gap(5);
            listingStandard.CheckboxLabeled("EnableAmmoSystem".Translate(), ref settings.enableAmmoSystem, "EnableTheAmmoSystem".Translate());
            if (settings.enableAmmoSystem)
            {
                listingStandard.Gap(5);
                listingStandard.CheckboxLabeled("-" + "EnableBackupAmmoSystemLabel".Translate(), ref settings.enableBackupAmmoSystem, "EnableBackupAmmoSystemLabelDesc".Translate());
                listingStandard.Gap(5);
                listingStandard.CheckboxLabeled("-" + "OnlyShowAmmoGizmoWhenSelectedOneThingLabel".Translate(), ref settings.onlyShowAmmoGizmoWhenSelectedOneThing, "OnlyShowAmmoGizmoWhenSelectedOneThingDesc".Translate());
            }
            listingStandard.Gap(5);
            listingStandard.CheckboxLabeled("RandomWeaponModeForNonPlayerPawnLabel".Translate(), ref settings.randomWeaponModeForNonPlayerPawn, "RandomWeaponModeForNonPlayerPawnDesc".Translate());
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "AWL_Mod".Translate();
        }
    }
    public class AWL_Settings : ModSettings
    {
        public bool enableAmmoSystem = true;
        public bool enableBackupAmmoSystem = true;
        public bool onlyShowAmmoGizmoWhenSelectedOneThing = true;
        public bool randomWeaponModeForNonPlayerPawn = true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enableAmmoSystem, "enableAmmoSystem", true);
            Scribe_Values.Look(ref enableBackupAmmoSystem, "enableBackupAmmoSystem", true);
            Scribe_Values.Look(ref onlyShowAmmoGizmoWhenSelectedOneThing, "onlyShowAmmoGizmoWhenSelectedOneThing", true);
            Scribe_Values.Look(ref randomWeaponModeForNonPlayerPawn, "randomWeaponModeForNonPlayerPawn", true);

            base.ExposeData();
        }

    }
}
