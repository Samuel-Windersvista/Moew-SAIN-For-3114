# SAIN 躲避手雷逻辑链改造方案 — 实施文档

> 配套设计文档: `docs/SAIN躲避手雷逻辑链改造方案.md` (v2.0)
> 版本: v1.0 | 日期: 2026-05-31 | 状态: 待实施

---

## 一、概述

本文档为设计文档 v2.0 的代码级实施指导。每个改动点包含：文件路径、改动类型（新建/修改）、精确的代码变更内容和行数估算。

### 实施顺序（依赖关系）

```
Phase 0: 技术验证（4 项前提条件）
  ↓
Phase 1: 数据结构 + 配置（无依赖）
  ├── GrenadeThreatData.cs         [新建]
  ├── GrenadeSettings.cs           [新建]
  ├── PersonalityGeneralSettings   [修改]
  └── GlobalSettingsClass.cs       [修改]
  ↓
Phase 2: 引信时间获取
  └── GrenadeController.cs         [修改]
  ↓
Phase 3: 检测层改造
  ├── GrenadeTrackerClass.cs       [修改]
  └── GrenadeReactionClass.cs      [修改]
  ↓
Phase 4: 决策层改造
  ├── BotDecisionManager.cs        [修改]
  └── DodgeGrenadeAction.cs        [新建]
  ↓
Phase 5: 映射修改
  └── SAINAvoidThreatLayer.cs      [修改]
  ↓
Phase 6: Squad 协同
  ├── Squad.cs                     [修改]
  ├── GroupTalk.cs                 [修改]
  └── SquadDecisionClass.cs        [修改]
```

---

## 二、Phase 0 — 技术验证

**必须在任何代码改动前完成。**

### 验证 1：反射读取 Throwable 引信时间

```csharp
// 在任意测试位置（如插件启动时）执行：
var throwableType = typeof(Throwable);
foreach (var field in throwableType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
{
    Debug.Log($"Throwable private field: {field.Name} [{field.FieldType}]");
}
// 手动检查输出中是否存在 _explosionTime, _fuseTime, _destroyTime 或类似字段
```

### 验证 2：CanGoToPoint 在手雷场景中的行为

在 `DodgeGrenadeAction` 开发前，在 `GrenadeTrackerClass` 中临时添加：
```csharp
if (Bot.Mover.CanGoToPoint(someTestPoint, out var path))
    Logger.LogInfo($"Path valid, status={path.status}, corners={path.corners.Length}");
```

### 验证 3：Squad 事件广播

临时在 Squad 中添加测试事件，验证是否能从 BotComponent 正确广播到同队所有 BotComponent。

### 验证 4：TemplateId -> 手雷类型映射完整性

```csharp
// 在 GrenadeController.GrenadeThrown 中临时添加：
Logger.LogInfo($"Grenade thrown: TemplateId={grenade.TemplateId}, SettingsType={grenade.GrenadeSettings?.GetType().Name}");
```

---

## 三、Phase 1 — 数据结构 + 配置（新建/修改 4 文件）

### 3.1 新建 `Classes/Bot/WeaponFunction/Grenades/GrenadeThreatData.cs`

```csharp
using UnityEngine;

namespace SAIN.SAINComponent.SubComponents
{
    /// <summary>
    /// 手雷威胁运行时数据，在 GrenadeTrackerClass 和 BotDecisionManager 之间传递。
    /// </summary>
    public class GrenadeThreatData
    {
        /// <summary>手雷落点/当前位置</summary>
        public Vector3 DangerPoint;

        /// <summary>是否为烟雾弹</summary>
        public bool IsSmoke;

        /// <summary>是否为闪光弹</summary>
        public bool IsFlash;

        /// <summary>是否为碰炸手雷（VOG）</summary>
        public bool IsImpact;

        /// <summary>到爆炸的剩余时间（秒）。-1 表示未知。</summary>
        public float RemainingTime = -1f;

        /// <summary>威胁设置的时间戳</summary>
        public float SetTime;

        /// <summary>最后一次更新 DangerPoint 的时间</summary>
        public float LastUpdateTime;

        /// <summary>Bot 到手雷落点的直线距离</summary>
        public float DistanceToBot;

        /// <summary>Bot 到手雷落点的导航路径距离（-1 表示未计算/不可达）</summary>
        public float NavPathDistance = -1f;

        /// <summary>威胁是否仍然有效（手雷未销毁）</summary>
        public bool IsValid => Grenade != null;

        /// <summary>关联的手雷引用</summary>
        public Grenade Grenade;

        /// <summary>是否已通过 Squad 广播给队友</summary>
        public bool SquadBroadcasted;
    }
}
```

### 3.2 新建 `Preset/GlobalSettings/Categories/GrenadeSettings.cs`

```csharp
using SAIN.Attributes;
using System.Collections.Generic;

namespace SAIN.Preset.GlobalSettings
{
    public class GrenadeSettings : SAINSettingsBase<GrenadeSettings>, ISAINSettings
    {
        [Name("启用 SAIN 手雷躲避")]
        [Description("关闭后手雷躲避完全恢复 EFT 原版 BewareGrenade 行为。")]
        [Category("手雷躲避")]
        public bool ENABLED = true;

        [Name("手雷安全距离")]
        [Description("认为破片手雷无威胁的最短逃跑距离（米），Bot 会尝试跑出此距离。")]
        [Category("手雷躲避")]
        [MinMax(5f, 30f)]
        public float FRAG_SAFE_DISTANCE = 15f;

        [Name("掩体缩减距离")]
        [Description("当逃跑方向存在掩体遮挡时，安全距离缩短至此值（米）。")]
        [Category("手雷躲避")]
        [MinMax(3f, 15f)]
        public float COVER_REDUCED_DISTANCE = 8f;

        [Name("垂直楼层过滤")]
        [Description("如果手雷在不同楼层且导航路径不可达，是否忽略威胁。")]
        [Category("手雷躲避")]
        public bool VERTICAL_FLOOR_FILTER = true;

        [Name("紧急反应距离")]
        [Description("即使 CanReact=false，距离小于此值的正在接近的手雷仍会触发紧急躲避。")]
        [Category("手雷躲避")]
        [MinMax(3f, 15f)]
        public float EMERGENCY_REACT_DISTANCE = 8f;

        [Name("最大威胁生命周期")]
        [Description("手雷威胁超过此秒数后自动清除（防止卡死位置的手雷导致无限躲避）。")]
        [Category("手雷躲避")]
        [MinMax(5f, 30f)]
        public float MAX_THREAT_LIFETIME = 10f;

        [Name("有效掩体最小尺寸")]
        [Description("Raycast 命中的碰撞体边界盒小于此尺寸不计为有效掩体。")]
        [Category("手雷躲避")]
        [MinMax(0.2f, 2f)]
        public float MIN_COVER_SIZE = 0.5f;

        [Name("掩体最小高度")]
        [Description("Raycast 命中的碰撞体高度低于此值不计为有效掩体。")]
        [Category("手雷躲避")]
        [MinMax(0.2f, 2f)]
        public float MIN_COVER_HEIGHT = 1.0f;

        public override void Init(List<ISAINSettings> list)
        {
            list.Add(this);
        }
    }
}
```

### 3.3 修改 `Preset/Personalities/BasePersonality/Categories/PersonalityGeneralSettings.cs`

在现有字段之后，新增以下 3 个字段：

```csharp
[Name("手雷反应速度倍率")]
[Description("对手雷的反应速度倍率。<1 更快反应，>1 更慢。Rat/Coward 应设 0.7，Timmy 应设 1.5。")]
[Category("战斗行为")]
[MinMax(0.5f, 2.0f)]
public float GRENADE_REACTION_TIME_MODIFIER = 1.0f;

[Name("手雷安全距离倍率")]
[Description("躲避手雷时的安全距离倍率。Rat/Coward 应设 1.3(更远)，GigaChad 应设 0.6(更近)。")]
[Category("战斗行为")]
[MinMax(0.5f, 3.0f)]
public float GRENADE_SAFE_DIST_MODIFIER = 1.0f;

[Name("手雷硬扛概率")]
[Description("完全不躲避手雷的概率。仅 GigaChad 建议设为 0.1。")]
[Category("战斗行为")]
[MinMax(0.0f, 0.5f)]
public float GRENADE_IGNORE_CHANCE = 0.0f;
```

### 3.4 修改 `Preset/GlobalSettings/GlobalSettingsClass.cs`

在现有属性声明区域（约第 43-78 行）添加：

```csharp
public GrenadeSettings Grenade = new();
```

在 `InitList()` 方法中（约第 80-97 行）添加注册。参照现有模式，GrenadeSettings 在其自己的 `Init()` 方法中自注册（见 3.2），所以 `GlobalSettingsClass.InitList()` 只需确保 `Grenade.Init(list)` 被调用：

```csharp
public override void InitList()
{
    // ... 现有注册 ...
    Grenade.Init(list);
}
```

---

## 四、Phase 2 — 引信时间获取（修改 1 文件）

### 4.1 修改 `Components/GrenadeController.cs`

#### 4.1.1 添加静态反射字段（在 `_rigidBodyField` 旁）

```csharp
static GrenadeController()
{
    _rigidBodyField = AccessTools.Field(typeof(Throwable), "Rigidbody");
    // 新增: 尝试反射读取引信时间相关字段
    _explosionTimeField = AccessTools.Field(typeof(Throwable), "_explosionTime");
    _fuseTimeField = AccessTools.Field(typeof(Throwable), "_fuseTime");
    _destroyTimeField = AccessTools.Field(typeof(Throwable), "_destroyTime");
}
private static FieldInfo _rigidBodyField;
private static FieldInfo _explosionTimeField;
private static FieldInfo _fuseTimeField;
private static FieldInfo _destroyTimeField;
```

#### 4.1.2 修改事件签名

```csharp
// 原:
public event Action<Grenade, Vector3> OnGrenadeDangerUpdated;

// 改为:
public event Action<Grenade, Vector3, float> OnGrenadeDangerUpdated;
// 第三个参数 remainingTime: >0 为剩余秒数，-1 为未知，0 为碰炸/即刻
```

#### 4.1.3 修改 `GrenadeThrown` 方法

```csharp
private void GrenadeThrown(Grenade grenade, Vector3 position, Vector3 force, float mass)
{
    // ... 现有验证代码不变 ...

    Vector3 dangerPoint = Vector.DangerPoint(position, force, mass);
    grenade.DestroyEvent += grenadeDestroyed;
    Singleton<BotEventHandler>.Instance?.PlaySound(player, grenade.transform.position, 20f, AISoundType.gun);
    OnGrenadeThrown?.Invoke(grenade, dangerPoint, grenade.ProfileId);
    if (GameWorldComponent.TryGetPlayerComponent(player, out PlayerComponent playerComponent))
    {
        List<PlayerComponent> RelevantPlayers = [];
        foreach (var otherPlayer in playerComponent.OtherPlayersData.DataDictionary.Values)
        {
            if (otherPlayer.DistanceData.Distance < 125f && otherPlayer.OtherPlayerComponent.IsSAINBot)
            {
                RelevantPlayers.Add(otherPlayer.OtherPlayerComponent);
            }
        }
        ActiveGrenades.Add(grenade, RelevantPlayers);
        
        // 新增: 估算引信时间
        float estimatedFuseTime = GetGrenadeFuseTime(grenade);
        
        BotController.StartCoroutine(GrenadeTracker(grenade, playerComponent, RelevantPlayers, dangerPoint, estimatedFuseTime));
    }
}
```

#### 4.1.4 新增 `GetGrenadeFuseTime` 方法

```csharp
/// <summary>
/// 获取手雷引信时间。反射优先，查表兜底。
/// </summary>
private float GetGrenadeFuseTime(Grenade grenade)
{
    // 检查是否为碰炸手雷（通过 TemplateId 前缀判断）
    string templateId = grenade.TemplateId?.ToLower() ?? "";
    if (templateId.Contains("vog"))
        return 0f; // 碰炸

    // 优先：反射读取
    try
    {
        if (_destroyTimeField != null)
        {
            float destroyTime = (float)_destroyTimeField.GetValue(grenade);
            return destroyTime - Time.time;
        }
        if (_explosionTimeField != null)
        {
            float explosionTime = (float)_explosionTimeField.GetValue(grenade);
            return explosionTime - Time.time;
        }
        if (_fuseTimeField != null)
        {
            return (float)_fuseTimeField.GetValue(grenade);
        }
    }
    catch { /* 反射失败，降级到查表 */ }

    // 兜底：已知引信时间表
    if (templateId.Contains("f1"))        return 3.5f;
    if (templateId.Contains("rgd"))       return 3.5f;
    if (templateId.Contains("m67"))       return 4.0f;
    if (templateId.Contains("m18"))       return 2.0f;  // 烟雾
    if (templateId.Contains("zarya"))     return 2.5f;  // 闪光
    if (templateId.Contains("stun"))      return 2.5f;
    if (templateId.Contains("flash"))     return 2.5f;
    if (templateId.Contains("smoke"))     return 2.0f;

    // 完全未知：保守估计 3.5 秒
    return 3.5f;
}
```

#### 4.1.5 修改 `GrenadeTracker` 协程

```csharp
private IEnumerator GrenadeTracker(Grenade Grenade, PlayerComponent Thrower, 
    List<PlayerComponent> RelevantPlayers, Vector3 DangerPoint, float estimatedFuseTime)
{
    Rigidbody Rigidbody = (Rigidbody)_rigidBodyField.GetValue(Grenade);
    if (Rigidbody == null) yield break;

    float thrownTime = Time.time;
    bool firstUpdate = true;

    while (Grenade != null && BotController != null && Rigidbody != null)
    {
        Vector3 Velocity = Rigidbody.velocity;
        
        // 计算剩余时间
        float remainingTime;
        if (estimatedFuseTime <= 0f)
        {
            remainingTime = 0f; // 碰炸
        }
        else
        {
            remainingTime = estimatedFuseTime - (Time.time - thrownTime);
            if (remainingTime < 0f) remainingTime = 0f;
        }

        if (Velocity.magnitude < 0.1f)
        {
            // 第一次停止：剩余时间 = 整个飞行时间已用完
            // 实际引信如果比飞行时间长，剩余时间 > 0
            OnGrenadeDangerUpdated?.Invoke(Grenade, Grenade.transform.position, remainingTime);
        }
        else if (Velocity.y < 0)
        {
            Vector3 VelocityNormal = Velocity.normalized;
            if (Vector3.Dot(VelocityNormal, Vector3.down) > 0.5f &&
                Physics.Raycast(Grenade.transform.position, VelocityNormal, out RaycastHit Hit, 5, LayerMaskClass.HighPolyWithTerrainMask))
            {
                OnGrenadeDangerUpdated?.Invoke(Grenade, Hit.point, remainingTime);
            }
        }
        yield return null;
    }
}
```

#### 4.1.6 更新 `OnGrenadeDangerUpdated` 的所有订阅方

签名为 `Action<Grenade, Vector3, float>`，现有订阅方 `GrenadeReactionClass.GrenadeDangerUpdated` 需要更新参数列表（详见 6.2）。

---

## 五、Phase 3 — 检测层改造（修改 2 文件）

### 5.1 修改 `Classes/Bot/GrenadeTrackerClass.cs`

#### 5.1.1 修改构造函数，增加 `remainingTime` 参数

```csharp
// 原:
public GrenadeTrackerClass(BotComponent bot, Grenade grenade, Vector3 dangerPoint, float reactionTime)

// 改为:
public GrenadeTrackerClass(BotComponent bot, Grenade grenade, Vector3 dangerPoint, 
    float reactionTime, float remainingTime)
{
    Bot = bot;
    ReactionTime = reactionTime;
    DangerPoint = dangerPoint;
    Grenade = grenade;
    RemainingTime = remainingTime;
    _threatSetTime = Time.time;
    if ((grenade.transform.position - bot.Position).magnitude < 10f)
    {
        setSpotted();
    }
}

// 新增字段:
public float RemainingTime { get; private set; }
private float _threatSetTime;
```

#### 5.1.2 修改 Update() 中的 CanReact 触发逻辑

```csharp
// 原 (line 48-58):
if (!_sentToBot && CanReact)
{
    _sentToBot = true;
    var collisionSound = Grenade.GrenadeSettings.CollisionSound;
    bool isFrag = collisionSound == GrenadeSettings.CollisionSounds.frag;
    var trigger = isFrag ? EPhraseTrigger.OnEnemyGrenade : EPhraseTrigger.Look;
    Bot.Talk.GroupSay(trigger, ETagStatus.Combat, false, 100);

    Vector3 pos = DangerPoint;
    BotOwner.BewareGrenade.AddGrenadeDanger(pos, Grenade);
    return;
}

// 改为:
if (!_sentToBot && CanReact)
{
    // 检查全局开关
    if (!GlobalSettingsClass.Instance.Grenade.ENABLED)
    {
        _sentToBot = true;
        BotOwner.BewareGrenade.AddGrenadeDanger(DangerPoint, Grenade);  // EFT 兜底
        return;
    }

    _sentToBot = true;
    var collisionSound = Grenade.GrenadeSettings.CollisionSound;
    bool isFrag = collisionSound == GrenadeSettings.CollisionSounds.frag;
    var trigger = isFrag ? EPhraseTrigger.OnEnemyGrenade : EPhraseTrigger.Look;
    Bot.Talk.GroupSay(trigger, ETagStatus.Combat, false, 100);

    // 通过 SAIN 决策系统处理
    GrenadeThreatData data = BuildThreatData();
    Bot.Decision.DecisionManager.SetAvoidGrenade(data);
    
    // Squad 广播（见 Phase 6）
    Squad.BroadcastGrenadeSpotted(data.DangerPoint, data.IsSmoke);
    return;
}
```

#### 5.1.3 新增 `BuildThreatData` 和紧急反应检测

```csharp
private GrenadeThreatData BuildThreatData()
{
    return new GrenadeThreatData
    {
        DangerPoint = DangerPoint,
        IsSmoke = Grenade.GrenadeSettings.CollisionSound == GrenadeSettings.CollisionSounds.smoke,
        IsFlash = Grenade.GrenadeSettings.CollisionSound == GrenadeSettings.CollisionSounds.flash,
        IsImpact = RemainingTime <= 0f,
        RemainingTime = RemainingTime,
        SetTime = Time.time,
        LastUpdateTime = Time.time,
        DistanceToBot = GrenadeDistance,
        Grenade = Grenade
    };
}

// 新增紧急反应检测（在 Update() 的 _spotted 检查和 _sentToBot 检查之间添加）
// 位置：约 line 61 之后
private void checkEmergencyReact()
{
    if (_sentToBot) return;
    
    float emergencyDist = GlobalSettingsClass.Instance.Grenade.EMERGENCY_REACT_DISTANCE;
    if (GrenadeDistance < emergencyDist && IsGrenadeClosingIn())
    {
        // 即使未 spotted，也触发紧急反应
        setSpotted();
        // 直接设置反应时间为 0（立即反应）
        ReactionTime = 0f;
    }
}

private float _lastGrenadeDistance = float.MaxValue;
private bool IsGrenadeClosingIn()
{
    bool closing = GrenadeDistance < _lastGrenadeDistance;
    _lastGrenadeDistance = GrenadeDistance;
    return closing;
}
```

#### 5.1.4 修改 UpdateGrenadeDanger

```csharp
// 原:
public void UpdateGrenadeDanger(Vector3 Danger)
{
    DangerPoint = Danger;
    if (_sentToBot && !_updated)
    {
        _updated = true;
        BotOwner.BewareGrenade.AddGrenadeDanger(Danger, Grenade);
    }
}

// 改为:
public void UpdateGrenadeDanger(Vector3 Danger, float newRemainingTime = -1f)
{
    DangerPoint = Danger;
    if (newRemainingTime > 0f)
        RemainingTime = newRemainingTime;

    if (_sentToBot && !_updated)
    {
        _updated = true;
        // 更新决策系统中的威胁数据
        Bot.Decision.DecisionManager.UpdateGrenadeDangerPoint(Danger);
    }
}
```

#### 5.1.5 新增威胁生命周期检查

```csharp
// 在 ManualUpdate 被调用时（通过 GrenadeReactionClass）检查:
public bool HasExpired()
{
    float maxLifetime = GlobalSettingsClass.Instance.Grenade.MAX_THREAT_LIFETIME;
    return Time.time - _threatSetTime > maxLifetime;
}
```

### 5.2 修改 `Classes/Bot/WeaponFunction/Grenades/GrenadeReactionClass.cs`

#### 5.2.1 更新 `OnGrenadeDangerUpdated` 事件订阅签名

```csharp
// 原:
grenadeController.OnGrenadeDangerUpdated += GrenadeDangerUpdated;

// 改为:
grenadeController.OnGrenadeDangerUpdated += GrenadeDangerUpdated;
```

更新方法签名：
```csharp
// 原:
private void GrenadeDangerUpdated(Grenade grenade, Vector3 Danger)

// 改为:
private void GrenadeDangerUpdated(Grenade grenade, Vector3 Danger, float remainingTime)
{
    if (EnemyGrenadesList.TryGetValue(grenade, out var Tracker))
    {
        Tracker.UpdateGrenadeDanger(Danger, remainingTime);
    }
}
```

#### 5.2.2 修改 EnemyGrenadeThrown

```csharp
// 原 line 116:
EnemyGrenadesList.Add(grenade, new GrenadeTrackerClass(Bot, grenade, dangerPoint, GetReactionTime()));

// 改为 — 增加 remainingTime 参数:
// 需要在 GrenadeController.OnGrenadeThrown 事件中也传递 estimatedFuseTime
// 或从 GrenadeController 获取

// 临时方案：构造函数中 remainingTime 设为 -1（未知），
// 等 OnGrenadeDangerUpdated 回调时由协程更新
EnemyGrenadesList.Add(grenade, new GrenadeTrackerClass(Bot, grenade, dangerPoint, 
    GetReactionTime(), -1f));
```

#### 5.2.3 补充 `GetReactionTime` 支持个性参数

```csharp
public float GetReactionTime()
{
    float reactionTime = 0.25f;
    reactionTime /= Bot.Info.Profile.DifficultyModifier;
    reactionTime *= Random.Range(0.75f, 1.25f);
    
    // 新增：个性参数调整
    reactionTime *= Bot.Info.PersonalitySettings.General.GRENADE_REACTION_TIME_MODIFIER;
    
    return Mathf.Clamp(reactionTime, 0.1f, 2f); // 放宽上限，允许 Timmy 慢反应
}
```

#### 5.2.4 新增 HasActiveGrenadeThreat 方法

```csharp
/// <summary>
/// 检查是否有活跃手雷威胁。对手雷已销毁（但 Tracker 未清理）的情况处理。
/// </summary>
public bool HasActiveGrenadeThreat(out GrenadeThreatData data)
{
    if (DangerGrenade != null && DangerGrenade.Grenade != null && !DangerGrenade.HasExpired())
    {
        data = DangerGrenade.BuildThreatData();
        return true;
    }
    data = null;
    
    // 清理无效威胁 — 通知决策系统
    if (DangerGrenade != null && (DangerGrenade.Grenade == null || DangerGrenade.HasExpired()))
    {
        Bot.Decision.DecisionManager.ClearGrenadeThreat();
        DangerGrenade = null;
    }
    
    return false;
}
```

#### 5.2.5 修改 ManualUpdate

```csharp
public override void ManualUpdate()
{
    // 清理已完成/超时的 tracker
    var toRemove = new List<Throwable>();
    foreach (var kvp in EnemyGrenadesList)
    {
        if (kvp.Value?.Grenade == null || kvp.Value.HasExpired())
            toRemove.Add(kvp.Key);
        else
            kvp.Value.Update();
    }
    foreach (var key in toRemove)
        EnemyGrenadesList.Remove(key);
    
    // 如果当前 DangerGrenade 已无效，清理决策
    if (DangerGrenade?.Grenade == null || DangerGrenade?.HasExpired() == true)
    {
        Bot.Decision.DecisionManager.ClearGrenadeThreat();
        DangerGrenade = null;
    }
    
    // 更新 DangerGrenade 为最紧急威胁（多手雷场景）
    UpdateDangerGrenade();
    
    base.ManualUpdate();
}

private void UpdateDangerGrenade()
{
    GrenadeTrackerClass mostUrgent = null;
    float bestScore = float.MaxValue;

    foreach (var tracker in EnemyGrenadesList.Values)
    {
        if (tracker?.Grenade == null || tracker.HasExpired()) continue;
        
        // 评分：距离越近 + 剩余时间越少 = 越紧急
        float score = tracker.GrenadeDistance * (tracker.RemainingTime > 0 ? tracker.RemainingTime : 1f);
        if (score < bestScore)
        {
            bestScore = score;
            mostUrgent = tracker;
        }
    }
    DangerGrenade = mostUrgent;
}
```

---

## 六、Phase 4 — 决策层改造（新建 1，修改 1 文件）

### 6.1 修改 `Classes/Bot/Decision/BotDecisionManager.cs`

#### 6.1.1 修改 getDecision() — 注入手雷威胁检查

```csharp
private void getDecision()
{
    // [新增] 手雷威胁 — 最高优先级，必须在 enemy==null 之前
    if (GlobalSettingsClass.Instance.Grenade.ENABLED 
        && BaseClass.GrenadeReaction.HasActiveGrenadeThreat(out var grenadeData))
    {
        _activeGrenadeThreat = grenadeData;
        SetDecisions(ECombatDecision.AvoidGrenade, ESquadDecision.None, 
            ESelfActionType.None, null);  // enemy 可为 null
        return;
    }

    Enemy enemy = Bot.EnemyController.ChooseEnemy();
    if (enemy == null)
    {
        // ... 现有代码不变 ...
    }
    // ... 其余现有代码不变 ...
}
```

#### 6.1.2 新增字段和方法

```csharp
// 在手雷威胁激活期间存储数据（提供给 DodgeGrenadeAction 读取）
private GrenadeThreatData _activeGrenadeThreat;
public GrenadeThreatData ActiveGrenadeThreat => _activeGrenadeThreat;

public void SetAvoidGrenade(GrenadeThreatData data)
{
    _activeGrenadeThreat = data;
}

public void UpdateGrenadeDangerPoint(Vector3 newPoint)
{
    if (_activeGrenadeThreat != null)
    {
        _activeGrenadeThreat.DangerPoint = newPoint;
        _activeGrenadeThreat.LastUpdateTime = Time.time;
    }
}

public void ClearGrenadeThreat()
{
    _activeGrenadeThreat = null;
    // 如果当前决策是 AvoidGrenade，将被 IsCurrentActionEnding 检测到并切换
}
```

#### 6.1.3 添加 GrenadeReaction 引用

在 `BotDecisionManager` 或 `SAINDecisionClass` 中暴露 `GrenadeReactionClass` 引用，使 `HasActiveGrenadeThreat` 可被 `getDecision` 调用。

```csharp
// 在 SAINDecisionClass.cs 中添加:
public GrenadeReactionClass GrenadeReaction => Bot.Grenade.GrenadeReaction;

// 需要确认 Bot.Grenade 路径是否存在，或通过 Bot.BotGrenadeManager 访问。
// 查看现有代码，GrenadeReactionClass 继承自 BotSubClass<BotGrenadeManager>，
// 通过 Bot.BotGrenadeManager?.GrenadeReaction 访问（确认实际路径）。
```

### 6.2 新建 `Layers/Combat/Solo/DodgeGrenadeAction.cs`

```csharp
using EFT;
using SAIN.Models.Enums;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.SubComponents;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace SAIN.Layers.Combat.Solo
{
    /// <summary>
    /// SAIN 智能手雷躲避行为。
    /// 根据剩余时间、距离、手雷类型和个性参数执行不同的躲避策略。
    /// </summary>
    internal class DodgeGrenadeAction(BotOwner bot, int priority) : SAINAction(bot, priority, Name)
    {
        public static readonly string Name = "Dodge Grenade";

        private GrenadeThreatData _threatData;
        private Vector3 _safePoint;
        private EDodgePhase _phase = EDodgePhase.Init;
        private float _actionStartTime;
        private bool _shouldProne;
        private float _safeDistance;

        private enum EDodgePhase
        {
            Init,           // 初始化
            Pathfinding,    // 寻找安全点
            Moving,         // 向安全点移动
            Proning,        // 扑倒
            HoldProne,      // 保持扑倒等待爆炸
            Done            // 完成
        }

        public override void Start()
        {
            _actionStartTime = Time.time;
            _threatData = Bot.Decision.DecisionManager.ActiveGrenadeThreat;
            
            if (_threatData == null)
            {
                Logger.LogDebug($"{Bot.name}: DodgeGrenade — 无威胁数据，退出");
                return;
            }

            // 个性：GigaChad 硬扛
            float ignoreChance = Bot.Info.PersonalitySettings.General.GRENADE_IGNORE_CHANCE;
            if (Random.value < ignoreChance)
            {
                Logger.LogDebug($"{Bot.name}: 硬扛手雷");
                Bot.Decision.DecisionManager.ClearGrenadeThreat();
                return;
            }

            // 计算有效距离（垂直楼层感知）
            float effectiveDistance = CalculateEffectiveDistance(_threatData);
            _threatData.DistanceToBot = effectiveDistance;

            // 应用个性倍率
            var settings = GlobalSettingsClass.Instance.Grenade;
            _safeDistance = settings.FRAG_SAFE_DISTANCE * Bot.Info.PersonalitySettings.General.GRENADE_SAFE_DIST_MODIFIER;

            // 确定初始行为
            DetermineBehavior();
        }

        public override void Update()
        {
            if (_threatData == null || !_threatData.IsValid) { _phase = EDodgePhase.Done; return; }

            switch (_phase)
            {
                case EDodgePhase.Pathfinding:
                    FindAndNavigateToSafePoint();
                    break;
                case EDodgePhase.Moving:
                    CheckArrivalAndTransition();
                    break;
                case EDodgePhase.Proning:
                    ExecuteProne();
                    break;
                case EDodgePhase.HoldProne:
                    if (_threatData.RemainingTime <= 0f || Time.time - _actionStartTime > 3f)
                        _phase = EDodgePhase.Done;
                    break;
                case EDodgePhase.Done:
                    break;
            }
        }

        public override void Stop()
        {
            Bot.Mover.Prone.SetProne(false);
            Bot.Mover.SprintController.CancelSprint();
        }

        // ==================== 行为决策 ====================

        private void DetermineBehavior()
        {
            float dist = _threatData.DistanceToBot;
            float timeLeft = _threatData.RemainingTime;

            // 碰炸手雷 → 紧急行为
            if (_threatData.IsImpact)
            {
                if (dist < 3f) { _shouldProne = true; _phase = EDodgePhase.Proning; }
                else { _phase = EDodgePhase.Pathfinding; }
                return;
            }

            // 烟雾弹 / 闪光弹 → 简化处理
            if (_threatData.IsSmoke)
            {
                if (dist < 8f) MoveAwayFromGrenade(8f);
                else _phase = EDodgePhase.Done;
                return;
            }
            if (_threatData.IsFlash)
            {
                TurnAwayFromGrenade();
                if (dist < 5f) MoveAwayFromGrenade(5f);
                else _phase = EDodgePhase.Done;
                return;
            }

            // 碎弹 — 时间-距离分层
            if (timeLeft > 2f)
            {
                if (dist < 10f) { _safeDistance = Mathf.Max(_safeDistance, 15f); _phase = EDodgePhase.Pathfinding; }
                else if (dist < 15f) WalkAway();
                else _phase = EDodgePhase.Done;
            }
            else if (timeLeft > 1f)
            {
                if (dist < 5f) { _shouldProne = true; _phase = EDodgePhase.Pathfinding; }
                else { _safeDistance = 15f; _phase = EDodgePhase.Pathfinding; }
            }
            else // < 1s
            {
                if (dist < 3f) { _shouldProne = true; _phase = EDodgePhase.Proning; }
                else SprintAway();
            }
        }

        // ==================== 寻路 ====================

        private void FindAndNavigateToSafePoint()
        {
            Vector3 awayDir = (Bot.Position - _threatData.DangerPoint).normalized;
            var settings = GlobalSettingsClass.Instance.Grenade;
            Vector3 bestPoint = Vector3.zero;
            bool foundCover = false;
            float bestScore = float.MinValue;

            // 扇形采样: 9 个方向 x 3 个距离
            for (int angleStep = -4; angleStep <= 4; angleStep++)
            {
                float angle = angleStep * 15f;
                Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * awayDir;

                for (int distLevel = 0; distLevel < 3; distLevel++)
                {
                    float radius = _safeDistance * (1f - distLevel * 0.25f);
                    Vector3 target = Bot.Position + dir * radius;

                    if (!Bot.Mover.CanGoToPoint(target, out NavMeshPath path))
                        continue;

                    float score = EvaluateSafePoint(target, ref foundCover);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestPoint = target;
                    }
                }
            }

            if (bestPoint != Vector3.zero)
            {
                _safePoint = bestPoint;
                Bot.Mover.RunToPoint(_safePoint, out _);
                _phase = EDodgePhase.Moving;
            }
            else
            {
                // 寻路失败 → 降级到扑倒
                Logger.LogDebug($"{Bot.name}: 手雷躲避寻路失败，降级到扑倒");
                _shouldProne = true;
                _phase = EDodgePhase.Proning;
            }
        }

        private float EvaluateSafePoint(Vector3 point, ref bool foundCover)
        {
            var settings = GlobalSettingsClass.Instance.Grenade;
            float score = 1f;

            // 掩体检测：从手雷向候选点做 Raycast
            Vector3 dirToPoint = (point - _threatData.DangerPoint).normalized;
            float dist = Vector3.Distance(_threatData.DangerPoint, point);
            if (Physics.Raycast(_threatData.DangerPoint, dirToPoint, out RaycastHit hit, dist - 0.3f,
                LayerMaskClass.HighPolyWithTerrainMask))
            {
                // 碰撞体尺寸检查
                Bounds b = hit.collider.bounds;
                float minSize = settings.MIN_COVER_SIZE;
                float minHeight = settings.MIN_COVER_HEIGHT;
                bool largeEnough = (b.size.x > minSize || b.size.z > minSize) && b.size.y > minHeight;

                if (largeEnough)
                {
                    score += 5f;
                    foundCover = true;
                }
            }

            // 距离加分
            float pointDist = Vector3.Distance(Bot.Position, point);
            score += pointDist / _safeDistance;

            return score;
        }

        // ==================== 移动执行 ====================

        private void SprintAway()
        {
            Vector3 awayPoint = Bot.Position + (Bot.Position - _threatData.DangerPoint).normalized * 15f;
            if (Bot.Mover.CanGoToPoint(awayPoint, out _))
            {
                Bot.Mover.RunToPoint(awayPoint, out _);
                _phase = EDodgePhase.Moving;
            }
            else
            {
                _shouldProne = true;
                _phase = EDodgePhase.Proning;
            }
        }

        private void MoveAwayFromGrenade(float distance)
        {
            Vector3 awayPoint = Bot.Position + (Bot.Position - _threatData.DangerPoint).normalized * distance;
            if (Bot.Mover.CanGoToPoint(awayPoint, out _))
            {
                Bot.Mover.WalkToPoint(awayPoint, out _);
                _phase = EDodgePhase.Moving;
            }
            else
            {
                _phase = EDodgePhase.Done;
            }
        }

        private void WalkAway()
        {
            MoveAwayFromGrenade(15f);
        }

        private void TurnAwayFromGrenade()
        {
            Vector3 lookDir = (Bot.Position - _threatData.DangerPoint).normalized;
            BotOwner.Steering.LookToPoint(Bot.Position + lookDir * 10f);
        }

        private void CheckArrivalAndTransition()
        {
            if (Bot.Mover.SprintController.IsAtDestination)
            {
                if (_shouldProne)
                {
                    _phase = EDodgePhase.Proning;
                }
                else
                {
                    _phase = EDodgePhase.HoldProne;
                }
            }
        }

        private void ExecuteProne()
        {
            Bot.Mover.Prone.SetProne(true);
            // 面向远离手雷的方向
            TurnAwayFromGrenade();
            _phase = EDodgePhase.HoldProne;
        }

        // ==================== 辅助 ====================

        private float CalculateEffectiveDistance(GrenadeThreatData data)
        {
            Vector3 gp = data.DangerPoint;
            Vector3 bp = Bot.Position;

            float verticalDist = Mathf.Abs(gp.y - bp.y);
            float horizontalDist = new Vector2(gp.x - bp.x, gp.z - bp.z).magnitude;

            // 垂直距离 > 2m 且水平很近 → 可能不同楼层
            if (GlobalSettingsClass.Instance.Grenade.VERTICAL_FLOOR_FILTER 
                && verticalDist > 2f && horizontalDist < 5f)
            {
                if (Bot.Mover.CanGoToPoint(gp, out NavMeshPath path))
                {
                    float pathLen = 0f;
                    for (int i = 1; i < path.corners.Length; i++)
                        pathLen += Vector3.Distance(path.corners[i - 1], path.corners[i]);
                    return pathLen;
                }
                return float.MaxValue; // 不可达 → 视为无威胁
            }
            return Vector3.Distance(bp, gp);
        }
    }
}
```

---

## 七、Phase 5 — 映射修改（修改 1 文件）

### 7.1 修改 `Layers/SAINAvoidThreatLayer.cs`

仅改一行：

```csharp
// 原 (line 36-37):
case ECombatDecision.AvoidGrenade:
    return new Action(typeof(SeekCoverAction), $"Avoid Grenade");

// 改为:
case ECombatDecision.AvoidGrenade:
    return new Action(typeof(DodgeGrenadeAction), $"Avoid Grenade");
```

`CheckDecisionsForBot` (line 58) 已有 `AvoidGrenade` 检查，无需修改。

---

## 八、Phase 6 — Squad 协同（修改 3 文件）

### 8.1 修改 `Classes/BotManager/Squad.cs`

#### 8.1.1 新增事件声明（在其他事件旁，约 line 28）

```csharp
/// <summary>
/// Squad 成员发现手雷威胁。参数: dangerPoint, isSmoke, isFlash, reportingBot
/// </summary>
public event Action<Vector3, bool, bool, BotComponent> OnMemberSpottedGrenade;
```

#### 8.1.2 新增广播方法

```csharp
/// <summary>
/// 由 GrenadeTrackerClass 在 CanReact 触发时调用。
/// 向 Squd 所有成员广播手雷威胁位置。
/// </summary>
public void BroadcastGrenadeSpotted(Vector3 dangerPoint, bool isSmoke, bool isFlash = false)
{
    OnMemberSpottedGrenade?.Invoke(dangerPoint, isSmoke, isFlash, null);
    // reportingBot 在事件处理中由订阅方自行确定
}
```

### 8.2 修改 `Classes/Bot/Talk/GroupTalk.cs`

#### 8.2.1 订阅 Squad 手雷事件

在 GroupTalk 的 Init 或 Squad 初始化逻辑中，参照现有 `squad.OnMemberKilled += friendlyDown` 的模式（约 line 442）：

```csharp
squad.OnMemberSpottedGrenade += grenadeSpottedBySquadMember;
```

#### 8.2.2 新增处理方法

```csharp
/// <summary>
/// 收到 Squad 成员的手雷报告。
/// 存储到近期报告列表，供 SquadDecisionClass 查询。
/// </summary>
private readonly List<RecentGrenadeReport> _recentGrenadeReports = new();

private void grenadeSpottedBySquadMember(Vector3 dangerPoint, bool isSmoke, bool isFlash, BotComponent reportingBot)
{
    // 不处理自己报告的手雷
    if (reportingBot != null && reportingBot.ProfileId == Bot.ProfileId)
        return;

    _recentGrenadeReports.Add(new RecentGrenadeReport
    {
        DangerPoint = dangerPoint,
        IsSmoke = isSmoke,
        IsFlash = isFlash,
        ReportTime = Time.time,
        ReportingBotPosition = reportingBot?.Position ?? dangerPoint
    });

    // 清理过期报告
    _recentGrenadeReports.RemoveAll(r => Time.time - r.ReportTime > 5f);
}

/// <summary>
/// 供 SquadDecisionClass 调用：获取最近的手雷报告
/// </summary>
public RecentGrenadeReport GetClosestRecentGrenadeReport()
{
    return _recentGrenadeReports
        .Where(r => Time.time - r.ReportTime < 3f)
        .OrderBy(r => Vector3.Distance(Bot.Position, r.DangerPoint))
        .FirstOrDefault();
}

// 内部数据结构
public class RecentGrenadeReport
{
    public Vector3 DangerPoint;
    public bool IsSmoke;
    public bool IsFlash;
    public float ReportTime;
    public Vector3 ReportingBotPosition;
}
```

### 8.3 修改 `Classes/Bot/Decision/SquadDecisionClass.cs`

#### 8.3.1 新增 Squad 决策方法

```csharp
/// <summary>
/// 判断是否应因 Squad 成员的手雷报告而后撤/暂停前进。
/// </summary>
public bool ShallRetreatFromGrenade(out Vector3 retreatFrom)
{
    retreatFrom = Vector3.zero;

    var report = Bot.Talk.GroupTalk.GetClosestRecentGrenadeReport();
    if (report == null) return false;

    float dist = Vector3.Distance(Bot.Position, report.DangerPoint);
    if (dist < 30f && !report.IsSmoke && !report.IsFlash)
    {
        retreatFrom = report.DangerPoint;
        return true;
    }
    return false;
}
```

#### 8.3.2 在 GetDecision 中调用

在 `GetDecision()` 方法中，在检查其他 Squad 决策之前或之内：

```csharp
// 如果队友报告了接近的手雷，暂停前进
if (ShallRetreatFromGrenade(out var retreatFrom))
{
    // 暂不修改 SquadDecision 枚举 — 用现有机制实现
    // 选项: 返回 ESquadDecision.None（中断当前 Squad 行动）
    // 或通过 SetDecisions 设置单独的躲避行为
}
```

> 注：Squad 手雷回避的逻辑可先实现为 `ESquadDecision.None`（打断当前 Squad 命令），后续版本可扩展专用枚举值。

---

## 九、数据流完整图

```
[手雷抛出]
  │
  ▼
GrenadeController.GrenadeThrown()
  ├── 反射/查表获取 estimatedFuseTime
  └── 启动 GrenadeTracker 协程 (带 estimatedFuseTime)
       │
       ├─ 每帧检测速度 + 计算 remainingTime
       └─ 触发 OnGrenadeDangerUpdated(grenade, point, remainingTime)
            │
            ▼
       GrenadeReactionClass.GrenadeDangerUpdated()
            └─ Tracker.UpdateGrenadeDanger(point, remainingTime)
                 └─ Bot.Decision.DecisionManager.UpdateGrenadeDangerPoint(point)
                      └─ _activeGrenadeThreat.DangerPoint 更新

[Tracker 感知]
  │
  ▼
GrenadeTrackerClass.Update()
  ├── CanReact 检查 (spotted + TimeSinceSpotted > ReactionTime)
  │    └── 个性: ReactionTime *= GRENADE_REACTION_TIME_MODIFIER
  ├── 紧急反应检查 (距离 < EMERGENCY_REACT_DISTANCE + 正在接近)
  │
  └── CanReact = true:
       ├── GroupSay(OnEnemyGrenade)  // 语音
       ├── Squad.BroadcastGrenadeSpotted(dangerPoint, isSmoke)
       │    └── Squad.OnMemberSpottedGrenade 事件
       │         └── GroupTalk.grenadeSpottedBySquadMember()
       │              └── _recentGrenadeReports 存储
       │                   └── SquadDecisionClass.ShallRetreatFromGrenade()
       │
       └── Bot.Decision.DecisionManager.SetAvoidGrenade(data)
            └── _activeGrenadeThreat = data

[决策链]
  │
  ▼
BotDecisionManager.getDecision()
  ├── [1st] HasActiveGrenadeThreat? → ECombatDecision.AvoidGrenade
  │    └── _activeGrenadeThreat 有效? → 是: return AvoidGrenade
  │                                  → 否: 自动清除，继续
  ├── [2nd] enemy == null? → 战后恢复
  ├── [3rd] SelfAction? → SeekCover
  ├── ... 其余现有逻辑
  │
  └── AvoidGrenade 决策发出
       │
       ▼
  SAINAvoidThreatLayer.GetNextAction()
       └── case AvoidGrenade → new DodgeGrenadeAction

[DodgeGrenadeAction]
  │
  ├── Start(): 个性参数初始化，GigaChad 硬扛，确定行为
  ├── Update(): 分层表执行
  │    ├── Pathfinding: FindSafePoint() → CanGoToPoint + 掩体评估
  │    ├── Moving: RunToPoint / WalkToPoint
  │    ├── Proning: Bot.Mover.Prone.SetProne(true)
  │    └── HoldProne: 等待爆炸，超时退出
  └── Stop(): 清理扑倒状态

[清理]
  │
  ▼
GrenadeReactionClass.ManualUpdate()
  ├── 清理已销毁 tracker
  ├── DangerGrenade 无效 → ClearGrenadeThreat()
  └── UpdateDangerGrenade() (多手雷取最紧急)
```

---

## 十、需要关注的现有代码路径

### 10.1 确保 Bot.Grenade 或等效路径可访问

目前 GrenadeReactionClass 通过 `BotGrenadeManager` 管理。需要确认 `BotDecisionManager` 或 `SAINDecisionClass` 如何访问它。

检查路径（需实际代码中验证）：
```csharp
// 候选路径 1:
Bot.Grenade.GrenadeReaction

// 候选路径 2:
Bot.BotGrenadeManager

// 候选路径 3:
Bot.Info.WeaponInfo.GrenadeReaction
```

### 10.2 CombatSoloLayer 中 AvoidGrenade 的状态

当前 `CombatSoloLayer` 的 switch 中没有 `AvoidGrenade` case。由于 `SAINAvoidThreatLayer` 优先级高于 `CombatSoloLayer`，`AvoidGrenade` 决策会先在 `SAINAvoidThreatLayer` 被处理，不会落到 `CombatSoloLayer` 的 default。但确认两者优先级数值。

### 10.3 Grenade.IsSmoke 的实际字段名

方案中多次使用 `Grenade.IsSmoke`，但这个属性可能不存在。需要在代码中验证实际字段名：

```csharp
// 可能的替代方案：通过 GrenadeSettings 的属性判断
grenade.GrenadeSettings.GetType().Name  // 可能包含 "Smoke" "Flash" 等关键词
// 或通过 CollisionSound 枚举的完整值集合
```

---

## 十一、测试策略

### 11.1 单元级测试

| 测试项 | 方法 | 预期 |
|--------|------|------|
| 引信时间获取 | 在每种子雷类型上调用 GetGrenadeFuseTime | 返回合理的正值或 0(VOG) |
| CanGoToPoint 可用性 | 在手雷场景的 NavMesh 上采样点 | PathComplete 或至少 PathPartial |
| 个性参数读取 | 在 DodgeGrenadeAction.Start() 中 Log | 值与 PersonalitySettings 中一致 |
| Squad 事件广播 | 2 人队中一人触发 BroadcastGrenadeSpotted | 另一人收到事件 |
| VOG 紧急反应 | 向 Bot 脚下扔 VOG | 立即扑倒，不经过分层表 |

### 11.2 集成测试

| 场景 | 地图 | 预期行为 |
|------|------|----------|
| 开阔地手雷 | Woods | Bot 反向跑 15m+ |
| 室内手雷 | Factory 办公室 | Bot 跑向墙后掩体 |
| 多层建筑 | Interchange 商场 | 不同楼层不反应/正确判断 |
| 死后手雷 | 任意 | 手雷仍在飞行时 Bot 仍会躲避 |
| 全局开关关闭 | 任意 | 恢复 EFT BewareGrenade 行为 |
| Squad 协同 | Customs 2 人队 | 队友听到报告后暂停前进 |
| 多手雷 | 扔 2 颗 | 取最紧急威胁处理 |
| 闪光弹 | 任意 | Bot 转身不看而非逃跑 |
| 烟雾弹 | 任意 | Bot 移出烟雾区 |

### 11.3 性能测试

```csharp
// 在 DodgeGrenadeAction 中添加性能日志（DEBUG 模式）
#if DEBUG
float startTime = Time.realtimeSinceStartup;
// ... 寻路逻辑
float elapsed = Time.realtimeSinceStartup - startTime;
if (elapsed > 0.005f) Logger.LogWarning($"FindSafePoint 耗时 {elapsed*1000:F1}ms");
#endif
```

---

## 十二、风险登记

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|----------|
| Throwable 私有字段不存在 | 低 | 高 | 查表兜底方案已准备好 |
| CanGoToPoint 过于严格 | 中 | 中 | 可调整为 PathPartial 也接受 |
| Squad 事件未传播到所有成员 | 中 | 低 | 仅影响 Squad 协同，不影响核心躲避 |
| DodgeGrenadeAction 与 BewareGrenade 残留冲突 | 中 | 中 | GlobalSettings 关闭时回退，多重保障 |
| 性能退化（多 Bot+多手雷） | 低 | 中 | CanGoToPoint 路径缓存，采样点数可调 |
| EFT 更新改变 Grenade 类结构 | 低 | 高 | 反射 try-catch 包裹，降级到查表 |

---

> 本实施文档基于设计文档 v2.0 编写。所有改动点均以 2026-05-31 代码库为基准。Preparing for the Future!
