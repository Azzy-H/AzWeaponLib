using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace AzWeaponLib.HeavyWeapon
{
    [HarmonyPatch(typeof(EquipmentUtility))]
    public class PatchEquipmentUtility
    {
        [HarmonyPatch("CanEquip", 
            new Type[] { 
                typeof(Thing),
                typeof(Pawn), 
                typeof(string), 
                typeof(bool) }, 
            new ArgumentType[] { 
                ArgumentType.Normal, 
                ArgumentType.Normal, 
                ArgumentType.Out, 
                ArgumentType.Normal })]
        [HarmonyPostfix]
        public static void Postfix_CanEquip(ref bool __result, Thing thing, Pawn pawn, ref string cantReason, bool checkBonded = true)
        {
            HeavyWeaponDef heavyWeaponDef = thing.def.GetModExtension<HeavyWeaponDef>();
            if (heavyWeaponDef != null)//普通武器不计算
            {
                List<Apparel> apparels = pawn.apparel?.WornApparel;
                if (apparels == null || apparels.Empty())//没有衣服
                {
                    __result = false;
                    cantReason = "SR_NoAvailableApparel".Translate();
                    return;
                }
                foreach (Apparel apparel in apparels)
                {
                    if (heavyWeaponDef.apparelGroupDef.availableApparels.Contains(apparel.def))//满足条件
                    {
                        return;
                    }
                }
                //有衣服但没有满足条件
                __result = false;
                cantReason = "SR_NoAvailableApparel".Translate();
                return;
            }
        }
    }
}
