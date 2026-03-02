using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace AzWeaponLib.MultiVerb
{
    public class VerbSwitchWorker
    {
        public ThingWithComps weapon;
        private CompMultiVerb compMultiVerb;
        public CompMultiVerb CompMultiVerb
        {
            get 
            {
                if(compMultiVerb == null) compMultiVerb = weapon.TryGetComp<CompMultiVerb>();
                return compMultiVerb;
            }
        }
        public virtual float GetPriority(Pawn pawn)
        {
            return -1f;
        }
        public virtual void Notify_PawnUsedVerb(Verb verb, LocalTargetInfo target)
        {
        }
    }
}
