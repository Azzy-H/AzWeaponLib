# SpecialProjectile
***
## 目的
***
  某些我认为可能会复用的特殊投射物
## 例程
***
  - 电弧
  ```
  <ThingDef ParentName="BaseBullet" Name="SR_ElectricArcBase">
    <defName>SR_ElectricArc</defName>
    <label>Electric Arc</label>
    <graphicData>
      <texPath>Things/NullDraw</texPath>
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <projectile>
      <damageDef>Burn</damageDef>
      <speed>200</speed>
      <damageAmountBase>45</damageAmountBase>
      <armorPenetrationBase>1</armorPenetrationBase>
      <extraDamages>
        <li>
          <def>EMP</def>
          <amount>15</amount>
        </li>
        <li>
          <def>Stun</def>
          <amount>15</amount>
          <chance>0.3</chance>
        </li>
      </extraDamages>
    </projectile>
    <thingClass>AzWeaponLib.SpecialProjectile.ElectricArc</thingClass>
    <modExtensions>
      <li Class="AzWeaponLib.SpecialProjectile.ElectricArcDef">
        <noDeviation>true</noDeviation>        <!-- 无偏 -->
        <conductChance>0.75</conductChance>        <!-- 基础传导几率 -->
        <conductRange>2.9</conductRange>        <!-- 基础传导距离 -->
        <conductChanceExtra>1.0</conductChanceExtra>        <!-- 特殊情况下传导几率 -->
        <conductRangeExtra>6.9</conductRangeExtra>        <!-- 特殊情况下传导几率 -->
        <damageMultiplierExtra>2.0</damageMultiplierExtra>        <!-- 特殊情况下伤害乘数 -->
        <penetrationMultiplierExtra>2.0</penetrationMultiplierExtra>        <!-- 特殊情况下穿甲乘数 -->
        <damageMultiplierPerConduct>0.8</damageMultiplierPerConduct>        <!-- 每次传导衰减至 -->
        <conductTicks>6</conductTicks>        <!-- 传导时间 -->
        <fleckDef>LineEMP</fleckDef>        <!-- 传导特效 -->
        <hediffDefsToExtra>          <!-- 特殊情况 -->
          <li>WaterInEyes</li>
        </hediffDefsToExtra>
        <weatherDefsToExtra>          <!-- 特殊情况 -->
          <li>Rain</li>
          <li>RainyThunderstorm</li>
          <li>FoggyRain</li>
          <li MayRequire="Ludeon.RimWorld.Anomaly">BloodRain</li>
        </weatherDefsToExtra>        <!-- 特殊情况 -->
        <gameConditionDefsToExtra>
          <li>Flashstorm</li>
        </gameConditionDefsToExtra>
      </li>
    </modExtensions>
  </ThingDef>
  ```
  - 导弹
  ```
  <ThingDef ParentName="SR_MissleBase">
    <defName>SR_Missle_DMG</defName>
    <label>missle</label>
    <graphicData>
      <texPath>Things/Projectile/minimissle_HE</texPath>
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <thingClass>AzWeaponLib.SpecialProjectile.Projectile_Homing_Explosive</thingClass>
    <projectile>
      <damageDef>SR_MissleExplosion</damageDef>
      <damageAmountBase>180</damageAmountBase>
      <armorPenetrationBase>2</armorPenetrationBase>
      <stoppingPower>5</stoppingPower>
    </projectile>
    <modExtensions>
      <li Class="AzWeaponLib.SpecialProjectile.HomingProjectileDef">
        <speedRangeOverride>24~36</speedRangeOverride>
        <hitChance>1</hitChance>
        <homingSpeed>0.07</homingSpeed>
        <initRotateAngle>15</initRotateAngle>
        <homingRotateErrorRange>15</homingRotateErrorRange>
        <recalculateRotateErrorTick>20</recalculateRotateErrorTick>
        <speedChangePerTick>0.4</speedChangePerTick>
        <proximityFuseRange>1.9</proximityFuseRange><!--近炸距离，大于0为近炸投射物-->
        <destroyTicksAfterLosingTrack>0~2</destroyTicksAfterLosingTrack>
      </li>
      <li Class="AzWeaponLib.SpecialProjectile.ModExtension_Cone">
        <repeatExplosionCount>1</repeatExplosionCount>
        <coneAngle>15</coneAngle>
        <coneRange>4</coneRange>
        <fragment>SR_Bullet_Fragment_Small</fragment>
        <fragmentCount>20</fragmentCount>
        <fragmentRange>2.9~8.9</fragmentRange>
      </li>
    </modExtensions>
  </ThingDef>
  ```
  - 穿透投射物
  ```
    <ThingDef ParentName="BaseBullet">
    <defName>Bullet_A17RRshoot</defName>
    <label>Railgun Bullet</label>
    <graphicData>
      <texPath>Things/Projectile/ChargeLanceShot</texPath>
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <projectile>
      <damageDef>Bullet</damageDef>
      <speed>200</speed>
      <damageAmountBase>11</damageAmountBase>
      <armorPenetrationBase>0.49</armorPenetrationBase>
    </projectile>
    <thingClass>AzWeaponLib.SpecialProjectile.PiercingProjectile</thingClass>
    <modExtensions>
      <li Class="AzWeaponLib.SpecialProjectile.PiercingProjectileDef">
        <rangeOverride IsNull="true" />
        <!-- 强制子弹射程 -->
        <penetratingPower>4</penetratingPower>        <!-- 可以对多少thing造成伤害 -->
        <reachMaxRangeAlways>true</reachMaxRangeAlways>        <!-- 始终到达射程极限 -->
        <minDistanceToAffectAlly>3.9</minDistanceToAffectAlly>        <!-- 友军无伤范围 -->
        <minDistanceToAffectAny>1.1</minDistanceToAffectAny>        <!-- 最短作用范围 -->
        <penetratingPowerCostByShield>11111</penetratingPowerCostByShield>        <!-- 碰到护盾的能量削减 -->
        <alwaysHitStandingEnemy>true</alwaysHitStandingEnemy>        <!-- 无视敌方体型修正 -->
      </li>
    </modExtensions>
  </ThingDef>
  ```
  - 火焰射流
  ```
  <ThingDef ParentName="BaseBullet">
    <defName>SEA_FlameBullet</defName>
    <label>flame jet</label>
    <thingClass>AzWeaponLib.SpecialProjectile.JetProjectile_FlameField</thingClass>
    <graphicData>
      <texPath>Blank/Blank</texPath>
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <projectile>
      <damageDef>SEA_FlameAP</damageDef>
      <speed>45</speed>
      <damageAmountBase>8</damageAmountBase>
      <armorPenetrationBase>1.4</armorPenetrationBase>
      <screenShakeFactor>0</screenShakeFactor>
    </projectile>
    <modExtensions>
      <li Class="AzWeaponLib.SpecialProjectile.JetProjectileDef">
        <!-- 穿透：不限数量，每命中一个目标伤害 ×0.85 自然衰减 -->
        <damageRetainPerHit>0.85</damageRetainPerHit>

        <!-- 撞墙/护盾反射：法向压扁 + 切向补偿。
             法向按恢复系数衰减（几乎不往回弹），目标总速度剩下的份额全补到切向，
             所以射流一撞墙就"摊平"贴墙滑走：
               掠射 → 几乎不掉速；斜撞 → 出射角远低于镜面角；
               正面撞 → 切向为零，随机挑一侧转 90° 沿墙走，绝不折回发射位置。
             反射次数不限，终止条件只有"射程耗尽"与"两次散射点间距不足"。 -->
        <deflectOnWall>true</deflectOnWall>
        <deflectOnPawn>false</deflectOnPawn>
        <deflectionAngle>20</deflectionAngle>
        <restitutionHeadOn>0.1</restitutionHeadOn>      <!-- 垂直命中几乎无法向恢复 -->
        <restitutionGrazing>0.6</restitutionGrazing>
        <speedRetainHeadOn>0.6</speedRetainHeadOn>      <!-- 切向补偿后保留的总速度 -->
        <speedRetainGrazing>0.95</speedRetainGrazing>
        <minSpeedAfterDeflect>0.12</minSpeedAfterDeflect>
        <!-- 墙面不是绝对光滑：扰法线 ±4°，切分出来的出射方向才不会每次都一样 -->
        <deflectionNormalJitter>4</deflectionNormalJitter>

        <!-- 湍流：以"基准航向"为轴来回摆，幅度 = turbulenceWanderAngle（不会累积）。
             6~14 tick 换一次目标偏角，出膛 2 格内按距离渐入。 -->
        <turbulence>true</turbulence>
        <turbulenceWanderAngle>4</turbulenceWanderAngle>
        <turbulenceWanderInterval>6~14</turbulenceWanderInterval>
        <turbulenceWanderLerp>0.25</turbulenceWanderLerp>
        <turbulenceJitterAngle>1.2</turbulenceJitterAngle>
        <turbulenceRampTiles>2</turbulenceRampTiles>

        <!-- 沿路径点燃地面：0.35/格，实际还要过地形易燃度 -->
        <igniteChance>0.35</igniteChance>
        <igniteFireSize>0.2~0.6</igniteFireSize>

        <!-- 留油：由弹体处理，与伤害/点燃共用同一条路径与枪口安全区
             （枪口 1.9 格内既不留油也不点火）。 -->
        <oilFilthDef>Filth_Fuel</oilFilthDef>
        <oilChancePerCell>0.25</oilChancePerCell>

        <!-- 火焰填满格子：Pawn 恒为必中（代码固定），这里只剩非生物的概率 -->
        <hitChanceOther>0.5</hitChanceOther>
      </li>
    </modExtensions>
  </ThingDef>
  ```
## 字段含义
***
  - 电弧
  
  |字段|含义|
  |:--:|:--:|
  |conductChance|能继续传导的概率|
  |conductRange|传导索敌半径|
  |maxConductNum|最大传导数|
  |conductChanceExtra|能继续传导的概率（满足特殊条件情况下）|
  |conductRangeExtra|传导索敌半径（满足特殊条件情况下）|
  |maxConductNumExtra|最大传导数（满足特殊条件情况下）|
  |damageMultiplierExtra|伤害倍率（满足特殊条件情况下）|
  |penetrationMultiplierExtra|穿透倍率（满足特殊条件情况下）|
  |damageMultiplierPerConduct|伤害衰减乘数|
  |conductTicks|传导间隔|
  |noDeviation|是否采用原版子弹偏移，true为不采用|
  |fleckDef|传导特效|
  |hediffDefsToExtra|目标若有该hediff则满足特殊条件（或运算）|
  |weatherDefsToExtra|特殊条件天气（或运算）|
  |gameConditionDefsToExtra|特殊条件环境（或运算）|
  - 导弹
  1. HomingProjectileDef

  |字段|含义|
  |:--:|:--:|
  |hitChance|该导弹能直接击中目标的概率（高角为0）|
  |homingSpeed|导弹调整角度的速率，若大于1会震荡|
  |initRotateAngle|导弹初始偏转角|
  |homingRotateErrorRange|导弹寻踪最大误差角，用于实现移动过程中的折线，过大可能导致无法击中目标，缺省为禁用|
  |recalculateRotateErrorTick|导弹重寻踪时间，配合homingRotateErrorRange实现移动过程中的折线，缺省为禁用|
  |proximityFuseRange|近炸范围（通常用于高角或是和下方锥形爆炸配合，若有数值则击中目标的概率可简单视为1）|
  |destroyTicksAfterLosingTrack|丢失目标后的自毁时间（当目标和当前方向的夹角大于90°时丢失目标）|
  |extraProjectile|朝当前格或者击中目标射击额外的投射物，用于生成特殊效果（如电弧导弹或者气体导弹）|
  |speedChangePerTick|速度变化率，用于实现中段加速|
  |speedRangeOverride|速度分布，用于一次性发射多枚导弹时从视觉上将其分开，缺省则用ProjectileProperties数据|
  
  2. ModExtension_Cone

  |字段|含义|
  |:--:|:--:|
  |coneAngle|锥形爆炸角度，缺省为Null，为圆形范围|
  |coneRange|锥形爆炸半径|
  |repeatExplosionCount|爆炸重复次数，可以将高数值爆炸裁成多次低数值爆炸降低致死率|
  |fragment|弹片，通常和穿透投射物或者爆炸投射物一同使用|
  |fragmentCount|弹片数量|
  |fragmentRange|弹片目标半径分布|
  |showConeEffect|显示爆炸特效|
  - 穿透投射物
  
  |字段|含义|
  |:--:|:--:|
  |penetratingPower|穿透力|
  |reachMaxRangeAlways|总是到达射程极限，用于武器射击|
  |rangeOverride|射程极限，用于技能释放|
  |minDistanceToAffectAlly|击中友军判定豁免半径（和原版击中判定分开判定）|
  |minDistanceToAffectAny|击中判定豁免半径（和原版击中判定分开判定）|
  |penetratingPowerCostByShield|击中护盾/墙壁的穿透力损失|
  |alwaysHitStandingEnemy|站立敌人必定击中（否则采用原版流弹概率判定）|
  - 火焰射流
  1. JetProjectileDef
  
  |字段|含义|
  |:--:|:--:|
  |damageRetainPerHit|每次命中目标或发生一次反射后的伤害乘数（贯穿与反射共用同一数值）|
  |deflectOnWall|撞到满格建筑时是否反射，false 则直接熄灭|
  |deflectOnPawn|命中生物后是否随机散射（火焰一般 false）|
  |deflectionAngle|生物散射的角度范围（±度）|
  |restitutionHeadOn|垂直入射时的法向恢复系数，0 表示完全不往回弹|
  |restitutionGrazing|掠射时的法向恢复系数|
  |speedRetainHeadOn|垂直入射反射后保留的总速度比例|
  |speedRetainGrazing|掠射反射后保留的总速度比例|
  |minSpeedAfterDeflect|反射后的最低速度（格/tick），防止射流停在墙上|
  |deflectionNormalJitter|撞墙时对法线施加的随机扰动（±度），模拟墙面粗糙|
  |turbulence|是否启用湍流|
  |turbulenceWanderAngle|低频摆动幅度（度），"看起来像火"的主要来源|
  |turbulenceWanderInterval|低频摆动重新选目标的间隔（tick）|
  |turbulenceWanderLerp|每 tick 向目标偏角靠拢的比例（0~1），越小越柔和|
  |turbulenceJitterAngle|高频抖动幅度（度），叠在低频摆动上提供"沸腾"细节|
  |turbulenceRampTiles|出膛后多少格达到满幅湍流，枪口收束、越远越散|
  |igniteChance|路径每格的点火概率（还会再乘地形易燃度判定）|
  |igniteFireSize|点燃时生成的火焰大小|
  |oilFilthDef|路径留下的污渍，留空则不生成|
  |oilChancePerCell|每进入一个新格子的留油概率|
  |hitChanceOther|对非 Pawn 物体的命中概率（Pawn 恒为必中）|

  2. 类内字段与常量（不走 XML）

  |字段|含义|
  |:--:|:--:|
  |collisionStep = 0.2|碰撞采样步长（格），公有字段，默认与原版 CheckForFreeInterceptBetween 一致|
  |MinAffectRange = 1.9|枪口安全区。更近的目标不判定命中、不留油、不点火、不写火焰场|
  |MinDeflectionSpacing = 1.9|两次散射点（撞墙反射 / 生物散射）的最小间距，防止在墙角来回抖动|

  3. thingClass 选择

  |thingClass|行为|
  |:--:|:--:|
  |AzWeaponLib.SpecialProjectile.JetProjectile|飞行、碰撞、穿透伤害、留油、点燃|
  |AzWeaponLib.SpecialProjectile.JetProjectile_FlameField|在上面基础上把速度与热量写入地图级火焰速度场（AzWeaponLib.VFX.FlameFieldComponent）|

## 注意事项
***
  导弹要注意是否为高角。
  反射采用"法向压扁 + 切向补偿"而不是镜面反射：正面撞墙时射流沿墙面转 90° 走掉，不会折回发射位置；
  弹性碰撞就调高 restitutionHeadOn，非弹性碰撞就调低 speedRetainHeadOn。


