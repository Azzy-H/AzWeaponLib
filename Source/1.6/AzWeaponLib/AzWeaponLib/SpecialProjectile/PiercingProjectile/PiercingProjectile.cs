using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace AzWeaponLib.SpecialProjectile
{
    public class PiercingProjectileDef : DefModExtension, IStatable
    {
        public int penetratingPower = 255;
        public bool reachMaxRangeAlways;
        public float? rangeOverride = null;
        public float minDistanceToAffectAlly = 3.9f;
        public float minDistanceToAffectAny = 1.1f;
        public int penetratingPowerCostByShield = 255;
        public bool alwaysHitPawnInCell = true;
        private static StatCategoryDef statCategoryDef;
        private const int displayPriority = 5300;

        public virtual IEnumerable<StatDrawEntry> GetStatDrawEntries(object i = null)
        {
            int priority = 0;
            if (statCategoryDef == null) statCategoryDef = StatCategoryDefOf.Weapon_Ranged;
            yield return PenetratingPowerDisp(ref priority);
            yield return ShieldPenetrationDisp(ref priority);
        }
        private StatDrawEntry PenetratingPowerDisp(ref int priorityOffset)
        {
            priorityOffset--;
            var num = penetratingPower;
            string Label = "AWL_PenetratingPowerLabel".Translate();
            string Text = "AWL_PenetratingPowerText".Translate();
            return new StatDrawEntry(reportText: StatDispUtility.StringBuilderInit(Text, num).ToString(), category: statCategoryDef, label: Label, valueString: num.ToString(), displayPriorityWithinCategory: displayPriority + priorityOffset);
        }
        private StatDrawEntry ShieldPenetrationDisp(ref int priorityOffset)
        {
            priorityOffset--;
            var num = penetratingPower > penetratingPowerCostByShield;
            string Label = "AWL_ShieldPenetrationLabel".Translate();
            string Text = "AWL_ShieldPenetrationText".Translate();
            StringBuilder resultStringBuilder = new StringBuilder(Text);
            resultStringBuilder.AppendLine();
            resultStringBuilder.AppendLine();
            resultStringBuilder.AppendLine("AWL_PenetratingPowerCostByShield".Translate() + ": " + penetratingPowerCostByShield.ToString());
            resultStringBuilder.AppendLine();
            return new StatDrawEntry(reportText: resultStringBuilder.ToString(), category: statCategoryDef, label: Label, valueString: num ? "Yes".Translate() : "No".Translate(), displayPriorityWithinCategory: displayPriority + priorityOffset);
        }

    }
    public class PiercingProjectile : Projectile
    {
        private PiercingProjectileDef piercingProjectileDefInt;
        public PiercingProjectileDef piercingProjectileDef
        { 
            get
            {
                if(piercingProjectileDefInt == null) piercingProjectileDefInt = def.GetModExtension<PiercingProjectileDef>();
                return piercingProjectileDefInt;
            }
        }
        private int penetratingPowerLeft;
        public int PenetratingPowerLeft => penetratingPowerLeft;
        private Vector3 prevPosition;
        private HashSet<Thing> hitHashSet = new HashSet<Thing>();
        //private List<Thing> hitList;
        private Vector3 startPosition;
        public override bool AnimalsFleeImpact => true;
        public static readonly HashSet<AltitudeLayer> altitudeLayersBlackList = new HashSet<AltitudeLayer>
    {
        AltitudeLayer.Item,
        AltitudeLayer.ItemImportant,
        AltitudeLayer.Conduits,
        AltitudeLayer.Floor,
        AltitudeLayer.FloorEmplacement
    };
        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
        {
            base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);
            Init(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);
        }
        /// <summary>
        /// 初始化射程和穿透数
        /// </summary>
        /// <param name="launcher"></param>
        /// <param name="origin"></param>
        /// <param name="usedTarget"></param>
        /// <param name="intendedTarget"></param>
        /// <param name="hitFlags"></param>
        /// <param name="preventFriendlyFire"></param>
        /// <param name="equipment"></param>
        /// <param name="targetCoverDef"></param>
        private void Init(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null) 
        {
            if (piercingProjectileDef == null)
            {
                Log.Error(this.ToString() + " failed on init to get piercingProjectileDef.");
                Destroy();
                return;
            }
            penetratingPowerLeft = piercingProjectileDef.penetratingPower;
            prevPosition = new Vector3(origin.x, 0f, origin.z);
            startPosition = new Vector3(origin.x, 0f, origin.z);
            if (piercingProjectileDef.rangeOverride != null)
            {
                SetRangeTo(piercingProjectileDef.rangeOverride.Value);
            }
            else if (piercingProjectileDef.reachMaxRangeAlways && equipment != null && (intendedTarget.Thing == null || !(intendedTarget.Thing.def.Fillage != FillCategory.Full) || piercingProjectileDef.penetratingPower > piercingProjectileDef.penetratingPowerCostByShield || intendedTarget.Cell.DistanceToSquared(origin.ToIntVec3()) > 16))
            {
                SetDestinationToMax(equipment, launcher);
            }
        }
        /// <summary>
        /// 设置子弹射程
        /// </summary>
        /// <param name="range"></param>
        public void SetRangeTo(float range)
        {
            Vector3 direct = (destination - origin).normalized;
            Vector3 tendDest = direct * range + origin;
            ShootLine sl = new ShootLine(origin.ToIntVec3(), tendDest.ToIntVec3());
            List<IntVec3> potints = sl.Points().ToList();
            while (potints.Count > 0)
            {
                IntVec3 realDest = potints.Pop();
                //Log.Message("realDest" + realDest.ToString());
                if (realDest.InBounds(Map))
                {
                    //Log.Error("Yes");
                    destination = realDest.ToVector3();
                    destination.x += Rand.Value;
                    destination.z += Rand.Value;
                    break;
                }
            }
            ticksToImpact = Mathf.CeilToInt(base.StartingTicksToImpact);
        }
        /// <summary>
        /// 将子弹射程设置为武器极限射程
        /// </summary>
        /// <param name="equipment"></param>
        /// <param name="launcher"></param>
        public void SetDestinationToMax(Thing equipment, Thing launcher)
        {
            SetRangeTo(Mathf.Min(Mathf.Max(base.Map.Size.x, base.Map.Size.z), GetEquipmentRange(equipment)));
        }
        /// <summary>
        /// 获取武器的射程
        /// </summary>
        /// <param name="equipment"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private float GetEquipmentRange(Thing equipment)
        {
            CompEquippable compEquippable = equipment.TryGetComp<CompEquippable>();
            if (compEquippable != null)
            {
                return compEquippable.PrimaryVerb.verbProps.range;
            }
            throw new Exception("Couldn'hitThing determine max range for " + Label);
        }
        //public override void Tick()
        //{
        //    if (Destroyed) return;
        //    //if (this.IsHashIntervalTick(piercingProjectileDef.tickDamageRate))
        //    //{
        //    //    HitThingsInterval();
        //    //}
        //    if (Destroyed) return;
        //    base.Tick();
        //}
        /// <summary>
        /// 每隔一段时间计算一次结果
        /// 逻辑挪到Postfix_CheckForFreeIntercept了
        /// </summary>
        //private void HitThingsInterval()
        //{
        //    if (Map == null)
        //    {
        //        Log.Message("NULL map");
        //        return;
        //    }
        //    if (Map.thingGrid == null)
        //    {
        //        Log.Message("NULL thingGrid");
        //        return;
        //    }
        //    HashSet<IntVec3> cellsToHit = MakeProjectileLine(prevPosition, DrawPos);
        //    prevPosition = DrawPos;
        //    foreach (IntVec3 item in cellsToHit)
        //    {
        //        if (Map == null)
        //        {
        //            Log.Message("NULL map1");
        //            return;
        //        }
        //        ThingGrid thingGrid = Map.thingGrid;
        //        if (thingGrid == null)
        //        {
        //            Log.Message("NULL thingGrid1");
        //            return;
        //        }
        //        List<Thing> list = Map.thingGrid.ThingsListAt(item);
        //        if (list == null)
        //        {
        //            Log.Message("NULL list");
        //            return;
        //        }
        //        for (int num = list.Count - 1; num >= 0; num--)
        //        {
        //            if (Destroyed) return;
        //            Impact(list[num]);
        //        }
        //    }
        //}
        /// <summary>
        /// 获取经过的格子
        /// </summary>
        /// <param name="prevPosition"></param>
        /// <param name="curPosition"></param>
        /// <returns></returns>
        //private HashSet<IntVec3> MakeProjectileLine(Vector3 prevPosition, Vector3 curPosition)
        //{
            
        //    HashSet<IntVec3> result = new ShootLine(prevPosition.ToIntVec3(), curPosition.ToIntVec3()).Points().ToHashSet();
        //    if (piercingProjectileDef.debugMode)
        //    {
        //        foreach (IntVec3 item in result)
        //        {
        //            Map.debugDrawer.FlashCell(item, 0.5f);
        //        }
        //    }
        //    return result;
        //}
        /// <summary>
        /// 对Thing的互动总逻辑
        /// </summary>
        /// <param name="t"></param>
        /// <param name="needToBeDestroy"></param>
        /// <param name="blockedByShield"></param>
        /// <returns></returns>
        public bool TryHitThing(Thing t, out bool needToBeDestroy, bool blockedByShield = false)
        {
            needToBeDestroy = false;
            bool result = false;
            if (!hitHashSet.Contains(t))//是否曾经参与计算
            {
                if (IsDamagable(t, blockedByShield))
                {
                    if (!CanPiercing(t)) needToBeDestroy = true;
                    HitThing(t, blockedByShield);
                    result = true;
                }
                else MissThing(t);
            }
            
            if (penetratingPowerLeft <= 0)//穿透目标达到上限
            {
                needToBeDestroy = true;
            }
            return result;
        }
        /// <summary>
        /// 判断是否能够击中Thing
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="blockedByShield"></param>
        /// <returns></returns>
        private bool IsDamagable(Thing thing, bool blockedByShield = false)
        {
            if (blockedByShield) return true;//护盾互动
            if (intendedTarget.Thing == thing) return true;//目标判定
            if (thing == null) return false;//击中地面判定
            if (altitudeLayersBlackList.Contains(thing.def.altitudeLayer)) return false;//黑名单排除
            if (thing.Position.DistanceToSquared(startPosition.ToIntVec3()) < piercingProjectileDef.minDistanceToAffectAny * piercingProjectileDef.minDistanceToAffectAny) return false;//全局安全距离判定
            float chanceToHit = 0f;
            if (thing is Pawn pawn)
            {
                chanceToHit = 0.4f * Mathf.Clamp(pawn.BodySize, 0.1f, 2f);
                if (launcher != null && pawn.Faction != null && launcher.Faction != null && !pawn.Faction.HostileTo(launcher.Faction))
                {
                    if (preventFriendlyFire || pawn.Position.DistanceToSquared(startPosition.ToIntVec3()) < (piercingProjectileDef.minDistanceToAffectAlly * piercingProjectileDef.minDistanceToAffectAlly))//友军判定
                    {
                        chanceToHit = 0f;
                    }
                    else
                    {
                        chanceToHit *= Find.Storyteller.difficulty.friendlyFireChanceFactor;
                    }
                }
                else if (piercingProjectileDef.alwaysHitPawnInCell)
                {
                    return true;
                }
                return Rand.Chance(chanceToHit);
            }
            //else if (thing is Building building)
            //{
                if (thing.def.Fillage == FillCategory.Full)
                {
                    if (!(thing is Building_Door door && door.Open))
                    {
                        return true;
                    }
                    return Rand.Chance(0.05f);
                }
                if (thing.def.fillPercent > 0.2f)
                {
                    chanceToHit = intendedTarget.Cell.AdjacentTo8Way(thing.Position) ? (thing.def.fillPercent * 1f) : (thing.def.fillPercent * 0.15f);
                }
                return Rand.Chance(chanceToHit);
            //}
        }
        /// <summary>
        /// 判定为可击中后
        /// </summary>
        /// <param name="hitThing"></param>
        /// <param name="blockedByShield"></param>
        private void HitThing(Thing hitThing, bool blockedByShield = false)
        {
            if (blockedByShield)
            {
                penetratingPowerLeft -= piercingProjectileDef.penetratingPowerCostByShield;
            }
            else
            {
                penetratingPowerLeft--;
            }
            if (hitThing == null) return;
            hitHashSet.Add(hitThing);
            BattleLogEntry_RangedImpact battleLogEntry_RangedImpact = ((equipmentDef != null) ? new BattleLogEntry_RangedImpact(launcher, hitThing, intendedTarget.Thing, equipmentDef, def, targetCoverDef) : new BattleLogEntry_RangedImpact(launcher, hitThing, intendedTarget.Thing, ThingDef.Named("Gun_Autopistol"), def, targetCoverDef));
            Find.BattleLog.Add(battleLogEntry_RangedImpact);
            DamageInfo dinfo = new DamageInfo(def.projectile.damageDef, DamageAmount, base.ArmorPenetration, ExactRotation.eulerAngles.y, launcher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget.Thing);
            hitThing.TakeDamage(dinfo).AssociateWithLog(battleLogEntry_RangedImpact);
            if (hitThing != null && hitThing is Pawn && (hitThing as Pawn).stances != null)
            {
                Pawn pawn = (Pawn)hitThing;
                if (pawn.BodySize <= def.projectile.stoppingPower + 0.001f)
                {


                    pawn.stances.stagger.StaggerFor(95);
                }
            }
            if (def.projectile.extraDamages != null)
            {
                foreach (ExtraDamage extraDamage in def.projectile.extraDamages)
                {
                    if (Rand.Chance(extraDamage.chance))
                    {
                        DamageInfo dinfo2 = new DamageInfo(extraDamage.def, extraDamage.amount, extraDamage.AdjustedArmorPenetration(), ExactRotation.eulerAngles.y, launcher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget.Thing);
                        hitThing.TakeDamage(dinfo2).AssociateWithLog(battleLogEntry_RangedImpact);
                    }
                }
            }
        }
        /// <summary>
        /// 判定为不可击中后
        /// </summary>
        /// <param name="t"></param>
        private void MissThing(Thing t)
        {
            if (t == null) return;
            hitHashSet.Add(t);
        }
        /// <summary>
        /// 判断子弹是否能够穿透目标（检查黑名单）
        /// </summary>
        /// <param name="thing"></param>
        /// <returns></returns>
        public virtual bool CanPiercing(Thing thing)
        {
            if (thing == null) return true;
            if (thing.def.Fillage == FillCategory.Full && !(thing is Building_Door)) return false;
            return true;
        }
        /// <summary>
        /// 所有待击中目标的判定及实施
        /// </summary>
        /// <param name="hitThing"></param>
        /// <param name="blockedByShield"></param>
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            if (TryHitThing(hitThing, out bool needToBeDestroy, blockedByShield))
            {
                GenClamor.DoClamor(this, 12f, ClamorDefOf.Impact);
                if (!blockedByShield && def.projectile.landedEffecter != null)
                {
                    def.projectile.landedEffecter.Spawn(base.Position, base.Map).Cleanup();
                }
            }
            if (needToBeDestroy && !Destroyed)
            {
                Destroy();
            }
        }
        /// <summary>
        /// 子弹到达目的地
        /// </summary>
        protected override void ImpactSomething()
        {
            //HitThingsInterval();
            if (penetratingPowerLeft == piercingProjectileDef.penetratingPower)
            {
                base.ImpactSomething();
            }
            if(!Destroyed) Destroy();
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref penetratingPowerLeft, "penetratingPowerLeft", 1);
            Scribe_Values.Look(ref prevPosition, "prevPosition");
            Scribe_Values.Look(ref startPosition, "startPosition");
            Scribe_Collections.Look(ref hitHashSet, "hitHashSet", LookMode.Reference);
            //if (Scribe.mode == LoadSaveMode.Saving)
            //{
            //    hitList = hitHashSet.ToList();
            //}
            //Scribe_Collections.Look(ref hitList, "hitList");
            //if (Scribe.mode == LoadSaveMode.PostLoadInit)
            //{
            //    hitHashSet = hitList.ToHashSet();
            //}

        }
    }
}
