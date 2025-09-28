# Gas
***
## 目的
***
  对原版Gas进行了预封装，方便对当格做一些行为，以及处理受到伤害的行为，只有抽象类。
## 字段含义
***
### AWLGas：
  - AWLGasProperties : GasProperties
  
  |字段|含义|
  |:--:|:--:|
  |tickEffectRate|触发率，每x tick触发一次效果|
  |damageDefsToDestroyGas|可以使得该气体消散的伤害类型|
  |preDestroySpawnThingDef|消散时产生的物品，用于产生污渍|
  |preDestroySpawnThingChance|消散时产生物品的概率，同上|
  |preDestroySpawnThingCount|消散时产生物品的数量，同上|
  |heatOutput|每tick产热|
  - GasExplosionDef : DefModExtension(置于thingClass == Projectile_Gas的爆炸投射物上)

  |字段|含义|
  |:--:|:--:|
  |radius|生成气体半径|
  |density|生成气体浓度|
  |gasDef|生成气体类型|
## 例程
***
  - [SRC]Miho,Star Ring Corporation: 
  ```SRM.EMPGas```
  ```SRM.ExplosiveDefenderGas```
  ```SRM.HeatGas```


