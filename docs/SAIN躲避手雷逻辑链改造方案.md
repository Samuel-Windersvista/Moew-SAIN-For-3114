# SAIN 躲避手雷逻辑链改造方案

> 版本: v2.0 | 日期: 2026-05-31 | 状态: 已通过理事会审议

---

## 一、当前状态

### 1.1 现状

SAIN 的检测系统相当精良（轨迹追踪、碰撞声扩展、距离/视线复合判定），但实际躲避**完全交给 EFT 原版**：

```
GrenadeTrackerClass.CanReact = true
  → GroupSay(OnEnemyGrenade)    // 全队通报（仅语音，不传递坐标）
  → BotOwner.BewareGrenade.AddGrenadeDanger()  // EFT 原生躲避
```

`ECombatDecision.AvoidGrenade` 枚举（`SAINEnum.cs:17`）和 `SAINAvoidThreatLayer` 中的映射（`AvoidGrenade → SeekCoverAction`，`SAINAvoidThreatLayer.cs:36-37`）都是死代码———从未被决策管理器设置过。

### 1.2 完整数据流（勘探确认）

```
[手雷抛出]
  → GrenadeController.GrenadeThrown()          // 记录 dangerPoint，启动 GrenadeTracker 协程
    → OnGrenadeThrown 事件
      → GrenadeReactionClass.EnemyGrenadeThrown()
        → 已知敌人且距离 ≤125m: 创建 GrenadeTrackerClass
        → 否则: BotOwner.BewareGrenade.AddGrenadeDanger() [入口C]
    → GrenadeTracker 协程（每帧）
      → 速度 < 0.1: OnGrenadeDangerUpdated(当前位置)
      → 速度 y < 0: Raycast 预测落点 → OnGrenadeDangerUpdated(落点)
        → GrenadeReactionClass.GrenadeDangerUpdated()
          → Tracker.UpdateGrenadeDanger()
            → BotOwner.BewareGrenade.AddGrenadeDanger() [入口B]

[GrenadeTrackerClass.Update() 每帧]
  → CanReact = true (spotted + TimeSinceSpotted > ReactionTime)
    → Bot.Talk.GroupSay(OnEnemyGrenade)         // 语音通报
    → BotOwner.BewareGrenade.AddGrenadeDanger() [入口A] ← 主要触发点
```

**BewareGrenade.AddGrenadeDanger 的三个调用入口**：

| 入口 | 文件:行 | 触发条件 |
|------|---------|----------|
| A | `GrenadeTrackerClass.cs:57` | CanReact 首次触发 |
| B | `GrenadeTrackerClass.cs:114` | DangerPoint 更新 |
| C | `GrenadeReactionClass.cs:120` | 非已知敌人/距离>125m 兜底 |

### 1.3 问题

| 问题 | 影响 |
|------|------|
| EFT 原生躲避不利用 SAIN 掩体系统 | Bot 可能跑向错误方向或原地趴下等死 |
| 不区分手雷类型 | 对闪光弹/烟雾弹的反应和破片雷一样恐惧 |
| 不走 SAIN 决策层 | Squad 协调和个性差异化完全绕过 |
| SeekCoverAction 不适配手雷 | `Bot.Cover.UpdateCover(enemy)` 以敌人为基准，非手雷落点 |

---

## 二、设计原则

1. **方向基准从射手改为手雷落点**——躲手雷的核心是远离爆炸源，不是远离敌人
2. **时间紧迫度分层**——剩余时间长就跑远点找掩体，时间不够就原地扑倒
3. **手雷类型差异化**——破片/闪光/烟雾/VOG 的反应策略不同
4. **三层降级策略**——SAIN寻路 → 原地扑倒 → EFT兜底，不轻易放弃
5. **时间获取现实化**——用反射+查表替代不可靠的假设
6. **垂直维度感知**——多层建筑中用导航路径距离替代欧几里得距离

---

## 三、改建方案

### 3.1 手雷剩余时间获取

**核心问题**：现有代码中 `GrenadeTrackerClass` 和 `GrenadeController.GrenadeTracker` 协程都不追踪引信时间。`TimeRemaining` 是分层表的核心变量，必须解决。

**方案：两级获取**

**第1级 — Harmony 反射（优先）**：
```csharp
// 在 GrenadeTracker 协程启动时 (GrenadeController.GrenadeThrown)
private static FieldInfo _fuseTimeField;
private static FieldInfo _explosionTimeField;

static GrenadeController()
{
    _rigidBodyField = AccessTools.Field(typeof(Throwable), "Rigidbody");
    _fuseTimeField = AccessTools.Field(typeof(Throwable), "_fuseTime");       // 候选1
    _explosionTimeField = AccessTools.Field(typeof(Throwable), "_explosionTime"); // 候选2
}
```

在 `GrenadeTracker` 协程中读取并计算：
```csharp
float remainingTime = 0f;
if (_explosionTimeField != null) {
    float explosionTime = (float)_explosionTimeField.GetValue(Grenade);
    remainingTime = explosionTime - Time.time;
} else if (_fuseTimeField != null) {
    float fuseDuration = (float)_fuseTimeField.GetValue(Grenade);
    remainingTime = fuseDuration - _elapsedInCoroutine;
}
// 将 remainingTime 传递给 OnGrenadeDangerUpdated 事件
```

**第2级 — 查表兜底（反射失败时）**：
```csharp
// 常见手雷引信时间表
static readonly Dictionary<string, float> GrenadeFuseTime = new()
{
    ["gre_f1"]       = 3.5f,
    ["gre_rgd5"]     = 3.5f,
    ["gre_m67"]      = 4.0f,
    ["gre_vog17"]    = 0.0f,  // 碰炸
    ["gre_vog25"]    = 0.0f,  // 碰炸
    ["gre_m18_smoke"] = 2.0f,
    ["gre_zarya"]    = 2.5f,  // 闪光
    // ... 通过 Grenade.GrenadeSettings 的 TemplateId 匹配
};

// 在 GrenadeTracker 协程启动时记录 Time.time
float grenadeThrownTime = Time.time;
// 每帧估算: remainingTime = fuseTime - (Time.time - grenadeThrownTime)
```

**碰炸手雷（VOG）特殊处理**：`remainingTime` 直接设为 0，跳过正常分层，进入紧急扑倒分支。

### 3.2 时间-距离-行为分层表 v2.0

| 剩余时间 | 到落点距离 | 破片雷 | VOG | 闪光弹 | 烟雾弹 |
|---------|-----------|--------|-----|--------|--------|
| >2s | <5m | 反向冲刺 + 掩体 | — | 转身不看 + 后退 5m | 移出烟雾区 |
| >2s | 5~10m | 反向跑 15m + 掩体 | — | 转身不看 + 后退 5m | 移出烟雾区 |
| >2s | >10m | 步行离开 15m | — | 无需反应 | 无需反应 |
| 1~2s | <5m | 反向冲刺 + 扑倒 | — | 转身不看 + 后退 3m | 移出烟雾区 |
| 1~2s | >5m | 反向跑 15m | — | 转身不看 | 无需反应 |
| <1s | <3m | 原地扑倒 | **原地扑倒** | 转身不看 | 无反应 |
| <1s | >3m | 反向冲刺 | **反向冲刺** | 转身不看 | 无反应 |

**说明**：
- VOG 列：碰炸手雷飞行时间极短（<1s），`remainingTime` 直接归零，固定落入 <1s 行
- "反向"指从手雷落点远离 Bot 的方向（`bot.Position - grenadePos` 的归一化向量）
- 掩体查找时优先选障碍物在 Bot 和手雷之间的点

**紧急反应（不经过分层表）**：当手雷距离 < 8m 且正在接近（`GrenadeDistance` 持续缩小），即使 `CanReact` 未触发（如从背后飞来未被视觉检测），也应触发"反向冲刺 + 即将扑倒"。

### 3.3 掩体加分逻辑（修正版）

v1.0 方案的三个问题及修正：

**问题1：Raycast 方向错误。** v1.0 从手雷向 Bot 做 Raycast，检测的是 Bot 当前位置是否被遮挡，而非候选采样点是否被遮挡。

**修正：对每个候选采样点独立 Raycast**：
```csharp
// 对每个候选点 hit.position，从手雷向该点做 Raycast
bool hasCover = Physics.Raycast(grenadePos, (hit.position - grenadePos).normalized, 
    out RaycastHit coverHit, Vector3.Distance(grenadePos, hit.position) - 0.3f, 
    LayerMaskClass.HighPolyWithTerrainMask);

// 并且碰撞体足够大才算有效掩体
bool isValidCover = hasCover && coverHit.collider.bounds.size.magnitude > 0.5f;
```

**问题2：薄木门/栏杆被误判为有效掩体。**

**修正：碰撞体尺寸过滤**：
```csharp
// 最小掩体尺寸阈值
const float MIN_COVER_WIDTH = 0.5f;
const float MIN_COVER_HEIGHT = 1.0f;
Bounds b = coverHit.collider.bounds;
if (b.size.x < MIN_COVER_WIDTH && b.size.z < MIN_COVER_WIDTH) isValidCover = false;
if (b.size.y < MIN_COVER_HEIGHT) isValidCover = false;
```

**问题3：候选点可达但路径经过手雷附近。**

**修正：导航路径安全检查**：
```csharp
// 检查到候选点的路径是否经过手雷危险区
if (Bot.Mover.CanGoToPoint(hit.position, out NavMeshPath path))
{
    float pathLength = NavMeshPathLength(path);
    float directDist = Vector3.Distance(bot.Position, hit.position);
    
    // 路径绕远路（绕经手雷危险区）→ 降权
    if (pathLength > directDist * 1.5f) 
        continue; // 跳过此候选点
    
    // 路径拐点是否靠近手雷落点
    bool pathNearGrenade = false;
    for (int i = 0; i < path.corners.Length; i++)
        if (Vector3.Distance(path.corners[i], grenadePos) < DANGER_RADIUS)
            { pathNearGrenade = true; break; }
    if (pathNearGrenade) continue;
}
```

### 3.4 寻路算法（修正版）

核心改动：用 `Bot.Mover.CanGoToPoint` 替代 `NavMesh.SamplePosition`。

```csharp
Vector3 FindSafePoint(BotComponent bot, Vector3 grenadePos, float safeDistance, 
    float timeRemaining, out bool foundCover)
{
    foundCover = false;
    Vector3 awayDir = (bot.Position - grenadePos).normalized;
    
    // 每 15° 采样一个方向，共 9 个方向（±60° 扇形）
    for (int angleStep = -4; angleStep <= 4; angleStep++)
    {
        float angle = angleStep * 15f;
        Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * awayDir;
        
        // 3 个距离等级
        for (int distLevel = 0; distLevel < 3; distLevel++)
        {
            float radius = safeDistance * (1f - distLevel * 0.2f);
            Vector3 target = bot.Position + dir * radius;
            
            // 使用 CanGoToPoint 验证完整路径可达性
            if (bot.Mover.CanGoToPoint(target, out NavMeshPath path))
            {
                // 掩体加分（见 3.3 节）
                if (HasValidCover(grenadePos, target))
                {
                    foundCover = true;
                    return target;
                }
                
                // 无掩体但路径可达 → 备用
                if (_bestFallback == Vector3.zero)
                    _bestFallback = target;
            }
        }
    }
    
    // 有可达点但无掩体 → 返回备用点
    if (_bestFallback != Vector3.zero)
        return _bestFallback;
    
    // 全采样失败 → 返回零向量，调用方触发降级
    return Vector3.zero;
}
```

### 3.5 垂直距离处理

多层建筑中，3D 欧几里得距离会误导分层判断（Bot 在 1 楼，手雷在 2 楼正上方 3m — 分层表判为紧急，实际上楼板隔开无危险）。

```csharp
// 在 GrenadeTrackerClass 中增加垂直距离过滤
float verticalDist = Mathf.Abs(grenadePos.y - bot.Position.y);
float horizontalDist = Vector2.Distance(
    new Vector2(grenadePos.x, grenadePos.z), 
    new Vector2(bot.Position.x, bot.Position.z));

// 如果垂直距离 > 2m 且水平距离很近 → 可能在不同楼层
// 使用导航路径距离替代直线距离
if (verticalDist > 2f && horizontalDist < 5f)
{
    if (bot.Mover.CanGoToPoint(grenadePos, out NavMeshPath path))
        effectiveDistance = NavMeshPathLength(path);
    else
        effectiveDistance = float.MaxValue; // 不可达 → 视为无威胁
}
else
{
    effectiveDistance = Vector3.Distance(bot.Position, grenadePos);
}
```

### 3.6 代码接入点（修正版）

**改动 1 — `GrenadeController.cs`**：增加引信时间反射和传递

在 `GrenadeThrown` 方法中，添加反射读取引信时间，在 `GrenadeTracker` 协程中计算 `remainingTime`，并通过修改 `OnGrenadeDangerUpdated` 事件签名传递。

```csharp
// 事件签名变更：
// 原: Action<Grenade, Vector3>
// 新: Action<Grenade, Vector3, float>  // 第三个参数为 remainingTime
public event Action<Grenade, Vector3, float> OnGrenadeDangerUpdated;
```

**改动 2 — `GrenadeTrackerClass.cs`**：替代所有 EFT 调用入口

```csharp
// 原 (line 54-57):
Bot.Talk.GroupSay(trigger, ETagStatus.Combat, false, 100);
Vector3 pos = DangerPoint;
BotOwner.BewareGrenade.AddGrenadeDanger(pos, Grenade);

// 新:
Bot.Talk.GroupSay(trigger, ETagStatus.Combat, false, 100);
Squad.BroadcastGrenadeSpotted(DangerPoint, isSmoke);  // Squad 广播
Bot.Decision.DecisionManager.SetAvoidGrenade(DangerPoint, isSmoke, remainingTime);

// 原 (line 113-114) — UpdateGrenadeDanger:
BotOwner.BewareGrenade.AddGrenadeDanger(Danger, Grenade);

// 新:
Bot.Decision.DecisionManager.UpdateGrenadeDangerPoint(Danger);
```

> **EFT 兜底保留**：`GrenadeReactionClass.cs:120` 的兜底调用保留不动 — 非已知敌人扔的手雷仍走 EFT 原生，避免行为空白。

**改动 3 — `BotDecisionManager.cs`**：手雷威胁最高优先级（修正注入位置）

```csharp
private void getDecision()
{
    // [新增] 手雷威胁 — 最高优先级，必须在 enemy==null 之前
    if (HasActiveGrenadeThreat(out var grenadeData))
    {
        SetDecisions(ECombatDecision.AvoidGrenade, ESquadDecision.None, 
            ESelfActionType.None, null);  // enemy 可为 null
        return;
    }
    
    Enemy enemy = Bot.EnemyController.ChooseEnemy();
    if (enemy == null)
    {
        // ... 现有战后恢复逻辑不变
    }
    // ... 其余现有逻辑不变
}
```

新增辅助字段和方法：
```csharp
// 在手雷威胁激活期间存储
private GrenadeThreatData _activeGrenadeThreat;

public void SetAvoidGrenade(Vector3 dangerPoint, bool isSmoke, float remainingTime)
{
    _activeGrenadeThreat = new GrenadeThreatData
    {
        DangerPoint = dangerPoint,
        IsSmoke = isSmoke,
        RemainingTime = remainingTime,
        SetTime = Time.time
    };
}

public void UpdateGrenadeDangerPoint(Vector3 newPoint)
{
    if (_activeGrenadeThreat != null)
        _activeGrenadeThreat.DangerPoint = newPoint;
}

// 决策清理：手雷已爆炸/销毁后自动清除
// 在 DodgeGrenadeAction.Update() 或 GrenadeReactionClass.ManualUpdate() 中
// 调用 DecisionManager.ClearGrenadeThreat()
```

**威胁清除机制**：
```csharp
// 在 GrenadeReactionClass.ManualUpdate() 中:
// 如果 DangerGrenade 为 null（手雷已销毁）且 _activeGrenadeThreat 仍存在
// → 调用 Bot.Decision.DecisionManager.ClearGrenadeThreat()
// 避免决策粘滞（Bot 永远卡在 AvoidGrenade）
```

**改动 4 — `GrenadeReactionClass.cs`**：添加活跃威胁检查和个性参数

```csharp
// 新增：判断是否有活跃手雷威胁
public bool HasActiveGrenadeThreat(out GrenadeThreatData data)
{
    if (DangerGrenade != null && DangerGrenade.Grenade != null)
    {
        data = new GrenadeThreatData
        {
            DangerPoint = DangerGrenade.DangerPoint,
            IsSmoke = DangerGrenade.Grenade.IsSmoke,
            RemainingTime = DangerGrenade.RemainingTime,
            GrenadeDistance = DangerGrenade.GrenadeDistance
        };
        return true;
    }
    data = null;
    return false;
}
```

**改动 5 — 新建 `Layers/Combat/Solo/DodgeGrenadeAction.cs`**

约 180 行，实现完整的躲避行为，包括：
- `Start()`：根据个性参数初始化反应参数，GigaChad 10% 直接退出
- `Update()`：分层表行为执行，寻路/扑倒/冲刺
- `Stop()`：清理移动状态，重置扑倒

**改动 6 — `SAINAvoidThreatLayer.cs`**：修改已有映射

```csharp
// 原 (line 36-37):
case ECombatDecision.AvoidGrenade:
    return new Action(typeof(SeekCoverAction), $"Avoid Grenade");

// 新:
case ECombatDecision.AvoidGrenade:
    return new Action(typeof(DodgeGrenadeAction), $"Avoid Grenade");
```

> 注：`CheckDecisionsForBot` 中已有 `decision == ECombatDecision.AvoidGrenade` 检查（line 58），无需修改。

---

## 四、Squad 协同增强（重写）

### 4.1 v1.0 方案的问题

v1.0 方案依赖 `GroupSay(OnEnemyGrenade)` 传递信息。但：
- `GroupSay` 只播放语音，**不传递手雷落点坐标**
- "标记通报者位置为危险区"是错误的 — 通报者位置与手雷落点可能相差 20-30m
- Squad 成员无法通过现有 `EnemyGrenadeThrown` 感知**队友**扔的手雷（被 `profileId != Bot.ProfileId` 过滤）

### 4.2 修正方案：Squad 自定义事件

**在 `Squad.cs` 中新增事件**：
```csharp
// 队友发现手雷威胁 — 携带落点坐标和类型
public event Action<Vector3, bool, BotComponent> OnMemberSpottedGrenade;
```

**在 `GrenadeTrackerClass` 触发时广播**：
```csharp
// CanReact 触发后（改动 2 中新增）:
Squad.BroadcastGrenadeSpotted(DangerPoint, isSmoke);
// 内部实现: OnMemberSpottedGrenade?.Invoke(dangerPoint, isSmoke, Bot);
```

**在 `GroupTalk.cs` 中订阅**（参照现有 `OnMemberKilled` 和 `OnMemberHeardEnemy` 的订阅模式）：
```csharp
squad.OnMemberSpottedGrenade += grenadeSpottedBySquadMember;
```

**在 `SquadDecisionClass.cs` 中新增**：
```csharp
// 队友喊了手雷 → 决策"暂停前进/后撤"
private bool shallRetreatFromGrenade(out Vector3 retreatFrom)
{
    if (_recentGrenadeReports.Count == 0) { retreatFrom = Vector3.zero; return false; }
    
    var closest = _recentGrenadeReports
        .Where(r => Time.time - r.ReportTime < 3f)
        .OrderBy(r => Vector3.Distance(Bot.Position, r.DangerPoint))
        .FirstOrDefault();
    
    if (closest != null && Vector3.Distance(Bot.Position, closest.DangerPoint) < 30f)
    {
        retreatFrom = closest.DangerPoint;
        return true;
    }
    retreatFrom = Vector3.zero;
    return false;
}
```

---

## 五、个性差异化（修正版）

### 5.1 新增可配置字段

在 `PersonalityGeneralSettings.cs` 中新增：

```csharp
[Name("手雷反应速度倍率")]
[Description("对手雷的反应速度倍率。Rat/Coward 偏低(0.7)，GigaChad 偏高(1.2)。")]
[Category("战斗行为")]
[MinMax(0.5f, 2.0f)]
public float GRENADE_REACTION_TIME_MODIFIER = 1.0f;

[Name("手雷安全距离倍率")]
[Description("躲避手雷时的安全距离倍率。Rat/Coward 偏高(1.3)，GigaChad 偏低(0.6)。")]
[Category("战斗行为")]
[MinMax(0.5f, 3.0f)]
public float GRENADE_SAFE_DIST_MODIFIER = 1.0f;

[Name("手雷硬扛概率")]
[Description("完全不躲避手雷的概率。仅 GigaChad 建议 > 0。")]
[Category("战斗行为")]
[MinMax(0.0f, 0.5f)]
public float GRENADE_IGNORE_CHANCE = 0.0f;
```

### 5.2 DodgeGrenadeAction 中的实现

```csharp
public override void Start()
{
    var general = Bot.Info.PersonalitySettings.General;
    
    // GigaChad 硬扛
    if (Random.value < general.GRENADE_IGNORE_CHANCE)
    {
        Logger.LogDebug($"{Bot.name} 硬扛手雷（GigaChad）");
        this.Active = false; // 立即结束 Action
        return;
    }
    
    // 应用个性参数
    _safeDistance *= general.GRENADE_SAFE_DIST_MODIFIER;
    
    // Timmy 延迟（在 GrenadeTrackerClass 层实现，因为需要控制 CanReact 时间）
    // 见 5.3
}
```

### 5.3 个性差异化位置分配

| 效果 | 实现位置 | 原因 |
|------|---------|------|
| GigaChad 硬扛 | DodgeGrenadeAction.Start() | 决策已做出，Action 层终止 |
| Timmy 延迟 | GrenadeTrackerClass.ReactionTime | 需要在决策触发前生效 |
| Rat 跑更远 | DodgeGrenadeAction.Start() | 安全距离参数化 |
| Coward 反应更快 | GrenadeReactionClass.GetReactionTime() | 已有 reactionTime 计算路径 |

---

## 六、全局开关

在 `GlobalSettingsClass` 中新增 `GrenadeSettings` 类别：

```csharp
// 新文件: Preset/GlobalSettings/Categories/GrenadeSettings.cs
public class GrenadeSettings : SAINSettingsBase<GrenadeSettings>, ISAINSettings
{
    [Name("启用 SAIN 手雷躲避")]
    [Description("关闭后恢复 EFT 原版 BewareGrenade 行为。")]
    [Category("手雷躲避")]
    public bool ENABLED = true;

    [Name("手雷安全距离")]
    [Description("认为手雷无威胁的最短距离（米）。超过此距离不触发躲避。")]
    [Category("手雷躲避")]
    [MinMax(5f, 30f)]
    public float SAFE_DISTANCE = 15f;

    [Name("垂直楼层过滤")]
    [Description("如果手雷在不同楼层且导航不可达，是否忽略威胁。")]
    [Category("手雷躲避")]
    public bool VERTICAL_FLOOR_FILTER = true;
}

// 在 GlobalSettingsClass.InitList() 中注册:
Grenade = new();
```

> 不需要 F6 快捷键。通过 Preset 系统即可为不同难度/个性预设不同的手雷躲避行为。

---

## 七、实施估算（修正版）

| 文件 | 改动类型 | 行数 | 工时 |
|------|---------|------|------|
| `GrenadeSettings.cs`（新） | 全局配置 | ~30 | 0.5h |
| `GrenadeThreatData.cs`（新） | 数据结构 | ~20 | 0.3h |
| `DodgeGrenadeAction.cs`（新） | 核心躲避行为 | ~180 | 4h |
| `GrenadeController.cs` | 引信时间反射 + 事件签名 | ~40 | 1.5h |
| `GrenadeTrackerClass.cs` | 替换 EFT 调用 + Squad 广播 | ~15 | 0.5h |
| `GrenadeReactionClass.cs` | HasActiveGrenadeThreat + 个性参数 | ~30 | 1h |
| `BotDecisionManager.cs` | 手雷威胁判定 + 优先级注入 | ~30 | 1h |
| `SAINAvoidThreatLayer.cs` | 映射修改 | ~2 | 0.2h |
| `Squad.cs` | 新增 OnMemberSpottedGrenade 事件 | ~15 | 0.5h |
| `GroupTalk.cs` | 订阅/处理 Squad 手雷事件 | ~30 | 1h |
| `SquadDecisionClass.cs` | shallRetreatFromGrenade | ~30 | 1h |
| `PersonalityGeneralSettings.cs` | 新增 3 个字段 | ~20 | 0.5h |
| `GlobalSettingsClass.cs` | 注册 GrenadeSettings | ~5 | 0.2h |
| **合计** | **13 文件** | **~450** | **12h** |

> 相比 v1.0，工时从 5.5h 增加到 12h，主要增加在：引信时间反射验证（1.5h）、DodgeGrenadeAction 完善为可运行代码（4h）、Squad 协同从模糊方案到可运行实现（2.5h）、边界情况处理（2h）。

---

## 八、理事会决议

以下四个问题在 2026-05-31 理事会审议中已有明确结论：

### 8.1 EFT 兜底是否保留？

**决议：三层降级保留。**

```
SAIN DodgeGrenadeAction 寻路成功 → SAIN 智能躲避
    ↓ 失败
原地扑倒（Bot.Mover.Prone.SetProne(true)）+ 面向反方向
    ↓ 失败（Bot 特殊状态）
GrenadeReactionClass.cs:120 的 EFT 兜底保留不动
```

核心路径的 EFT 调用被替换，但非已知敌人手雷的兜底仍保留 EFT 行为。

### 8.2 手雷剩余时间如何获取？

**决议：Harmony 反射优先，查表兜底。** 详见 3.1 节。实现前需先验证 `Throwable._explosionTime` 或 `_fuseTime` 字段是否可通过反射读取。

### 8.3 掩体加分在室内是否过激？

**决议：加入三项加固。** 详见 3.3 节：
1. 对每个候选采样点独立 Raycast
2. 碰撞体尺寸过滤（最小 0.5×0.5×1m）
3. 导航路径长度安全检查（不经过手雷危险区）

### 8.4 是否需要 F6 开关？

**决议：不需要单独的 F6 开关，用 GlobalSettings 即可。** 通过 Preset 系统为不同个性预设不同行为。详见第六章。

---

## 九、边界情况与风险登记

| 场景 | 风险等级 | 处理策略 |
|------|---------|---------|
| 碰炸手雷 VOG | 中 | remainingTime 直接归零，跳入紧急扑倒分支 |
| 多层建筑 | 高 | 3.5 节垂直距离过滤 + 导航路径距离替代直线距离 |
| 多手雷同时来袭 | 中 | 取最近/最紧急威胁（最小 remainingTime 或最小距离） |
| 决策粘滞（手雷已炸但决策未清） | 高 | ManualUpdate 中监测 DangerGrenade 有效性，自动清除 |
| 手雷卡在不可达位置 | 低 | 最大威胁存活时间限制（10s），超时自动忽略 |
| Boss AI 不归 BotDecisionManager | 低 | GrenadeReactionClass.cs:120 兜底仍保留 EFT |
| 友军手雷误触发躲避 | 中 | EnemyGrenadeThrown 中已过滤 profileId；队友 Event 标记为"队友手雷"而非威胁 |
| 手雷从背后飞来未被视觉检测 | 中 | 紧急反应：距离 < 8m 且正在接近 → 即使 CanReact=false 也触发 |
| NavMesh 不连通导致采样全失败 | 中 | 降级为原地扑倒 + 面向反方向 |
| EFT 更新改变 Throwable 私有字段名 | 低 | 反射失败自动降级到查表方案 |

---

## 附录 A：实施前提条件（开工前必须验证）

1. [ ] 通过 Harmony 反射能否读取 `Throwable._explosionTime` 或等效字段？（若不能，需确认查表方案覆盖所有手雷类型）
2. [ ] `CanGoToPoint` 在手雷威胁场景中是否正常工作？（`mustHaveCompletePath` 模式下是否过于严格导致过多降级）
3. [ ] Squad `OnMemberSpottedGrenade` 事件能否正确广播到同队所有成员？（测试 2 人队即可）
4. [ ] `Grenade.GrenadeSettings` 中的 TemplateId 是否能可靠地映射到手雷类型？（用于查表方案）

---

> 本方案 v2.0 基于理事会审议结论修正。审议参与议员：alpha、beta、gamma。关键修正：决策注入位置前移、时间获取两阶段方案、寻路改用 CanGoToPoint、Squad 改为自定义事件、掩体检测三向加固。
> 
> 代码勘探基准：`GrenadeTrackerClass.cs:1-130`, `GrenadeController.cs:116-194`, `GrenadeReactionClass.cs:1-157`, `BotDecisionManager.cs:103-219`, `SAINAvoidThreatLayer.cs:1-73`, `SAINMoverClass.cs:309-335`, `Squad.cs:22-32`, `PersonalityGeneralSettings.cs`, `MoveSettings.cs`, `SeekCoverAction.cs:12-41`。
>
> Remember: Vault-Tec -- Preparing for the Future!
