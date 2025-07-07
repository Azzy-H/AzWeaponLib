using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace AzWeaponLib.HeavyWeapon
{
    public class HeavyWeaponDef : DefModExtension, IStatable
    {
        public ApparelGroupDef apparelGroupDef;
        public IEnumerable<StatDrawEntry> GetStatDrawEntries(StatRequest req)
        {
            List<ThingDef> availableApparels = apparelGroupDef.availableApparels;
            string Label = "AWL_ApparelGroupDefLabel".Translate();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("AWL_ApparelGroupDefText".Translate());
            sb.AppendLine(apparelGroupDef.label);
            sb.AppendLine();
            foreach (ThingDef def in availableApparels)
            {
                sb.AppendLine("-" + def.label);
            }
            string Text = sb.ToString();
            yield return new StatDrawEntry(reportText: Text, category: StatCategoryDefOf.Weapon, label: Label, valueString: "Yes".Translate(), displayPriorityWithinCategory: 9999);
        }
    }
}
