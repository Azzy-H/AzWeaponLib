using AzWeaponLib;
using RimWorld;
using RimWorld.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using static HarmonyLib.Code;

namespace AzWeaponLib.AmmoSystem
{
    public class CompProperties_Ammo : CompProperties
    {
        public bool singleShotLoading = false;
        public int ammunitionCapacity = 1;
        public bool canLoadExtra = false;
        public float reloadingTime = 1f;
        public bool pawnStatsAffectReloading = true;
        public ThingDef ammunitionDef;
        public bool exhaustable;
        public ThingDef exhaustedDef;
        public int maxBackupAmmo = 10;
        public int ammoCountPerAmmunitionBox = 3;
        public bool canMoveWhenReload = false;
        private const int displayPriority = 300;
        private static StatCategoryDef statCategoryDef;
        public CompProperties_Ammo()
        {
            compClass = typeof(CompAmmo);
        }
        public override void ResolveReferences(ThingDef parentDef)
        {
            //base.ResolveReferences(parentDef);
            statCategoryDef = StatCategoryDefOf.Weapon_Ranged;
        }
        public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
        {
            if(req.Thing == null) return GetStatDrawEntries(req);
            return Enumerable.Empty<StatDrawEntry>();
        }
        public virtual IEnumerable<StatDrawEntry> GetStatDrawEntries(StatRequest req)
        {
            CompAmmo compAmmo = null;
            Thing t = req.Thing;
            if (t != null)
            {
                compAmmo = t.TryGetComp(this) as CompAmmo;
            }
            int priority = 0;
            if (singleShotLoading) yield return SingleShotLoadingDisp(ref priority, compAmmo);
            yield return AmmunitionCapacityDisp(ref priority, compAmmo);
            yield return ReloadingTimeDisp(ref priority, compAmmo);
            if (canMoveWhenReload) yield return MoveReloadDisp(ref priority, compAmmo);
            if (ammunitionDef != null)
            {
                yield return AmmunitionCostDisp(ref priority, compAmmo);
                yield return MaxBackupAmmoDisp(ref priority, compAmmo);
            }
        }
        private StatDrawEntry SingleShotLoadingDisp(ref int priorityOffset, CompAmmo compAmmo = null)
        {
            priorityOffset--;
            var num = singleShotLoading;
            string Label = "AWL_SingleShotLoadingLabel".Translate();
            string Text = "AWL_SingleShotLoadingText".Translate();
            return new StatDrawEntry(reportText: StatDispUtility.StringBuilderInit(Text, num).ToString(), category: statCategoryDef, label: Label, valueString: num ? "Yes".Translate() : "No".Translate(), displayPriorityWithinCategory: displayPriority - priorityOffset);
        }
        private StatDrawEntry AmmunitionCapacityDisp(ref int priorityOffset, CompAmmo compAmmo = null)
        {
            priorityOffset--;
            int num = ammunitionCapacity;
            string valueString;
            string Label = "AWL_AmmunitionCapacityLabel".Translate();
            string Text = "AWL_AmmunitionCapacityText".Translate();
            StringBuilder resultStringBuilder = new StringBuilder(Text);
            resultStringBuilder.AppendLine();
            resultStringBuilder.AppendLine();
            resultStringBuilder.AppendLine("StatsReport_BaseValue".Translate() + ": " + num.ToString());
            if (canLoadExtra)
            {
                resultStringBuilder.Append("--");
                resultStringBuilder.AppendLine("AWL_CanLoadExtra".Translate() + ": (+1)");
                valueString = num.ToString() + "(" + (num + 1).ToString() + ")";
            }
            else 
            {
                valueString = num.ToString();
            }
            resultStringBuilder.AppendLine();
            return new StatDrawEntry(reportText: resultStringBuilder.ToString(), category: statCategoryDef, label: Label, valueString: valueString, displayPriorityWithinCategory: displayPriority - priorityOffset);
        }
        private StatDrawEntry ReloadingTimeDisp(ref int priorityOffset, CompAmmo compAmmo = null)
        {
            priorityOffset--;
            float num = reloadingTime;
            string Label = "AWL_ReloadingTimeLabel".Translate();
            string Text = "AWL_ReloadingTimeText".Translate();
            StringBuilder resultStringBuilder = new StringBuilder(Text);
            if (reloadingTime < 0)
            {
                resultStringBuilder.AppendLine();
                resultStringBuilder.AppendLine();
                resultStringBuilder.Append("--");
                resultStringBuilder.AppendLine("AWL_Disposable".Translate());
                resultStringBuilder.AppendLine();
                resultStringBuilder.AppendLine("StatsReport_FinalValue".Translate() + ": " + "NotUsable".Translate());
                return new StatDrawEntry(reportText: resultStringBuilder.ToString(), category: statCategoryDef, label: Label, valueString: "NotUsable".Translate(), displayPriorityWithinCategory: displayPriority - priorityOffset);
            }
            resultStringBuilder.AppendLine();
            resultStringBuilder.AppendLine();
            resultStringBuilder.AppendLine("StatsReport_BaseValue".Translate() + ": " + num.ToString("0.##") + " " + "LetterSecond".Translate());
            if (compAmmo != null && pawnStatsAffectReloading)
            {
                Dictionary<string, float> d = AmmoUtility.GetReloadMultipilerFactors(compAmmo.pawn, compAmmo.parent);
                foreach (string s in d.Keys)
                {
                    resultStringBuilder.Append("--");
                    resultStringBuilder.AppendLine(s.Translate() + ": " + "*" + d[s].ToString("F2"));
                    resultStringBuilder.AppendLine((s + "_Tips").Translate());
                    resultStringBuilder.AppendLine();
                    num *= d[s];
                }
                resultStringBuilder.AppendLine();
                resultStringBuilder.AppendLine("StatsReport_FinalValue".Translate() + ": " + num.ToString("0.##") + " " + "LetterSecond".Translate());
            }
            resultStringBuilder.AppendLine();
            return new StatDrawEntry(reportText: resultStringBuilder.ToString(), category: statCategoryDef, label: Label, valueString: num.ToString("F2"), displayPriorityWithinCategory: displayPriority - priorityOffset);
        }
        private StatDrawEntry AmmunitionCostDisp(ref int priorityOffset, CompAmmo compAmmo = null)
        {
            priorityOffset--;
            var num = ammoCountPerAmmunitionBox;
            string Label = "AWL_AmmunitionCostLabel".Translate();
            string Text = "AWL_AmmunitionCostText".Translate();
            return new StatDrawEntry(reportText: StatDispUtility.StringBuilderInit(Text, num).ToString(), category: statCategoryDef, label: Label, valueString: ammunitionDef.label, displayPriorityWithinCategory: displayPriority - priorityOffset);
        }
        private StatDrawEntry MaxBackupAmmoDisp(ref int priorityOffset, CompAmmo compAmmo = null)
        {
            priorityOffset--;
            var num = maxBackupAmmo;
            string Label = "AWL_MaxBackupAmmoLabel".Translate();
            string Text = "AWL_MaxBackupAmmoText".Translate();
            return new StatDrawEntry(reportText: StatDispUtility.StringBuilderInit(Text, num).ToString(), category: statCategoryDef, label: Label, valueString: num.ToString(), displayPriorityWithinCategory: displayPriority - priorityOffset);
        }
        private StatDrawEntry MoveReloadDisp(ref int priorityOffset, CompAmmo compAmmo = null)
        {
            priorityOffset--;
            var num = canMoveWhenReload;
            string Label = "AWL_MoveReloadLabel".Translate();
            string Text = "AWL_MoveReloadText".Translate();
            return new StatDrawEntry(reportText: StatDispUtility.StringBuilderInit(Text, num).ToString(), category: statCategoryDef, label: Label, valueString: num ? "Yes".Translate() : "No".Translate(), displayPriorityWithinCategory: displayPriority - priorityOffset);
        }

    }
    public class CompAmmo : ThingComp
    {
        public CompProperties_Ammo Props => (CompProperties_Ammo)props;
        public static AWL_Settings AWL_Settings = LoadedModManager.GetMod<AWL_Mod>().GetSettings<AWL_Settings>();
        private int ammunitionCapacity => Props.ammunitionCapacity;
        private float reloadingTime => Props.reloadingTime;
        protected virtual HediffDef hediffDef => AWL_DefOf.AWL_AmmoGizmoDisp;
        protected Hediff hediff;
        public bool NeedReload
        {
            get
            {
                return ammo < ammunitionCapacity;
            }
        }
        public virtual bool needReloadBackupAmmo
        {
            get
            {
                return (Props.maxBackupAmmo - BackupAmmo) >= Props.ammoCountPerAmmunitionBox && useBackupAmmo;
            }
        }
        public virtual int maxAmmoNeeded => (Props.maxBackupAmmo - BackupAmmo) / Props.ammoCountPerAmmunitionBox;
        public bool isEmpty
        {
            get
            {
                return ammo <= 0;
            }
        }
        public bool useAmmoSystem => AWL_Settings.enableAmmoSystem;
        private bool onlyShowAmmoGizmoWhenSelectedOneThing => AWL_Settings.onlyShowAmmoGizmoWhenSelectedOneThing;
        public virtual bool canReloadNow
        { 
            get 
            {
                //float manipulation = pawn.health?.capacities?.GetLevel(PawnCapacityDefOf.Manipulation) ?? 1f;
                //if (manipulation < 0.5f) return false;
                if (Props.reloadingTime < 0) return false;//一次性
                if (!NeedReload) return false;//满弹
                if (!enableReloadOverall) return false;//弹药架殉爆
                return !useBackupAmmo || !NoBackupAmmo;
            }
        }
        public AcceptanceReport enableReloadOverall = true;
        private int ammo;
        public int Ammo
        {
            get
            {
                return ammo;
            }
            set 
            {
                ammo = value;
                //if (value <= 0) NotifyExhausted();
                //else if (value > ammo) NotifyReloaded();
            }
        }

        public bool autoReload = true;
        public Pawn pawn;
        private int backupAmmo;
        public virtual int BackupAmmo
        {
            get
            {
                return backupAmmo;
            }
            set 
            {
                backupAmmo = value;
            }
        }
        public virtual bool NoBackupAmmo => Props.ammunitionDef != null && BackupAmmo <= 0 && (pawn?.Faction?.IsPlayer ?? false) && AWL_Settings.enableBackupAmmoSystem;
        public virtual int MaxReloadTick => Props.reloadingTime.SecondsToTicks() * 3;
        public virtual bool useBackupAmmo => Props.ammunitionDef != null && AWL_Settings.enableAmmoSystem && AWL_Settings.enableBackupAmmoSystem;
        public override void Notify_Equipped(Pawn pawn)
        {
            this.pawn = pawn;
            hediff = HediffMaker.MakeHediff(hediffDef, pawn);
            pawn.health.AddHediff(hediff);
        }
        public override void Notify_Unequipped(Pawn pawn)
        {
            this.pawn = null;
            base.Notify_Unequipped(pawn);
            if (hediff != null) pawn.health.RemoveHediff(hediff);
            hediff = null;
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            string reason = "true";
            Scribe_References.Look(ref hediff, "hediff_CompAmmo");
            Scribe_References.Look(ref pawn, "pawn_CompAmmo");
            Scribe_Values.Look(ref ammo, "ammo_CompAmmo");
            Scribe_Values.Look(ref autoReload, "autoReload_CompAmmo");
            Scribe_Values.Look(ref backupAmmo, "backupAmmo_CompAmmo");
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                if(!enableReloadOverall.Accepted) reason = enableReloadOverall.Reason;
                Scribe_Values.Look(ref reason, "enableReloadOverall_CompAmmo");
            }
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                Scribe_Values.Look(ref reason, "enableReloadOverall_CompAmmo");
                if (reason != "true") enableReloadOverall = new AcceptanceReport(reason);
            }
        }
        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            ammo = Props.ammunitionCapacity;
            if (Props.reloadingTime < 0)
            {
                autoReload = false;
            }
        }
        public override void ReceiveCompSignal(string signal)
        {
            if (signal == "AWL_SetMaxAmmo")
            {
                ReloadToMax();
                BackupAmmo = Props.maxBackupAmmo;
            }
            else if (signal == "AWL_Undrafted" || signal == "AWL_Released" || signal == "AWL_Reloaded")
            {
                if (!pawn.Spawned) return;
                TryMakeReloadJob(forced: false);
            }
            else if (signal == "AWL_GoTo" && Props.canMoveWhenReload)
            {
                if (!pawn.Spawned) return;
                TryMakeReloadJob(forced: false);
            }
            else if (signal == "AWL_HuntFinished")
            {
                if (!pawn.Spawned) return;
                TryMakeReloadJob(forced: false, delay: true);
            }
        }
        public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        { 
            foreach(StatDrawEntry sde in Props.GetStatDrawEntries(StatRequest.For(parent))) yield return sde;
        }
        public virtual void ReloadByBackupAmmo()
        {
            int needAmmoNum = isEmpty ? ammunitionCapacity : ammunitionCapacity + 1;
            if (pawn.Faction != null && pawn.Faction.IsPlayer)
            {
                needAmmoNum -= ammo;
            }
            else
            {
                needAmmoNum = 0;
            }
            if (needAmmoNum <= BackupAmmo)
            {
                ReloadToMax();
                BackupAmmo -= needAmmoNum;
            }
            else
            {
                ReloadByNum(BackupAmmo);
                BackupAmmo = 0;
            }
            NotifyReloaded();
        }
        public virtual void ReloadByBackupAmmoOnce()
        {
            ammo++;
            BackupAmmo--;
            NotifyReloaded();
        }
        public virtual void ReloadToMax()
        {
            if (!Props.canLoadExtra || isEmpty) ammo = ammunitionCapacity;
            else ammo = ammunitionCapacity + 1;
            NotifyReloaded();
        }
        public virtual void ReloadTo(int num)
        {
            ammo = num;
            NotifyReloaded();
        }
        public virtual void ReloadByOne()
        {
            if (NeedReload) ammo++;
            NotifyReloaded();
        }
        public virtual void ReloadByNum(int num)
        {
            ammo += num;
            ammo = Mathf.Min(ammo, ammunitionCapacity + 1);
            NotifyReloaded();
        }
        public virtual void ReloadByAmmoBox(Thing t)
        { 
            if (t == null) return;
            if (!needReloadBackupAmmo) return;
            int num = Mathf.Min(maxAmmoNeeded, t.stackCount);
            BackupAmmo += num * Props.ammoCountPerAmmunitionBox;
            t.SplitOff(num).Destroy();
            parent.BroadcastCompSignal("AWL_Reloaded");
        }
        public virtual void UsedOnce()
        {
            ammo--;
            if (ammo <= 0) NotifyExhausted();
        }
        public virtual void UsedByNum(int num)
        {
            Ammo -= num;
            if (ammo <= 0) NotifyExhausted();
        }
        public virtual void NotifyExhausted()
        {
            if (!Props.exhaustable) return;
            Pawn p = pawn;
            if (Props.exhaustedDef != null)
            {
                Thing newThing = ThingMaker.MakeThing(Props.exhaustedDef);
                p.equipment.GetDirectlyHeldThings().Remove(parent);
                Thing _;
                if (newThing is ThingWithComps t && t.HasComp<CompEquippable>()) p.equipment.AddEquipment(t);
                else GenDrop.TryDropSpawn(newThing, p.Position, p.Map, ThingPlaceMode.Near, out _);
            }
            parent.Destroy();
        }
        protected virtual void NotifyReloaded()
        {
        }
        public virtual void TryMakeReloadJob(bool forced = false, bool delay = false, bool resumeCurJob = true)
        {
            if (!canReloadNow) return;
            if (!forced && (GetReloadTicks() > MaxReloadTick || !autoReload)) return;
            if (pawn.CurJobDef == AWL_DefOf.AWL_ReloadWeapon) 
            { 
                if (pawn.CurJob.playerForced || !forced)
                {
                    return;
                } 
                else
                {
                    pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                }
            }
            if (Props.ammunitionDef != null && NoBackupAmmo) return;
            Job reload = JobMaker.MakeJob(AWL_DefOf.AWL_ReloadWeapon);
            reload.targetA = pawn;
            reload.targetB = parent;
            reload.count = !useBackupAmmo ? 0 : 1;
            reload.playerForced = forced;
            //pawn.jobs.SuspendCurrentJob(JobCondition.InterruptOptional);
            if (delay)
            {
                pawn.jobs.jobQueue.EnqueueLast(reload);
            }
            else 
            {
                if (pawn.CurJobDef == JobDefOf.Goto)
                {
                    pawn.jobs.StartJob(reload, lastJobEndCondition: JobCondition.InterruptOptional, resumeCurJobAfterwards: !Props.canMoveWhenReload && resumeCurJob, cancelBusyStances: false);
                }
                else 
                {
                    pawn.jobs.StartJob(reload, lastJobEndCondition: JobCondition.InterruptOptional, resumeCurJobAfterwards: resumeCurJob, cancelBusyStances: false);
                }
            }
        }
        public virtual IEnumerable<Gizmo> GetAmmoGizmos()
        {
            if (onlyShowAmmoGizmoWhenSelectedOneThing && Find.Selector.NumSelected > 1) yield break;
            //bool canReloadNow = (Props.reloadingTime > 0)  && (GetReloadTicks() <= MaxReloadTick) && ((Props.ammunitionDef == null && Props.ammoSourceFinder == null) || backupAmmo > 0);//非一次性武器，装填时间低于阈值且足够弹药
            yield return GetAmmoStatusGizmo();
            if (DebugSettings.ShowDevGizmos)
            {
                yield return new Command_Action
                {
                    icon = null,
                    defaultLabel = "Dev:Reload insantly",
                    defaultDesc = "Dev:Reload insantly",
                    onHover = null,
                    action = delegate
                    {
                        ReloadToMax();
                        pawn.stances.SetStance(new Stance_Mobile());
                    },
                    activateSound = SoundDef.Named("Click"),
                    hotKey = null
                };
                yield return new Command_Action
                {
                    icon = null,
                    defaultLabel = "Dev:Magazine Set 1",
                    defaultDesc = "Dev:Magazine Set 1",
                    onHover = null,
                    action = delegate
                    {
                        ammo = 1;
                    },
                    activateSound = SoundDef.Named("Click"),
                    hotKey = null
                };
                yield return new Command_Action
                {
                    icon = null,
                    defaultLabel = "Dev:Clear magazine",
                    defaultDesc = "Dev:Clear magazine",
                    onHover = null,
                    action = delegate
                    {
                        ammo = 0;
                    },
                    activateSound = SoundDef.Named("Click"),
                    hotKey = null
                };
                if (useBackupAmmo)
                {
                    yield return new Command_Action
                    {
                        icon = null,
                        defaultLabel = "Dev:Clear backup ammo",
                        defaultDesc = "Dev:Clear backup ammo",
                        onHover = null,
                        action = delegate
                        {
                            BackupAmmo = 0;
                        },
                        activateSound = SoundDef.Named("Click"),
                        hotKey = null
                    };
                    yield return new Command_Action
                    {
                        icon = null,
                        defaultLabel = "Dev:Set full backup ammo",
                        defaultDesc = "Dev:Set full backup ammo",
                        onHover = null,
                        action = delegate
                        {
                            BackupAmmo = Props.maxBackupAmmo;
                        },
                        activateSound = SoundDef.Named("Click"),
                        hotKey = null
                    };
                }
            }
        }
        protected virtual Gizmo GetAmmoStatusGizmo()
        {
            Gizmo_AmmoStatus gizmo_AmmoStatus = new Gizmo_AmmoStatus()
            {
                ammunitionCapacity = ammunitionCapacity,
                amunitionRemained = ammo,
                autoReload = autoReload,
                autoReloadToggle = AutoReloadToggle,
                makeReloadJob = TryMakeReloadJob,
                canAutoReloadToggleNow = Props.reloadingTime > 0,
                canReloadNow = canReloadNow,
                backupAmmo = !useBackupAmmo ? -1 : BackupAmmo
            };
            if (!canReloadNow)
            {
                if (Props.reloadingTime < 0)
                {
                    gizmo_AmmoStatus.failedReason = "AWL_DisposableWeapon".Translate().Colorize(Color.yellow);
                }
                else if (!NeedReload)
                {
                    gizmo_AmmoStatus.failedReason = "AWL_NoNeedToReload".Translate();
                }
                else if (!enableReloadOverall)
                {
                    gizmo_AmmoStatus.failedReason = enableReloadOverall.Reason.Colorize(Color.yellow);
                }
                else if (NoBackupAmmo)
                {
                    gizmo_AmmoStatus.failedReason = "AWL_NoEnoughBackupAmmo".Translate().Colorize(Color.yellow);
                }
                else if (GetReloadTicks() > MaxReloadTick)
                {
                    gizmo_AmmoStatus.failedReason = "AWL_LowManipulation".Translate().Colorize(Color.yellow);
                }
            }
            return gizmo_AmmoStatus;
        }
        public virtual bool AutoReloadToggle()
        {
            autoReload = !autoReload;
            return autoReload;
        }
        public virtual int GetReloadTicks()
        {
            if (!Props.pawnStatsAffectReloading) return GenTicks.SecondsToTicks(reloadingTime);
            return GenTicks.SecondsToTicks(reloadingTime * AmmoUtility.GetReloadMultipiler(pawn, parent));
        }
        /// <summary>
        /// 获取本次job的重复次数
        /// </summary>
        /// <param name="Forced"></param>
        /// <returns></returns>
        public virtual int ReloadLoop(bool Forced = false)
        {
            if (Props.singleShotLoading)
            {
                if (Forced || (pawn.drafter != null && !pawn.drafter.Drafted))
                    return ammunitionCapacity - ammo;
                else
                {
                    Verb v = parent.TryGetComp<CompEquippable>().PrimaryVerb;
                    int result = v.verbProps.burstShotCount;
                    if (v is Verb_ShootWithAmmo vswa)
                    {
                        result *= vswa.VerbProps.ammoCostPerShot;
                    }
                    return Mathf.Min(result, ammunitionCapacity - ammo);
                }
            }
            return 1;
        }
    }
    
}
