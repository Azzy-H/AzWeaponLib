using RimWorld;
using System.Collections.Generic;
using Verse;
using LudeonTK;
using System.Linq;

namespace AzWeaponLib.Gas
{
    public static class DebugToolsGas
    {
        [DebugAction("Gas", "Explosion with Gas...", false, false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static List<DebugActionNode> ExplosionToGenerateSRMGas()
        {
            List<DebugActionNode> list = new List<DebugActionNode>();
            IEnumerable<ThingDef> GasDefs = DefDatabase<ThingDef>.AllDefs.Where(x => (x.category == ThingCategory.Gas));
            foreach (ThingDef GasDef in GasDefs)
            {
                list.Add(new DebugActionNode(GasDef.defName.CapitalizeFirst(), DebugActionType.ToolMap, delegate
                {
                    GenExplosion.DoExplosion(UI.MouseCell(), Find.CurrentMap, 4.9f, DamageDefOf.Smoke, null, postExplosionSpawnThingDef: GasDef, postExplosionSpawnChance: 1f, postExplosionSpawnThingCount: 1, doSoundEffects: false, doVisualEffects: false);
                }));
            }
            return list;
        }
        [DebugAction("Gas", "Destroy All Gas", false, false, false, false,false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void DestroyAllSRMGas()
        {
            IEnumerable<ThingDef> GasDefs = DefDatabase<ThingDef>.AllDefs.Where(x => (x.category == ThingCategory.Gas));
            foreach (ThingDef GasDef in GasDefs)
            {
                Thing[] array = Find.CurrentMap.listerThings.ThingsOfDef(GasDef).ToArray();
                for (int i = 0; i < array.Length; i++)
                {
                    array[i].Destroy();
                }
            }
        }
    }
}

