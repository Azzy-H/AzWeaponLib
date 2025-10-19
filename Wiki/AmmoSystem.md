# AmmoSystem
***
## 目的
***
  实现需要装填的武器（可配置是否消耗弹药），用于给武器一个新的平衡维度，或是避开傻逼泰南的乱开枪+射击专家的神秘0前摇。
## 例程
***
  下列均为缺省值
```
<statBases>
  <AWL_AmmoCapacity>0</AWL_AmmoCapacity>
  <AWL_BackAmmoCapacity>0</AWL_BackAmmoCapacity>
  <AWL_ReloadingTime>0</AWL_ReloadingTime>
</statBases>
<comps>
  <li Class="AzWeaponLib.AmmoSystem.CompProperties_Ammo">
    <singleShotLoading>false</singleShotLoading>
    <canLoadExtra>false</canLoadExtra>
    <pawnStatsAffectReloading>true</pawnStatsAffectReloading>
    <ammunitionDef IsNull="true" />
    <exhaustable>false</exhaustable>
    <exhaustedDef IsNull="true" />
    <ammoCountPerAmmunitionBox>3</ammoCountPerAmmunitionBox>
    <canMoveWhenReload>false</canMoveWhenReload>
  </li>
</comps>
```
## 字段含义
***
  |StatDef|含义|
  |:--:|:--:|
  |AWL_AmmoCapacity|弹匣容量|
  |AWL_BackAmmoCapacity|最大备弹数|
  |AWL_ReloadingTime|装填时间，单位为秒|
***
  |字段|含义|
  |:--:|:--:|
  |singleShotLoading|逐发装填，当为true时每次装填仅装填一发子弹进入弹匣。空弹自动装填数量为单轮射击弹药消耗数，其余情况装填至满|
  |canLoadExtra|是否允许额外装填一发，用于模拟枪机能多容纳一发的情况|
  |pawnStatsAffectReloading|装填速度是否受单位质量影响，为true时单位的操作能力和射击技能会影响装填速度|
  |exhaustable|是否为可抛弃武器，当为true时弹匣空时将会被摧毁|
  |exhaustedDef|当exhaustable为true时，摧毁时会产生的物品，若为可装备物品则会生成在单位手上|
  |ammunitionDef|装填弹药所消耗的物品，当非Null时，启用备用弹药系统|
  |ammoCountPerAmmunitionBox|每个弹药物品给与的弹药数量|
  |canMoveWhenReload|是否可移动装填|
## 重要方法/属性
***
  ```
  protected virtual Gizmo GetAmmoStatusGizmo()
  ```
  通过继承重写该方法，Gizmo_AmmoStatus或使用自己的显示gizmo以及可以实现自定义面板显示
  example：
  - [SRC]Miho,Star Ring Corporation: 
  ```SRM.SPCompAmmo```
  - HellDivers Weapon Pack: 
  ```HDWeapon.CompC4Ammo```
  ```HDWeapon.CompLaserAmmo```

  ```
  public virtual int BackupAmmo
  public virtual int maxAmmoNeeded
  public virtual bool needReloadBackupAmmo
  ```
  通过继承重写该属性，可以实现自定义的备用弹药源
  example：
  - [SRC]Miho,Star Ring Corporation: 
  ```SRM.SPCompAmmo```
  - HellDivers Weapon Pack: 
  ```HDWeapon.CompAmmoSourceable```
## 注意事项
***
  下列字段已经废弃，若使用会自动兼容为statBases
  |字段|含义|
  |:--:|:--:|
  |ammunitionCapacity|弹匣容量|
  |maxBackupAmmo|最大备弹数|
  |reloadingTime|装填时间，单位为秒|
  
  gizmo绘制是通过hediff实现的，若有mod清除了对应hediff将会使得显示失效，但不影响功能，重新装备也能修复。
  考虑到武器掉落清除hediff曾导致邪教徒吟诵袭击发生递归错误（本mod已修复），如遇到相似情况可向我汇报修复。
  
