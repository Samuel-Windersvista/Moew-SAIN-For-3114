# SAIN 武器隐蔽值扩展 — 实施计划

> 基于方案 v1.0 + 理事会审阅报告
> 日期: 2026-05-31

---

## Phase 0: 修复现有装备隐蔽系统（前置，2h）

### 0.1 清理 AIGearModifierClass 硬编码
**文件**: `Classes/Player/Equipment/AIGearModifierClass.cs`

- 删除 `getBackpackMod()`、`getHeadWearMod()`、`getFaceCoverMod()` 三个硬编码方法
- 删除相关的 8 个 `const string` templateId（与 GearStealthValuesClass 重复）
- 修复 `catch {}` → `catch (Exception e) { Logger.LogError(e); }`

### 0.2 删除 initDefaults 硬编码
**文件**: `Preset/GearStealthValues/GearStealthValuesClass.cs`

- 删除 `initDefaults()` 方法中的 8 条硬编码
- 改为：仅在无 JSON 文件时执行一次基础导出（空模板）
- 删除与 AIGearModifierClass 重复的 8 个 `const string`

### 0.3 补全空白装备类型默认值
**文件**: `Preset/GlobalSettings/` 下新增 `ItemStealthDefaults.json`

为 4 个空白装备类型添加合理默认值：
- FaceCover: 基础 1.00（无影响），特定面罩单独配置
- EyeWear: 基础 1.00，墨镜 0.98（轻微遮挡识别）
- ArmorVest: 轻型 0.95 / 中型 0.88 / 重型 0.80
- Rig: 轻型 0.95 / 重型 0.85

### 0.4 修复 fallback 路径
**文件**: `Classes/Player/Equipment/AIGearModifierClass.cs`

`calcGearEffects()` 异常时的回退不再调用已删除的硬编码方法，改为返回默认值 `1.0f` 并记录完整异常日志。

---

## Phase 1: 武器听觉暴露（2h）

### 1.1 修正枚举引用
**文件**: `Classes/Player/PlayerComponent.cs`

使用正确的 EWeaponClass 枚举值（小驼峰）:
```
pistol, smg, assaultRifle, assaultCarbine, shotgun,
marksmanRifle, sniperRifle, machinegun, grenadeLauncher, specialWeapon
```

### 1.2 实现 IsMovementSound + GetWeaponNoiseModifier
**文件**: `Classes/Player/PlayerComponent.cs` — `AddCachedAISoundEvent` 方法

在第 162 行（`FootstepAudioMultiplier` 应用后）插入：
```csharp
if (IsMovementSound(type))
    inVolume *= GetWeaponNoiseModifier();
```

移动声音判定：
```csharp
FootStep, Sprint, GearSound, TurnSound, Jump, Land, Prone, Bush, Looting, Reload
```

武器噪音乘数（修订后）:
```
pistol/smg → 0.90
assaultRifle → 1.00
assaultCarbine → 1.00
shotgun → 1.10
marksmanRifle → 1.15
sniperRifle/machinegun → 1.30
grenadeLauncher → 1.35
specialWeapon → 1.00
背挂第二主武器 → 额外 ×1.15
```

背挂检测: `WeaponInfos` 中 `SecondPrimaryWeapon` 存在且非当前武器

### 1.3 F6 配置
**文件**: `Preset/GlobalSettings/Categories/HearingSettings.cs`

```csharp
[Name("武器移动噪音")]
[Description("重型武器增加移动时被听到的距离。默认开启。")]
[Category("武器暴露")]
public bool WEAPON_NOISE_ENABLED = true;
```

---

## Phase 2: 武器视觉暴露（1.5h）

### 2.1 修正代码路径
**文件**: `Classes/Player/Equipment/AIGearModifierClass.cs`

在 `calcGearEffects()` 的 6 类装备遍历后添加：
```csharp
var weapon = AIData.PlayerComponent.Equipment.CurrentWeaponInfo;
```

（不是 `GearInfo.EquipmentPlayerComponent`——该属性不存在）

### 2.2 武器乘数表（修订后）
```
pistol/smg → 1.00
assaultRifle/assaultCarbine → 0.95
shotgun → 0.90
marksmanRifle → 0.82
sniperRifle → 0.70
machinegun → 0.65
grenadeLauncher → 0.55
specialWeapon → 1.00
背挂第二主武器 → 额外 ×0.90
```

### 2.3 F6 配置
**文件**: `Preset/GlobalSettings/Categories/Look/VisionDistance/VisionDistanceSettings.cs`

```csharp
[Name("武器轮廓影响视觉隐蔽")]
[Description("长枪管/重型武器让AI在远距离更容易发现你。默认开启。")]
[Category("武器暴露")]
public bool WEAPON_VISUAL_STEALTH_ENABLED = true;
```

---

## Phase 3: F6 GUI 完善（2h）

### 3.1 装备隐蔽值新增条目按钮
在 F6 的"装备隐蔽值"Tab 中增加"添加新条目"按钮，弹出输入框（装备名称 + 类型 + ItemID + 隐蔽值）。

### 3.2 武器暴露参数独立 Category
在 F6 中新增"武器暴露"独立分组，展示：
- 启用开关（听觉 + 视觉独立）
- 背挂武器加成滑块

---

## 实施估算

| Phase | 内容 | 工时 |
|-------|------|------|
| 0 | 修复现有装备系统 | 2h |
| 1 | 武器听觉暴露 | 2h |
| 2 | 武器视觉暴露 | 1.5h |
| 3 | F6 GUI 完善 | 2h |
| **合计** | | **7.5h** |
