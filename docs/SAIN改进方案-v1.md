# SAIN 改进方案 v1

> 基于 ODDBA 帖子 188608 的对比分析和自主设计
> 适用于 Moew-SAIN-For-3114 仓库版本
> 原则：保留 SAIN 现有架构，做增量改进而非替代核心逻辑

---

## 一、Bug 修复（必须修）

### 1.1 IsBotDeafened 比较方向错误

- **文件:** `Classes\Bot\Sense\Hearing\HearingInputClass.cs` 第 36 行
- **问题:** `_BotDeafedTime < Time.time` 应为 `>`
- **影响:** 近距离震聋机制完全失效，AI 在枪声巨响后听力不受任何影响
- **修复:** 一行改动

```csharp
// 修改前
if (_BotDeafedTime < Time.time)

// 修改后
if (_BotDeafedTime > Time.time)
```

---

### 1.2 对话声音传入错误列表

- **文件:** `Classes\Bot\Sense\Hearing\HearingInputClass.cs` 第 146 行
- **问题:** `ProcessSounds(AISoundCachedEvents, ...)` 传入通用列表，应为 `AISoundCachedEvents_Conversations`
- **影响:** AI 完全不对语音、脚步、换弹等非枪声做出反应
- **修复:** 一行改动

```csharp
// 修改前
ProcessSounds(AISoundCachedEvents, AlreadyDeafened, DeafenCoef_Convo, SoundDataToReactTo);

// 修改后
ProcessSounds(AISoundCachedEvents_Conversations, AlreadyDeafened, DeafenCoef_Convo, SoundDataToReactTo);
```

---

### 1.3 HearingDispersion ratio 缺少括号

- **文件:** `Classes\Bot\Sense\Hearing\HearingDispersionClass.cs` 第 30 行
- **问题:** 运算符优先级导致实际计算为 `distanceFromLastKnown - 0.06 - 3`，失去了"距离已知位置越近定位越准"的渐变效果
- **影响:** 除极近距离外，所有场景的定位随机化系数都被 clamp 到最大值，AI 的听觉定位丧失了精度梯度
- **修复:** 加回括号

```csharp
// 修改前
float ratio = distanceFromLastKnown - MIN_DISTANCE_LAST_KNOWN_NO_RANDOMIZATION
            / MAX_DISTANCE_LASTKNOWN_REDUCE_RANDOM - MIN_DISTANCE_LAST_KNOWN_NO_RANDOMIZATION;

// 修改后
float ratio = (distanceFromLastKnown - MIN_DISTANCE_LAST_KNOWN_NO_RANDOMIZATION)
            / (MAX_DISTANCE_LASTKNOWN_REDUCE_RANDOM - MIN_DISTANCE_LAST_KNOWN_NO_RANDOMIZATION);
```

---

## 二、设计改进

### 2.1 视觉系统：倍镜分级 + 距离衰减（不改概率模型）

#### 设计原则

保留仓库现有的"发现速度累积"模型（`EnemyGainSightClass.CalcTimeModifier`），不改为每帧掷骰子的概率模型。

理由是：
- 累积模型模拟真实人眼——隐蔽目标需要盯一会儿才能发现
- 概率模型导致"运气好时一帧索敌 / 运气差时永远发现不了"，不可预测
- 仓库已有的天气/姿势/角度/灯光等因素的累积调节体系不应丢弃

#### 改进方向

在现有 `CalcTimeModifier` 基础上引入两个新因素：

**A. 有效视距（Effective Vision Range）—— 基于倍镜倍率**

```csharp
/// <summary>
/// 获取 AI 装备的瞄具放大倍率。无瞄具返回 0（即机瞄默认），最高 8x+。
/// 如果无法直接读取倍率，退而使用当前武器是否有除机瞄外的光学瞄具来判断。
/// </summary>
private static float GetBotScopeMagnification(Enemy enemy)
{
    // 从 enemy.Bot 获取当前武器信息
    // 如果有光学瞄具：读取倍率（1x ~ 8x+）
    // 如果仅有机瞄/无镜：返回 0
    // 如果无法判断：保守推断，bosstype sniper → 4x，普通 pmc → 0
    var equipment = enemy.Bot.PlayerComponent.Equipment;
    var currentWeapon = equipment.CurrentWeaponInfo;
    if (currentWeapon == null)
        return 0f;

    // 检查 Mod 上的光学瞄具（scope）
    float scopeMag = currentWeapon.GetScopeMagnification();
    if (scopeMag > 0f)
        return scopeMag;

    // 无光学瞄具则视为机瞄
    return 0f;
}

/// <summary>
/// 根据倍镜倍率计算有效视距。无镜 AI 超过 80m 的目标发现极其困难，
/// 8x 镜 AI 有效视距可达 350m+。
/// </summary>
private static float CalcEffectiveVisionRange(Enemy enemy)
{
    float scopeMag = GetBotScopeMagnification(enemy);
    float baseRange = 60f;  // 机瞄基础有效视距
    float maxRange = 400f;  // 8x 镜最大有效视距

    // 倍率映射到有效视距
    float effectiveRange = Mathf.Lerp(baseRange, maxRange, scopeMag / 8f);

    // 天气/光照衰减（保留现有逻辑）
    effectiveRange *= BaseWeatherMod(false, enemy);

    return effectiveRange;
}
```

**B. 超出有效视距的发现速度大幅衰减**

在 `CalcTimeModifier` 的结果上叠加：

```csharp
private static float CalcScopeDistanceModifier(Enemy enemy)
{
    float effectiveRange = CalcEffectiveVisionRange(enemy);
    float enemyDist = enemy.RealDistance;

    if (enemyDist <= effectiveRange)
        return 1f;  // 有效视距内，原有逻辑不变

    // 超出有效视距后，发现速度平滑大幅衰减
    float excess = enemyDist - effectiveRange;
    float attenuation = Mathf.Clamp01(excess / 100f);  // 每超出100m衰减到一个更极端的值
    return Mathf.Lerp(1f, 0.05f, attenuation);  // 最低降至 1/20 的发现速度
}
```

然后将 `scopeDistanceMod` 乘入 `CalcModifier` 的结果中。

**效果：**
- 无镜 AI：80m 外几乎无法主动发现静止隐蔽的玩家（但仍能发现跑步/站立的）
- 4x 镜 AI：200m 外仍可能发现你
- 8x 镜狙击手：300m+ 保持威胁
- 保留 SAIN 累积模型：需要盯一段时间才能发现，不会瞬索

---

### 2.2 转向系统：梯度角度误差 + 多枪声定位

#### 设计原则

- 近距被击中（<30m）：AI 应精确转向——这么近还判断不了方向不合理
- 中距被击中（30-80m）：方向逐渐模糊，误差线性增长
- 远距被击中（>80m）：只能判断大扇区，需要多次射击辅助定位
- 连续射击帮助定位：同方向多次枪声逐步缩小误差

#### 改进

**A. LookToUnderFirePos 和 LookToLastHitPos 加角度误差**

在 `SAINSteeringClass.cs` 中：

```csharp
// 距离梯度配置（可考虑抽取到 GlobalSettings）
private const float STEER_ACCURACY_MIN_DIST = 30f;   // 此距离内精确
private const float STEER_ACCURACY_MAX_DIST = 100f;  // 此距离外最大误差
private const float STEER_MAX_ANGLE_ERROR = 60f;      // 最大角度误差（度）

/// <summary>
/// 根据被压制距离计算转向角度误差。距离越远误差越大。
/// 连续被压制（同一敌人在短时间内对该bot造成多次压制事件）缩减误差。
/// </summary>
private Vector3 GetDirectionWithAngleError(Vector3 targetDirection, float distance, int consecutiveSuppressionCount)
{
    if (distance <= STEER_ACCURACY_MIN_DIST)
        return targetDirection;  // 30m 内不引入误差

    // 基础误差随距离线性增长
    float baseErrorRatio = Mathf.InverseLerp(STEER_ACCURACY_MIN_DIST, STEER_ACCURACY_MAX_DIST, distance);
    float baseAngleError = Mathf.Lerp(0f, STEER_MAX_ANGLE_ERROR, baseErrorRatio);

    // 连续被压制缩减误差（模拟"他一直在朝我开火，我能判断方向了"）
    float consecutiveMultiplier = Mathf.Clamp01(1f - consecutiveSuppressionCount * 0.25f);  // 每次-25%
    float finalAngleError = baseAngleError * consecutiveMultiplier;

    float randomAngle = UnityEngine.Random.Range(-finalAngleError, finalAngleError);
    return Quaternion.AngleAxis(randomAngle, Vector3.up) * targetDirection;
}
```

修改 `LookToUnderFirePos()`:

```csharp
private void LookToUnderFirePos()
{
    if (LookToLastKnownEnemyPosition(Bot.Memory.LastUnderFireEnemy))
        return;

    Vector3 underFireDir = (Bot.Memory.UnderFireFromPosition - Bot.Position).normalized;
    float distance = Vector3.Distance(Bot.Position, Bot.Memory.UnderFireFromPosition);

    // 获取连续被压制次数
    int consecutiveCount = Bot.Memory.ConsecutiveUnderFireCount; // 需要在 BotMemory 中新增

    Vector3 randomizedDir = GetDirectionWithAngleError(underFireDir, distance, consecutiveCount);
    LookToDirection(randomizedDir);
}
```

`LookToLastHitPos()` 同理。

**B. 在 BotMemory 中新增连续压制计数**

```csharp
// BotMemory 新增字段
public int ConsecutiveUnderFireCount { get; set; }
public float LastUnderFireTime { get; set; }

// 在被压制触发时更新
public void OnUnderFire(Vector3 fromPosition)
{
    float timeSinceLast = Time.time - LastUnderFireTime;
    if (timeSinceLast < 3f)  // 3秒内的连续压制
        ConsecutiveUnderFireCount++;
    else
        ConsecutiveUnderFireCount = 1;

    LastUnderFireTime = Time.time;
    UnderFireFromPosition = fromPosition;
}
```

---

### 2.3 听觉定位：bulletImpacted 距离分级误差

#### 设计原则

- 弹着点是听觉线索，不是视觉线索——误差应比枪声更大
- 材质影响：泥土/石头（清晰）vs 水面/树叶（模糊）
- 多次弹着点帮助缩小定位范围

#### 改进

```csharp
private const float IMPACT_MIN_ACCURATE_DIST = 15f;   // 15m 内弹着点定位相对准
private const float IMPACT_MAX_RANDOM_DIST = 80f;     // 80m 外极不可靠
private const float IMPACT_MIN_DISPERSION = 2f;        // 最近的基础分散
private const float IMPACT_MAX_DISPERSION = 40f;       // 最远的分散（会触发 fallback 极大随机值）

// 新增：连续弹着点记忆
private Dictionary<string, ImpactMemory> _impactMemories = new();

private class ImpactMemory
{
    public Vector3 LastEstimatedPos;
    public int ConsecutiveHits;
    public float LastHitTime;
}
```

修改 `bulletImpacted` 方法：

```csharp
private void bulletImpacted(EftBulletClass bullet)
{
    // ... 现有前置检查保持不变 ...

    float sqrDistance = (bullet.CurrentPosition - Bot.Position).sqrMagnitude;
    if (sqrDistance > IMPACT_MAX_HEAR_DISTANCE)
    {
        _nextHearImpactTime = currentTime + IMPACT_HEAR_FREQUENCY_FAR;
        return;
    }
    _nextHearImpactTime = currentTime + IMPACT_HEAR_FREQUENCY;

    float distance = Mathf.Sqrt(sqrDistance);

    // 获取或创建弹着点记忆
    string enemyId = enemy.EnemyPlayer.ProfileId;
    if (!_impactMemories.TryGetValue(enemyId, out var memory))
    {
        memory = new ImpactMemory();
        _impactMemories[enemyId] = memory;
    }

    // 判断是否连续命中
    bool isConsecutive = (currentTime - memory.LastHitTime) < 2f;
    if (isConsecutive)
        memory.ConsecutiveHits++;
    else
        memory.ConsecutiveHits = 1;

    memory.LastHitTime = currentTime;

    // 计算基础分散
    float baseDispersion;
    if (distance <= IMPACT_MIN_ACCURATE_DIST)
    {
        // 15m 内线性增长
        baseDispersion = Mathf.Lerp(IMPACT_MIN_DISPERSION, IMPACT_MIN_DISPERSION * 2f,
            (distance - 3f) / (IMPACT_MIN_ACCURATE_DIST - 3f));
    }
    else if (distance <= IMPACT_MAX_RANDOM_DIST)
    {
        // 15-80m：曲线过渡到极大随机
        float ratio = (distance - IMPACT_MIN_ACCURATE_DIST)
                    / (IMPACT_MAX_RANDOM_DIST - IMPACT_MIN_ACCURATE_DIST);
        // 使用 EaseInQuad 让误差在后半段加速膨胀
        float easedRatio = ratio * ratio;
        baseDispersion = Mathf.Lerp(IMPACT_MIN_DISPERSION * 2f, IMPACT_MAX_DISPERSION, easedRatio);
    }
    else
    {
        // 80m+：直接给予极大随机，配合后续的 Max(finaDispersion, 90f) 逻辑
        baseDispersion = IMPACT_MAX_DISPERSION * 3f;
    }

    // 连续命中缩减分散（弹着点越多定位越准）
    float consecutiveMultiplier = 1f / (1f + (memory.ConsecutiveHits - 1) * 0.4f); // 第1发1.0, 第2发0.71, 第3发0.56...
    float finalDispersion = baseDispersion * consecutiveMultiplier;

    Vector3 random = UnityEngine.Random.onUnitSphere;
    random.y = 0;
    random = random.normalized * finalDispersion;
    Vector3 estimatedPos = enemy.EnemyPosition + random;

    memory.LastEstimatedPos = estimatedPos;

    SAINHearingReport report = new() {
        position = estimatedPos,
        soundType = SAINSoundType.BulletImpact,
        placeType = EEnemyPlaceType.Hearing,
        isDanger = distance < 25f,
        shallReportToSquad = distance < 40f,  // 40m内才通知队友
    };
    enemy.Hearing.SetHeard(report, currentTime);
}
```

---

### 2.4 声音定位：getBaseDispersion 引入倍率系数

在 `HearingDispersionClass.getBaseDispersion` 中，让声音定位精度也受倍镜影响：

```csharp
private float getBaseDispersion(float enemyDistance, SAINSoundType soundType)
{
    HearingSettings hearingSettings = GlobalSettingsClass.Instance.Hearing;
    if (!hearingSettings.HEAR_DISPERSION_VALUES.TryGetValue(soundType, out float dispersionValue))
    {
        dispersionValue = 12.5f;
    }

    float baseResult = enemyDistance / dispersionValue;

    // 新增：高倍镜狙击手的枪声更难定位（消音器+远距+枪声传播方向性）
    if (soundType == SAINSoundType.SuppressedShot || soundType == SAINSoundType.Shot)
    {
        var currentEnemy = Bot.GoalEnemy;  // 当前正在处理的声音来源敌人
        if (currentEnemy != null)
        {
            float scopeMag = GetBotScopeMagnification(currentEnemy);
            // 高倍镜通常配合长枪管和专用弹药，枪声传播特性不同
            // 狙击枪声更难精确定位（主要是消音器改变了声学特征）
            float sniperMultiplier = 1f + scopeMag * 0.1f;  // 8x 镜 → 1.8x 的定位难度
            baseResult *= sniperMultiplier;
        }
    }

    return baseResult;
}
```

---

### 2.5 DoorOpener：退后 + 等门物理完成

#### 设计原则

- 不让 AI 看起来穿透实体门
- 不让交互过程打断 AI 的战斗节奏
- 不同角色差异化处理（普通 AI 温柔，Boss 暴力）

#### 改进

在 `DoorOpener.cs` 的 `TryInteractWithDoor` 中：

```csharp
public bool TryInteractWithDoor(EInteractionType interactionType, float time, DoorDataStruct data)
{
    if (!InteractWithDoor(ref data, interactionType))
    {
        Clear();
        return false;
    }

    _interactionDoors[_interactionDoorIndex] = data;
    Interacting = true;
    ActiveDoor = data;
    InteractionType = interactionType;

    // --- 改进点 ---

    // 1. 门交互前让 AI 停下，退后一步
    BotOwner.StopMove();
    Vector3 backDir = (Bot.Position - data.Door.transform.position).normalized;
    BotOwner.Mover.SetPose(1f);  // 站直
    // 退后 0.5m（给门足够的打开空间）
    Vector3 standoffPos = Bot.Position + backDir * 0.5f;
    BotOwner.GoToPoint(standoffPos, true, -1f, false, false);

    // 2. 估算门打开所需时间（不再用固定值）
    // 门的打开角度和目标角度决定动画时间
    float doorProgress = data.Door.GetAngleFraction();  // 0=关, 1=全开
    float doorOpenDuration = (1f - doorProgress) * 1.2f;  // 门完全打开约需 1.2 秒

    // 3. 交互结束时间 = 门打开时间 + 小缓冲
    _doorInteractionEndTime = time + doorOpenDuration + 0.15f;

    // 4. 当前阶段暂不移除碰撞（等门真正打开后再移除，或完全不移除而是依赖退后）
    // 如果碰撞确实需要移除，延迟到门打开 80% 后再移除
    _removeCollisionTime = time + doorOpenDuration * 0.8f;

    Bot.Player.MovementContext.IgnoreInteractionCollision(data.Door.Collider, false);  // 先不移除碰撞
    return true;
}
```

在 `SelectDoor` 的 update 中：

```csharp
if (Interacting)
{
    float time = Time.time;

    // 门打开 80% 后才移除碰撞，大幅减少穿门
    if (_removeCollisionTime > 0 && time >= _removeCollisionTime)
    {
        Bot.Player.MovementContext.IgnoreInteractionCollision(ActiveDoor.Door.Collider, true);
        _removeCollisionTime = -1f;
    }

    if (_doorInteractionEndTime < time)
    {
        Clear();
        interactionType = EInteractionType.Open;
        currentDoor = default;
        return false;
    }
    // ...
}
```

新增字段：
```csharp
private float _removeCollisionTime = -1f;
```

---

## 三、改进优先级与实施顺序

| 优先级 | 编号 | 改进项 | 行数估计 | 依赖 |
|--------|------|--------|---------|------|
| P0 | 1.1 | IsBotDeafened 比较方向 | 1 行 | 无 |
| P0 | 1.2 | 对话声音列表错误 | 1 行 | 无 |
| P0 | 1.3 | ratio 括号 | 1 行 | 无 |
| P1 | 2.2 | 转向系统梯度误差 | ~80 行 | 需新增 BotMemory 字段 |
| P1 | 2.3 | 弹着点分级误差 | ~60 行 | 无 |
| P2 | 2.1 | 视觉倍镜分级 | ~80 行 | 需实现 GetBotScopeMagnification |
| P2 | 2.4 | getBaseDispersion 倍率系数 | ~20 行 | 依赖 2.1 的实现 |
| P3 | 2.5 | DoorOpener 退后逻辑 | ~30 行 | 无 |

**建议实施顺序：先修 3 个 P0 Bug → 转向系统 → 弹着点 → 视觉倍镜 → 声音倍率 → DoorOpener**

---

## 四、与帖子改法的关键差异

| 维度 | 帖子改法 | 本方案 |
|------|---------|--------|
| 视觉模型 | 概率替代累积 | 保留累积，调节速度参数 |
| 距离阈值 | 硬编码一刀切（80m） | 梯度过渡，无缝衰减 |
| 倍镜判断 | 有/无二元 | 倍率分级（0x~8x+连续映射） |
| 转向精度 | 精确/迷茫二元开关 | 角度误差随距离梯度增长 |
| 多枪行为 | 无 | 连续射击帮助定位精度提升 |
| 与仓库架构关系 | 替换核心逻辑 | 增量改进现有体系 |
