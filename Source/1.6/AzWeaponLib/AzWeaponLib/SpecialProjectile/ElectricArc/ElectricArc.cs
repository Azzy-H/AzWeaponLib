using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Noise;
using Verse.Sound;
using static RimWorld.ColonistBar;

namespace AzWeaponLib.SpecialProjectile
{
    public class ElectricArc : Bullet
    {
        private ElectricArcDef electricArcDefDefInt;
        public ElectricArcDef electricArcDef
        {
            get
            {
                if (electricArcDefDefInt == null) electricArcDefDefInt = def.GetModExtension<ElectricArcDef>();
                return electricArcDefDefInt;
            }
        }
        private Sustainer ambientSustainer;
        private List<ThingComp> comps;
        protected Vector3 exactPositionInt;
        public override Vector3 ExactPosition
        {
            get { return exactPositionInt; }
        }
        public int conductTickLeft;
        protected bool extra;
        protected List<Thing> victims = new List<Thing>();
        protected float damageMultiplierByConduct = 1f;
        protected virtual float ArcDamageMultiplierOverall => damageMultiplierByConduct;
        public override int DamageAmount => GenMath.RoundRandom(base.DamageAmount * ArcDamageMultiplierOverall);
        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
        {
            victims.Add(launcher);
            base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);
            if (electricArcDef.noDeviation)
            {
                exactPositionInt = intendedTarget.CenterVector3.Yto0() + Vector3.up * def.Altitude;
                Position = intendedTarget.Cell;
            }
            else
            {
                exactPositionInt = usedTarget.CenterVector3.Yto0() + Vector3.up * def.Altitude;
                Position = usedTarget.Cell;
            }
            ReflectInit();
            DrawFleckBetween(origin, exactPositionInt);
            TryConduct();
        }
        protected void ReflectInit()
        {
            if (!def.projectile.soundAmbient.NullOrUndefined())
            {
                ambientSustainer = (Sustainer)(NonPublicFields.Projectile_AmbientSustainer.GetValue(this));
            }
            comps = (List<ThingComp>)(NonPublicFields.ThingWithComps_comps.GetValue(this));
        }
        protected override void Tick()
        {
            ThingWithCompsTick();
            TryConduct();
            ambientSustainer?.Maintain();
        }
        protected override void TickInterval(int delta)
        {
            ThingWithCompsTickInterval(delta);
        }
        private void ThingWithCompsTick()
        {
            if (comps != null)
            {
                int i = 0;
                for (int count = comps.Count; i < count; i++)
                {
                    comps[i].CompTick();
                }
            }
        }
        private void ThingWithCompsTickInterval(int delta)
        {
            if (comps != null)
            {
                int i = 0;
                for (int count = comps.Count; i < count; i++)
                {
                    comps[i].CompTickInterval(delta);
                }
            }
        }
        //protected virtual void TryConduct(int delta = 1)
        //{
        //    conductTickLeft -= delta;
        //    while (conductTickLeft <= 0)
        //    {
        //        conductTickLeft += electricArcDef.conductTicks;
        //        float conductRange;
        //        if (extra)
        //        {
        //            conductRange = electricArcDef.conductRangeExtra;
        //        }
        //        else
        //        {
        //            conductRange = electricArcDef.conductRange;
        //        }
        //        int num = GenRadial.NumCellsInRadius(conductRange);
        //        for (int i = 0; i < num; i++)
        //        {
        //            IntVec3 c = Position + GenRadial.RadialPattern[i];
        //            if (!c.InBounds(Map) || !GenSight.LineOfSight(Position, c, Map))
        //            {
        //                continue;
        //            }
        //            //Map.debugDrawer.FlashCell(c, 0.1f);
        //            List<Thing> thingList = c.GetThingList(Map);
        //            for (int j = 0; j < thingList.Count; j++)
        //            {
        //                if ((thingList[j] is Pawn && (thingList[j].Faction != launcher.Faction || thingList[j].Faction != null || Rand.Chance(Find.Storyteller.difficulty.friendlyFireChanceFactor) || i == 0)) || 
        //                    (thingList[j] is Building && (thingList[j].HostileTo(this) || i == 0)))
        //                {
        //                    if (victim.Contains(thingList[j]))
        //                    {
        //                        continue;
        //                    }
        //                    //victimTemp.Add(thingList[j]);
        //                    //Map.debugDrawer.FlashCell(c, 0.5f);
        //                    DrawFleckBetween(Position.ToVector3(), thingList[j].Position.ToVector3());
        //                    Impact(thingList[j]);
        //                    return;
        //                }
        //            }
        //        }
        //        Destroy();
        //        return;
        //    }
        //}
        protected virtual void TryConduct()
        {
            conductTickLeft--;
            if (conductTickLeft <= 0)
            {
                conductTickLeft = electricArcDef.conductTicks;
                float conductRange;
                if (extra)
                {
                    conductRange = electricArcDef.conductRangeExtra;
                }
                else
                {
                    conductRange = electricArcDef.conductRange;
                }
                int num = GenRadial.NumCellsInRadius(conductRange);
                for (int i = 0; i < num; i++)
                {
                    IntVec3 c = Position + GenRadial.RadialPattern[i];
                    if (!c.InBounds(Map) || !GenSight.LineOfSight(Position, c, Map))
                    {
                        continue;
                    }
                    //Map.debugDrawer.FlashCell(c, 0.1f);
                    List<Thing> thingList = c.GetThingList(Map);
                    for (int j = 0; j < thingList.Count; j++)
                    {

                        if ((thingList[j] is Pawn pawn && (launcher == null || pawn.Faction == null || launcher.Faction == null || pawn.Faction.HostileTo(launcher.Faction) || (!preventFriendlyFire && !Rand.Chance(Find.Storyteller.difficulty.friendlyFireChanceFactor)) || i == 0)) ||
                            (thingList[j] is Building && (thingList[j].HostileTo(this) || i == 0)))
                        {
                            if (victims.Contains(thingList[j]))
                            {
                                continue;
                            }
                            //victimTemp.Add(thingList[j]);
                            //Map.debugDrawer.FlashCell(c, 0.5f);
                            DrawFleckBetween(Position.ToVector3(), thingList[j].Position.ToVector3());
                            Impact(thingList[j]);
                            return;
                        }
                    }
                }
                Destroy();
                return;
            }
        }
        protected void DrawFleckBetween(Vector3 start, Vector3 end)
        {
            if (electricArcDef.fleckDef != null && (start - end).sqrMagnitude > 0.01)
            {
                FleckMaker.ConnectingLine(start, end, electricArcDef.fleckDef, Map);
            }
        }
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = base.Map;
            IntVec3 position = base.Position;
            BattleLogEntry_RangedImpact battleLogEntry_RangedImpact = new BattleLogEntry_RangedImpact(launcher, hitThing, intendedTarget.Thing, equipmentDef, def, targetCoverDef);
            Find.BattleLog.Add(battleLogEntry_RangedImpact);
            NotifyImpact(hitThing, map, position);
            if (hitThing != null)
            {
                extra = electricArcDef.ShouldExtra(hitThing, map);
                Position = hitThing.Position;
                exactPositionInt = Position.ToVector3();
                victims.Add(hitThing);
                damageMultiplierByConduct *= electricArcDef.damageMultiplierPerConduct;
                bool instigatorGuilty = !(launcher is Pawn pawn) || !pawn.Drafted;
                DamageInfo dinfo;
                float conductChance;
                int maxConductNum;
                if (extra)
                {
                    dinfo = new DamageInfo(def.projectile.damageDef, DamageAmount * electricArcDef.damageMultiplierExtra, ArmorPenetration * electricArcDef.penetrationMultiplierExtra, ExactRotation.eulerAngles.y, launcher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget.Thing, instigatorGuilty);
                    conductChance = electricArcDef.conductChanceExtra;
                    maxConductNum = electricArcDef.maxConductNumExtra;
                }
                else
                {
                    dinfo = new DamageInfo(def.projectile.damageDef, DamageAmount, ArmorPenetration, ExactRotation.eulerAngles.y, launcher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget.Thing, instigatorGuilty);
                    conductChance = electricArcDef.conductChance;
                    maxConductNum = electricArcDef.maxConductNum;
                }
                dinfo.SetWeaponQuality(equipmentQuality);
                hitThing.TakeDamage(dinfo).AssociateWithLog(battleLogEntry_RangedImpact);
                Pawn pawn2 = hitThing as Pawn;
                pawn2?.stances?.stagger.Notify_BulletImpact(this);
                if (def.projectile.extraDamages != null)
                {
                    foreach (ExtraDamage extraDamage in def.projectile.extraDamages)
                    {
                        if (Rand.Chance(extraDamage.chance))
                        {
                            DamageInfo dinfo2 = new DamageInfo(extraDamage.def, extraDamage.amount, extraDamage.AdjustedArmorPenetration(), ExactRotation.eulerAngles.y, launcher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget.Thing, instigatorGuilty);
                            hitThing.TakeDamage(dinfo2).AssociateWithLog(battleLogEntry_RangedImpact);
                        }
                    }
                }
                if (Rand.Chance(def.projectile.explosionChanceToStartFire) && (pawn2 == null || Rand.Chance(FireUtility.ChanceToAttachFireFromEvent(pawn2))))
                {
                    hitThing.TryAttachFire(1, launcher);
                }
                if (!Rand.Chance(conductChance) || (maxConductNum > 0 && victims.Count >= maxConductNum)) { Destroy(); }
                return;
            }
            if (Rand.Chance(def.projectile.explosionChanceToStartFire))
            {
                FireUtility.TryStartFireIn(base.Position, map, 1, launcher);
            }
            def.projectile.soundImpactAnticipate.PlayOneShot(this);
        }
        private void NotifyImpact(Thing hitThing, Map map, IntVec3 position)
        {
            BulletImpactData bulletImpactData = default(BulletImpactData);
            bulletImpactData.bullet = this;
            bulletImpactData.hitThing = hitThing;
            bulletImpactData.impactPosition = position;
            BulletImpactData impactData = bulletImpactData;
            hitThing?.Notify_BulletImpactNearby(impactData);
            int num = 9;
            for (int i = 0; i < num; i++)
            {
                IntVec3 c = position + GenRadial.RadialPattern[i];
                if (!c.InBounds(map))
                {
                    continue;
                }
                List<Thing> thingList = c.GetThingList(map);
                for (int j = 0; j < thingList.Count; j++)
                {
                    if (thingList[j] != hitThing)
                    {
                        thingList[j].Notify_BulletImpactNearby(impactData);
                    }
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref exactPositionInt, "exactPosition");
            Scribe_Values.Look(ref extra, "extra");
            Scribe_Values.Look(ref damageMultiplierByConduct, "damageMultiplierByConduct");
            Scribe_Collections.Look(ref victims, "victim");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                ReflectInit();
            }
        }
    }
}
