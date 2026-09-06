using AzWeaponLib;
using AzWeaponLib.AmmoSystem;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace AzWeaponLib.AmmoSystem
{
    public class CompProperties_NoMagzineAmmo : CompProperties_Ammo
    {
        private const int displayPriority = 300;
        public CompProperties_NoMagzineAmmo()
        {
            compClass = typeof(CompNoMagzineAmmo);
        }
        public override IEnumerable<StatDrawEntry> GetStatDrawEntries(StatRequest req)
        {
            CompAmmo compAmmo = null;
            Thing t = req.Thing;
            if (t != null)
            {
                compAmmo = t.TryGetComp(this) as CompAmmo;
            }
            int priority = 0;
            if (ammunitionDef != null)
            {
                yield return AmmunitionCostDisp(ref priority, compAmmo);
            }
        }
        private StatDrawEntry AmmunitionCostDisp(ref int priorityOffset, CompAmmo compAmmo = null)
        {
            priorityOffset--;
            var num = ammoCountPerAmmunitionBox;
            string Label = "AWL_AmmunitionCostLabel".Translate();
            string Text = "AWL_AmmunitionCostText".Translate();
            return new StatDrawEntry(reportText: StatDispUtility.StringBuilderInit(Text, num).ToString(), category: StatCategoryDefOf.Weapon_Melee, label: Label, valueString: ammunitionDef.label, displayPriorityWithinCategory: displayPriority - priorityOffset);
        }
    }
    public class CompNoMagzineAmmo : CompAmmo
    {
        public override int MaxAmmoNeeded => (AmmunitionCapacity - Ammo) / Props.ammoCountPerAmmunitionBox;
        public override bool NeedReloadBackupAmmo => AmmunitionCapacity >= Ammo + Props.ammoCountPerAmmunitionBox; 
        public override void ReloadByAmmoBox(Thing t)
        {
            int num = Mathf.Min(MaxAmmoNeeded, t.stackCount);
            ReloadByNum(num * Props.ammoCountPerAmmunitionBox);
            t.SplitOff(num).Destroy();
            parent.BroadcastCompSignal("AWL_Reloaded");
        }
        protected override Gizmo GetAmmoStatusGizmo()
        {
            Gizmo_NoMagzineAmmoStatus gizmo_AmmoStatus = new Gizmo_NoMagzineAmmoStatus()
            {
                gizmoLabel = Props.gizmoLabel ?? "AWL_AmmunitionGizmoLabel".Translate(),
                gizmoTip = Props.gizmoTip ?? "AWL_AmmunitionGizmoTip".Translate(),
                ammunitionCapacity = AmmunitionCapacity,
                amunitionRemained = Ammo,
                autoReload = autoReload,
                autoReloadToggle = AutoReloadToggle,
                makeReloadJob = TryMakeReloadJob,
                canAutoReloadToggleNow = ReloadingTime > 0,
                canReloadNow = CanReloadNow,
                backupAmmo = -1
            };
            return gizmo_AmmoStatus;
        }
        public override void PostPostMake()
        {
            OnAmmoReloaded += NotifyReloaded;
        }
        protected virtual void NotifyReloaded(CompAmmo compAmmo)
        {
            if (parent.HitPoints < parent.MaxHitPoints)
            {
                int num = Mathf.Min(parent.MaxHitPoints - parent.HitPoints, Ammo);
                UsedByNum(num);
                parent.HitPoints += num;
            }
        }
    }
}
