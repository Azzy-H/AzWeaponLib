using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace AzWeaponLib.AmmoSystem
{
    public class StatWorker_ReloadTime : StatWorker
    {
        public static Pawn GetCurrentWeaponUser(Thing weapon)
        {
            if (weapon == null)
            {
                return null;
            }
            if (weapon.ParentHolder is Pawn_EquipmentTracker pawn_EquipmentTracker)
            {
                return pawn_EquipmentTracker.pawn;
            }
            if (weapon.ParentHolder is Pawn_ApparelTracker pawn_ApparelTracker)
            {
                return pawn_ApparelTracker.pawn;
            }
            return null;
        }
        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            ThingDef thingDef = req.Def as ThingDef;
            if (thingDef == null)
            {
                return 0f;
            }
            if (req.Thing != null)
            {
                return base.GetValueUnfinalized(req, applyPostProcess) * AmmoUtility.GetReloadMultipiler(GetCurrentWeaponUser(req.Thing), req.Thing as ThingWithComps);
            }
            return base.GetValueUnfinalized(req, applyPostProcess);
        }
        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            StringBuilder stringBuilder = new StringBuilder();
            float baseValueFor = GetBaseValueFor(req);
            if (baseValueFor != 0f || stat.showZeroBaseValue)
            {
                stringBuilder.AppendLine("StatsReport_BaseValue".Translate() + ": " + stat.ValueToString(baseValueFor, numberSense));
            }
            GetOffsetsAndFactorsExplanation(req, stringBuilder, baseValueFor);
            if (req.Thing != null && (req.Thing.def.GetCompProperties<CompProperties_Ammo>()?.pawnStatsAffectReloading ?? false))
            {
                var dic = AmmoUtility.GetReloadMultipilerFactors(GetCurrentWeaponUser(req.Thing), req.Thing as ThingWithComps);
                foreach (string s in dic.Keys)
                {
                    stringBuilder.Append("--");
                    stringBuilder.AppendLine(s.Translate() + ": " + "*" + dic[s].ToString("F2"));
                    stringBuilder.AppendLine((s + "_Tips").Translate());
                    stringBuilder.AppendLine();
                }
            }
            return stringBuilder.ToString();
        }
    }
}
