using AzWeaponLib.AmmoSystem;
using Verse;

namespace AzWeaponLib.MultiVerb
{
    public class VerbSwitchWorker_NoBackupAmmo : VerbSwitchWorker
    {
        private CompAmmo compAmmo;
        public CompAmmo CompAmmo
        {
            get
            {
                if (compAmmo == null) compAmmo = weapon.TryGetComp<CompAmmo>();
                return compAmmo;
            }
        }
        public override void Notify_PawnUsedVerb(Verb verb, LocalTargetInfo target)
        {
            if (verb is Verb_ShootWithAmmo v && !v.canShootNow && !CompAmmo.canReloadNow)
            {
                CompMultiVerb.SetNextVerbIndex();
            }
        }
    }
}
