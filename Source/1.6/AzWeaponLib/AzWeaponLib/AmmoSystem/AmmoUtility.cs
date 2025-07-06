using RimWorld;
using RimWorld.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using AzWeaponLib;

namespace AzWeaponLib.AmmoSystem
{
    public static class AmmoUtility
    {
        private static Dictionary<string, float> keyValues = new Dictionary<string, float>();
        public static float GetReloadMultipiler(Pawn pawn = null, ThingWithComps weapon = null)
        {
            GetReloadMultipilerFactors(pawn, weapon);
            float result = 1f;
            foreach (float multipiler in keyValues.Values)
            {
                result *= multipiler;
            }
            return result;
        }
        public static Dictionary<string, float> GetReloadMultipilerFactors(Pawn pawn = null, ThingWithComps weapon = null)
        {
            keyValues.Clear();
            if (pawn != null)
            {
                int shootingSkillLevel = pawn.skills?.GetSkill(SkillDefOf.Shooting).levelInt ?? 8;
                shootingSkillLevel = Mathf.Clamp(shootingSkillLevel, 0, 20);
                float shootingSkillMultipiler = 28f / (20 + shootingSkillLevel);
                keyValues.Add("AWL_ShootingSkillMultipiler", shootingSkillMultipiler);

                float manipulation = pawn.health?.capacities?.GetLevel(PawnCapacityDefOf.Manipulation) ?? 1f;
                manipulation = Mathf.Clamp(manipulation, 0.49f, 2f);
                float manipulationMultipiler = 1.5f / (0.5f + manipulation);
                keyValues.Add("AWL_ManipulationMultipiler", manipulationMultipiler);

                keyValues.Add("AWL_ReloadingTimeFactor", pawn.GetStatValue(AWL_DefOf.AWL_ReloadingTimeFactor));
            }
            if (weapon != null)
            { }
            return keyValues;
        }
        public static CompAmmo FindCompAmmo(Pawn pawn)
        {
            return pawn?.equipment?.Primary?.TryGetComp<CompAmmo>();
        }
        public static List<Thing> FindEnoughAmmo(Pawn pawn, IntVec3 rootCell, CompAmmo compAmmo, bool forceReload)
        {
            if (compAmmo == null)
            {
                return null;
            }
            IntRange desiredQuantity = new IntRange(1, compAmmo.maxAmmoNeeded);
            return RefuelWorkGiverUtility.FindEnoughReservableThings(pawn, rootCell, desiredQuantity, (Thing t) => t.def == compAmmo.Props.ammunitionDef);
        }
    }
}
