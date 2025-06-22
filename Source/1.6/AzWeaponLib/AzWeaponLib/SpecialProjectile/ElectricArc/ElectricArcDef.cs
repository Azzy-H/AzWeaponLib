using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace AzWeaponLib.SpecialProjectile
{
    public class ElectricArcDef : DefModExtension
    {
        public float conductChance;
        public float conductRange;
        public int maxConductNum;
        public float conductChanceExtra;
        public float conductRangeExtra;
        public int maxConductNumExtra;
        public float damageMultiplierExtra = 1f;
        public float penetrationMultiplierExtra = 1f;
        public float damageMultiplierPerConduct = 1f;
        public int conductTicks;
        public bool noDeviation = false;
        public FleckDef fleckDef;
        public List<HediffDef> hediffDefsToExtra;
        public List<WeatherDef> weatherDefsToExtra;
        public List<GameConditionDef> gameConditionDefsToExtra;
        private static List<GameCondition> gameConditionsTemp = new List<GameCondition>();
        private static List<GameConditionDef> gameConditionDefsTemp = new List<GameConditionDef>();
        public bool ShouldExtra(LocalTargetInfo target, Map map)
        {
            if (target.Thing == null || map == null)
            {
                return false;
            }
            if (weatherDefsToExtra != null && weatherDefsToExtra.Contains(map.weatherManager.curWeather))
            { 
                return true;
            }
            if (target.Pawn != null)
            {
                if (target.Pawn.RaceProps.IsMechanoid) 
                {
                    return true; 
                }
                if (hediffDefsToExtra != null)
                {
                    HediffSet hediffSet = target.Pawn.health.hediffSet;
                    foreach (HediffDef hediffDef in hediffDefsToExtra)
                    {
                        if (hediffSet.GetFirstHediffOfDef(hediffDef) != null)
                        {
                            return true;
                        }
                    }
                }
            }
            if (gameConditionDefsToExtra != null)
            {
                gameConditionsTemp.Clear();
                map.gameConditionManager.GetAllGameConditionsAffectingMap(map, gameConditionsTemp);
                foreach (GameCondition condition in gameConditionsTemp) 
                {
                    gameConditionDefsTemp.Add(condition.def);
                }
                if (gameConditionDefsTemp.Intersect(gameConditionDefsToExtra).Count() > 0)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
