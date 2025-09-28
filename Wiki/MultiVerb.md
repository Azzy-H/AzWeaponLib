# MultiVerb
***
## 目的
***
  实现武器拥有多种可切换的verb。如果在VEF环境中且Features.Feature_ExtraEquipmentVerbs处于激活状态则不使用该系统，转而使用MVCF的控制逻辑，同样能实现功能（傻逼MVCF我还关不掉它）。
## 例程
***
```
<verbs>
  <li Class="AzWeaponLib.AmmoSystem.VerbProperties_ShootWithAmmo">
    <verbClass>AzWeaponLib.AmmoSystem.Verb_ShootWithAmmo</verbClass>
    <hasStandardCommand>true</hasStandardCommand>
    <defaultProjectile>Bullet_A17RRshoot</defaultProjectile>
    <warmupTime>1.2</warmupTime>
    <range>37.9</range>
    <ticksBetweenBurstShots>10</ticksBetweenBurstShots>
    <burstShotCount>6</burstShotCount>
    <soundCast>SR_RailLight</soundCast>
    <soundCastTail>GunTail_Medium</soundCastTail>
    <muzzleFlashScale>9</muzzleFlashScale>
  </li>
  <li Class="AzWeaponLib.AmmoSystem.VerbProperties_ShootWithAmmo">
    <verbClass>AzWeaponLib.AmmoSystem.Verb_ShootWithAmmo</verbClass>
    <hasStandardCommand>true</hasStandardCommand>
    <defaultProjectile>Bullet_A17RRKshoot_FAP</defaultProjectile>
    <warmupTime>1.2</warmupTime>
    <range>37.9</range>
    <ticksBetweenBurstShots>10</ticksBetweenBurstShots>
    <burstShotCount>6</burstShotCount>
    <soundCast>SR_RailLight</soundCast>
    <soundCastTail>GunTail_Medium</soundCastTail>
    <muzzleFlashScale>9</muzzleFlashScale>
  </li>
</verbs>
<comps>
  <li Class="AzWeaponLib.MultiVerb.CompProperties_MultiVerb">
    <compClass>AzWeaponLib.MultiVerb.CompMultiVerbByHediff</compClass>
    <gizmoInfos>
      <li>
        <iconPath>Icons/Ability/Weapons/pierce</iconPath>
        <defaultLabel>railgun bullet</defaultLabel>
        <defaultDesc>Shoot with armor-piercing magnetic track projectiles that have high penetration but low aftereffect</defaultDesc>
      </li>
      <li>
        <iconPath>Icons/Ability/Weapons/sharp</iconPath>
        <defaultLabel>ceramic bullet</defaultLabel>
        <defaultDesc>Shoot with fragile ceramic projectiles that have low penetration but high aftereffect</defaultDesc>
      </li>
    </gizmoInfos>
  </li>
</comps>
```
## 字段含义
***
  |字段|含义|
  |:--:|:--:|
  |gizmoInfos|模式的显示数据|
  |defaultLabel|模式的Label（会在gizmo和info页显示）|
  |defaultDesc|模式的Desc（会在gizmo和info页显示）|
## 注意事项
***
  gizmoInfo的数量应与verb的数量一致。

  gizmo绘制是通过hediff实现的，若有mod清除了对应hediff将会使得显示失效，但不影响功能，重新装备也能修复。
  
  考虑到武器掉落清除hediff曾导致邪教徒吟诵袭击发生递归错误（本mod已修复），如遇到相似情况可向我汇报修复。

