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
        <speedRangeOverride>28~32</speedRangeOverride>
        <hitChance>1</hitChance>
        <homingSpeed>0.07</homingSpeed>
        <initRotateAngle>45</initRotateAngle>
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
  |proximityFuseRange|近炸范围（通常用于高角或是和下方锥形爆炸配合，若有数值则击中目标的概率可简单视为1）|
  |destroyTicksAfterLosingTrack|丢失目标后的自毁时间（当目标和当前方向的夹角大于90°时丢失目标）|
  |extraProjectile|朝当前格或者击中目标射击额外的投射物，用于生成特殊效果（如电弧导弹或者气体导弹）|
  |speedChangePerTick|速度变化率，用于实现中段加速|
  |speedRangeOverride|速度分布，用于一次性发射多枚导弹时从视觉上将其分开，缺省则用ProjectileProperties数据|
  
  1. ModExtension_Cone

  |字段|含义|
  |:--:|:--:|
  |coneAngle|锥形爆炸角度|
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

## 注意事项
***
  导弹要注意是否为高角。


