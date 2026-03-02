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
                settings.backupAmmoMultipiler = 1f;
                settings.onlyShowAmmoGizmoWhenSelectedOneThing = true;
                settings.randomWeaponModeForNonPlayerPawn = true;
            }
            listingStandard.Gap(5);
            listingStandard.CheckboxLabeled("AWL_EnableAmmoSystem".Translate(), ref settings.enableAmmoSystem, "AWL_EnableTheAmmoSystem".Translate());
            if (settings.enableAmmoSystem)
            {
                listingStandard.Gap(5);
                listingStandard.CheckboxLabeled("-" + "AWL_EnableBackupAmmoSystemLabel".Translate(), ref settings.enableBackupAmmoSystem, "AWL_EnableBackupAmmoSystemDesc".Translate());
                if (settings.enableBackupAmmoSystem)
                {
                    string backupAmmoMultipilerBuffer = settings.backupAmmoMultipiler.ToString();
                    listingStandard.Gap(5);
                    listingStandard.Label("-" + "AWL_BackupAmmoMultipilerLabel".Translate(), -1f, "AWL_BackupAmmoMultipilerDesc".Translate());
                    listingStandard.TextFieldNumeric(ref settings.backupAmmoMultipiler, ref backupAmmoMultipilerBuffer, 0, 100);
                }
                listingStandard.Gap(5);
                listingStandard.CheckboxLabeled("-" + "AWL_OnlyShowAmmoGizmoWhenSelectedOneThingLabel".Translate(), ref settings.onlyShowAmmoGizmoWhenSelectedOneThing, "AWL_OnlyShowAmmoGizmoWhenSelectedOneThingDesc".Translate());
            }
            listingStandard.Gap(5);
            listingStandard.CheckboxLabeled("AWL_RandomWeaponModeForNonPlayerPawnLabel".Translate(), ref settings.randomWeaponModeForNonPlayerPawn, "AWL_RandomWeaponModeForNonPlayerPawnDesc".Translate());
            listingStandard.End();
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
        public float backupAmmoMultipiler = 1f;
        public bool onlyShowAmmoGizmoWhenSelectedOneThing = true;
        public bool randomWeaponModeForNonPlayerPawn = true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enableAmmoSystem, "enableAmmoSystem", true);
            Scribe_Values.Look(ref enableBackupAmmoSystem, "enableBackupAmmoSystem", true);
            Scribe_Values.Look(ref backupAmmoMultipiler, "backupAmmoMultipiler", 1f);
            Scribe_Values.Look(ref onlyShowAmmoGizmoWhenSelectedOneThing, "onlyShowAmmoGizmoWhenSelectedOneThing", true);
            Scribe_Values.Look(ref randomWeaponModeForNonPlayerPawn, "randomWeaponModeForNonPlayerPawn", true);

            base.ExposeData();
        }

    }
}
