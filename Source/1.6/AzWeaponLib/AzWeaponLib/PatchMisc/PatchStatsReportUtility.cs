using AzWeaponLib.AmmoSystem;
using AzWeaponLib.MultiVerb;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace AzWeaponLib
{
    [HarmonyPatch(typeof(ThingDef))]
    internal class PatchThingDef
    {
        public static readonly HashSet<int> verbPriorities = new HashSet<int>()
        {
            StatDisplayOrder.Thing_Weapon_MeleeWarmupTime,
            StatDisplayOrder.Thing_Damage,
            StatDisplayOrder.Thing_Weapon_ArmorPenetration,
            StatDisplayOrder.Thing_Weapon_BuildingDamageFactor,
            StatDisplayOrder.Thing_WeaponBuildingDamageFactorPassable,
            StatDisplayOrder.Thing_WeaponBuildingDamageFactorImpassable,
            StatDisplayOrder.Thing_Weapon_BurstShotCount,
            StatDisplayOrder.Thing_Weapon_BurstShotFireRate,
            StatDisplayOrder.Thing_Weapon_Range,
            StatDisplayOrder.Thing_Weapon_StoppingPower,
            StatDisplayOrder.Thing_Weapon_MissRadius,
            StatDisplayOrder.Thing_Weapon_DirectHitChance
        };
        [HarmonyPatch("SpecialDisplayStats")]
        [HarmonyPostfix]
        [HarmonyAfter(new string[] { "legodude17.mvcf" })]
        //[HarmonyPriority(Priority.VeryLow)]
        //public static void Postfix_SpecialDisplayStats(ThingDef __instance, ref IEnumerable<StatDrawEntry> __result, StatRequest req)
        //{
        //    List<VerbProperties> verbProperties = __instance.Verbs;
        //    List<DefModExtension> defModExtensions = __instance.modExtensions;
        //    List<CompProperties> compProperties = __instance.comps;
        //    bool isTurret = __instance.building?.IsTurret ?? false;

        //    var resultList = new List<StatDrawEntry>(__result);
        //    if (verbProperties != null && !isTurret)
        //    {
        //        Log.Message(1);
        //        resultList.RemoveAll(x => x.category == (((__instance.category == ThingCategory.Pawn) ? StatCategoryDefOf.PawnCombat : null) ?? StatCategoryDefOf.Weapon_Ranged) && verbPriorities.Contains(x.DisplayPriorityWithinCategory));
        //        CompProperties_MultiVerb multiVerb = __instance.GetCompProperties<CompProperties_MultiVerb>();
        //        if (verbProperties.Count > 1 && multiVerb != null)
        //        {
        //            Log.Message(2);
        //            int dispPriorityOffset = 0;
        //            // char postfixChar = 'A';
        //            for (int i = 0; i < verbProperties.Count; i++)
        //            {
        //                //postfixChar++;
        //                string prefixString = multiVerb.gizmoInfos[i].defaultLabel + ": ";//("AWL_VerbDescPostFix_" + postfixChar).Translate();
        //                resultList.AddRange(GetStatDrawEntries_VerbProps(verbProperties[i], __instance.category, dispPriorityOffset, req, prefixString));
        //                dispPriorityOffset -= 10000;
        //            }
        //        }
        //        else if (verbProperties.Count == 1)
        //        {
        //            Log.Message(3);
        //            resultList.AddRange(GetStatDrawEntries_VerbProps(verbProperties.First(), __instance.category, 0, req));
        //        }
        //    }
        //    if (defModExtensions != null)
        //    {
        //        foreach (DefModExtension ext in defModExtensions)
        //        {
        //            if (ext is IStatable statable)
        //            {
        //                resultList.AddRange(statable.GetStatDrawEntries(req));
        //            }
        //        }
        //    }
        //    //if (compProperties != null && !req.HasThing)
        //    //{
        //    //    foreach (CompProperties compProp in compProperties)
        //    //    {
        //    //        if (compProp is IStatable statable)
        //    //        {
        //    //            resultList.AddRange(statable.GetStatDrawEntries(req.Thing));
        //    //        }
        //    //    }
        //    //}
        //    __result = resultList;
        //    return;
        //}
        public static IEnumerable<StatDrawEntry> Postfix_SpecialDisplayStats(IEnumerable<StatDrawEntry> entries, ThingDef __instance, StatRequest req)
        {
            List<VerbProperties> verbProperties = __instance.Verbs;
            List<DefModExtension> defModExtensions = __instance.modExtensions;
            List<CompProperties> compProperties = __instance.comps;
            bool isTurret = __instance.building?.IsTurret ?? false;

            var resultList = new List<StatDrawEntry>(entries);
            if (verbProperties != null && !isTurret)
            {
                resultList.RemoveAll(x => x.category == (((__instance.category == ThingCategory.Pawn) ? StatCategoryDefOf.PawnCombat : null) ?? StatCategoryDefOf.Weapon_Ranged) && verbPriorities.Contains(x.DisplayPriorityWithinCategory));
                CompProperties_MultiVerb multiVerb = __instance.GetCompProperties<CompProperties_MultiVerb>();
                if (verbProperties.Count > 1 && multiVerb != null)
                {
                    int dispPriorityOffset = 0;
                    for (int i = 0; i < verbProperties.Count; i++)
                    {
                        string prefixString = multiVerb.gizmoInfos[i].defaultLabel + ": ";
                        resultList.AddRange(GetStatDrawEntries_VerbProps(verbProperties[i], __instance.category, dispPriorityOffset, req, prefixString));
                        dispPriorityOffset -= 10000;
                    }
                }
                else if (verbProperties.Count == 1)
                {
                    resultList.AddRange(GetStatDrawEntries_VerbProps(verbProperties.First(), __instance.category, 0, req));
                }
            }
            if (defModExtensions != null)
            {
                foreach (DefModExtension ext in defModExtensions)
                {
                    if (ext is IStatable statable)
                    {
                        resultList.AddRange(statable.GetStatDrawEntries(req));
                    }
                }
            }
            return resultList;
        }
        public static IEnumerable<StatDrawEntry> GetStatDrawEntries_VerbProps(VerbProperties verb, ThingCategory category, int dispPriorityOffset, StatRequest req, string prefix = "", string postfix = "")
        {
            StatCategoryDef verbStatCategory = ((category == ThingCategory.Pawn) ? StatCategoryDefOf.PawnCombat : null);
            float warmupTime = verb.warmupTime;
            StringBuilder stringBuilder = new StringBuilder("Stat_Thing_Weapon_RangedWarmupTime_Desc".Translate());
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("StatsReport_BaseValue".Translate() + ": " + warmupTime.ToString("0.##") + " " + "LetterSecond".Translate());
            if (warmupTime > 0f)
            {
                if (req.HasThing)
                {
                    float statValue = req.Thing.GetStatValue(StatDefOf.RangedWeapon_WarmupMultiplier);
                    warmupTime *= statValue;
                    if (!Mathf.Approximately(statValue, 1f))
                    {
                        stringBuilder.AppendLine();
                        stringBuilder.AppendLine("Stat_Thing_Weapon_WarmupTime_Multiplier".Translate() + ": x" + statValue.ToStringPercent());
                        stringBuilder.Append(StatUtility.GetOffsetsAndFactorsFor(StatDefOf.RangedWeapon_WarmupMultiplier, req.Thing));
                    }
                }
                stringBuilder.AppendLine();
                stringBuilder.AppendLine("StatsReport_FinalValue".Translate() + ": " + warmupTime.ToString("0.##") + " " + "LetterSecond".Translate());
                yield return new StatDrawEntry(verbStatCategory ?? StatCategoryDefOf.Weapon_Ranged, prefix + "RangedWarmupTime".Translate() + postfix, warmupTime.ToString("0.##") + " " + "LetterSecond".Translate(), stringBuilder.ToString(), StatDisplayOrder.Thing_Weapon_MeleeWarmupTime + dispPriorityOffset);
            }
            if (verb.defaultProjectile?.projectile.damageDef != null && verb.defaultProjectile.projectile.damageDef.harmsHealth)
            {
                StatCategoryDef statCat = verbStatCategory ?? StatCategoryDefOf.Weapon_Ranged;
                StringBuilder stringBuilder2 = new StringBuilder();
                stringBuilder2.AppendLine("Stat_Thing_Damage_Desc".Translate());
                stringBuilder2.AppendLine();
                float num3 = verb.defaultProjectile.projectile.GetDamageAmount(req.Thing, stringBuilder2);
                yield return new StatDrawEntry(statCat, prefix + "Damage".Translate() + postfix, num3.ToString(), stringBuilder2.ToString(), StatDisplayOrder.Thing_Damage + dispPriorityOffset);
                if (verb.defaultProjectile.projectile.damageDef.armorCategory != null)
                {
                    StringBuilder stringBuilder3 = new StringBuilder();
                    float armorPenetration = verb.defaultProjectile.projectile.GetArmorPenetration(req.Thing, stringBuilder3);
                    TaggedString taggedString = "ArmorPenetrationExplanation".Translate();
                    if (stringBuilder3.Length != 0)
                    {
                        taggedString += "\n\n" + stringBuilder3;
                    }
                    yield return new StatDrawEntry(statCat, prefix + "ArmorPenetration".Translate() + postfix, armorPenetration.ToStringPercent(), taggedString, StatDisplayOrder.Thing_Weapon_ArmorPenetration + dispPriorityOffset);
                }
                float buildingDamageFactor = verb.defaultProjectile.projectile.damageDef.buildingDamageFactor;
                float dmgBuildingsImpassable = verb.defaultProjectile.projectile.damageDef.buildingDamageFactorImpassable;
                float dmgBuildingsPassable = verb.defaultProjectile.projectile.damageDef.buildingDamageFactorPassable;
                if (buildingDamageFactor != 1f)
                {
                    yield return new StatDrawEntry(statCat, prefix + "BuildingDamageFactor".Translate() + postfix, buildingDamageFactor.ToStringPercent(), "BuildingDamageFactorExplanation".Translate(), StatDisplayOrder.Thing_Weapon_BuildingDamageFactor + dispPriorityOffset);
                }
                if (dmgBuildingsImpassable != 1f)
                {
                    yield return new StatDrawEntry(statCat, prefix + "BuildingDamageFactorImpassable".Translate() + postfix, dmgBuildingsImpassable.ToStringPercent(), "BuildingDamageFactorImpassableExplanation".Translate(), StatDisplayOrder.Thing_WeaponBuildingDamageFactorImpassable + dispPriorityOffset);
                }
                if (dmgBuildingsPassable != 1f)
                {
                    yield return new StatDrawEntry(statCat, prefix + "BuildingDamageFactorPassable".Translate() + postfix, dmgBuildingsPassable.ToStringPercent(), "BuildingDamageFactorPassableExplanation".Translate(), StatDisplayOrder.Thing_WeaponBuildingDamageFactorPassable + dispPriorityOffset);
                }
                //投射物数据
                foreach(var v in GetExtraProjectileStatDrawEntries(verb, category, dispPriorityOffset, req, prefix: prefix, postfix: postfix, statCat: verbStatCategory)) yield return v;
            }
            if (verb.defaultProjectile == null && verb.beamDamageDef != null)
            {
                yield return new StatDrawEntry(verbStatCategory ?? StatCategoryDefOf.Weapon_Ranged, prefix + "ArmorPenetration".Translate() + postfix, verb.beamDamageDef.defaultArmorPenetration.ToStringPercent(), "ArmorPenetrationExplanation".Translate(), StatDisplayOrder.Thing_Weapon_ArmorPenetration + dispPriorityOffset);
            }
            if (verb.Ranged)
            {
                float num4 = verb.burstShotCount;
                float num5 = verb.ticksBetweenBurstShots;
                float dmgBuildingsPassable = (verb?.defaultProjectile?.projectile?.stoppingPower).GetValueOrDefault();
                StringBuilder stringBuilder4 = new StringBuilder("Stat_Thing_Weapon_BurstShotFireRate_Desc".Translate());
                stringBuilder4.AppendLine();
                stringBuilder4.AppendLine();
                stringBuilder4.AppendLine("StatsReport_BaseValue".Translate() + ": " + verb.burstShotCount.ToString());
                stringBuilder4.AppendLine();
                StringBuilder ticksBetweenBurstShotsExplanation = new StringBuilder("Stat_Thing_Weapon_BurstShotFireRate_Desc".Translate());
                ticksBetweenBurstShotsExplanation.AppendLine();
                ticksBetweenBurstShotsExplanation.AppendLine();
                ticksBetweenBurstShotsExplanation.AppendLine("StatsReport_BaseValue".Translate() + ": " + (60f / verb.ticksBetweenBurstShots.TicksToSeconds()).ToString("0.##") + " rpm");
                ticksBetweenBurstShotsExplanation.AppendLine();
                StringBuilder stoppingPowerExplanation = new StringBuilder("StoppingPowerExplanation".Translate());
                stoppingPowerExplanation.AppendLine();
                stoppingPowerExplanation.AppendLine();
                stoppingPowerExplanation.AppendLine("StatsReport_BaseValue".Translate() + ": " + dmgBuildingsPassable.ToString("F1"));
                stoppingPowerExplanation.AppendLine();
                if (req.HasThing && req.Thing.TryGetComp(out CompUniqueWeapon comp))
                {
                    bool flag = false;
                    bool flag2 = false;
                    bool flag3 = false;
                    foreach (WeaponTraitDef item2 in comp.TraitsListForReading)
                    {
                        if (!Mathf.Approximately(item2.burstShotCountMultiplier, 1f))
                        {
                            if (!flag)
                            {
                                stringBuilder4.AppendLine("StatsReport_WeaponTraits".Translate() + ":");
                                flag = true;
                            }
                            num4 *= item2.burstShotCountMultiplier;
                            stringBuilder4.AppendLine("    " + item2.LabelCap + ": " + item2.burstShotCountMultiplier.ToStringByStyle(ToStringStyle.PercentOne, ToStringNumberSense.Factor));
                        }
                        if (!Mathf.Approximately(item2.burstShotSpeedMultiplier, 1f))
                        {
                            if (!flag2)
                            {
                                ticksBetweenBurstShotsExplanation.AppendLine("StatsReport_WeaponTraits".Translate() + ":");
                                flag2 = true;
                            }
                            num5 /= item2.burstShotSpeedMultiplier;
                            ticksBetweenBurstShotsExplanation.AppendLine("    " + item2.LabelCap + ": " + item2.burstShotSpeedMultiplier.ToStringByStyle(ToStringStyle.PercentOne, ToStringNumberSense.Factor));
                        }
                        if (!Mathf.Approximately(item2.additionalStoppingPower, 0f))
                        {
                            if (!flag3)
                            {
                                stoppingPowerExplanation.AppendLine("StatsReport_WeaponTraits".Translate() + ":");
                                flag3 = true;
                            }
                            dmgBuildingsPassable += item2.additionalStoppingPower;
                            stoppingPowerExplanation.AppendLine("    " + item2.LabelCap + ": " + item2.additionalStoppingPower.ToStringByStyle(ToStringStyle.FloatOne, ToStringNumberSense.Offset));
                        }
                    }
                }
                stringBuilder4.AppendLine();
                stringBuilder4.AppendLine("StatsReport_FinalValue".Translate() + ": " + Mathf.CeilToInt(num4).ToString());
                float dmgBuildingsImpassable = 60f / ((int)num5).TicksToSeconds();
                ticksBetweenBurstShotsExplanation.AppendLine();
                ticksBetweenBurstShotsExplanation.AppendLine("StatsReport_FinalValue".Translate() + ": " + dmgBuildingsImpassable.ToString("0.##") + " rpm");
                stoppingPowerExplanation.AppendLine();
                stoppingPowerExplanation.AppendLine("StatsReport_FinalValue".Translate() + ": " + dmgBuildingsPassable.ToString("F1"));
                StatCategoryDef statCat = verbStatCategory ?? StatCategoryDefOf.Weapon_Ranged;
                if (verb.showBurstShotStats && verb.burstShotCount > 1)
                {
                    yield return new StatDrawEntry(statCat, prefix + "BurstShotCount".Translate() + postfix, Mathf.CeilToInt(num4).ToString(), stringBuilder4.ToString(), StatDisplayOrder.Thing_Weapon_BurstShotCount + dispPriorityOffset);
                    yield return new StatDrawEntry(statCat, prefix + "BurstShotFireRate".Translate() + postfix, dmgBuildingsImpassable.ToString("0.##") + " rpm", ticksBetweenBurstShotsExplanation.ToString(), StatDisplayOrder.Thing_Weapon_BurstShotFireRate + dispPriorityOffset);
                }
                if (dmgBuildingsPassable > 0f)
                {
                    yield return new StatDrawEntry(statCat, prefix + "StoppingPower".Translate() + postfix, dmgBuildingsPassable.ToString("F1"), stoppingPowerExplanation.ToString(), StatDisplayOrder.Thing_Weapon_StoppingPower + dispPriorityOffset);
                }
                float num6 = verb.range;
                StringBuilder stringBuilder5 = new StringBuilder("Stat_Thing_Weapon_Range_Desc".Translate());
                stringBuilder5.AppendLine();
                stringBuilder5.AppendLine();
                stringBuilder5.AppendLine("StatsReport_BaseValue".Translate() + ": " + num6.ToString("F0"));
                if (req.HasThing)
                {
                    float statValue2 = req.Thing.GetStatValue(StatDefOf.RangedWeapon_RangeMultiplier);
                    num6 *= statValue2;
                    if (!Mathf.Approximately(statValue2, 1f))
                    {
                        stringBuilder5.AppendLine();
                        stringBuilder5.AppendLine("Stat_Thing_Weapon_Range_Multiplier".Translate() + ": x" + statValue2.ToStringPercent());
                        stringBuilder5.Append(StatUtility.GetOffsetsAndFactorsFor(StatDefOf.RangedWeapon_RangeMultiplier, req.Thing));
                    }
                    Map obj = req.Thing.Map ?? req.Thing.MapHeld;
                    if (obj != null && obj.weatherManager.CurWeatherMaxRangeCap >= 0f)
                    {
                        WeatherManager weatherManager = (req.Thing.Map ?? req.Thing.MapHeld).weatherManager;
                        bool num7 = num6 > weatherManager.CurWeatherMaxRangeCap;
                        float num8 = num6;
                        num6 = Mathf.Min(num6, weatherManager.CurWeatherMaxRangeCap);
                        if (num7)
                        {
                            stringBuilder5.AppendLine();
                            stringBuilder5.AppendLine("    " + "Stat_Thing_Weapon_Range_Clamped".Translate(num6.ToString("F0").Named("CAP"), num8.ToString("F0").Named("ORIGINAL")));
                        }
                    }
                }
                stringBuilder5.AppendLine();
                stringBuilder5.AppendLine("StatsReport_FinalValue".Translate() + ": " + num6.ToString("F0"));
                yield return new StatDrawEntry(statCat, prefix + "Range".Translate() + postfix, num6.ToString("F0"), stringBuilder5.ToString(), StatDisplayOrder.Thing_Weapon_Range + dispPriorityOffset);
                //额外verb数据
                foreach (var v in GetVerbExtraStatDrawEntries(verb, dispPriorityOffset, req, prefix: prefix, postfix: postfix)) yield return v;
            }
            if (verb.ForcedMissRadius > 0f)
            {
                StatCategoryDef statCat = verbStatCategory ?? StatCategoryDefOf.Weapon_Ranged;
                yield return new StatDrawEntry(statCat, prefix + "MissRadius".Translate() + postfix, verb.ForcedMissRadius.ToString("0.#"), "Stat_Thing_Weapon_MissRadius_Desc".Translate(), StatDisplayOrder.Thing_Weapon_MissRadius + dispPriorityOffset);
                yield return new StatDrawEntry(statCat, prefix + "DirectHitChance".Translate() + postfix, (1f / (float)GenRadial.NumCellsInRadius(verb.ForcedMissRadius)).ToStringPercent(), "Stat_Thing_Weapon_DirectHitChance_Desc".Translate(), StatDisplayOrder.Thing_Weapon_DirectHitChance + dispPriorityOffset);
            }
        }
        private static IEnumerable<StatDrawEntry> GetExtraProjectileStatDrawEntries(VerbProperties verb, ThingCategory category, int dispPriorityOffset, StatRequest req, string prefix = "", string postfix = "", StatCategoryDef statCat = null)
        {
            //贯通弹信息
            if (verb.defaultProjectile.modExtensions != null)
            {
                foreach (DefModExtension defModExtension in verb.defaultProjectile.modExtensions)
                {
                    if (defModExtension is IStatable statable1)
                    {
                        foreach (var s in statable1.GetStatDrawEntries(req))
                        {
                            string labelInt = (string)NonPublicFields.StatDrawEntry_labelInt.GetValue(s);
                            NonPublicFields.StatDrawEntry_labelInt.SetValue(s, prefix + labelInt + postfix);
                            NonPublicFields.StatDrawEntry_displayOrderWithinCategory.SetValue(s, s.DisplayPriorityWithinCategory + dispPriorityOffset);
                            yield return s;
                        }
                    }
                }
            }
            if (verb.defaultProjectile.projectile is IStatable statable)
            {
                foreach (var v in statable.GetStatDrawEntries(req)) { yield return v; }
            }
            if (verb.defaultProjectile.projectile.extraDamages != null)
            {
                string Label = "AWL_ExtraDamageLabel".Translate();
                string Text = "AWL_ExtraDamageText".Translate();
                StringBuilder resultStringBuilder = new StringBuilder(Text);
                resultStringBuilder.AppendLine();
                float num = 0f;
                foreach (ExtraDamage extraDamage in verb.defaultProjectile.projectile.extraDamages)
                {
                    num += extraDamage.amount * extraDamage.chance;
                    resultStringBuilder.AppendInNewLine(extraDamage.def.label);
                    resultStringBuilder.AppendInNewLine("  " + "AWL_ExtraDamageAmount".Translate() + ": " + extraDamage.amount.ToString("F0"));
                    resultStringBuilder.AppendInNewLine("  " + "AWL_ExtraDamagePenetration".Translate() + ": " + extraDamage.AdjustedArmorPenetration().ToStringPercent());
                    if (extraDamage.chance < 1f)
                    {
                        resultStringBuilder.AppendInNewLine("  " + "AWL_ExtraDamageChance".Translate() + ": " + extraDamage.chance.ToStringPercent());
                    }
                    else
                    {
                        resultStringBuilder.AppendInNewLine("  " + "AWL_ExtraDamageChance_Must".Translate());
                    }
                }
                yield return new StatDrawEntry(statCat ?? StatCategoryDefOf.Weapon_Ranged, prefix + Label + postfix, num.ToString("F2"), resultStringBuilder.ToString(), StatDisplayOrder.Thing_Weapon_ArmorPenetration - 1 + dispPriorityOffset);
            }
        }
        private static IEnumerable<StatDrawEntry> GetVerbExtraStatDrawEntries(VerbProperties verb, int dispPriorityOffset, StatRequest req, string prefix = "", string postfix = "")
        {
            if (verb is IStatable statable)
            {
                foreach (var s in statable.GetStatDrawEntries(req))
                {
                    string labelInt = (string)NonPublicFields.StatDrawEntry_labelInt.GetValue(s);
                    NonPublicFields.StatDrawEntry_labelInt.SetValue(s, prefix + labelInt + postfix);
                    NonPublicFields.StatDrawEntry_displayOrderWithinCategory.SetValue(s, s.DisplayPriorityWithinCategory + dispPriorityOffset);
                    yield return s;
                }
            }
            //霰弹info
            
        }
    }
}
