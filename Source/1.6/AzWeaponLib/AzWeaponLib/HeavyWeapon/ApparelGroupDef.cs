using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace AzWeaponLib.HeavyWeapon
{
    public class ApparelGroupDef : Def
    {
        public List<ThingDef> availableApparels;
        public override IEnumerable<string> ConfigErrors()
        {
            if (availableApparels == null || availableApparels.Empty()) 
            {
                yield return defName + " has no availableApparels.";
            }
        }
    }
}
