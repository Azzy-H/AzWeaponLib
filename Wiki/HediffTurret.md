# HediffTurret
***
## 目的
***
  实现给pawn动态添加炮台
## 例程
***
```
<HediffDef>
  <defName>TurretHediff</defName>
  <hediffClass>AzWeaponLib.HediffTurret.Hediff_TurretGun</hediffClass>
  <modExtensions>
    <li Class="AzWeaponLib.HediffTurret.HediffDef_TurretGun">
      <!-- 本质是BuildingProperties，炮塔的xml可以直接使用 -->
      <turret>
        <turretGunDef>Gun_ChargeBlasterTurret</turretGunDef>
        <turretBurstCooldownTime>1</turretBurstCooldownTime><!-- 炮台内置后摇 -->
        <turretBurstWarmupTime>1</turretBurstWarmupTime><!-- 炮台内置前摇 -->
      </turret>
    </li>
  </modExtensions>
</HediffDef>
```
## 字段含义
***
  |字段|含义|
  |:--:|:--:|
  |turret|BuildingProperties，和原版炮台逻辑一致|

## 注意事项
***
  该炮台和pawn的武器独立运行，优先级高于pawn的武器，当瞄准时间不为0时，进入瞄准会打断pawn的武器瞄准，当pawn在射击后摇时触发时，射击完毕后后摇会加上炮台的后摇。
  内置前后摇和前后摇是不同的两个字段，内置前后摇不会影响pawn的stance，如果想让炮台只是一个额外输出可以让前后摇为0，内置前后摇非0。
  若想实现自定义索敌机制，可以参考：
  - [SRC]Miho,Star Ring Corporation: 
  ```SRM.Hediff_Interceptor```