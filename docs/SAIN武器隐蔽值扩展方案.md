# SAIN 武器隐蔽值系统扩展方案

> 版本: v1.0 | 日期: 2026-05-31 | 状态: 待同行评议
> 基于代码库调查（5项子调查，12个文件读取，精确行号引用）

---

## 一、背景

SAIN 现有的装备隐蔽系统（ItemStealthValues）仅覆盖 6 个装备类型中的 2 类（Headwear 和 BackPack），共 8 个硬编码条目。武器完全不参与隐蔽计算。但实际情况中，长枪管/重型武器会显著增加玩家的视觉和听觉暴露。

本方案在**不引入新 JSON 配置文件**的前提下，利用已有的 `EWeaponClass` 枚举实现武器隐蔽值。

---

## 二、证据基础

### 2.1 声音数据流（已验证）

```
EFT Patch → PlayAISound(type, pos, range, volume)
  → PlayerComponent.AddCachedAISoundEvent()
    → volume *= FootstepAudioMultiplier (默认1.0) + 雨天修正
    → SoundEvent 生成（含出声者 PlayerComponent 引用）
  → BotHearingClass 分发 → HearingInputClass
  → ProcessSounds → HearingAnalysisClass.CheckIfSoundHeard
    → 环境修正 × 状态修正 × 遮挡修正 × 难度修正
    → FinalRange = Range × Volume × FinalModifier
```

关键发现：
- `SoundEvent.PlayerComponent` **保留出声者完整引用**（`SoundEvent.cs:17`）
- 可通过 `PlayerComponent.Equipment.CurrentWeaponInfo.WeaponClass` 读取武器类型
- `volume` 参数在 `AddCachedAISoundEvent`（`PlayerComponent.cs:148-203`）中可被乘数修改
- 所有非枪声共用 `MaxFootstepAudioDistance`（有耳机70m/无耳机50m），**不区分声音类型**
- 脚步声**不区分地面材质**

### 2.2 视觉隐蔽数据流（已验证）

```
Enemy.EnemyPlayerComponent.AIData.AIGearModifier
  → gearStealthModifier (每1秒重算)
    → calcGearEffects(): 遍历 ItemStealthValues 字典
      → 匹配 6 类装备的 templateId
      → result *= ItemStealthValue.StealthValue
  → StealthModifier(distance): 30m内无效 / 30-60m Lerp / 60m+全效
```

关键发现：
- `calcGearEffects()` 在 `AIGearModifierClass.cs:63-122` 已遍历 6 类装备
- **武器不在遍历范围内**——天然存在扩展点
- 视觉系统两个接入点均已使用 `StealthModifier(distance)`：
  - `EnemyGainSightClass.cs:159`（发现速度）
  - `EnemyVisionDistanceClass.cs:122`（可见距离）

### 2.3 已排除的错误假设

| 假设 | 验证结果 |
|------|---------|
| "脚步声有材质区分" | **错误**——SAIN 不区分地面材质 |
| "不同声音类型有不同听力距离" | **错误**——所有非枪声共用 `MaxFootstepAudioDistance` |
| "需要给每把枪建 JSON" | **不需要**——`EWeaponClass` 枚举已覆盖全部武器分类 |
| "玩家和 Bot 数据源相同" | **不同**——`SoundEvent.IsAI` 区分，但 `PlayerComponent` 引用通用 |

---

## 三、设计方案

### 方案A：听觉武器暴露（脚步声/装备碰撞声）

**核心思路**：在声音产生端（`PlayerComponent.AddCachedAISoundEvent`）根据出声者的当前武器类型，乘入音量系数。

**生效条件**：仅在声音类型为移动类（FootStep/Sprint/GearSound/TurnSound/Jump/Land）时生效。**不**影响枪声、语音、手雷声。

**音量乘数表**（基于武器物理特性推断，可在 F6 中调整）：

| 武器类型 | 默认乘数 | 理由 |
|---------|---------|------|
| Pistol | 0.85 | 手枪轻便，几乎无碰撞噪音 |
| SMG | 0.85 | 冲锋枪紧凑，枪身短 |
| AssaultRifle | 1.00 | 基准步枪 |
| AssaultCarbine | 1.05 | 卡宾枪略短但枪身碰撞频率高 |
| Shotgun | 1.10 | 泵动/半自动霰弹枪枪身长且沉重 |
| MarksmanRifle | 1.15 | 精确步枪枪管明显更长 |
| DMR | 1.20 | 射手步枪通常 >20 英寸枪管 |
| SniperRifle | 1.30 | 狙击枪极其沉重，枪管极长 |
| MachineGun | 1.40 | 弹链/弹鼓碰撞 + 枪身庞大 |

**背挂第二主武器**：额外 ×1.10（背上长条碰撞噪音）

**代码接入位置**：

```
FILE: Classes/Player/PlayerComponent.cs
METHOD: AddCachedAISoundEvent()

在第162行（FootstepAudioMultiplier 应用之后）插入：

  // 武器暴露：重型武器增加移动噪音
  if (IsMovementSound(type)) {
      float weaponNoiseMod = GetWeaponNoiseModifier();
      inVolume *= weaponNoiseMod;
  }
```

新增辅助方法（同一文件或 `SAINEquipmentClass` 中）：

```csharp
private static readonly HashSet<SAINSoundType> MovementSounds = new() {
    SAINSoundType.FootStep, SAINSoundType.Sprint, SAINSoundType.GearSound,
    SAINSoundType.TurnSound, SAINSoundType.Jump, SAINSoundType.Land,
    SAINSoundType.Prone, SAINSoundType.Bush
};

private bool IsMovementSound(SAINSoundType type) => MovementSounds.Contains(type);

private float GetWeaponNoiseModifier()
{
    var weapon = Equipment?.CurrentWeaponInfo;
    if (weapon == null) return 1f;
    
    float baseMod = weapon.WeaponClass switch {
        EWeaponClass.Pistol => 0.85f,
        EWeaponClass.SMG => 0.85f,
        EWeaponClass.AssaultRifle => 1.00f,
        EWeaponClass.AssaultCarbine => 1.05f,
        EWeaponClass.Shotgun => 1.10f,
        EWeaponClass.MarksmanRifle => 1.15f,
        EWeaponClass.DMR => 1.20f,
        EWeaponClass.SniperRifle => 1.30f,
        EWeaponClass.MachineGun => 1.40f,
        _ => 1.00f
    };
    
    // 背挂武器额外加成
    if (Equipment?.WeaponInfos?.ContainsKey(EquipmentSlot.Scabbard) == true)
        baseMod *= 1.10f;
    
    return baseMod;
}
```

**影响范围**：`volume` ↑ → `BaseRangeWithVolume` ↑ → bot 听力管道所有后续计算的基础输入变大

**F6 GUI 配置**：在 `HearingSettings.cs` 中新增：

```csharp
[Name("武器移动噪音影响")]
[Description("重型武器增加移动时被听到的距离。默认开启。")]
[Category("武器暴露")]
public bool WEAPON_NOISE_ENABLED = true;

[Name("手枪/冲锋枪噪音乘数")]
[MinMax(0.5f, 1.5f, 100f)][Category("武器暴露")][Advanced]
public float WEAPON_NOISE_PISTOL = 0.85f;

[Name("突击步枪噪音乘数")]
[MinMax(0.5f, 1.5f, 100f)][Category("武器暴露")][Advanced]
public float WEAPON_NOISE_RIFLE = 1.00f;

[Name("机枪噪音乘数")]
[MinMax(0.5f, 2.0f, 100f)][Category("武器暴露")][Advanced]
public float WEAPON_NOISE_MG = 1.40f;

[Name("狙击步枪噪音乘数")]
[MinMax(0.5f, 2.0f, 100f)][Category("武器暴露")][Advanced]
public float WEAPON_NOISE_SNIPER = 1.30f;

[Name("背挂武器额外噪音")]
[MinMax(1.0f, 1.5f, 100f)][Category("武器暴露")][Advanced]
public float WEAPON_NOISE_SCABBARD = 1.10f;
```

---

### 方案B：视觉武器暴露（枪管伸出掩体/长轮廓暴露）

**核心思路**：在 `AIGearModifierClass.calcGearEffects()` 中，将武器视为一个额外的"装备槽"，参与隐蔽值计算。

**生效条件**：仅在 `StealthModifier(distance)` 的距离曲线上生效（30m 内无效，60m+ 全效）。即**只有远距离视觉受影响**——近身交火不管你拿什么枪。

**武器暴露乘数表**（基于武器轮廓/枪管长度推断）：

| 武器类型 | 默认乘数 | 理由 |
|---------|---------|------|
| Pistol | 1.00 | 掩体后完全隐藏 |
| SMG | 1.00 | 紧凑，几乎看不到 |
| AssaultRifle | 0.95 | 枪管可能伸出掩体/草丛 |
| AssaultCarbine | 0.95 | 同上 |
| Shotgun | 0.90 | 长枪管轮廓明显 |
| MarksmanRifle | 0.82 | 长枪管 + 瞄准镜轮廓 |
| DMR | 0.75 | 更长枪管，轮廓极明显 |
| SniperRifle | 0.65 | 极长枪管 >24 英寸，远距离一眼可见 |
| MachineGun | 0.60 | 体型庞大，无法有效隐蔽 |

**背挂第二主武器**：额外 ×0.85（背上长条轮廓，从侧面/背面极易发现）

**代码接入位置**：

```
FILE: Classes/Player/Equipment/AIGearModifierClass.cs
METHOD: calcGearEffects()

在现有 6 个装备类型的 foreach 之后，新增：

  // 武器暴露（仅视觉）
  {
      var weapon = GearInfo.EquipmentPlayerComponent?.Equipment?.CurrentWeaponInfo;
      if (weapon != null) {
          float weaponMod = weapon.WeaponClass switch {
              EWeaponClass.Pistol => 1.00f,
              EWeaponClass.SMG => 1.00f,
              EWeaponClass.AssaultRifle => 0.95f,
              EWeaponClass.AssaultCarbine => 0.95f,
              EWeaponClass.Shotgun => 0.90f,
              EWeaponClass.MarksmanRifle => 0.82f,
              EWeaponClass.DMR => 0.75f,
              EWeaponClass.SniperRifle => 0.65f,
              EWeaponClass.MachineGun => 0.60f,
              _ => 1.00f
          };
          // 背挂武器
          if (GearInfo.EquipmentPlayerComponent?.Equipment?.WeaponInfos?.ContainsKey(EquipmentSlot.Scabbard) == true)
              weaponMod *= 0.85f;
          result *= weaponMod;
      }
  }
```

**F6 GUI 配置**：在 `VisionDistanceSettings.cs` 中新增：

```csharp
[Name("武器轮廓影响视觉隐蔽")]
[Description("长枪管/重型武器让AI在远距离更容易发现你。默认开启。")]
[Category("武器暴露")]
public bool WEAPON_VISUAL_STEALTH_ENABLED = true;
```

（武器类型的精确乘数通过 F6 已有的 WeaponClass 配置体现，预设系统中 EWeaponClass 已有对应配置结构）

---

## 四、需要解决的问题

### 4.1 真人玩家 vs Bot 的武器暴露是否对称？

**问题**：上述方案中，听觉方案影响所有出声者（真人+Bot），视觉方案影响所有被看见的对象（真人+Bot）。

**影响评估**：
- **听觉对称性**：Bot 持机枪移动 → 其他 Bot 在更远距离听到 → AI-vs-AI 听力距离增长。但 SAIN 已有 `AILimitSetting`（最大 AI-vs-AI 听力距离限制），会对此形成上限。**风险可控**。
- **视觉对称性**：Bot 持狙击枪 → 其他 Bot 在更远距离发现它。这可能导致 Sniper Scav/Boss 狙击手之间的远距离对射更频繁。**这是期望的行为**（更像真人）。

**建议**：听觉方案通过 `IsAI` 标志可选关闭 AI-vs-AI 方向（仅玩家被检测）；视觉方案保持对称。

### 4.2 背挂武器的检测是否可行？

`Equipment.WeaponInfos` 字典已存在，key 为 `EquipmentSlot`。`EquipmentSlot.Scabbard` 在 `SAINEnum.cs` 中可能未定义——需要确认枚举值。如果不存在，改用遍历 `WeaponInfos` 检查是否有非当前武器的槽位。

### 4.3 现有 8 个硬编码装备默认值是否冲突？

不冲突。本方案新增的是 `EWeaponClass` → 乘数映射，不是 `templateId` → 乘数映射。两者在 `calcGearEffects()` 中独立计算——装备隐蔽和武器暴露是乘法叠加的。

---

## 五、排除的设计方向

| 方向 | 排除理由 |
|------|---------|
| 给每把枪建 JSON | 数百个武器 templateId，手工维护不可行。EWeaponClass 已提供足够粒度 |
| 在听力管道末端（CheckIfSoundHeard）修改 | 太晚——应修改源头 volume，让所有下游计算自动适配 |
| 按表面材质区分脚步声 | SAIN 不追踪材质。需要额外 Patch EFT 的 `PlayStepSound` 并传递材质 ID，工作量过大 |
| 仅影响 AI-vs-玩家方向 | 视觉对称性是合理的；听觉可通过 IsAI 开关控制 |

---

## 六、实施估算

| 任务 | 文件 | 行数 | 工时 |
|------|------|------|------|
| 听觉：移动声音武器乘数注入 | `PlayerComponent.cs` | ~40 | 2h |
| 听觉：F6 配置字段 | `HearingSettings.cs` | ~15 | 0.5h |
| 视觉：calcGearEffects 扩展 | `AIGearModifierClass.cs` | ~25 | 1.5h |
| 视觉：F6 配置字段 | `VisionDistanceSettings.cs` | ~5 | 0.5h |
| EWeaponClass 枚举完整性检查 | `SAINEnum.cs` | — | 0.5h |
| EquipmentSlot.Scabbard 枚举确认 | `SAINEnum.cs` | — | 0.5h |
| **合计** | **4 文件** | **~85** | **5.5h** |

---

## 七、待评议问题

1. 乘数表中的数值是否需要调整？当前值基于武器物理特性的推断，未经过实际游戏测试。
2. 听觉方案是否需要通过 `IsAI` 限制为"仅 AI 监听玩家时生效"（AI 之间不互相增加听力距离）？
3. 是否需要给 F6 增加"武器暴露总开关"（一个开关同时控制听觉和视觉）？
4. 手枪/SMG 的 0.85 听觉乘数是否太激进（让这两种武器过于安静）？

> 本方案基于代码库精确调查。`SoundEvent.PlayerComponent`（SoundEvent.cs:17）、`AddCachedAISoundEvent`（PlayerComponent.cs:148）、`calcGearEffects`（AIGearModifierClass.cs:63）的引用均已在代码中验证。Preparing for the Future!
