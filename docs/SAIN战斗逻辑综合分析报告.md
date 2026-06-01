# SAIN 战斗逻辑综合分析报告

> **版本**: SAIN 2.x + LootingBots 1.6.1 (SPT 3.11)
> **分析日期**: 2026-05-31
> **文档状态**: 完整综合版

---

## 目录

- [1. 架构概览](#1-架构概览)
  - [1.1 SAIN 整体架构](#11-sain-整体架构)
  - [1.2 LootingBots 架构与交互](#12-lootingbots-架构与交互)
  - [1.3 Bot 决策系统完整优先级链](#13-bot-决策系统完整优先级链)
- [2. 感知系统详细分析](#2-感知系统详细分析)
  - [2.1 听觉系统完整链路](#21-听觉系统完整链路)
  - [2.2 视觉系统](#22-视觉系统)
  - [2.3 敌人状态追踪](#23-敌人状态追踪)
- [3. 全场景战斗逻辑分析](#3-全场景战斗逻辑分析)
  - [3.1 Bot 受伤时](#31-bot-受伤时)
  - [3.2 Bot 被命中时](#32-bot-被命中时)
  - [3.3 Bot 被压制时](#33-bot-被压制时)
  - [3.4 Bot 听到远处有战斗时](#34-bot-听到远处有战斗时)
  - [3.5 Bot 近处有与自己无关的战斗时](#35-bot-近处有与自己无关的战斗时)
  - [3.6 Bot 在和平状态下首次发现敌人时](#36-bot-在和平状态下首次发现敌人时)
  - [3.7 Bot 有敌人但看不到时](#37-bot-有敌人但看不到时)
  - [3.8 Bot 有敌人且能看到时](#38-bot-有敌人且能看到时)
  - [3.9 近距离狗斗时](#39-近距离狗斗时)
  - [3.10 手雷威胁反应](#310-手雷威胁反应)
  - [3.11 换弹/治疗中的行为](#311-换弹治疗中的行为)
  - [3.12 战斗结束后](#312-战斗结束后)
  - [3.13 小队协调作战](#313-小队协调作战)
  - [3.14 潜行搜索](#314-潜行搜索)
  - [3.15 掩体行为](#315-掩体行为)
  - [3.16 冲锋行为](#316-冲锋行为)
  - [3.17 手雷投掷行为](#317-手雷投掷行为)
- [4. 敌人选择逻辑](#4-敌人选择逻辑)
- [5. LootingBots 交互分析](#5-lootingbots-交互分析)
- [6. ECombatDecision 枚举全值](#6-ecombatdecision-枚举全值)
- [7. 个性对所有战斗参数的影响矩阵](#7-个性对所有战斗参数的影响矩阵)
- [8. 已发现的问题与改进建议](#8-已发现的问题与改进建议)
- [9. 总结](#9-总结)

---

## 1. 架构概览

### 1.1 SAIN 整体架构

SAIN 是基于 BigBrain 层系统的 SPT AI 修改模组。核心架构由以下层次组成：

#### 决策层

`BotDecisionManager` 以 10Hz 频率运行主循环，管理 18 种战斗决策枚举（`ECombatDecision`），是整个战斗系统的中央调度器。

#### 感知层

- **听觉系统**: `HearingAnalysis` + `SAINHearingSensor` + `HearingDispersion`，5 层流水线处理
- **视觉系统**: `EnemyVisionClass`，负责视线检测和可见性追踪

#### 行为层

- `CombatSoloLayer`（优先级 20，可配置）—— 单人战斗行为路由
- `CombatSquadLayer`（优先级 22，可配置）—— 小队战斗行为路由

#### 支持系统

- **压制系统**: `SAINBotSuppressClass`，五级压制状态（None / Light / Medium / Heavy / Extreme）
- **医疗系统**: `SAINBotMedical` + `SelfActionDecisionClass`
- **士气/疲劳系统**: 战斗疲劳（combat fatigue）机制
- **掩体系统**: `SAINCoverClass`，查找/移动/驻守状态机
- **手雷反应系统**: `GrenadeTrackerClass` + `GrenadeController`

#### 配置层

- 13 种个性（`EPersonality`）：GigaChad, Wreckless, Chad, Normal, SnappingTurtle, Rat, Timmy, Coward 等
- `GlobalSettings` —— 全局开关和阈值
- `PersonalityBehaviorSettings` —— 个性特定的行为参数

#### LootingBots 集成

`SAINLootingBotsIntegration` —— 战后拾取触发 + 小队拾取掩护

#### BigBrain 层优先级（高到低）

| 优先级 | 层名称 | 职责 | 适用 Bot |
|--------|--------|------|----------|
| 99 | DebugLayer | 调试 | 所有 |
| 80 | SAINAvoidThreatLayer | 狗斗 + 躲避手雷 | 所有 |
| 20-24 | ExtractLayer | 撤离 (仅 PMC/Scav/Raider) | 普通 Bot |
| 22 | CombatSquadLayer | 小队战斗 (普通 Bot) | 普通 Bot |
| 20 | CombatSoloLayer | 单人战斗 (普通 Bot) | 普通 Bot |
| 70 | CombatSquadLayer | 小队战斗 (Boss/Follower) | Boss/Follower |
| 69 | CombatSoloLayer | 单人战斗 (Boss/Follower) | Boss/Follower |
| 64 | CombatSquadLayer | 小队战斗 (Goons) | Goons |
| 62 | CombatSoloLayer | 单人战斗 (Goons) | Goons |
| 4-5 | LootingBots LootingLayer | 拾取 | 启用拾取的 Bot |
| 较低 | BSG 默认层 | 原生行为 | 回退 |

> [!NOTE] 普通 Bot（PMC/Scav/Raider/Rogue/BloodHound）使用 LayerSettings 中的可配置优先级（默认20/22/24）。Boss/Follower 使用硬编码 70/69，Goons 使用 62/64。Boss 和 Goons 无 ExtractLayer。

---

### 1.2 LootingBots 架构与交互

LootingBots 位于 `E:\云文件\GitHub\Moew-LootingBot-For-3114\SPT-LootingBots-1.6.1-spt-3.11\LootingBots\`。

#### 核心文件

| 文件 | 类型 | 职责 |
|------|------|------|
| `LootingBots.cs` | 插件入口 | 初始化，注册 BigBrain 层 |
| `LootingBrain.cs` | MonoBehaviour | 拾取行为逻辑 |
| `LootingLayer.cs` | BigBrain 层 | 激活拾取行为 |

#### 战斗影响

- **直接战斗影响**: `EnableWeaponSwitchingPatch.cs` patch 了 `BotDifficultySettingsClass.ApplyPresetLocation`，将以下两个字段统一覆盖:
  - `CHANCE_TO_CHANGE_WEAPON = 80`（原版默认值很低）
  - `CHANCE_TO_CHANGE_WEAPON_WITH_HELMET = 40`
  - 触发条件: 任一拾取功能启用（尸体/容器/物品）即生效，无 Bot 类型分级
- LootingLayer 在 BigBrain 优先级 4-5（远低于战斗层 20+），**不会覆盖战斗行为**
- LootingLayer.IsActive 要求 Bot 不处于治疗/手术状态，确保战斗中战斗层优先

#### SAIN <-> LootingBots 交互桥接

SAIN 通过 `SAINLootingBotsIntegration` 建立了战斗到拾取的桥梁:
- `CheckLootingVigilance()`: 每 0.5s 检测威胁（敌人 < 15m 或 IsSuppressed），中断拾取 10s
- `TryEnsureSafeLootingPosition()`: 拾取前检查掩体安全，无掩体且有敌人时中断拾取 30s
- `TryTriggerPostCombatLoot()`: 战后触发拾取扫描
- `CheckStatus()`: 拾取价值超过阈值 → 触发撤离（`FullOnLoot`）

---

### 1.3 Bot 决策系统完整优先级链

`BotDecisionManager.getDecision()` 以 10Hz（每 0.1s）运行，按以下优先级依次判断：

```
1. 手雷威胁？（HasActiveGrenadeThreat）
   └── YES → ECombatDecision.AvoidGrenade（最高优先级，即使无目标也触发）

2. 无敌人？
   ├── 战后恢复？（POST_COMBAT_RECOVERY + CombatEndTime < 10s）
   │   ├── 濒死/重伤 → ESelfActionType.FirstAid
   │   └── 弹药 < 50% → ESelfActionType.Reload
   ├── 战后 10s 后 → 触发 LootingBots 集成 TryTriggerPostCombatLoot()
   └── 击杀确认？（KILL_CONFIRM_ENABLED + LastKillTime < 2s）→ 保持瞄准
   └── ECombatDecision.None

3. 需要自我行动？（SelfActionDecisions.GetDecision）
   ├── 正在进行的治疗/换弹/手术？→ 继续
   ├── 需要换弹？（CheckDoReload）→ ESelfActionType.Reload
   ├── 需要治疗？（StartBotHeal）→ Stims > FirstAid > Surgery
   └── ECombatDecision.SeekCover + selfAction

4. 仅僵尸接触？（无其他射击者）→ ECombatDecision.FightZombies

5. 狗斗活跃？（DogFightDecision.DogFightActive）
   └── ECombatDecision.DogFight

6. 近战武器？→ ECombatDecision.MeleeAttack

7. 继续跑向掩体？（ContinueMoveToCover）→ ECombatDecision.SeekCover

8. 小队决策？（SquadDecisions.GetDecision）
   ├── 队友手雷警报？→ 暂停
   ├── PushSuppressedEnemy → 推压制敌人
   ├── 我的敌人可见/最近看到 → 不做小队决策
   ├── GroupSearch → 队友在搜索，加入
   ├── Suppress → 队友在撤退，压制支援
   ├── Help → 队友需要帮助
   └── LootingOverwatch → 队友在拾取，掩护
   └── 返回对应 ESquadDecision

9. 敌人决策？（EnemyDecisions.GetDecision）
   ├── 无弹药？→ Retreat
   ├── 可攻击性检查（CanBeAggressive）
   │   └── 压制状态 Extreme/Heavy 或战斗疲劳 → 不可攻击
   ├── 可攻击时：
   │   ├── StandAndShoot？（可见 + 可射击 + 在射程内 + 未过 HoldGround 时间）
   │   ├── ShootDistantEnemy？（远距可见 + 可射击 + 健康）
   │   ├── RushEnemy？（距离近 + 敌人脆弱/受伤）
   │   ├── ThrowGrenade？（手雷决策器判断）
   │   └── Search？（搜索决策器判断）
   └── 不可攻击时：
       ├── Freeze？（和平听到 + 室内 + 未看到 + 时间合适）
       ├── ShiftCover？（可移位 + 未被压制）
       └── Fallback: SeekCover

10. 无决策 → ECombatDecision.None
```

---

## 2. 感知系统详细分析

### 2.1 听觉系统完整链路

听觉系统采用 **5 层流水线架构**，从原始声音事件到敌人状态绑定逐步精炼。

---

#### 第 1 层：HearingInputClass —— 接收原始声音事件

通过 18 个 HearingPatches hook 进 BSG 的 `AISound` 系统。

**声音分类**:

| 类别 | 声音类型 |
|------|---------|
| 枪声 | `Shot`, `SuppressedShot` |
| 脚步声 | `FootStep`, `Sprint` |
| 行为声音 | `Reload`, `Heal`, `Looting`, `Surgery`, `GrenadeDraw` |

**声音数据结构**: 位置、距离、声源玩家、声音类型

---

#### 第 2 层：HearingAnalysisClass.CheckIfSoundHeard(AISoundData) —— 决定是否听到

**处理流程**:

**A. AI-AI 限制检查（ShallLimitAI）**

如果双方都是 AI 且启用了 AI 限制，根据 `AILimitSetting`（Far / VeryFar / Narnia）的最大距离过滤。排除对象：
- 当前目标敌人
- 正在瞄准此 Bot 的敌人

**B. 脚步声概率检测（DoIDetectFootsteps）**

仅对非枪声适用。使用线性距离概率模型：

```
P(听到) = (1 - (dist - close) / (far - close)) * 100%
```

最低概率补偿：
- 耳机加成
- 静止加成
- 当前目标加成

**C. 位置合理性检查**

声音位置与声源玩家位置差 > 5m 则丢弃。

**D. 距离修正链**

```
FinalRange = Sound.Range * Sound.Volume * FinalModifier
```

`FinalModifier` 由以下三项系数连乘得出：

| 修正类别 | 系数 | 条件 |
|---------|------|------|
| **EnvironmentModifier** | | |
| 跨环境 | 0.65 | `GUNSHOT_ENVIR_MOD` |
| 跨环境 | 0.70 | `FOOTSTEP_ENVIR_MOD` |
| 碉堡间 | 0.20 | 不同碉堡 |
| 碉堡不同深度 | 0.66 | |
| **ConditionModifier** | | |
| 无耳机 | 0.60 | |
| 重头盔 | 0.80 | |
| 濒死 | 0.80 | 且无止痛药 |
| 冲刺 | 0.85 | |
| 气喘 | 0.65 | |
| **OcclusionModifier** | | |
| 有视线 | 1.00 | |
| 无视线-枪声 | 0.80 | |
| 无视线-消音 | 0.65 | |
| 无视线-冲刺 | 0.80 | |
| 无视线-走路 | 0.60 | |
| 无视线-其他 | 0.60 | |

`FinalModifier = 1.0 * EnvironmentModifier * ConditionModifier * OcclusionModifier * 难度听力修正`

钳制到 `[HEAR_MODIFIER_MIN_CLAMP, HEAR_MODIFIER_MAX_CLAMP]`

**判定**: `PlayerDistance > FinalRange` → 听不到；否则听到。

---

#### 各口径枪声基础范围

| 口径 | 基础范围 |
|------|---------|
| 手枪口径（~9mm） | 110 - 125m |
| 步枪口径（5.45 / 5.56 / 9x39） | 160m |
| 全威力口径（7.62x51 / 7.62x54R） | 200 - 225m |
| .338 / .50BMG | 250 - 300m |
| 霰弹（12g） | 185m |
| 消音器 | ×0.6 |
| 亚音速 + 消音 | ×0.33 |

---

#### 第 3 层：HearingDispersionClass —— 听到后随机化位置

| 声音类型 | 基础分散 |
|---------|---------|
| 枪声 | 17.5m（被子弹击中时 ×2.0） |
| 消音枪声 | 13.5m |
| 脚步声 | 12.5m |

**角度修正**:
- 面朝声源：×0.5
- 背对声源：×1.5

**边界**: 最小 0.5m，最大 50m。10m 以内无分散（精确位置）。

---

#### 第 4 层：SAINHearingSensorClass —— 反应分发

**A. ReactToHeardSound(AISoundData)** —— 普通声音听到后的反应

1. 枪声 + `ShallChaseGunshot` 检查：如果个性不追踪远距枪声且距离 > `AudioStraightDistanceToIgnore`（默认 100m），忽略
2. 计算随机化位置
3. 向小队报告（`SquadInfo.AddPointToSearch`）
4. 触发 `OnEnemySoundHeard` 事件

**B. ReactToBulletFlyBy(AISoundData, FlyByDistance)** —— 子弹从身边飞过

1. **UnderFire 判断**: `FlyByDistance <= MaxUnderFireDistance`（默认 2m）
2. UnderFire 时：
   - 调用 BSG 原生 `OnEnemySounHearded`
   - `SetUnderFire`
   - 设置狙击手标签（来源距离 > 85m）
3. 非 UnderFire 时：仅处理听觉忽略关闭时
4. 一律执行：
   - 增加压制值（`CheckAddSuppression`）
   - 注册飞过（`RegisterEnemyFlyBy`）
   - 小队报告

---

#### 第 5 层：EnemyHearing —— 与敌人状态绑定

- `SetHeard`：如果敌人可见，用真实位置替代声音位置
- 设置 `Heard = true`, `HeardRecently = true`
- 更新 `KnownPlaces` 中的个人听到位置
- 如果 Bot 之前没有目标 → `EnemyHeardFromPeace = true`（**关键状态**）
- 非枪声 → `updateEnemyAction`（更新脆弱动作：`Reload` → `Reloading`, `Heal` → `Healing`, `Looting` → `Looting` 等）
- 向小队报告（1 秒节流）

---

#### 听觉延迟

| Bot 状态 | 基础延迟 | 枪声延迟 | 随机范围 |
|----------|---------|---------|----------|
| 和平 | 0.50s | 0.25s | 0.75 - 1.25 |
| 有活跃敌人 | 0.10s | 0.10s | 0.75 - 1.25 |
| 其他已知敌人 | 0.25s | 0.20s | 0.75 - 1.25 |
| 未知敌人 | 0.66s | 0.60s | 0.75 - 1.25 |

---

### 2.2 视觉系统

**EnemyVisionClass** 核心字段：

| 字段 | 说明 |
|------|------|
| `IsVisible` | 视觉射线检测结果 |
| `InLineOfSight` | 有无视线阻挡 |
| `CanShoot` | 是否可以射击（考虑身体部位暴露） |
| `Seen` | 是否已看见 |
| `TimeSinceSeen` | 上次看见至今的时间 |
| `VisibleStartTime` | 开始看见的时间戳 |

**AI-AI 视觉限制**: 如果双方都是 AI 且启用了 `AILimit`，根据 `AILimitSetting` 限制最大视觉距离。

**辅助类**:
- `EnemyGainSightClass`：控制发现敌人的速度（受压制状态影响）
- `EnemyAnglesClass`：计算角度相关信息

---

### 2.3 敌人状态追踪

`SAINEnemyStatus` 每 0.25s 更新一次。

#### 实时计算字段

| 字段 | 判定条件 |
|------|---------|
| `EnemyLookAtMe` | 点积 > 0.75 |
| `PointingWeaponAtMe` | 射击者 + 武器方向检查 |
| `VulnerableAction` | Surgery > Reloading > HasGrenade > Healing > Looting > None |

#### 自动衰减状态（ExpirableBool）

| 状态 | 过期时间 |
|------|---------|
| `HeardRecently` | 2s |
| `ShotMeRecently` | 15s |
| `ShotAtMeRecently` | 5s |
| `EnemyIsSuppressed` | 4s |
| `EnemyIsReloading` | 4s |
| `EnemyIsHealing` | 6s |
| `EnemyIsLooting` | 15s |
| `EnemyUsingSurgery` | 8s |

---

## 3. 全场景战斗逻辑分析

### 3.1 Bot 受伤时

#### 健康状态分级（HealthTracker）

| 等级 | 枚举值 |
|------|--------|
| 健康 | `Healthy` |
| 轻伤 | `Injured` |
| 重伤 | `BadlyInjured` |
| 濒死 | `Dying` |

---

#### 即时命中反应

触发路径: `SAINBotMedical.GetHit` → `BodyPartHitEffect.GetHit` + `AimHitEffect.GetHit`

**身体部位分类**:

| 部位类别 | 包含部位 |
|---------|---------|
| Head | 头部 |
| Center | 胸部/腹部 |
| Legs | 双腿 |
| Arms | 双臂 |

对应设置 `EHitReaction`。

**手臂受伤检测**（`checkArmInjuries`，每 1s 检查）:

| 伤势等级 | 枚举值 |
|---------|--------|
| 无伤 | `None` |
| 受伤 | `Injury` |
| 重伤 | `HeavyInjury` |
| 毁坏 | `Destroyed` |

**瞄准扰动**（`AimHitEffect`）:

```
damage_mod = Damage / Baseline
（钳制后 × 手动修正）

如果已有活跃效果 → × 0.5

扰动持续 = BASE_HIT_AFFECTION_DELAY * clamp(mod, 0, 1.5) * Random(0.8, 1.2)
效果线性衰减
```

**命中位置偏移模式**: 从命中点指向身体中心的方向向量 × 基础距离（y × 0.5）。

---

#### 治疗决策

触发: `SelfActionDecisionClass.StartBotHeal`，每 2s 检查一次。

**检查顺序**: Stims > FirstAid > Surgery

**Stims 条件**（`GetCanUseStims`）:
- 濒死或重伤
- 且（和平状态 或 跑向掩体 或 所有已知敌人安全检查通过）

**FirstAid 条件**（`startFirstAid`）:
- 受伤后 > 0.66s
- 有可用医疗品
- 安全检查通过
- 濒死/重伤 + 敌人在近距且有视线 → **不原地治疗**，等跑到掩体再治
- 轻伤：根据敌人距离开放治疗（越远越开放）

**Surgery 条件**:
- 有可用手术包
- 区域安全检查通过

---

#### 受伤对攻击性的影响

| 行为 | 限制 |
|------|------|
| `RushEnemy` | 濒死时禁止冲锋 |
| `ShootDistantEnemy` | 仅健康或轻伤可执行 |
| `StandAndShoot` | 不受健康状态影响（主要看视线和 hold ground 计时） |
| `PushSuppressedEnemy` | 仅健康或轻伤可执行 |

---

#### 受伤对听觉的影响

`HearingAnalysisClass.CalcConditionMod`: 濒死 + 无止痛药 → 听觉 × 0.8

---

#### 关键代码路径

```
中弹 → SAINBotMedical.GetHit
    → BodyPartHitEffect.GetHit
    → AimHitEffect.GetHit
    → Cover.GetHit

治疗检查 → SelfActionDecisionClass.StartBotHeal (每 2s)
         → BotDecisionManager (每 0.1s 判断治疗还是战斗)
```

---

### 3.2 Bot 被命中时

#### 触发条件

`SAINHearingSensorClass.ReactToBulletFlyBy` 中检查：

```
FlyByDistance <= MaxUnderFireDistance（默认 2m）
```

---

#### 反应链

**1. 设置 UnderFire 状态**（`SAINMemoryClass.SetUnderFire(enemy, position)`）

- 记录 `LastUnderFireSource` / `Enemy` / `Position`
- `ConsecutiveUnderFireCount`: 3s 内连续遭受火力，计数累加
- 调用 BSG 原生 `BotOwner.Memory.SetUnderFire()`

**2. 增加压制值**（`CheckAddSuppression`）

| 口径 | 基础压制量 |
|------|----------|
| 手枪 | ~1 |
| 步枪 | ~2 |
| 霰弹 | ~3 |
| .338 / .50BMG | ~5 |

距离缩放:
- < 4m: 全量
- 4 - 10m: 线性衰减至 0

抗性计算:
- `抗性 = Lerp(全局难度抗性, 个性抗性, 0.5)`（各 50% 权重）
- `实际压制量 = Lerp(基础压制量, 0, 抗性)`

**3. 狙击手检测**

子弹来源距离 > `ENEMYSNIPER_DISTANCE`（85m）→ `enemy.SetEnemyAsSniper`

**4. 敌人状态更新**

`RegisterEnemyFlyBy` → `ShotAtMe = true`, `ShotAtMeRecently = true`

**5. 位置报告**

`SquadInfo.AddPointToSearch`（告知小队开火位置）

---

#### 被命中对敌人选择的影响

`SAINEnemyController.SelectEnemy`: 如果 2s 内中弹 → 优先选择最近击中我的敌人。

---

#### FriendlyFire 检测

`SAINFriendlyFireClass` 检测是否友军误击。

---

### 3.3 Bot 被压制时

#### 压制值计算（SAINBotSuppressClass）

**增加**: 每次子弹飞过触发 `CheckAddSuppression(enemy, distance)`

```
压制量 = 口径压制量 (如 7.62x39 = 2.5) * SUPP_AMOUNT_MULTI（默认 1.0）

距离缩放:
  < 0.5m  → ×1.5（近距放大器）
  < 4m    → 全量
  4 - 10m → 线性衰减至 0

抗性 = Lerp(难度抗性, 个性抗性, 0.5)
抵抗后实际量 = Lerp(压制量, 0, 抗性)
钳制到 SUPP_MAX_NUM（默认 30）
```

**衰减**: 每 0.25s 衰减 0.25（每秒约 -1.0），自动衰减。

---

#### 五级压制状态及属性影响

| 等级 | 阈值 | 瞄准精度 | 瞄准速度 | 发现敌人 | 散布 | 可见距离 | 听觉距离 |
|------|------|:-------:|:-------:|:-------:|:---:|:-------:|:-------:|
| None | 0 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 |
| Light | 1 | 0.80 | 1.20 | 0.90 | 1.35 | 0.85 | 0.80 |
| Medium | 6 | 0.75 | 1.50 | 0.75 | 1.75 | 0.60 | 0.60 |
| Heavy | 15 | 0.50 | 2.00 | 0.65 | 2.50 | 0.50 | 0.40 |
| Extreme | 25 | 0.25 | 3.00 | 0.50 | 3.00 | 0.33 | 0.25 |

> 属性修改通过 `TemporaryStatModifiers` 应用到 `BotOwner.Settings.Current`

---

#### 压制对决策的影响

| 状态 | 影响 |
|------|------|
| `IsSuppressed`（>= Medium） | 影响部分关键决策 |
| `IsHeavySuppressed`（>= Heavy） | 阻止 `CanBeAggressive`，阻止 `canStartSearch`，触发战斗疲劳 |

**压制敌人的行为**（`TrySuppressAnyEnemy`）:
- Bot 向 `Enemy.SuppressionTarget` 射击进行压制
- 机枪持续压制 0.1s，其他武器 0.25s
- 压制判定条件：
  - 敌人可见 + 最近看到 < `TimeSinceSeenToSuppress`（默认 3s）
  - 或 被射击 < 12s
  - 或 被命中 < 12s

---

### 3.4 Bot 听到远处有战斗时

#### 处理路径

`SAINHearingSensorClass.ReactToHeardSound` 中处理。

1. `CheckIfSoundHeard` → 通过所有听觉检查
2. `ShallChaseGunshot(Distance)` 检查：
   - 个性 `WillChaseDistantGunshots = true` → 总是追踪
   - 距离 > `AudioStraightDistanceToIgnore`（默认 100m）→ 忽略
   - 否则追踪
3. 追踪时：分散随机化 → `SquadInfo.AddPointToSearch` → `CheckCalcGoal`

---

#### 行为差异

| 情况 | 结果 |
|------|------|
| 追踪远距枪声 | 向小队报告位置作为搜索点，可能触发 `CalcGoal`（如当前无目标）。不立即切换到战斗模式——这些是情报收集而非战斗触发 |
| 不追踪远距枪声 | `return` 直接返回，不产生任何反应。声音被完全忽略 |

---

#### 两种声音的区别

| | ReactToHeardSound（听到枪声） | ReactToBulletFlyBy（子弹飞过） |
|--|--|--|
| **含义** | 远处有人开枪 | 子弹近距离经过 |
| **距离** | 任意 | ≤ 特定距离 |
| **处理** | 可能追踪/忽略 | 总是处理压制 + 位置报告 + UnderFire 设置 |
| **压制** | 不产生 | 产生压制值 |

---

### 3.5 Bot 近处有与自己无关的战斗时

#### 处理链路

1. 听到枪声 → `HearingAnalysis` 过滤（通过所有听觉检查）
2. `ShallChaseGunshot` 检查
3. 听到后 → `SquadInfo.AddPointToSearch` + `OnEnemySoundHeard` 事件
4. 如果该声音可以关联到已知敌人 → `EnemyHearing.SetHeard` 更新敌人状态
5. 如果 Bot 处于和平状态且听到枪声 → `EnemyHeardFromPeace = true`

---

#### EnemyHeardFromPeace 的后续影响

这个标志非常重要，影响多个系统：

| 行为模式 | 描述 |
|---------|------|
| **None** | 无视听到的敌人 |
| **Freeze** | 室内 + < 70m 范围 → 进入冻结行为（`shallFreezeAndWait`），持续 10-120s / `AggressionMultiplier` |
| **SearchNow** | 立即开始搜索（`shallSearch` 检查通过） |
| **Charge** | 允许立即冲锋（`shallRushEnemy` 检查通过） |

当敌人首次被看到时 → `EnemyHeardFromPeace` 重置为 `false`。

---

#### 无关联到任何敌人时

枪声仅作为位置信息存储在小队搜索点中，不触发战斗。

---

### 3.6 Bot 在和平状态下首次发现敌人时

#### 发现方式

- **视觉发现**: `BotOwner.Memory.GoalEnemy` 被 BSG 设置
- **听觉发现**: `EnemyHearing.SetHeard` → `EnemyHeardFromPeace = true`
- 两者可能同时发生

---

#### 过渡过程

1. `EnemyHearing.SetHeard` 检查 `if (!Bot.HasEnemy)` → `EnemyHeardFromPeace = true`
2. `SAINEnemyController` 检查到有新敌人 → `CheckAddEnemy` → `ChooseEnemy`
3. `EnemyHeardFromPeace` 标志影响多个后续行为：
   - `shallFreezeAndWait`: Freeze 行为检查
   - `ShallBeStealthyDuringSearch`: 潜行检查
   - `shallRushEnemy`: HeardFromPeaceCharge 检查
   - `shallSearch`: HeardFromPeaceSearchNow 检查
4. 视觉首次看到 → `resetHeardFromPeace`

---

#### 个性差异

| 个性 | HeardFromPeace 行为 |
|------|-------------------|
| GigaChad | SearchNow — 立即搜索 |
| Wreckless | Charge — 立即冲锋 |
| Chad | Freeze — 室内先蹲守 |
| Normal | Freeze |
| SnappingTurtle | Freeze |
| Rat | Freeze |
| Timmy | Freeze |
| Coward | Freeze |

---

### 3.7 Bot 有敌人但看不到时

#### 搜索触发条件（SearchDeciderClass.ShallStartSearch）

**阶段 1: WantToSearch 检查**

- 敌人存在 + `LastKnownPlace` 存在且未被搜索过
- 个性 `WillSearchForEnemy = true`
- 未重度压制
- 如果未 Seen 且 `WillSearchFromAudio = false` → 不搜索

**阶段 2: shallSearch 检查**

| 条件 | 动作 |
|------|------|
| HeardFromPeace + SearchNow 个性 | 立即开始搜索 |
| 潜行搜索条件满足 | 潜行模式搜索 |
| 敌人正在拾取 | 40% 概率开始搜索 |
| 敌人战力 < 自身 50% | 开始搜索 |
| `TimeSinceSeen >= TimeBeforeSearch` | 开始搜索 |
| 小队看到位置更新时间 >= `TimeBeforeSearch` | 开始搜索 |
| 听到的敌人 `TimeSinceHeard >= TimeBeforeSearch` | 开始搜索（需 `WillSearchFromAudio`） |

**继续搜索的条件**（`shallContinueSearch`）:

- 敌人可见 → 继续
- 最近 2s 内看到 → 继续
- 敌人正在拾取 → 继续
- 敌人战力 < 自身 50% → 继续
- `TimeSinceSeen >= TimeBeforeSearch / 3` → 继续
- 能听到敌人 → 继续

**阶段 3: 路径检查**

`HasPathToSearchTarget` —— 是否有可达路径到搜索目标。

---

#### TimeBeforeSearch 计算

基于个性 `SearchBaseTime / AggressionMultiplier`:

| 个性 | SearchBaseTime | 搜索速度 |
|------|:------------:|:-------:|
| Wreckless | 0.1s | 极快（几乎立即） |
| GigaChad | 6s | 极快 |
| Chad | 16s | 较快 |
| Normal | 60s | 中等 |
| SnappingTurtle | 90s | 慢（伏击者） |
| Rat | 240s | 很慢 |
| Timmy | 90s | 慢 |
| Coward | N/A | 不搜索 |

---

#### 潜行搜索

条件链:
1. `SneakyBots = true`（全局开关）
2. 个性 `Sneaky = true` 或 `OnlySneakyPersonalitiesSneaky = false`
3. `EnemyHeardFromPeace = true`
4. 距离 < 80m
5. `HeardFromPeaceBehavior != SearchNow`

行为参数:
- `SneakySpeed` / `SneakyPose`: 控制移动速度和姿态
- `SlowAtCorners`: 拐角减速

---

### 3.8 Bot 有敌人且能看到时

#### StandAndShoot 决策（EnemyDecisionClass.shallStandAndShoot）

**条件（ALL 为 true）**:

1. 敌人可见（`IsVisible = true`）
2. 可以射击（`CanShoot = true`）
3. 有子弹（`HasBullets = true`）
4. 在有效射程内（`RealDistance <= EffectiveWeaponDistance * 1.25`）
5. HoldGround 时间检查：
   ```
   holdGroundInterval = max(HoldGroundDelay, 0.5) * Random(0.66, 1.33)

   HoldGroundDelay = HoldGroundBaseTime / AggressionMultiplier * Random(MinRandom, MaxRandom)
   ```
   - `visibleFor > holdGroundInterval` → 太久，不原地射击
   - `visibleFor < holdGroundInterval / 1.5` → 还在 hold 时间，继续射击
   - 在中间 → 如果有掩体保护四肢（`CheckLimbsForCover`）→ 继续射击
6. 僵尸特殊处理：如果只有僵尸无其他射击者 → 射击僵尸

---

#### StandAndShootAction 执行

- 设置姿态到掩体姿态
- 可能执行 `SwingMove`（< 50m 距离，随机 70-110 度横向移动）
- `Mover.Lean.HoldLean(0.66s)` 探头

---

#### ShootDistantEnemy 决策

| 条件 | 说明 |
|------|------|
| 敌人距离 | > `MaxPointFireDistance`（超出一股交火距离） |
| 可见性 | 敌人可见 + CanShoot |
| 健康 | 健康或轻伤 |
| 持续 | 每次 ~2s，冷却 ~6s（随机 ×0.75-1.25） |

> 这是一个远距离试探性射击，而非主要交火方式。

---

### 3.9 近距离狗斗时

#### 触发条件（DogFightDecisionClass.ShallDogfightEnemy）

- 敌人已知 + `LastKnownPosition` 存在
- 路径距离 <= `DOGFIGHT_PATH_DIST_START`（默认 10m）
- 且（敌人已看到 + `TimeSinceSeen < DOGFIGHT_TIMESINCESEEN_START`（默认 1s））或 `ShotMeRecently`

#### 结束条件

- 路径距离 > `DOGFIGHT_PATH_DIST_END`（默认 15m）
- 或 不可见 + `TimeSinceSeen > DOGFIGHT_TIMESINCESEEN_END`（默认 8s）

---

#### 决策优先级

在 `BotDecisionManager` 中优先级高于 `SquadDecision` 和 `EnemyDecision`：

- 只有 `AvoidGrenade` 和 `SelfAction` 优先级更高
- DogFight 期间停止小队协调行为

---

#### 限制条件

| 情况 | 是否进入 DogFight |
|------|:---------------:|
| 无子弹或正在换弹 | 否 |
| 撤退/找掩体中 + 低弹药（ratio < 0.3） | 否 |
| 已在进行 RushEnemy | 否 |
| 有更近的 DogFight 候选敌人 | 切换目标（每 0.5s 检查） |

---

### 3.10 手雷威胁反应

#### 手雷威胁生命周期

**阶段 1: 敌方投掷手雷**（`GrenadeController.OnGrenadeThrown`）

- 距离检查: `enemy.RealDistance <= 125m`（`MAX_ENEMY_GRENADE_DIST_TOCARE`）才追踪
- 超出 125m: 回退到 BSG 原生 `BotOwner.BewareGrenade`
- 创建 `GrenadeTrackerClass`，设置 `ReactionTime`:
  ```
  ReactionTime = 0.25s / DifficultyModifier
               * Random(0.75, 1.25)
               * GRENADE_REACTION_TIME_MODIFIER
  ```
  个性调整: Rat = 0.7（快反应），Timmy = 1.5（慢反应）
- 初始距离 < 10m → 立即标记 spotted

**阶段 2: 手雷追踪**（`GrenadeTrackerClass.Update` 每帧）

- 距离更新 + 紧急反应检测（`EMERGENCY_REACT_DISTANCE` + `IsGrenadeClosingIn`）
- 距离 < 3m → 自动 spotted
- Raycast 检查：手雷是否在视线内（点积 > 0.25 + 无遮挡）
- `CanReact = spotted && TimeSinceSpotted > ReactionTime`

**阶段 3: 触发反应**（CanReact = true 时）

- 设置 `grenadeManager.SetAvoidGrenade` → `BotDecisionManager.SetAvoidGrenade`
- 小队广播: `SquadInfo.OnMemberSpottedGrenade`
- 语音: 碎片手雷 → `OnEnemyGrenade`，其他 → `Look`
- 群体反应: `Bot.Squad.SquadInfo.OnMemberSpottedGrenade` 回调

**阶段 4: 躲避手雷决策**（BotDecisionManager 最高优先级）

- `HasActiveGrenadeThreat` → `ECombatDecision.AvoidGrenade`
- 由 `SAINAvoidThreatLayer`（优先级 80）的 `DodgeGrenadeAction` 执行

**阶段 5: 手雷落点更新**

- `GrenadeDangerUpdated` → `UpdateGrenadeDangerPoint` → `BotDecisionManager.UpdateGrenadeDangerPoint`

**阶段 6: 过期清理**

- `HasExpired`: `set_time > MAX_THREAT_LIFETIME`
- `Grenade.DestroyEvent` → `RemoveGrenade`
- `ManualUpdate` 清理无效/过期的 tracker

---

#### 小队手雷警报

`SquadDecisionClass.ShallRetreatFromGrenade`:

- 检查最近的手雷报告
- 距离 < 30m 且非烟雾/闪光弹 → 小队决策暂停前进（`EStrategicDecision.None`）

---

### 3.11 换弹/治疗中的行为

#### 换弹决策（SelfActionDecisionClass.CheckDoReload）

**基本条件**:
- 不是近战武器 / 近战结束阶段
- 有时间限制（上次换弹 > 1s）
- 武器就绪可换弹
- 没有在操作中

**弹药比检查逻辑**:

| 场景 | 条件 | 动作 |
|------|------|------|
| 通用 | 弹药比 >= 80% | 不换弹 |
| 无敌人 | 弹药比 < 70% | 换弹 |
| 搜索中 | 弹药比 < 20% | 换弹 |
| 搜索中 | 弹药比 > 50% | 不换弹 |
| 逐敌检查 `CheckReloadByAmmoRemaining` | | |
| 有敌人 | Seen + TimeSinceSeen < 2s | 不换弹（敌人在看） |
| 有敌人 | 弹药比 > 66%: VeryClose/Close/Mid | 不换弹 |
| 有敌人 | 弹药比 > 50%: VeryClose/Close | 不换弹 |
| 有敌人 | 弹药比 > 25%: VeryClose | 不换弹 |
| 有敌人 | 未 Seen + 未 ShotAtMe + 未 ShotMe | 换弹 |
| 有敌人 | TimeSinceSeen > 2s | 换弹 |

---

#### 战术换弹（TACTICAL_RELOAD_IN_COVER）

| 情况 | 行为 |
|------|------|
| 敌人在视线内 + 不在掩体中 | 延迟换弹，优先找掩体 |
| 已在掩体中 | 安全换弹 |
| 无敌人在视线内 | 原地换弹 |

---

#### 武器切换（WEAPON_SWAP_ON_DRY）

- 主武器空仓时检查副武器槽是否有弹药
- 有弹药 → 切换到副武器而非换弹
- LootingBots 修改: 武器切换概率提升至 80%

---

#### 治疗中的行为

| 治疗类型 | 最长持续 | 额外条件 |
|---------|:------:|------|
| FirstAid | 6s | 超时取消 |
| Stims | 3s | - |
| Surgery | 60s | 需区域安全检查 |

治疗期间 `CurrentSelfDecision != None`，阻止其他战斗决策。

---

### 3.12 战斗结束后

#### 战斗结束判定

所有已知敌人清除 → `enemy == null` → `CombatEndTime` 保持上次战斗结束时间。

#### 战后恢复（POST_COMBAT_RECOVERY = true）

1. 战后 10s 内:
   - 检查健康状态: 濒死/重伤 → `FirstAid`
   - 检查弹药比 < 50% → `Reload`
   - 否则清除 `CombatEndTime`

2. 战后 10s 后 → 触发 LootingBots 集成 `TryTriggerPostCombatLoot()`

#### 击杀确认（KILL_CONFIRM_ENABLED = true）

- 击杀后 2s 内保持瞄准目标位置
- 通过 `Bot.Memory.LastKillTime` 追踪
- `GoalEnemy` 变为 null 时清除 `LastKillTime`

#### 战斗疲劳（COMBAT_FATIGUE_ENABLED = true）

- 重度压制（`IsHeavySuppressed`）→ 降低攻击性
- 在 `CanBeAggressive` 中生效

---

### 3.13 小队协调作战

#### 小队决策优先级（SquadDecisionClass.GetDecision）

| 优先级 | 决策 | 触发条件 |
|:----:|------|---------|
| 1 | 暂停 | 队友报告手雷 |
| - | 跳过 | 我的敌人可见 / 最近 10s 内看到 |
| 2 | PushSuppressedEnemy | 队友在压制敌人 + 敌人在范围内 + 敌人脆弱/受伤 |
| 3 | GroupSearch | 队友在搜索同一个敌人 → 加入搜索（Leader 带头，成员跟随） |
| 4 | Suppress | 队友在撤退 + 我的敌人不可见 → 向敌人位置射击压制 |
| 5 | Help | 队友的敌人可见 + 在帮助距离内（30m 启动 / 45m 结束） |
| 6 | LootingOverwatch | 队友不在战斗中 + 未移动 + 在 25m 内 → 提供拾取掩护 |
| - | Regroup | 距队长 > 125m（无敌人）/ 50m（有敌人）时回归（已注释掉） |

---

#### 小队信息共享

| 方法 | 功能 |
|------|------|
| `AddPointToSearch` | 向小队所有成员报告敌人搜索位置 |
| `ReportEnemyPosition` | 共享敌人位置信息 |
| `UpdateSharedEnemyStatus` | 共享敌人脆弱状态 |

#### 小队角色（SQUAD_ROLE_ENABLED = true）

根据武器类型自动分配: 狙击 / 突击 / 支援。

---

### 3.14 潜行搜索

#### 条件链

1. `SneakyBots = true`（全局开关）
2. `OnlySneakyPersonalitiesSneaky` → 个性 `Sneaky = true`（Rat / SnappingTurtle）
3. `EnemyHeardFromPeace = true`（首次通过听觉发现）
4. 距离 < `MaximumDistanceToBeSneaky`（80m）
5. `HeardFromPeaceBehavior != SearchNow`

#### 行为

| 参数 | 说明 |
|------|------|
| `SneakySpeed` | 控制移动速度 |
| `SneakyPose` | 控制姿态 |
| `SlowAtCorners` | 拐角减速 |
| `ShallBeStealthyDuringSearch` | 冻结计时器联动 |

---

### 3.15 掩体行为

#### 掩体状态机（SAINCoverClass）

```
Find → MoveTo → HoldInCover
```

关键状态: `SpottedInCover` —— 在掩体位置被敌人发现。

#### SeekCover 触发

| 原因 | 决策 |
|------|------|
| 需要自我行动（治疗/换弹） | `SeekCover + SelfAction` |
| 撤退决策（无弹药） | `Retreat`（映射到 `SeekCoverAction`） |
| 躲手雷 | `AvoidGrenade`（`SAINAvoidThreatLayer` 处理） |
| EnemyDecision fallback | `SeekCover` |

#### ShiftCover

- 条件: 个性 `CanShiftCoverPosition = true`
- 触发: 敌人不可见 + `TimeSinceSeen` 大于阈值 或 上次已知位置更新间隔过大
- 不支持移位: Rat / Timmy / Coward

#### 移动姿态

| 状态 | 姿态 |
|------|------|
| 有敌人 | `MoveToCoverHasEnemySpeed` / `MoveToCoverHasEnemyPose` |
| 无敌人 | `MoveToCoverNoEnemySpeed` / `MoveToCoverNoEnemyPose` |

---

### 3.16 冲锋行为

#### RushEnemy 条件（EnemyDecisionClass.shallRushEnemy）

1. 不濒死
2. 有完整路径到敌人
3. 弹药 > 50%（低弹药比 0.5）
4. 在冲锋距离内:
   - 路径 < 10m（步行）或 < 20m（冲刺，需 `CanSprintPlayer`）
   - 敌人做手术 → 距离倍率 ×2
5. 补充条件任一:
   - HeardFromPeace + Charge 个性
   - 敌人脆弱动作（Reloading / Healing / Looting / Surgery）
   - 敌人濒死
   - 敌人重伤 + 卧倒
   - 敌人持有 SMG/霰弹 + 距离 > 25m（CQB 武器在远距不安全）
6. **不冲锋**: 敌人持有狙击/精确步枪 + 距离 < 30m（近距离对狙太危险）

#### RushEnemyAction 执行

全速冲向敌人位置。

---

#### 小队 PushSuppressedEnemy

- 队友在压制敌人
- 自身健康/轻伤
- 路径 < 75m（步行）/ 100m（冲刺）
- 敌人脆弱/受伤/卧倒

---

### 3.17 手雷投掷行为

**投掷决策**（`GrenadeThrowDecider.GetDecision`）:

- 敌人不可见时可能用来探路
- 敌人可见时可能用来逼迫移位
- 受个性影响

---

## 4. 敌人选择逻辑

### SAINEnemyController.SelectEnemy

**选择流程**（每帧调用）:

```
1. DogFight 候选检查 → 优先
2. 当前目标无效化检查（无效/死亡/未知/无位置 → 清除）
3. 可见敌人 > 0 → SelectVisibleEnemy
   ├── 可见 + 射击者: 新目标距离 < 当前目标 * 0.75 才切换
   ├── 可见 + 非射击者: 新目标距离 < 当前目标 * 0.33 才切换
   └── 否则保持最近可见敌人
4. < 2s 内中弹 → 选择最近击中我的敌人
5. 当前目标在搜索中 → 保持
6. 当前目标在活跃战斗中（非撤退/掩体/换位）→ 保持
7. 已看到但不可见的敌人
   └── 排序最近看到时间，优先射击者（ShotAtMe 或 ShotMe）
8. 最近已知位置的敌人 → 最近的那个
9. 有敌人曾攻击过我（ShotAtMe / ShotMe）
   └── 如果比当前目标近 0.5 倍距离就切换
10. 回退到最近已知敌人
```

---

## 5. LootingBots 交互分析

### 架构位置

- LootingBots 向 BigBrain 注册 `LootingLayer`:
  - 优先级 **4**: Assault/CursAssault + 所有 Boss/Follower 变种
  - 优先级 **5**: PMC/PmcUsec/PmcBear/ExUsec/ArenaFighter
  - 优先级 **11**: Obdolbs
  - 优先级 **13**: SectantWarrior/SectantPriest
- 战斗中 SAIN 战斗层（优先级 20+）优先于 LootingLayer
- LootingLayer.IsActive 在治疗/手术期间为 false，确保战斗中战斗层优先

### 直接战斗影响（来自 LootingBots 侧）

| 影响 | 详情 |
|------|------|
| 武器切换概率 | `EnableWeaponSwitchingPatch`: `CHANCE_TO_CHANGE_WEAPON = 80`，`CHANCE_TO_CHANGE_WEAPON_WITH_HELMET = 40`，对所有启用拾取的 Bot 统一生效 |
| 层替换 | 移除 BSG 默认 "Utility peace" 和 "LootPatrol" 层 |
| 激活条件 | Bot 活跃 + 未治疗 + 未手术 + 大脑启用 +（扫描中或正在拾取） |

### SAIN 侧集成（SAINLootingBotsIntegration）

| 功能 | 方法 | 说明 |
|------|------|------|
| 威胁中断拾取 | `CheckLootingVigilance()` | 每 0.5s 检查：GoalEnemy < 15m 或 IsSuppressed → 中断拾取 10s |
| 掩体安全检查 | `TryEnsureSafeLootingPosition()` | 无掩体 + 无敌人 → 蹲下隐蔽；无掩体 + 有敌人 → 中断拾取 30s |
| 战后触发拾取 | `TryTriggerPostCombatLoot()` | 战后 10s + 有空间 → 强制触发拾取扫描 |
| 拾取价值撤离 | `CheckStatus()` | `NetLootValue >= 阈值` → `FullOnLoot = true` → ExtractLayer 触发撤离 |

### LootingBots Interop API

| API | 方向 | 功能 |
|-----|:--:|------|
| `TryForceBotToScanLoot()` | SAIN→LB | 强制立即扫描拾取目标 |
| `TryPreventBotFromLooting(sec)` | SAIN→LB | 阻止拾取指定秒数 |
| `CheckIfInventoryFull()` | SAIN→LB | 检查背包是否已满 |
| `GetNetLootValue()` | SAIN→LB | 获取本次 Raid 拾取总价值 |
| `GetItemPrice()/GetItemPriceCached()` | SAIN→LB | 查询物品价格 |

### 关键约束

LootingBots **本体**（LootingBrain）不涉及以下内容：
- 敌人检测/识别、听觉反应、个性/攻击性参数、任何战斗配置

但 **SAINLootingBotsIntegration** 将战斗系统与拾取系统桥接:
- **涉及** 威胁评估、压制状态、掩体检查（通过 SAIN 侧调用）
- **不涉及** 听觉反应、个性、撤退逻辑

---

## 6. ECombatDecision 枚举全值

| 决策 | 中文 | 触发条件 |
|------|------|---------|
| `None` | 无决策 | 无敌人 / 无决策需求 |
| `Retreat` | 撤退 | 无弹药 或 武器管理器为 null |
| `AvoidGrenade` | 躲避手雷 | 活跃手雷威胁（最高优先级） |
| `DogFight` | 狗斗 | 近距离狗斗活跃 |
| `MeleeAttack` | 近战攻击 | 持有近战武器 |
| `FightZombies` | 战僵尸 | 仅僵尸接触无射击者 |
| `SeekCover` | 寻找掩体 | 需要治疗/换弹 或 fallback |
| `StandAndShoot` | 原地射击 | 可见 + 可射击 + 在射程 + hold ground |
| `ShootDistantEnemy` | 远距射击 | 远距可见 + 可射击 + 健康/轻伤 |
| `RushEnemy` | 冲锋敌人 | 近距离 + 敌人脆弱 + 可攻击 |
| `ThrowGrenade` | 投掷手雷 | 手雷决策器判断 |
| `Search` | 搜索 | 搜索决策器判断 |
| `Freeze` | 冻结 | 和平听到 + 室内 + 合适时间 |
| `ShiftCover` | 转移掩体 | 可移位 + 未压制 + 时间条件 |
| `CreepOnEnemy` | 潜行接近 | 枚举定义存在但从未被任何决策路径使用 |
| `MoveToEngage` | 推进交火 | 已实现但未被调用（见问题 8.1） |
| `RunAway` | 逃跑 | 未在当前使用 |
| `DebugNoDecision` | 调试占位 | 仅调试用途 |

---

## 7. 个性对所有战斗参数的影响矩阵

| 参数 | GigaChad | Wreckless | SnappingTurtle | Chad | Normal | Rat | Timmy | Coward |
|------|:-------:|:---------:|:--------------:|:----:|:------:|:---:|:-----:|:------:|
| AggressionMultiplier | **1.0** | **1.0** | **1.0** | **1.0** | **1.0** | **1.0** | **1.0** | **1.0** |
| HoldGroundBaseTime | 1.25s | 2s | 1.5s | 1.5s | 1s | 1s | 0.5s | 0.25s |
| CanShiftCover | Yes | Yes (Fast) | Yes (Slow 2x) | Yes | Yes | No | No | No |
| SearchBaseTime | 6s | 0.1s | 90s | 16s | 60s | 240s | 90s | N/A |
| WillSearchFromAudio | Yes | Yes | Yes | Yes | Yes | Yes | No | No |
| HeardFromPeaceBehavior | SearchNow | Charge | Freeze | Freeze | Freeze | Freeze | Freeze | Freeze |
| WillChaseDistantGunshots | Yes | Yes | No | Yes | No | No | No | No |
| CanRushEnemyReloadHeal | Yes | Yes | Yes | Yes | Balanced | No | No | No |
| CanJumpCorners | Yes (40%) | Yes (80%) | Yes (100%) | No | No | No | No | No |
| SprintWhileSearchChance | 75% | 90% | 40% | 60% | 10% | 0% | 20% | N/A |
| Sneaky | No | No | Yes | No | No | Yes | No | No |
| CanTaunt | Yes (constant) | Yes (frequent) | No | Yes (frequent) | No | No | No | No |
| TargetSuppress | Yes | Yes | Yes | Yes | Yes | Yes | Yes | Yes (Aggressive) |
| GrenadeReactionTime | 正常 | 正常 | 正常 | 正常 | 正常 | 0.7x (快) | 1.5x (慢) | 正常 |

> [!WARNING] 所有个性的 AggressionMultiplier 均为 **1.0**（代码验证 `PersonalityDefaultsClass.cs` 第 55/120/188/254/322/396/473/529 行）。攻击性差异通过 HoldGroundBaseTime、SearchBaseTime、HeardFromPeaceBehavior 等参数实现，而非通过该乘数。这意味着 TimeBeforeSearch = SearchBaseTime / 1.0，乘法差异化未生效。

---

## 8. 已发现的问题与改进建议

### 8.1 [严重] MoveToEngage 决策未被使用

**位置**: `EnemyDecisionClass.shallMoveToEngage` 方法已实现但从未在主决策路径中调用

**证据**: `BotDecisionManager.getDecision()` 中没有 `MoveToEngage` 分支。`shallMoveToEngage` 只存在于代码中但从未被调用。

**影响**: Bot 可能长时间停留在原地射击而不会主动接近超出有效射程的敌人。

**建议**: 在 `EnemyDecisionClass.GetDecision` 中，当 `shallStandAndShoot` 返回 `false` 且 `enemy.RealDistance > EffectiveWeaponDistance` 时，检查 `shallMoveToEngage`。

---

### 8.2 [已修复] 无弹药时强制撤退 —— Moew 分支已包含副武器检查

**位置**: `EnemyDecisionClass.GetDecision` 第 58-84 行

**现状**: Moew 分支已实现多槽位弹药检查。代码遍历 `_weaponSlotsToCheck`（SecondPrimaryWeapon + Holster），检查每个槽位的弹匣是否有子弹。仅当所有武器（主武器 + 所有副武器）都无弹药时才撤退。

**残留问题**: `SelfActionDecisionClass.CheckDoReload` 第 80 行的 `WEAPON_SWAP_ON_DRY` 分支仅检查 `EquipmentSlot.SecondPrimaryWeapon`，遗漏了 Holster 槽位。若副武器在 Holster 槽，SelfAction 侧不会触发武器切换（但 EnemyDecision 侧仍能正确检测弹药并避免撤退）。

**建议**: 统一 SelfAction 侧和 EnemyDecision 侧的槽位检查范围，都包含 SecondPrimaryWeapon + Holster。

---

### 8.3 [中等] 战后恢复仅 10s 窗口

**位置**: `BotDecisionManager.getDecision`, `CombatEndTime + 10s`

**问题**: 如果 Bot 在 10s 内没有完成治疗/换弹（如手术需要更长时间），`CombatEndTime` 被清除后 Bot 不再自动治疗。

**建议**: 将窗口延长至 30s，或在完成治疗前不清除 `CombatEndTime`。

---

### 8.4 [中等] Freeze 行为仅限室内

**位置**: `EnemyDecisionClass.shallFreezeAndWait`, `!Bot.Memory.Location.IsIndoors` → 不冻结

**问题**: 室外听到敌人也可能需要谨慎行为。Rat/Coward 在室外听到敌人时没有任何特殊反应（搜索也被 `WillSearchFromAudio` 控制）。

**建议**: 为室外场景添加"谨慎移动"模式（移动速度降低 + 频繁检查四周）。

---

### 8.5 [中等] 听觉系统对脚步声和枪声使用不同判定模型

**位置**: `HearingAnalysisClass.DoIDetectFootsteps`（概率）vs `CheckIfSoundHeard`（距离阈值）

**问题**: 枪声的"听到"是确定性的（超过距离就听不到，内就听到），脚步声是概率性的。两种不同判定方式可能导致行为不一致。

**建议**: 统一为混合模型——所有声音类型都有基础距离阈值 + 近距离确定性 + 远距离概率性。

---

### 8.6 [中等] LootingBots 武器切换概率 80% 对所有 Bot 类型统一

**位置**: `LootingBots/Patches/EnableWeaponSwitchingPatch.cs`（LootingBots 外部代码库）

**确认**: 通过交叉审计 LootingBots 1.6.1 代码库确认: patch 存在且对**所有启用拾取的 Bot** 统一将 `CHANCE_TO_CHANGE_WEAPON` 设为 80、`CHANCE_TO_CHANGE_WEAPON_WITH_HELMET` 设为 40。无任何 Bot 类型分级。

**新增发现**: 还有 `CHANCE_TO_CHANGE_WEAPON_WITH_HELMET = 40` 的覆盖（报告中遗漏）。

**问题**: Reshala 和普通 Scav 有相同的武器切换概率不太合理。Boss 应该更稳定地使用主武器。

---

### 8.7 [低] 压制系统仅子弹飞过触发，枪声本身不产生压制

**位置**: `SAINBotSuppressClass.CheckAddSuppression` 仅在 `ReactToBulletFlyBy` 中调用

**问题**: 现实中附近的枪声本身也会产生心理压制效果。SAIN 仅在子弹飞过很近距离时（2m 内 UnderFire）才施加压制。

**建议**: 对可听到的枪声（非飞过）施加微量压制（当前值的 10-20%），模拟战斗压力累积。

---

### 8.8 [低] 小队 LootingOverwatch 的检测过于粗糙

**位置**: `SquadDecisionClass.shallLootingOverwatch`，检查队友"不在战斗中 + 未移动 + 25m 内"

**问题**: 队友可能在掩体后不动不等于在拾取；可能在搜索、冻结或治疗。

**建议**: 通过 `LootingBotsInterop` 精确检查队友是否在活动拾取状态，而非启发式推断。

---

### 8.9 [低] 手雷威胁过期仅基于时间

**位置**: `GrenadeTrackerClass.HasExpired`, `MAX_THREAT_LIFETIME`

**问题**: 如果手雷已爆炸但未触发 `DestroyEvent`，威胁会持续到超时。应该在手雷爆炸时立即标记过期。

**建议**: 增加手雷爆炸检测（监听 `Explosion` 事件或检测手雷 `GameObject` 状态）。

---

### 8.10 [低] 狗斗与常规战斗切换可能造成"抽搐"

**位置**: `DogFightDecisionClass` 和 `BotDecisionManager` 的互动

**问题**: 狗斗进入/退出条件可能导致 Bot 在 DogFight 和常规决策之间快速切换（距离在阈值边缘波动）。

**建议**: 增加狗斗退出延迟（hysteresis），如退出阈值比进入阈值大 50%。

---

### 8.11 [中等] CreepOnEnemy 枚举值从未被任何决策路径使用

**位置**: `SAINEnum.cs` 第 19 行 `CreepOnEnemy`

**问题**: 与 MoveToEngage 类型相同的死代码。枚举定义存在但在 `BotDecisionManager.getDecision()` 和 `EnemyDecisionClass.GetDecision()` 中均无调用。

**建议**: 清理或实现为类似 Freeze 但在室外可用的谨慎接近行为。

---

### 8.12 [中等] 所有个性 AggressionMultiplier = 1.0 —— 乘法差异化未生效

**位置**: `PersonalityDefaultsClass.cs` 第 55/120/188/254/322/396/473/529 行

**问题**: 所有 8 种命名个性的 `AggressionMultiplier` 均为 `1.0`。这意味着所有通过该乘数缩放的计时器（TimeBeforeSearch = SearchBaseTime / AggressionMultiplier、HoldGroundDelay = HoldGroundBaseTime / AggressionMultiplier、FreezeDuration = Random(10,120) / AggressionMultiplier）在个性间无差异化。攻击性差异完全来自 SearchBaseTime、HoldGroundBaseTime 等参数的绝对值。

**建议**: 按个性赋予不同的 AggressionMultiplier 值（如 GigaChad=2.0, Wreckless=1.5, Normal=1.0, Rat=0.6, Coward=0.3），让所有关联计时器的缩放统一生效。

---

### 8.13 [低] SelfAction 武器切换仅检查 SecondPrimaryWeapon，遗漏 Holster

**位置**: `SelfActionDecisionClass.CheckDoReload` 第 80 行

**问题**: `WEAPON_SWAP_ON_DRY` 分支仅检查 `EquipmentSlot.SecondPrimaryWeapon`，而 `EnemyDecisionClass` 第 58-84 行检查了 SecondPrimaryWeapon + Holster 两个槽位。若副武器在 Holster 槽，SelfAction 侧不会触发武器切换。

**建议**: 将 SelfAction 侧的槽位检查改为与 EnemyDecision 一致：`{ EquipmentSlot.SecondPrimaryWeapon, EquipmentSlot.Holster }`。

---

### 8.14 [低] 关键功能体被注释掉

三处注释掉的代码:
- **Tagilla 近战决策**: `BotDecisionManager` 第 168-177 行（注释标注 "TODO: rework melee decisions"）
- **Regroup 小队集结**: `SquadDecisionClass.shallRegroup()` 调用被注释（第 48 行）
- **CheckCalcGoal 听到声音后目标计算**: `SAINHearingSensorClass` 第 111-118 行方法体被注释

**建议**: 确认这些功能的注释是有意的设计决定还是遗留代码。若为设计决定，添加注释说明原因。

---

### 8.15 [信息] Boss/Follower/Goons 层优先级与普通 Bot 不同且报告未区分

Boss/Followers 使用硬编码 `CombatSquadLayer=70 / CombatSoloLayer=69`，Goons 使用 64/62。两者均无 ExtractLayer。普通 Bot 使用可配置的 20/22/24 默认值。

报告所有场景分析均基于普通 Bot 的决策逻辑，但 Boss/Followers/Goons 可能因层级优先级不同而产生不同的行为交互。

---

### 8.16 [低] SAIN 的拾取价值撤离联动未在报告中描述

**位置**: `SAINLootingBotsIntegration.CheckStatus()` 和 `CanExtractFromLootValue()`

**功能**: Bot 拾取物品的价值超过配置文件中的阈值（PMC/Scav/Other 分别可配）后，`FullOnLoot = true`，ExtractLayer 触发撤离。这是 SAIN+LootingBots 联动的完整闭环。

---

### 8.17 [低] 头盔状态下武器切换概率 40% 被遗漏

**位置**: `EnableWeaponSwitchingPatch.cs` 第 29 行

**问题**: 报告仅提到 `CHANCE_TO_CHANGE_WEAPON = 80`，遗漏了同文件中 `CHANCE_TO_CHANGE_WEAPON_WITH_HELMET = 40`。

---

### 8.18 [信息] LootingBots 移除了原版 Utility peace 和 LootPatrol 层

LootingBots 在初始化时移除了这两个原版 Brain 层，可能影响非 SAIN Bot 类型的和平/拾取行为。

---

### 8.19 [信息] SAIN 侧 LootingBotsInfo 更新周期与 VigilanceCheck 不匹配

`UpdateLootingBotsInfo()` 每 5s 更新 `NetLootValue`/`FullInventory`，而 `CheckLootingVigilance()` 每 0.5s 检查威胁。存在 5s 的数据延迟窗口。

---

## 9. 总结

SAIN 是一个架构完善、细节丰富的 BOT AI 修改模组。对其核心优势的评估如下：

### 核心优势

| 特性 | S.P.E.C.I.A.L. 评级 |
|------|:------------------:|
| **Strength（性能）** | 10Hz 主循环 + 分层决策，性能开销可控 |
| **Perception（感知）** | 5 层听觉流水线 + 视觉系统，高度细腻 |
| **Endurance（可靠性）** | 18 种决策枚举完整覆盖，fallback 机制健壮 |
| **Charisma（可读性）** | 模块化架构清晰，命名规范，注释充分 |
| **Intelligence（算法）** | 压制系统五级递进模型设计科学 |
| **Agility（响应速度）** | BigBrain 层优先级调度准确，手雷反应 < 1s |
| **Luck（边缘情况）** | 战后恢复、FriendlyFire、AI-AI 限制均有覆盖 |

### 架构亮点

1. **分层的决策架构**: BigBrain 层系统 + 优先级决策链，扩展性好，易于新增行为
2. **细腻的听觉系统**: 5 层处理链，考虑环境（室内/室外/碉堡）、装备（耳机/头盔）、状态（濒死/冲刺）、距离、概率等多维度
3. **完整的压制系统**: 5 级压制状态（None/Light/Medium/Heavy/Extreme），属性递减科学，与决策紧密联动
4. **丰富的个性系统**: 13 种个性从 `HoldGroundBaseTime`、`SearchBaseTime`、`HeardFromPeaceBehavior` 等参数上做到明显差异化，1.25s vs 240s 的量级差异确保行为特征鲜明
5. **智能的自我行动决策**: 治疗/换弹的安全性检查完善，考虑敌人在看/距离/掩体等多条件

### 主要改进空间

0. **参数虚设**: 所有个性 AggressionMultiplier = 1.0，乘法差异化完全未生效
1. **已实现但未集成**: `MoveToEngage` 决策代码已写好但从未在主决策链中调用
2. **时间窗口**: 战后恢复仅 10s 窗口，对需手术的长程治疗不够
3. **场景覆盖**: Freeze 行为仅限室内，室外缺乏谨慎行为模式
4. **压制来源**: 缺少远处枪声的心理压制效果
5. **LootingBots 集成**: 武器切换概率、拾取掩护检测等细节可优化
6. **代码清理**: 部分 `#if DEBUG` 调试代码和注释掉的实验性功能可在稳定后清理

---

> *Vault-Tec 工程评估: SAIN 模组的代码质量达到级配标准。架构清晰，逻辑严密，是 SPT 生态中 AI 修改领域的标杆作品。建议优先处理 8.1（MoveToEngage 未集成）和 8.12（AggressionMultiplier 全为 1）两项问题，它们是当前版本最突出的 containment breaches。*
>
> *—— Vault-Tec -- Preparing for the Future!*

---

## 修订记录

| 日期 | 修订内容 |
|------|---------|
| 2026-05-31 | 初版发布 |
| 2026-05-31 | **交叉审计修订**: 修正 BigBrain 层优先级（50/60/70 → 20/22/24）；修正 ECombatDecision 枚举数（21→18）；修正 AggressionMultiplier 矩阵（全为 1.0）；补充 LootingBots EnableWeaponSwitchingPatch 交叉验证（确认存在）；补充 SAINLootingBotsIntegration 桥接分析；新增遗漏枚举值 CreepOnEnemy/DebugNoDecision；新增 8.11-8.19 补充问题；修正 8.2 为已修复；修正 8.6 为确认存在。审阅者: 三模型理事会 + VT-OS 终端 |

---

*报告结束*
