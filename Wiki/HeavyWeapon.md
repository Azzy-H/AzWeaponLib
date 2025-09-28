# HeavyWeapon
***
## 目的
***
  实现某种武器必须和某套护甲绑定
## 例程
***
  武器Def：
```
<modExtensions>
  <li Class="AzWeaponLib.HeavyWeapon.HeavyWeaponDef">
    <apparelGroupDef>SR_TAC</apparelGroupDef>
  </li>
</modExtensions>
```
  护甲组Def
```
<AzWeaponLib.HeavyWeapon.ApparelGroupDef>
  <defName>SR_TAC</defName>
  <label>"Pheromone" tracking auto cannon set</label>
  <availableApparels>
    <li>SR_Apparel_MissleArmor_DMG</li>
    <li>SR_Apparel_MissleArmor_EA</li>
    <li>SR_Apparel_MissleArmor_PD</li>
    <li>Miho_Apparel_Middle_Powered</li>
    <li>Miho_Apparel_Middle_PoweredMechanitor</li>
    <li>Apparel_ArmorRecon</li>
    <li>Apparel_PowerArmor</li>
    <li MayRequire="Ludeon.RimWorld.Royalty">Apparel_ArmorReconPrestige</li>
    <li MayRequire="Ludeon.RimWorld.Royalty">Apparel_ArmorLocust</li>
    <li MayRequire="Ludeon.RimWorld.Royalty">Apparel_ArmorMarinePrestige</li>
    <li MayRequire="Ludeon.RimWorld.Royalty">Apparel_ArmorMarineGrenadier</li>
    <li MayRequire="Ludeon.RimWorld.Royalty">Apparel_ArmorCataphract</li>
    <li MayRequire="Ludeon.RimWorld.Royalty">Apparel_ArmorCataphractPrestige</li>
    <li MayRequire="Ludeon.RimWorld.Royalty">Apparel_ArmorCataphractPhoenix</li>
    <li MayRequire="Ludeon.RimWorld.Biotech">Apparel_MechlordSuit</li>
  </availableApparels>
</AzWeaponLib.HeavyWeapon.ApparelGroupDef>
```
## 字段含义
***
  |字段|含义|
  |:--:|:--:|
  |apparelGroupDef|使用的护甲套件信息|
## 实现方法
***
  给所有的列表中的护甲增加脱事件响应comp，实现脱护甲卸武器

  修改装备武器的floatmenu，实现未满足条件无法穿着
  
  对于其他mod添加的装备武器事件未作处理，以实现最大程度的兼容
