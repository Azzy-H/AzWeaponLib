using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace AzWeaponLib.HeavyWeapon
{
    public class CompApparelWithHeavyWeapon : ThingComp
    {
        public override void Notify_Unequipped(Pawn pawn)
        {
            ThingWithComps eq = pawn.equipment.Primary;
            if (eq != null)
            {
                HeavyWeaponDef heavyWeaponDef = eq.def.GetModExtension<HeavyWeaponDef>();
                if (heavyWeaponDef != null) 
                {
                    List<Apparel> apparels = pawn.apparel?.WornApparel;
                    if (apparels == null || apparels.Empty())//没有衣服
                    {
                        DropEquipmentOrPutIntoCaravan(pawn, eq);
                    }
                    foreach (Apparel apparel in apparels)
                    {
                        if (heavyWeaponDef.apparelGroupDef.availableApparels.Contains(apparel.def))//满足条件
                        {
                            return;
                        }
                    }
                    //有衣服但没有满足条件
                    DropEquipmentOrPutIntoCaravan(pawn, eq);
                    return;
                }
            }
        }
        private void DropEquipmentOrPutIntoCaravan(Pawn pawn, ThingWithComps eq, bool forbid = true)
        {
            if (pawn.SpawnedParentOrMe != null)
            {
                pawn.equipment.TryDropEquipment(eq, out _, pawn.SpawnedParentOrMe.Position, forbid);
            }
            else
            {
                Caravan caravan = pawn.GetCaravan();
                pawn.equipment.Remove(eq);
                if (caravan != null)
                {
                    CaravanInventoryUtility.GiveThing(caravan, eq);
                }
            }
        }
    }
}
