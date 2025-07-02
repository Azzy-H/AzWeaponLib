using RimWorld;
using System.Collections.Generic;
using System.Text;
using Verse;
using UnityEngine;
using System;

namespace AzWeaponLib.Gas
{
    public abstract class AWLGasProperties : GasProperties
    {
        public int tickEffectRate;
        public List<DamageDef> damageDefsToDestroyGas;
        public ThingDef preDestroySpawnThingDef;
        public float preDestroySpawnThingChance;
        public int preDestroySpawnThingCount;
        public float heatOutput = 0f;
    }
    public abstract class AWLGas : RimWorld.Gas
    {
        public AWLGasProperties gasProps => (AWLGasProperties)(def.gas);
        public List<Thing> thingsInCell => Position.GetThingList(Map);
        public bool HasThingInPosition => thingsInCell != null && thingsInCell.Count != 0;
        public float density => 1f - (Find.TickManager.TicksGame - spawnedTick) / (float)(destroyTick - spawnedTick);
        protected string DensityPercentString => density.ToStringPercent();
        private string cachedLabelMouseover;
        public override string LabelMouseover
        { 
            get
            {
                if (cachedLabelMouseover == null)
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.Append(def.LabelCap);
                    stringBuilder.Append(" (" + "Density".Translate(DensityPercentString) + ")");
                    cachedLabelMouseover = stringBuilder.ToString();
                }
                return cachedLabelMouseover;
            }
        }
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            TryDoEffectToCell();
        }
        protected override void Tick()
        {
            base.Tick();
            if (!Spawned) return;
            cachedLabelMouseover = null;
            TryDoSomeThingToCellPerTick();
        }
        protected override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (!Spawned) return;
            cachedLabelMouseover = null;
            TryDoSomeThingToCellPerTickInterval(delta);
        }
        public virtual void SetDensityTo(float densityAim)
        {
            float densityOffset = densityAim - density;
            int tickOffset = Mathf.RoundToInt((destroyTick - spawnedTick) * densityOffset);
            destroyTick += tickOffset;
            spawnedTick += tickOffset;
        }
        public virtual void TryDoSomeThingToCellPerTick()
        {
            if (this.IsHashIntervalTick(gasProps.tickEffectRate))
            {
                TryDoEffectToCell();
            }
        }
        public virtual void TryDoSomeThingToCellPerTickInterval(int delta)
        {
            //int loop = this.HashOffsetTicks() / gasProps.tickEffectRate - (this.HashOffsetTicks() - delta) / gasProps.tickEffectRate;
            TryDoEffectToCellTickInterval(delta);
            GenTemperature.PushHeat(Position, Map, gasProps.heatOutput * delta);
        }
        protected abstract void TryDoEffectToCell();
        protected virtual void TryDoEffectToCellTickInterval(int delta)
        { }
        public override void PostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.PostApplyDamage(dinfo, totalDamageDealt);
            if (gasProps.damageDefsToDestroyGas == null) return;
            foreach (DamageDef damageDef in gasProps.damageDefsToDestroyGas)
            {
                if (dinfo.Def.defName == damageDef.defName)
                {
                    DestroyWithPreAndPost();
                    break;
                }
            }

        }//用于处理受到特定伤害消散
        public virtual void DestroyWithPreAndPost(DestroyMode mode = DestroyMode.Vanish)
        {
            PreDestroy();
            Destroy(mode);
            PostDestroy();
        }//用于处理消散特效
        protected virtual void PreDestroy()
        {
            if (Rand.Chance(gasProps.preDestroySpawnThingChance) && Position.Walkable(base.Map))
            {
                if (gasProps.preDestroySpawnThingDef != null)
                {
                    if (gasProps.preDestroySpawnThingDef.IsFilth)
                    {
                        FilthMaker.TryMakeFilth(Position, base.Map, gasProps.preDestroySpawnThingDef, gasProps.preDestroySpawnThingCount);
                        return;
                    }
                    Thing thing = ThingMaker.MakeThing(gasProps.preDestroySpawnThingDef);
                    thing.stackCount = gasProps.preDestroySpawnThingCount;
                    GenSpawn.Spawn(thing, Position, base.Map);
                    thing.TryGetComp<CompReleaseGas>()?.StartRelease();
                }
            }
        }
        protected virtual void PostDestroy()
        { }
    }

}

