using System.Collections.Generic;
using AzWeaponLib.VFX;
using RimWorld;
using UnityEngine;
using Verse;

namespace AzWeaponLib.SpecialProjectile
{
    /// <summary>
    /// 射流投射物的全部可调参数。
    /// 用一个 DefModExtension 而不是塞进 VerbProperties，是为了让弹体本身（而不是发射它的武器）
    /// 决定"飞行时怎么判定、命中后怎么偏转、能量怎么衰减"。
    /// </summary>
    public class JetProjectileDef : DefModExtension
    {
        /// <summary>每命中一个目标后，后续目标受到的伤害倍率，反射同采用该数值。</summary>
        public float damageRetainPerHit = 0.85f;

        /// <summary>撞到满格建筑时是否反射。</summary>
        public bool deflectOnWall = true;

        /// <summary>命中生物后是否随机散射。</summary>
        public bool deflectOnPawn = false;

        /// <summary>生物散射的角度范围（±度）。</summary>
        public float deflectionAngle = 20f;

        // ---- 撞墙反射：法向压扁 + 切向补偿 ----
        // 不做镜面反弹，也不熄灭、不分裂。把速度拆成法向/切向：
        //   法向按恢复系数大幅衰减（几乎不往回弹），
        //   目标总速度剩余的份额全部补到切向，
        // 于是射流一撞墙就"摊平"贴着墙面滑走。
        //   掠射 → 切向本来就占多数，几乎不掉速；
        //   斜撞 → 法向被吃掉，出射角远低于镜面角；
        //   正面撞 → 切向为零，随机挑一侧转 90° 沿墙走，绝不折回发射位置。

        /// <summary>垂直入射时的法向恢复系数（0 = 完全不弹）。</summary>
        public float restitutionHeadOn = 0.1f;

        /// <summary>掠射时的法向恢复系数。</summary>
        public float restitutionGrazing = 0.6f;

        /// <summary>垂直入射后（切向补偿完）的总速度保留率。</summary>
        public float speedRetainHeadOn = 0.6f;

        /// <summary>掠射后（切向补偿完）的总速度保留率。</summary>
        public float speedRetainGrazing = 0.95f;

        /// <summary>反射后的最低速度（格/tick），防止射流停在墙上。</summary>
        public float minSpeedAfterDeflect = 0.12f;

        /// <summary>
        /// 撞墙时对法线做的随机扰动（±度）。墙面不是绝对光滑的，
        /// 法向带一点随机，切分出来的出射方向才不会每次都一模一样。
        /// </summary>
        public float deflectionNormalJitter = 10f;

        // ---- 湍流：让射流不是一条死直线 ----

        public bool turbulence = true;

        /// <summary>低频摆动的幅度（度）。这是"看起来像火焰"的主要来源。</summary>
        public float turbulenceWanderAngle = 7f;

        /// <summary>低频摆动重新选目标的间隔（tick）。</summary>
        public FloatRange turbulenceWanderInterval = new FloatRange(6f, 14f);

        /// <summary>每 tick 向当前目标角靠拢的比例（0~1），越小越柔和。</summary>
        public float turbulenceWanderLerp = 0.25f;

        /// <summary>高频抖动幅度（度），叠在低频摆动上，提供"沸腾"的细节。</summary>
        public float turbulenceJitterAngle = 2f;

        /// <summary>
        /// 出膛后多少格达到满幅湍流。枪口处射流是收束的，越远越散——
        /// 这个渐变既符合物理，也保证近距离不会因为抖动而打偏。
        /// </summary>
        public float turbulenceRampTiles = 2f;


        /// <summary>路径每格的点火概率（实际还要过地形易燃度判定）。</summary>
        public float igniteChance = 0.35f;

        public FloatRange igniteFireSize = new FloatRange(0.2f, 0.6f);

        // ---- 留油（与伤害/点燃共用同一条路径与枪口安全区） ----

        /// <summary>留下的污渍。留空时不生成。</summary>
        public ThingDef? oilFilthDef;

        /// <summary>每进入一个新格子的留油概率。</summary>
        public float oilChancePerCell = 0.25f;

        /// <summary>对非 Pawn 物体的命中概率（Pawn 恒为必中：火焰填满格子）。</summary>
        public float hitChanceOther = 0.5f;
    }

    /// <summary>
    /// 自管位置与生命周期的射流投射物。
    ///
    /// 为什么不用原版插值：
    ///   原版 Projectile 的位置是 ticksToImpact 的解析函数，所以它可以在 TickInterval(delta) 里
    ///   一次推进 15 tick 而不出错。射流需要偏转——速度方向会被碰撞事件突变，一旦突变就不再是
    ///   时间的解析函数，delta 聚合会得到完全不同的轨迹。因此这里把推进放在 Tick()，
    ///   利用"Thing.DoTick 每 tick 无条件调用 Tick()"这一点，保证差分方程严格 1 tick 一步。
    ///
    /// 为什么又保留 TickInterval 不覆盖：
    ///   base 的 TickInterval 负责 comps 的 CompTickInterval 与位置再同步，
    ///   对我们自管的 ExactPosition 来说它是幂等的，覆盖反而会漏掉 comps。
    /// </summary>
    public class JetProjectile : Projectile
    {
        private JetProjectileDef propsInt = null!;

        public JetProjectileDef Props
        {
            get
            {
                if (propsInt == null)
                {
                    propsInt = def.GetModExtension<JetProjectileDef>();
                }
                return propsInt;
            }
        }

        // ---- 自管状态（全部需要存档） ----
        protected Vector3 exactPos;           // 世界坐标，y = def.Altitude
        protected Vector3 velocity;           // 每 tick 位移
        protected float energyLeft;           // 剩余射程（格）
        protected float damageMul = 1f;
        protected int age;
        protected int maxAge;
        protected IntVec3 originCell;
        protected IntVec3 lastPathCell = IntVec3.Invalid;
        protected IntVec3 lastDeflectCell = IntVec3.Invalid;
        protected HashSet<Thing> hitThings = new HashSet<Thing>();

        // ---- 湍流状态（低频摆动的相位，需要存档） ----
        protected float initialEnergy;
        private float wanderAngle;
        private float wanderTarget;
        private int nextWanderAt;

        /// <summary>湍流的"基准方向"：摆动是相对它计算的，撞墙后重置为新方向。</summary>
        private Vector3 baseDir = Vector3.forward;

        /// <summary>
        /// 射流的最小作用范围（格）。离发射点比这更近的一律不判定，
        /// 保证贴脸、自己脚下、以及刚出膛的那一段永远不会伤到射手本人。
        /// 用 const 写死而不是走 XML：这是安全性约束，不该被数据随手改小。
        /// </summary>
        public const float MinAffectRange = 1.9f;

        /// <summary>
        /// 两次"散射点"（撞墙反射 / 生物散射的发生位置）之间的最小间距（格）。
        /// 射流在墙角来回弹时两次反弹点可能几乎重合，表现为原地抖动；
        /// 间距不足时这一次不再反射，直接熄灭。
        /// 同样用 const：几何稳定性约束，不该随数据调小。
        /// </summary>
        public const float MinDeflectionSpacing = 1.9f;

        /// <summary>碰撞采样步长（格）。0.2 与原版 CheckForFreeInterceptBetween 一致。</summary>
        public float collisionStep = 0.2f;

        /// <summary>飞行进度 0~1（已飞距离 / 总射程）。火焰场用它决定卷积核大小与热量衰减。</summary>
        public float TravelFraction =>
            initialEnergy <= 0.01f ? 0f : Mathf.Clamp01((initialEnergy - energyLeft) / initialEnergy);

        public override bool AnimalsFleeImpact => true;

        public override Vector3 ExactPosition => exactPos;

        public override Quaternion ExactRotation => Quaternion.LookRotation(velocity);

        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget,
            ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing? equipment = null, ThingDef? targetCoverDef = null)
        {
            base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);

            exactPos = origin.Yto0() + Vector3.up * def.Altitude;

            Vector3 dir = (destination - origin).Yto0();
            if (dir.sqrMagnitude < 1e-4f)
            {
                dir = Vector3.forward;
            }
            velocity = dir.normalized * def.projectile.SpeedTilesPerTick;
            baseDir = velocity.normalized;

            energyLeft = Mathf.Max(0.5f, dir.MagnitudeHorizontal());
            initialEnergy = energyLeft;
            wanderAngle = 0f;
            wanderTarget = 0f;
            nextWanderAt = 0;
            damageMul = 1f;
            age = 0;
            // 撞墙反射后速度会掉，这里按"最低可能速度"估算寿命上限，避免慢速射流被硬截断
            float slowestSpeed = Mathf.Min(def.projectile.SpeedTilesPerTick, 0.2f);
            maxAge = Mathf.CeilToInt(energyLeft / Mathf.Max(0.05f, slowestSpeed)) + 30;
            originCell = origin.ToIntVec3();

            // 关掉原版的终止条件，改由我们自己管生命周期
            ticksToImpact = int.MaxValue;
            lifetime = int.MaxValue;

            Position = exactPos.ToIntVec3();
        }

        protected override void Tick()
        {
            base.Tick();
            StepOnce();
        }

        private void StepOnce()
        {
            JetProjectileDef Props = this.Props;
            if (++age > maxAge)
            {
                Expire();
                return;
            }

            ApplyTurbulence(Props);

            Vector3 from = exactPos;
            Vector3 to = from + velocity;

            if (!to.InBounds(Map))
            {
                Expire();
                return;
            }

            if (TryInteractShield(from, to, Props))
            {
                SyncPosition();
                return;
            }

            if (TryFindHit(from, to, Props, out Thing? hitThing, out IntVec3 hitCell, out IntVec3 approachCell, out bool isWall) && hitThing != null)
            {
                hitThings.Add(hitThing);

                // 不可燃的墙不吃伤害：火焰烧不化石头/钢筋，但墙仍然阻挡并反射。
                // 生物、可燃物（木墙、家具等）照常结算。
                if (!isWall || IsFlammable(hitThing))
                {
                    // 先点火再结算伤害：伤害的连锁反应可能销毁弹体（Map 变 null）
                    TryIgniteHitCell(hitCell);
                    ApplyDamage(hitThing, Props);
                    if (Destroyed)
                    {
                        return;   // 伤害结算的连锁反应可能把弹体本身销毁掉
                    }
                }

                damageMul *= Props.damageRetainPerHit;

                bool stop = false;
                if (isWall)
                {
                    // 撞墙 → 法向压扁、切向补偿的贴墙反射。
                    // 与上一个散射点距离不足时不再反射，直接熄灭（防墙角抖动）。
                    if (!Props.deflectOnWall || !IsDeflectionSpaced(hitCell)
                        || !TryDeflectOnWall(approachCell, hitCell, Props))
                    {
                        stop = true;
                    }
                    else
                    {
                        SyncPosition();   // 偏转后本 tick 不再前进，下一 tick 沿新方向走
                        return;
                    }
                }
                else if (Props.deflectOnPawn && hitThing is Pawn && IsDeflectionSpaced(hitCell))
                {
                    TryScatter(hitCell, Props);
                    SyncPosition();
                    return;
                }

                if (stop)
                {
                    Expire(hitCell);
                    return;
                }
            }

            exactPos = to;
            SyncPosition();
            energyLeft -= velocity.MagnitudeHorizontal();
            OnEnterCell(Position, Props);

            if (energyLeft <= 0f)
            {
                Expire();
            }
        }

        /// <summary>同一 tick 内同步 Position：其他系统（护盾、别的投射物、GetThingList）都按 Thing.Position 查询。</summary>
        private void SyncPosition()
        {
            if (!Destroyed && exactPos.InBounds(Map))
            {
                Position = exactPos.ToIntVec3();
            }
        }

        /// <summary>
        /// 护盾交互：表现上按"墙"处理——射流被弹开而不是凭空消失；
        /// 但护盾没有伤害豁免：CheckIntercept 里按原版规则扣血、EMP 干扰、打破护盾都照常发生。
        /// 护盾被打碎的那一次不再反射，射流直接穿过去。
        /// </summary>
        private bool TryInteractShield(Vector3 from, Vector3 to, JetProjectileDef props)
        {
            List<Thing> interceptors = Map.listerThings.ThingsInGroup(ThingRequestGroup.ProjectileInterceptor);
            for (int i = 0; i < interceptors.Count; i++)
            {
                CompProjectileInterceptor? comp = interceptors[i].TryGetComp<CompProjectileInterceptor>();
                if (comp == null || !comp.CheckIntercept(this, from, to))
                {
                    continue;
                }

                // 护盾已被这一发打碎：不反弹，放行
                int hitPoints = comp.currentHitPoints >= 0 ? comp.currentHitPoints : comp.HitPointsMax;
                if (hitPoints <= 0)
                {
                    return false;
                }

                // 护盾是圆柱面：法线取"护盾中心 → 入射点"的径向，指向射流来的一侧
                Vector3 center = comp.parent.Position.ToVector3Shifted();
                Vector3 normal = (from - center).Yto0();

                if (!IsDeflectionSpaced(from.ToIntVec3())
                    || !TryComputeBounce(velocity, normal, props, out Vector3 bounced))
                {
                    Expire(from.ToIntVec3());   // 间距不足或无法计算反射：就地熄灭
                    return true;
                }

                velocity = bounced;
                exactPos = from;
                lastDeflectCell = from.ToIntVec3();
                damageMul *= props.damageRetainPerHit;
                AfterDeflect(props);
                return true;
            }
            return false;
        }

        /// <summary>沿 from→to 以 collisionStep 采样，返回碰到的第一个可命中目标，以及进入它所在格之前的格子。</summary>
        private bool TryFindHit(Vector3 from, Vector3 to, JetProjectileDef props,
            out Thing? hitThing, out IntVec3 hitCell, out IntVec3 approachCell, out bool isWall)
        {
            hitThing = null;
            hitCell = IntVec3.Invalid;
            isWall = false;

            Vector3 delta = to - from;
            float dist = delta.MagnitudeHorizontal();
            IntVec3 cellNow = from.ToIntVec3();
            approachCell = cellNow;
            if (dist < 1e-4f)
            {
                return false;
            }

            int steps = Mathf.Clamp(Mathf.CeilToInt(dist / Mathf.Max(0.05f, collisionStep)), 1, 16);

            for (int i = 1; i <= steps; i++)
            {
                IntVec3 cell = (from + delta * ((float)i / steps)).ToIntVec3();
                if (cell == cellNow || !cell.InBounds(Map))
                {
                    continue;
                }

                // 满格建筑优先：墙挡在人与物之前。已经撞过的墙不再判定，
                // 否则掠射反射后贴着墙走的那一 tick 会再"撞"一次、直接把射流打死。
                Building? edifice = cell.GetEdifice(Map);
                if (edifice != null && edifice.def.Fillage == FillCategory.Full
                    && !(edifice is Building_Door door && door.Open)
                    && !hitThings.Contains(edifice)
                    && !IsOwnCover(cell, props))
                {
                    hitThing = edifice;
                    hitCell = cell;
                    approachCell = cellNow;
                    isWall = true;
                    return true;
                }

                Thing? thing = FindThingAt(cell, props);
                if (thing != null)
                {
                    hitThing = thing;
                    hitCell = cell;
                    approachCell = cellNow;
                    return true;
                }

                cellNow = cell;
            }

            approachCell = cellNow;
            return false;
        }

        private Thing? FindThingAt(IntVec3 cell, JetProjectileDef props)
        {
            List<Thing> things = cell.GetThingList(Map);
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (!PassesFilters(thing))
                {
                    continue;
                }
                float chance = thing is Pawn ? 1f : props.hitChanceOther;
                if (chance < 1f && !Rand.Chance(chance))
                {
                    continue;
                }
                return thing;
            }
            return null;
        }

        /// <summary>
        /// 是不是"自己身前的掩体"（墙角探头的那面墙）。
        /// 枪口净空内的墙直接放行：射流视为已经从掩体边缘探出去，不再判定命中、也就不触发反射。
        /// </summary>
        private bool IsOwnCover(IntVec3 cell, JetProjectileDef props)
        {
            return cell.DistanceToSquared(originCell) < MinAffectRange * MinAffectRange;
        }

        /// <summary>
        /// 这次散射点是否离上一次散射点足够远（≥ MinDeflectionSpacing）。
        /// 第一次散射没有"上一次"，直接放行。
        /// </summary>
        private bool IsDeflectionSpaced(IntVec3 cell)
        {
            return lastDeflectCell == IntVec3.Invalid
                || cell.DistanceToSquared(lastDeflectCell) >= MinDeflectionSpacing * MinDeflectionSpacing;
        }

        /// <summary>
        /// 可燃性判定：不可燃的墙（石墙、钢筋墙等）不吃火焰伤害，但照样阻挡并反射射流。
        /// 用运行时 Stat 而不是 def 字段，才能正确反映"材质"（木墙 1.0 / 石墙 0）。
        /// </summary>
        private static bool IsFlammable(Thing thing)
        {
            return thing.GetStatValue(StatDefOf.Flammability) > 0.0001f;
        }

        private bool PassesFilters(Thing thing)
        {
            if (thing == null || thing == launcher)
            {
                return false;
            }
            if (thing.Destroyed || !thing.Spawned || thing.Map != Map)
            {
                return false;
            }
            if (hitThings.Contains(thing))
            {
                return false;
            }
            if (thing.def.altitudeLayer == AltitudeLayer.Item
                || thing.def.altitudeLayer == AltitudeLayer.ItemImportant
                || thing.def.altitudeLayer == AltitudeLayer.Conduits
                || thing.def.altitudeLayer == AltitudeLayer.Floor
                || thing.def.altitudeLayer == AltitudeLayer.FloorEmplacement)
            {
                return false;
            }
            if (thing.Position.DistanceToSquared(originCell) < MinAffectRange * MinAffectRange)
            {
                return false;
            }
            if (CoverUtility.ThingCovered(thing, Map))
            {
                return false;
            }
            if (thing is Pawn pawn)
            {
                if (pawn.Dead)
                {
                    return false;
                }
                if (preventFriendlyFire && launcher?.Faction != null && pawn.Faction != null
                    && !pawn.Faction.HostileTo(launcher.Faction))
                {
                    return false;
                }
                return true;
            }
            // 非生物：只有占地方的物件才拦得住射流（草、污渍之类不判定）
            return thing.def.Fillage == FillCategory.Full || thing.def.fillPercent >= 0.05f;
        }

        /// <summary>
        /// 命中目标时点燃它所在的格子。
        ///
        /// 原版 DamageWorker_Flame 只点目标本体（TryAttachFire），不点地面；而
        /// DamageDef.igniteCellChance 在原版里只被 Bullet.NotifyImpact 读取，
        /// 射流不是 Bullet，所以这条地面点燃由弹体自己负责。
        /// 概率取 igniteCellChance，是否真的起火仍由地形易燃度决定。
        /// </summary>
        private void TryIgniteHitCell(IntVec3 cell)
        {
            if (DamageDef.igniteCellChance <= 0f || !cell.InBounds(Map))
            {
                return;
            }
            if (!Rand.Chance(DamageDef.igniteCellChance))
            {
                return;
            }
            if (FireUtility.ChanceToStartFireIn(cell, Map) <= 0f)
            {
                return;
            }
            FireUtility.TryStartFireIn(cell, Map, Rand.Range(0.2f, 0.6f), launcher);
        }

        private void ApplyDamage(Thing hitThing, JetProjectileDef props)
        {
            BattleLogEntry_RangedImpact entry =
                new BattleLogEntry_RangedImpact(launcher, hitThing, intendedTarget.Thing, equipmentDef, def, targetCoverDef);
            Find.BattleLog.Add(entry);

            bool instigatorGuilty = !(launcher is Pawn p) || !p.Drafted;

            DamageInfo dinfo = new DamageInfo(
                DamageDef,
                DamageAmount * damageMul,
                ArmorPenetration,
                ExactRotation.eulerAngles.y,
                launcher,
                null,
                equipmentDef,
                DamageInfo.SourceCategory.ThingOrUnknown,
                intendedTarget.Thing,
                instigatorGuilty);
            dinfo.SetWeaponQuality(equipmentQuality);
            hitThing.TakeDamage(dinfo).AssociateWithLog(entry);

            // 不是 Bullet 子类，用不了 Notify_BulletImpact，按 stoppingPower 做等效硬直
            if (hitThing is Pawn victim && victim.stances != null && victim.BodySize <= stoppingPower + 0.001f)
            {
                victim.stances.stagger.StaggerFor(95);
            }

            if (ExtraDamages == null)
            {
                return;
            }
            foreach (ExtraDamage extra in ExtraDamages)
            {
                if (!Rand.Chance(extra.chance))
                {
                    continue;
                }
                DamageInfo extraInfo = new DamageInfo(
                    extra.def,
                    extra.amount * damageMul,
                    extra.AdjustedArmorPenetration(),
                    ExactRotation.eulerAngles.y,
                    launcher,
                    null,
                    equipmentDef,
                    DamageInfo.SourceCategory.ThingOrUnknown,
                    intendedTarget.Thing,
                    instigatorGuilty);
                extraInfo.SetWeaponQuality(equipmentQuality);
                hitThing.TakeDamage(extraInfo).AssociateWithLog(entry);
            }
        }

        /// <summary>
        /// 撞墙反射：法向压扁 + 切向补偿。
        ///
        ///   vn = v·n（朝墙里，负值），切向 vt = v − vn·n
        ///   出射法向 = |vn| × restitution(i)         —— 大幅衰减，几乎不往回弹
        ///   目标总速度 = |v| × speedRetain(i)        —— i 越大（越正撞）损失越多
        ///   出射切向 = √(目标² − 出射法向²)         —— 剩下的份额全部补到切向
        ///
        /// i 是入射角余弦（1 = 垂直命中）。于是：
        ///   掠射 → 切向本来就占多数，几乎不掉速，沿墙滑开；
        ///   斜撞 → 法向被吃掉、切向补上，出射角远低于镜面角；
        ///   正面撞 → 切向为零，随机挑一侧转 90° 沿墙走。
        /// 三种情形的出射都不含"回程"分量，所以射流不会折回发射位置。
        /// </summary>
        private bool TryDeflectOnWall(IntVec3 approachCell, IntVec3 wallCell, JetProjectileDef props)
        {
            Vector3 normal = WallNormal(approachCell, wallCell);
            if (!TryComputeBounce(velocity, normal, props, out Vector3 bounced))
            {
                return false;
            }

            velocity = bounced;
            exactPos = approachCell.ToVector3Shifted() + Vector3.up * def.Altitude;
            lastDeflectCell = wallCell;      // 记录散射点，供下一次间距检查
            damageMul *= props.damageRetainPerHit;
            AfterDeflect(props);
            return true;
        }

        /// <summary>
        /// 法向压扁 + 切向补偿的出射速度计算，墙与护盾共用。
        /// normal 指向射流来的一侧（墙/护盾的外法线）。
        ///
        ///   vn = v·n（朝里，负值），切向 vt = v − vn·n
        ///   出射法向 = |vn| × restitution(i)         —— 大幅衰减，几乎不往回弹
        ///   目标总速度 = |v| × speedRetain(i)        —— i 越大（越正撞）损失越多
        ///   出射切向 = √(目标² − 出射法向²)         —— 剩下的份额全部补到切向
        /// </summary>
        private bool TryComputeBounce(Vector3 incoming, Vector3 normal, JetProjectileDef props, out Vector3 result)
        {
            result = incoming;

            Vector3 n = normal.Yto0();
            if (n.sqrMagnitude < 1e-6f)
            {
                return false;
            }
            n = n.normalized;

            float speed = incoming.MagnitudeHorizontal();
            if (speed < 1e-4f)
            {
                return false;
            }
            Vector3 v = incoming.Yto0();

            // 表面粗糙度：扰动"法线"而不是"出射方向"——扰法线能保证出射永远落在表面外侧，
            // 扰出射方向则可能把它直接转回去。扰动后如果不再朝里（点积变正），就退回未扰动的法线。
            if (props.deflectionNormalJitter > 0f)
            {
                Vector3 perturbed = n.RotatedBy(Rand.Range(-props.deflectionNormalJitter, props.deflectionNormalJitter));
                if (Vector3.Dot(v, perturbed) < 0f)
                {
                    n = perturbed;
                }
            }

            float vNormalScalar = Vector3.Dot(v, n);                          // < 0：朝里
            float vnAbs = Mathf.Abs(vNormalScalar);
            float incidence = Mathf.Clamp01(vnAbs / speed);                   // 1 = 垂直命中，0 = 掠射

            float restitution = Mathf.Lerp(props.restitutionGrazing, props.restitutionHeadOn, incidence);
            Vector3 outNormal = n * (vnAbs * restitution);                    // 指向外侧

            float targetSpeed = speed * Mathf.Lerp(props.speedRetainGrazing, props.speedRetainHeadOn, incidence);
            float tangentMag = Mathf.Sqrt(Mathf.Max(0f, targetSpeed * targetSpeed - outNormal.sqrMagnitude));

            Vector3 vTangent = v - n * vNormalScalar;
            Vector3 tangentDir = vTangent.sqrMagnitude > 1e-6f
                ? vTangent.normalized
                : RandomWallTangent(n);                                       // 正面命中：切向为零，随机挑一侧贴面走

            Vector3 bounced = outNormal + tangentDir * tangentMag;
            if (bounced.sqrMagnitude < props.minSpeedAfterDeflect * props.minSpeedAfterDeflect)
            {
                bounced = tangentDir * props.minSpeedAfterDeflect;
            }

            result = new Vector3(bounced.x, 0f, bounced.z);
            return true;
        }

        /// <summary>反射/反弹后的公共收尾：重置基准方向与湍流相位，否则射流会被拽回撞之前的航向。</summary>
        private void AfterDeflect(JetProjectileDef props)
        {
            baseDir = velocity.normalized;
            wanderAngle = 0f;
            wanderTarget = 0f;
            nextWanderAt = age + 1;
        }


        /// <summary>取被撞面的外法线（指向射流来的一侧）。斜角进入时按速度分量较大的轴判定是撞了哪一面。</summary>
        private Vector3 WallNormal(IntVec3 approachCell, IntVec3 wallCell)
        {
            IntVec3 d = wallCell - approachCell;
            if (d.x != 0 && d.z != 0)
            {
                return Mathf.Abs(velocity.x) >= Mathf.Abs(velocity.z)
                    ? new Vector3(-Mathf.Sign(velocity.x), 0f, 0f)
                    : new Vector3(0f, 0f, -Mathf.Sign(velocity.z));
            }
            if (d.x != 0)
            {
                return new Vector3(-Mathf.Sign(d.x), 0f, 0f);
            }
            if (d.z != 0)
            {
                return new Vector3(0f, 0f, -Mathf.Sign(d.z));
            }
            return Vector3.zero;
        }

        /// <summary>
        /// 正面命中时切向为零，随机挑一个与墙面平行的水平方向贴墙走。
        /// 这是"法向压扁 + 切向补偿"在入射角 90° 时的极限情形：
        /// 出射几乎完全落在墙面上，所以射流不会折回发射位置。
        /// </summary>
        private Vector3 RandomWallTangent(Vector3 normal)
        {
            Vector3 tangent = Vector3.Cross(Vector3.up, normal);
            if (tangent.sqrMagnitude < 1e-6f)
            {
                tangent = Vector3.forward;
            }
            tangent = tangent.normalized;
            if (!Rand.Bool)
            {
                tangent = -tangent;
            }
            JetProjectileDef? def2 = Props;
            if (def2 != null && def2.deflectionNormalJitter > 0f)
            {
                tangent = tangent.RotatedBy(Rand.Range(-def2.deflectionNormalJitter, def2.deflectionNormalJitter));
            }
            return tangent;
        }

        private void TryScatter(IntVec3 cell, JetProjectileDef props)
        {
            float angle = Rand.Range(-props.deflectionAngle, props.deflectionAngle);
            velocity = velocity.RotatedBy(angle);
            lastDeflectCell = cell;
            damageMul *= props.damageRetainPerHit;
            AfterDeflect(props);
        }

        /// <summary>
        /// 湍流：每 tick 把速度方向转一个小角度。
        ///
        /// 纯白噪声（每 tick 独立随机）看起来是"抖动"，不是"流动"——随机游走的横向偏移只按 √t 增长，
        /// 24 tick 下来还不到半格，肉眼几乎看不出来。所以这里用两层：
        ///   · 低频摆动 wander：每 6~14 tick 重选一个目标角，逐 tick 向它靠拢。
        ///     方向在一段时间内基本一致，横向偏移按 t 增长，才有"火焰在飘"的形状；
        ///   · 高频抖动 jitter：每 tick 独立 ±几度，提供"沸腾"的细节。
        /// 两层都乘以一个随飞行距离 0→1 的强度，枪口处收束、越远越散。
        /// 旋转保模，所以速度大小不变，energyLeft 的消耗速率也不变。
        /// </summary>
        private void ApplyTurbulence(JetProjectileDef props)
        {
            if (!props.turbulence)
            {
                return;
            }

            float travelled = initialEnergy - energyLeft;
            float intensity = props.turbulenceRampTiles <= 0.01f
                ? 1f
                : Mathf.Clamp01(travelled / props.turbulenceRampTiles);
            if (intensity <= 0f)
            {
                return;
            }

            if (age >= nextWanderAt)
            {
                wanderTarget = Rand.Range(-props.turbulenceWanderAngle, props.turbulenceWanderAngle);
                nextWanderAt = age + Mathf.Max(1, Mathf.RoundToInt(props.turbulenceWanderInterval.RandomInRange));
            }
            wanderAngle = Mathf.Lerp(wanderAngle, wanderTarget, Mathf.Clamp01(props.turbulenceWanderLerp));

            float jitter = props.turbulenceJitterAngle > 0f
                ? Rand.Range(-props.turbulenceJitterAngle, props.turbulenceJitterAngle)
                : 0f;

            // 关键：是"转向某个目标偏角"，不是"每 tick 转一个固定角度"。
            // 后者等于恒定角速度，会一路累积——wander=6° 时 24 tick 能累计上百度的偏转，
            // 射流会绕成弧线。这里把当前偏角与目标偏角的差乘一个增益，形成弹簧式收敛，
            // 偏角始终被限制在 turbulenceWanderAngle 附近，不会漂走。
            // 出膛渐变直接乘在"目标偏角"上，所以枪口附近幅度本身就小。
            float desired = (wanderAngle + jitter) * intensity;
            float currentOffset = Vector3.SignedAngle(baseDir, velocity, Vector3.up);
            float gain = Mathf.Clamp01(props.turbulenceWanderLerp);
            velocity = velocity.RotatedBy((desired - currentOffset) * gain);
        }

        /// <summary>
        /// 射流进入新格子时的地面效果：留油 + 点燃。
        ///
        /// 与伤害判定共用同一条规矩：**枪口 MinAffectRange 之内一律不生效**，
        /// 否则射流的第一格就是射手脚下。
        /// </summary>
        private void OnEnterCell(IntVec3 cell, JetProjectileDef props)
        {
            if (cell == lastPathCell || !cell.InBounds(Map))
            {
                return;
            }
            lastPathCell = cell;

            // 枪口安全区：既不撒油也不点火
            if (cell.DistanceToSquared(originCell) < MinAffectRange * MinAffectRange)
            {
                return;
            }

            if (props.oilChancePerCell > 0f && cell.WalkableByAny(Map) && Rand.Chance(props.oilChancePerCell))
            {
                ThingDef? filth = props.oilFilthDef;
                if (filth != null)
                {
                    FilthMaker.TryMakeFilth(cell, Map, filth, 1);
                }
            }

            if (!Rand.Chance(props.igniteChance))
            {
                return;
            }
            if (FireUtility.ChanceToStartFireIn(cell, Map) <= 0f)
            {
                return;
            }
            FireUtility.TryStartFireIn(cell, Map, props.igniteFireSize.RandomInRange, launcher);
        }

        private void Expire(IntVec3? cell = null)
        {
            if (Destroyed)
            {
                return;
            }
            if (cell.HasValue && cell.Value.InBounds(Map))
            {
                Position = cell.Value;
            }
            if (def.projectile.landedEffecter != null)
            {
                def.projectile.landedEffecter.Spawn(Position, Map).Cleanup();
            }
            Destroy();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref exactPos, "exactPos");
            Scribe_Values.Look(ref velocity, "velocity");
            Scribe_Values.Look(ref energyLeft, "energyLeft");
            Scribe_Values.Look(ref damageMul, "damageMul", 1f);
            Scribe_Values.Look(ref age, "age", 0);
            Scribe_Values.Look(ref maxAge, "maxAge", 0);
            Scribe_Values.Look(ref originCell, "originCell");
            Scribe_Values.Look(ref lastPathCell, "lastPathCell");
            Scribe_Values.Look(ref lastDeflectCell, "lastDeflectCell");
            Scribe_Values.Look(ref initialEnergy, "initialEnergy");
            Scribe_Values.Look(ref wanderAngle, "wanderAngle");
            Scribe_Values.Look(ref wanderTarget, "wanderTarget");
            Scribe_Values.Look(ref nextWanderAt, "nextWanderAt", 0);
            Scribe_Values.Look(ref baseDir, "baseDir");
            Scribe_Collections.Look(ref hitThings, "hitThings", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && hitThings == null)
            {
                hitThings = new HashSet<Thing>();
            }
        }
    }
    public class JetProjectile_FlameField : JetProjectile
    {
        private FlameFieldComponent flameFieldComponentInt = null!;
        public FlameFieldComponent FlameFieldComponent
        {
            get
            {
                if (flameFieldComponentInt == null)
                {
                    flameFieldComponentInt = Map.GetComponent<FlameFieldComponent>();
                }
                return flameFieldComponentInt;
            }
        }
        /// <summary>
        /// 把本 tick 的速度与热度撒进地图级速度场（场归地图所有，弹体消失后仍在衰减）。
        ///
        /// 枪口 MinAffectRange 之内不撒：那段距离既不造成伤害、不留油也不点火，
        /// 那么速度场也不该在那里积累，否则会看到"火焰从发射者身上冒出来"。
        /// </summary>
        private void SplatFlameField()
        {
            Vector3 originPos = originCell.ToVector3();
            float dx = exactPos.x - originPos.x;
            float dz = exactPos.z - originPos.z;
            if (dx * dx + dz * dz < MinAffectRange * MinAffectRange)
            {
                return;
            }
            FlameFieldComponent.Splat(exactPos, velocity, TravelFraction);
        }
        protected override void Tick()
        {
            base.Tick();
            if (!Destroyed)
            {
                SplatFlameField();
            }
        }
    }
}
