# SAIN 对比分析 — ODDBA 帖子 188608

> 来源：[【Sain优化大改！】现在，你有资格与我对枪！！！](https://sns.oddba.cn/188608.html)
> 作者：qxq18080
> 分析日期：2026-05-30
> 仓库版本：Moew-SAIN-For-3114

## 帖子核心改动概述

帖主对 SAIN（适配 SPT 4.0.13）做了以下核心修改：

1. 修复近距离震聋效果 — 枪声可能导致 AI 短暂的听力削弱
2. AI 会对非枪声做出反应（说话、脚步、换弹等）
3. AI 通过记忆和枪声准确判断近距离敌人位置
4. 无倍镜 AI 无法通过"蜘蛛感应"发现远距离隐蔽的玩家，会逃跑/找掩体
5. 有倍镜 AI 也无法立刻精确转向，会四处张望后用倍镜发现

---

## 逐文件对比分析

### 1. HearingInputClass.cs — IsBotDeafened 比较方向错误

**文件位置:** `Classes\Bot\Sense\Hearing\HearingInputClass.cs`

**帖子描述:**
```csharp
if (_BotDeafedTime < Time.time)  // 应为 >
{
    return true;
}
```

**仓库现状 (第 32-43 行):**
```csharp
public bool IsBotDeafened {
    get
    {
        if (_BotDeafedTime > 0)
        {
            if (_BotDeafedTime < Time.time)  // <-- 与帖子描述的 bug 完全一致
            {
                return true;
            }
            _BotDeafedTime = -1;
        }
        return false;
    }
}
```

**分析:** `_BotDeafedTime` 存储的是"失聪结束时间点"（第 167 行 `= Time.time + BOT_DEAF_TIME_INTERVAL`，间隔 0.75 秒）。正确逻辑：当 `_BotDeafedTime > Time.time`（当前时间未到达失聪结束点，仍在失聪期内）时返回 true。但第 36 行用的是 `<`，导致**近距离震聋机制完全失效**。

**结论: 需要修复。** 将第 36 行 `<` 改为 `>`。

---

### 2. HearingInputClass.cs — 对话声音传入错误列表

**文件位置:** `Classes\Bot\Sense\Hearing\HearingInputClass.cs`

**帖子描述:**
```csharp
if (AISoundCachedEvents_Conversations.Count > 0)
{
    ProcessSounds(AISoundCachedEvents, ...);  // Bug: 传入了通用列表，应为 _Conversations
}
```

**仓库现状 (第 144-147 行):**
```csharp
if (AISoundCachedEvents_Conversations.Count > 0)
{
    ProcessSounds(AISoundCachedEvents, AlreadyDeafened, DeafenCoef_Convo, SoundDataToReactTo);
    //                            ↑ 传入了通用列表而非对话列表
}
```

**分析:** 第 144 行检查的是 `AISoundCachedEvents_Conversations` 的计数，但第 146 行传入的是 `AISoundCachedEvents`（通用列表）。这导致**AI 不会对玩家的语音/脚步/换弹等非枪声做出反应**。

**结论: 需要修复。** 将第 146 行的 `AISoundCachedEvents` 改为 `AISoundCachedEvents_Conversations`。

---

### 3. HearingInputClass.cs — bulletImpacted 精确坐标泄露

**文件位置:** `Classes\Bot\Sense\Hearing\HearingInputClass.cs`

**帖子描述:** 子弹击中物体后直接返回敌人精确坐标+极小随机误差。

**仓库现状 (第 307-311 行):**
```csharp
float dispersion = distance / IMPACT_DISPERSION;  // IMPACT_DISPERSION = 25 (5^2)
Vector3 random = UnityEngine.Random.onUnitSphere;
random.y = 0;
random = random.normalized * dispersion;
Vector3 estimatedPos = enemy.EnemyPosition + random;
```

**分析:** 仓库使用简单除法（`distance / 25`），近距离误差极小。仓库已有 `IMPACT_MAX_HEAR_DISTANCE = 2500`（约 50 米平方距离）限制超远距离的检测频率。帖子作者做了更精细的距离分级误差曲线（0-50m 线性 / 50-100m 曲线过渡 / >100m 极大误差）。

**结论: 可改进但非紧急。** 基础限制已有，距离分级是优化设计。

---

### 4. HearingDispersionClass.cs — ratio 括号错误

**文件位置:** `Classes\Bot\Sense\Hearing\HearingDispersionClass.cs`

**帖子描述:**
```csharp
float ratio = distanceFromLastKnown
    - MIN_DISTANCE_LAST_KNOWN_NO_RANDOMIZATION
    / MAX_DISTANCE_LASTKNOWN_REDUCE_RANDOM
    - MIN_DISTANCE_LAST_KNOWN_NO_RANDOMIZATION;
// 减法除法没加括号！
```

**仓库现状 (第 30 行):**
```csharp
float ratio = distanceFromLastKnown - MIN_DISTANCE_LAST_KNOWN_NO_RANDOMIZATION / MAX_DISTANCE_LASTKNOWN_REDUCE_RANDOM - MIN_DISTANCE_LAST_KNOWN_NO_RANDOMIZATION;
// 即：distanceFromLastKnown - (3/50) - 3 ≈ distanceFromLastKnown - 3.06
```

**分析:** C# 运算符优先级导致实际计算为 `distanceFromLastKnown - (3/50) - 3 ≈ distanceFromLastKnown - 3.06`。设计意图是归一化到 [0,1]：`(distanceFromLastKnown - 3) / (50 - 3)`，根据"距离上次已知位置"的远近平滑过渡随机化系数。

由于 `Mathf.Lerp(0.05f, 1f, ratio)` 会自动 clamp 到 [0,1]，当 `distanceFromLastKnown` 在 3-50 范围内时，ratio 处于 [-0.06, 46.94] 之间，大部分场景都被 clamp 到了极值。这意味着除了极近距离外的所有情况都得到最大随机化，失去了"距离近时定位更准"的设计意图。

**结论: 建议修复。** 正确写法：
```csharp
float ratio = (distanceFromLastKnown - MIN_DISTANCE_LAST_KNOWN_NO_RANDOMIZATION)
            / (MAX_DISTANCE_LASTKNOWN_REDUCE_RANDOM - MIN_DISTANCE_LAST_KNOWN_NO_RANDOMIZATION);
```

---

### 5. HearingDispersionClass.cs — getBaseDispersion 距离分级

**文件位置:** `Classes\Bot\Sense\Hearing\HearingDispersionClass.cs` (第 167-176 行)

**帖子描述:** 将线性增长改为 0-50m 线性 / 50-100m 曲线过渡 / >100m 极大误差。

**仓库现状:** 简单线性 `enemyDistance / dispersionValue`。

**结论: 可选增强。** 仓库线性逻辑虽不如帖子精细，但配合其他机制（如 `CalcRandomizedPosition` 中的 `finalDispersion` clamp）仍能工作。

---

### 6. SAINSteeringClass.cs — LookToUnderFirePos / LookToLastHitPos 无距离限制

**文件位置:** `Classes\Bot\Steering\SAINSteeringClass.cs`

**帖子描述:** 当 AI 被压制或击中时，会无视距离精确转向子弹来向。帖主加了约 80m 的距离门槛，远距离被击中时 AI 的反应从"锁头级"降为"迷茫级"。

**仓库现状:**

`LookToUnderFirePos()` (第 192-199 行):
```csharp
private void LookToUnderFirePos()
{
    if (LookToLastKnownEnemyPosition(Bot.Memory.LastUnderFireEnemy))
        return;
    LookToPoint(Bot.Memory.UnderFireFromPosition + WeaponRootOffset);  // 无距离判断
}
```

`LookToLastHitPos()` (第 201-218 行):
```csharp
private void LookToLastHitPos()
{
    var enemyWhoShotMe = _steerPriorityClass.EnemyWhoLastShotMe;
    if (LookToLastKnownEnemyPosition(enemyWhoShotMe))
        return;
    if (enemyWhoShotMe != null)
    {
        var lastShotPos = enemyWhoShotMe.Status.LastShotPosition;
        if (lastShotPos != null)
        {
            LookToPoint(lastShotPos.Value + WeaponRootOffset);  // 无距离判断
            return;
        }
    }
    LookToRandomPosition();
}
```

**分析:** 两个方法都没有任何距离检查。这与帖子描述的核心问题完全一致——"千里眼葫芦娃"的根源之一。帖子的 5 个核心改进中，第 3、4、5 条都依赖这个距离判断。

**结论: 强烈建议添加距离判断。** 这是帖子"蜘蛛感应"问题的关键修复。

---

### 7. VisionPatches.cs + EnemyGainSightClass.cs — 视觉系统与倍镜判断

**文件位置:** `Patches\VisionPatches.cs` + `Classes\Bot\EnemyClasses\Vision\EnemyGainSightClass.cs`

**帖子描述:**
- 将"发现因子"改为"发现概率"模型
- 引入距离相关概率开关（50m 内 100% / 50-80m 线性衰减）
- 引入倍镜判断：无倍镜远距离不触发对枪，改为逃跑/找掩体

**仓库现状:**

仓库版本已有一套较完善的视觉速度系统（`EnemyGainSightClass`），通过调节"发现速度"（而非"发现概率"）工作，综合了以下因素：

- **距离+时间** (`CalcTimeModifier`): 200m/250m 为最大距离，10m/65m 为最小距离
- **天气** (`CalcWeatherModifier`): 配合 `WeatherVisionPatch`
- **姿势** (`PoseModifier`): 趴下/蹲下
- **可见部位数** (`CalcPartsMod`): 暴露部位越多越容易被发现
- **角度** (`CalcAngleMod`, `CalcThirdPartyMod`): 视野外/非当前目标
- **敌人灯光** (`EnemyUsingLight`): 手电/激光/夜视
- **移动速度** (`CalcMoveModifier`)
- **高度差** (`CalcElevationModifier`)
- **AI 是否看向玩家** (`SAINNotLooking`)

**缺少的部分: 倍镜判断。** 仓库目前的距离判断是基于固定距离值，没有考虑 AI 是否携带光学瞄具。这是帖子"有倍镜 vs 无倍镜 AI 差异化行为"的核心逻辑。

**结论: 仓库视觉系统已有不错的基础**（甚至比帖子方案更全面），但**缺少倍镜判断**是显著差异。是否引入需考虑仓库的速度调节模型 vs 帖子的概率模型两种不同范式。

---

### 8. DoorOpener.cs — 穿门问题

**文件位置:** `Classes\Bot\Doors\DoorOpener.cs`

**帖子描述:** SAIN 在门交互过程中去掉物理碰撞导致 AI 穿门。帖主做了"退后→站稳→等门完全打开→再前进"的逻辑。主要影响 Boss（锤形态锤哥冲锋时穿门）。

**仓库现状:**
- 使用 `IgnoreInteractionCollision` 忽略碰撞（第 42 行）
- 有 `_doorInteractionEndTime` 时间控制交互结束（第 41 行）
- `doorInteractionEndTime = time + (IsDoorPullOpen(...) ? 1.25f : 1f)` — 给 1-1.25 秒的门交互时间
- 到时间后 `Clear()` 恢复碰撞（第 109 行）

**分析:** 仓库有时间控制但缺少帖主的"退后+等门真正打开"逻辑。仓库的方案是固定的时间等待，帖主的方案更主动。

**结论: 仓库版本可能部分缓解但不能完全解决。** 特别是 Boss 冲锋时，固定时间窗口可能不足以等门真正物理上打开。

---

## 汇总

### 高优先级 Bug（与帖子完全一致，必须修复）

| # | 文件 | 行号 | 问题 | 修复方向 |
|---|------|------|------|----------|
| 1 | `HearingInputClass.cs` | 36 | `IsBotDeafened` 比较方向 `<` → `>` | 一行改动 |
| 2 | `HearingInputClass.cs` | 146 | 对话声音传入 `AISoundCachedEvents` 应为 `_Conversations` | 一行改动 |
| 3 | `HearingDispersionClass.cs` | 30 | ratio 计算缺少括号 | 一行改加括号 |

### 中优先级改善建议

| # | 文件 | 行号 | 问题 | 说明 |
|---|------|------|------|------|
| 4 | `SAINSteeringClass.cs` | 192-218 | `LookToUnderFirePos` / `LookToLastHitPos` 无距离限制 | 帖子核心改进，需要引入距离阈值 |
| 5 | `EnemyGainSightClass.cs` | — | 缺少倍镜判断 | 仓库视觉模型以速度调节为主，需考虑如何融入 |

### 低优先级 / 可选增强

| # | 文件 | 行号 | 问题 | 说明 |
|---|------|------|------|------|
| 6 | `HearingInputClass.cs` | 307 | `bulletImpacted` 误差曲线可做距离分级 | 已有 50m 限制作为基础保护 |
| 7 | `HearingDispersionClass.cs` | 175 | `getBaseDispersion` 可做距离分级 | 当前线性配合 clamp 仍可接受 |
| 8 | `DoorOpener.cs` | — | 穿门修复可更主动（退后+等门逻辑） | 当前时间窗口方案可能打折扣 |

### 帖子中已在仓库有更好方案的

| 帖子做法 | 仓库已有方案 |
|----------|--------------|
| 发现概率替代发现因子 | `EnemyGainSightClass` 综合多因素的速度调节模型 |
| 简单距离+概率 | `CalcTimeModifier` + `CalcWeatherModifier` + `CalcPartsMod` + ... |
| 硬编码 80m 阈值 | 可配置的 `TIME_MAX_DIST_CLAMP` (200m/250m) |

---

## 附：帖子原文提及的改动文件映射

| 帖子原文 | 仓库对应文件 |
|----------|-------------|
| HearingInputClass.cs | `Classes\Bot\Sense\Hearing\HearingInputClass.cs` |
| HearingDispersionClass.cs | `Classes\Bot\Sense\Hearing\HearingDispersionClass.cs` |
| SAINSteeringClass.cs | `Classes\Bot\Steering\SAINSteeringClass.cs` |
| VisionPatches.cs | `Patches\VisionPatches.cs` |
| DoorOpener.cs | `Classes\Bot\Doors\DoorOpener.cs` |
