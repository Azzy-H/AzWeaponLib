# HediffAbilityTurret
***
## 目的
***
  让pawn自动索敌并施放带有 `TurretAbility` 扩展的 Ability，并可通过不同的 `HediffDef` 将多组 Ability 分开控制。
## 例程
***

  最简单的单组配置：
```
  <AbilityDef>
    <defName>MyAutoAbility</defName>
    <label>my auto ability</label>
    <gizmoClass>Command_Ability</gizmoClass>
    <aiCanUse>false</aiCanUse>
    <verbProperties>
      <!-- 省略正常 Ability 所需字段 -->
    </verbProperties>
    <modExtensions>
      <li Class="AzWeaponLib.HediffTurret.TurretAbility">
        <priority>0</priority>
        <hediffDef IsNull="true" />
      </li>
    </modExtensions>
  </AbilityDef>
  ```
  当 `hediffDef` 为空时，将使用默认的 `AWL_AbilityTurret`：
  ```
  <HediffDef>
    <defName>AWL_AbilityTurret</defName>
    <label>ability turret</label>
    <description>Automatically casts enabled abilities.</description>
    <hediffClass>AzWeaponLib.HediffTurret.Hediff_AbilityTurret</hediffClass>
    <isBad>false</isBad>
    <scenarioCanAdd>false</scenarioCanAdd>
    <stages>
      <li>
        <becomeVisible>false</becomeVisible>
      </li>
    </stages>
  </HediffDef>
  ```
  若想将不同 Ability 分配给不同的自动炮台组，可额外定义自己的 `HediffDef`，并在扩展中指定，该设计主要目的是为了允许自定义索敌逻辑：
  ```
  <HediffDef>
    <defName>MyAbilityTurret_Fire</defName>
    <label>fire ability turret</label>
    <hediffClass>MyClass</hediffClass>
    <isBad>false</isBad>
    <scenarioCanAdd>false</scenarioCanAdd>
    <stages>
      <li>
        <becomeVisible>false</becomeVisible>
      </li>
    </stages>
  </HediffDef>
  <AbilityDef>
    <defName>MyFireAbility</defName>
    <label>fire ability</label>
    <gizmoClass>Command_Ability</gizmoClass>
    <verbProperties>
      <!-- 省略正常 Ability 所需字段 -->
    </verbProperties>
    <modExtensions>
      <li Class="AzWeaponLib.HediffTurret.TurretAbility">
        <priority>1</priority>
        <hediffDef>MyAbilityTurret_Fire</hediffDef>
      </li>
    </modExtensions>
  </AbilityDef>
  <AbilityDef>
    <defName>MyIceAbility</defName>
    <label>ice ability</label>
    <gizmoClass>Command_Ability</gizmoClass>
    <verbProperties>
      <!-- 省略正常 Ability 所需字段 -->
    </verbProperties>
    <modExtensions>
      <li Class="AzWeaponLib.HediffTurret.TurretAbility">
        <priority>2</priority>
        <hediffDef>AWL_AbilityTurret</hediffDef>
      </li>
    </modExtensions>
  </AbilityDef>
```
## 字段含义
***
  |字段|含义|
  |:--:| :--:|
  |priority|自动施放优先级，数值越大越优先尝试|
  |hediffDef|该 Ability 归属的自动炮台 HediffDef，留空时使用 `AWL_AbilityTurret`|
## 工作方式
***
  1. pawn 身上存在带 `TurretAbility` 扩展的 Ability 时，会自动补上对应的 `Hediff_AbilityTurret`。
  2. 若多个 Ability 指向同一个 `hediffDef`，它们会由同一个 `Hediff_AbilityTurret` 实例统一控制。
  3. 若多个 Ability 指向不同 `hediffDef`，则会生成多个 `Hediff_AbilityTurret`，各自只控制自己负责的 Ability。
  4. 自动炮台每 10 tick 检查一次可用 Ability，按 `priority` 从高到低尝试索敌并施放。
  5. 右键 Ability 按钮可切换该 Ability 是否允许自动施放，右上角 checkbox 角标会显示当前状态。
## 注意事项
***
  - `hediffDef` 指向的 `HediffDef` 的`hediffClass`必须继承`AzWeaponLib.HediffTurret.Hediff_AbilityTurret`

  - 本系统索敌时会绕过 `AbilityDef.aiCanUse`，因此即使 `aiCanUse=false` 也可自动施放；但仍会保留 `CanCast`、`CanApplyOn` 与各 `EffectComp.AICanTargetNow` 的限制。

  - `Hediff_AbilityTurret` 只会控制与自己 `def` 对应的 Ability，不同 `hediffDef` 之间互不影响。

  - 右键切换自动施放和角标显示都依赖对应的 `Hediff_AbilityTurret` 已存在；该 Hediff 的创建/移除由能力变更通知自动维护。