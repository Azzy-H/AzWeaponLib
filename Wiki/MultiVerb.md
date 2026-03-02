# MultiVerb
***
## 目的
***
  实现武器拥有多种可切换的verb。如果在VEF环境中且Features.Feature_ExtraEquipmentVerbs处于激活状态则不使用该系统，转而使用MVCF的控制逻辑，同样能实现功能（傻逼MVCF我还关不掉它）。
## 例程
***
```
    <verbs>
      <li Class="SRM.VerbProperties_Psychic">
        <verbClass>SRM.Verb_ShootPsychic</verbClass>
        <hasStandardCommand>true</hasStandardCommand>
        <defaultProjectile>Bullet_TT2shoot_Collect</defaultProjectile>
        <warmupTime>0.3</warmupTime>
        <range>25.9</range>
        <soundCast>Shot_NeedleGun</soundCast>
        <soundCastTail>GunTail_Light</soundCastTail>
        <muzzleFlashScale>9</muzzleFlashScale>
        <psychicLevel>6</psychicLevel>
        <entropyGain>10</entropyGain>
        <psyfocusCost>0.02</psyfocusCost>
      </li>
      <li Class="AzWeaponLib.AmmoSystem.VerbProperties_ShootWithAmmo">
        <verbClass>AzWeaponLib.AmmoSystem.Verb_ShootWithAmmo</verbClass>
        <hasStandardCommand>true</hasStandardCommand>
        <defaultProjectile>Bullet_TT2shoot</defaultProjectile>
        <warmupTime>1.2</warmupTime>
        <range>25.9</range>
        <ticksBetweenBurstShots>10</ticksBetweenBurstShots>
        <burstShotCount>1</burstShotCount>
        <bulletsPerShot>5</bulletsPerShot>
        <ammoCostPerShot>5</ammoCostPerShot>
        <soundCast>Shot_NeedleGun</soundCast>
        <soundCastTail>GunTail_Medium</soundCastTail>
        <muzzleFlashScale>9</muzzleFlashScale>
      </li>
    </verbs>
    <comps>
      <li Class="SRM.CompProperties_SPAmmo">
        <ammunitionDef>SR_SoildTime</ammunitionDef>
        <ammoCountPerAmmunitionBox>10</ammoCountPerAmmunitionBox>
      </li>
      <li Class="AzWeaponLib.MultiVerb.CompProperties_MultiVerb">
        <compClass>AzWeaponLib.MultiVerb.CompMultiVerbByHediff</compClass>
        <verbInfos>
          <li>
            <iconPath>Icons/Ability/Weapons/TT2_Standard</iconPath>
            <defaultLabel>standard mode</defaultLabel>
            <defaultDesc>Using psychic abilities to assemble bullets</defaultDesc>
          </li>
          <li>
            <iconPath>Icons/Ability/Weapons/TT2_Overclock</iconPath>
            <defaultLabel>overclock mode</defaultLabel>
            <defaultDesc>Using soild time to assemble bullets</defaultDesc>
            <styleDef>Horaxian_Axe</styleDef>
            <verbSwitchWorker>AzWeaponLib.MultiVerb.VerbSwitchWorker_NoBackupAmmo</verbSwitchWorker>
          </li>
        </verbInfos>
      </li>
    </comps>
```
## 字段含义
***
  |字段|含义|
  |:--:|:--:|
  |verbInfos|模式的显示数据|
  |defaultLabel|模式的Label（会在gizmo和info页显示）|
  |defaultDesc|模式的Desc（会在gizmo和info页显示）|
  |styleDef|当前模式的对应style|
  |verbSwitchWorker|模式切换逻辑，支持verb回调/tick查询，缺省值为不被自动选择，若全为缺省值则禁止自动切换|
## 注意事项
***
  gizmoInfo的数量应与verb的数量一致。

  gizmo绘制是通过hediff实现的，若有mod清除了对应hediff将会使得显示失效，无法切换/自动切换，重新装备能修复。
  
  考虑到武器掉落清除hediff曾导致邪教徒吟诵袭击发生递归错误（本mod已修复），如遇到相似情况可向我汇报修复。

