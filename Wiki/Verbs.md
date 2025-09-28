# Verbs
***
## 目的
***
  和AmmoSystem配合实现需要装填的武器，以及一些花里胡哨的小功能。
  可用脱离CompAmmo单独使用其霰弹和机枪特性
## 例程
***
  下列均为缺省值，缺失部分verbprop的通用字段
```
<li Class="AzWeaponLib.AmmoSystem.VerbProperties_ShootWithAmmo">
  <bulletsPerShot>1<bulletsPerShot>
  <ammoCostPerShot>1<ammoCostPerShot>
  <retargetRange>0<retargetRange>
  <shotgunRetargetRange>0<shotgunRetargetRange>
  <shotgunRetargetChanceFromRange IsNull="true" />
</li>
```
## 字段含义
***
  |字段|含义|
  |:--:|:--:|
  |bulletsPerShot|每次射击的弹丸数|
  |ammoCostPerShot|每次射击消耗的弹药数|
  |retargetRange|重新寻找目标的半径，仅在Verb_ShootWithAmmoConstantly上生效|
  |shotgunRetargetRange|未命中时是否重新寻找目标，用于实现霰弹的多目标攻击|
  |shotgunRetargetChanceFromRange|重新寻找到的目标的可用概率分布，类型为SimpleCurve|
## 可用类型
***
  ```
  Verb_ShootWithAmmo
  Verb_ShootWithAmmoConstantly
  ```
  Verb_ShootWithAmmo为基类，Verb_ShootWithAmmoConstantly实现了机枪的连续射击功能
  通过继承重写这些类，可以使用自定义的verb调用弹药系统
  example：
  - HellDivers Weapon Pack: 
  ```HDWeapon.Verb_ShootLaserWithAmmo```
  ```HDWeapon.Verb_SpewWithAmmo```

