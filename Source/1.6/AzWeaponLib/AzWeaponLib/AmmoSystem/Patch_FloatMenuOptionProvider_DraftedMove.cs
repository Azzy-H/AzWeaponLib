using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;

namespace AzWeaponLib.AmmoSystem
{
    [HarmonyPatch(typeof(FloatMenuOptionProvider_DraftedMove))]
    internal class Patch_FloatMenuOptionProvider_DraftedMove
    {
        [HarmonyPatch("PawnGotoAction")]
        [HarmonyPrefix]
        public static bool Prefix_PawnGotoAction(IntVec3 clickCell, Pawn pawn, IntVec3 gotoLoc)
        {
            pawn.BroadcastCompSignalToPawn("AWL_GoTo");
            if (pawn.CurJobDef == AWL_DefOf.AWL_ReloadWeapon && ((JobDriver_ReloadWeapon)pawn.CurJob.GetCachedDriver(pawn)).canMove)
            {
                if (pawn.Position == gotoLoc)
                {
                    pawn.pather.StopDead();
                }
                else
                {
                    pawn.pather.StartPath(gotoLoc, PathEndMode.OnCell);
                    Job job = JobMaker.MakeJob(JobDefOf.Goto, gotoLoc);
                }
                FleckMaker.Static(gotoLoc, pawn.Map, FleckDefOf.FeedbackGoto);
                return false;
            }
            return true;
        }
    }
}
