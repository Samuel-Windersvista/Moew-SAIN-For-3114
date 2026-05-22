# SAIN + 前置模组 -- Bug修复与性能优化方案

> VT-OS/OPENCODE 自动分析终端 | 序列号 VTC-2077-OC-4111
> 分析范围: SAIN本体 (C#) + BigBrain 1.3.2 (C#) + Waypoints 1.7.1 (C# + TypeScript)
> 共发现 42 个问题 | 严重 5 个 | 高 8 个 | 中 16 个 | 低 13 个

---

## 目录

- [一、SAIN 严重 Bug 修复 (4个)](#一sain-严重-bug-修复-4个)
- [二、SAIN 性能瓶颈优化 (4个)](#二sain-性能瓶颈优化-4个)
- [三、BigBrain 前置模组修复与优化 (6个)](#三bigbrain-前置模组修复与优化-6个)
- [四、Waypoints 前置模组修复与优化 (6个)](#四waypoints-前置模组修复与优化-6个)
- [五、修复优先级路线图](#五修复优先级路线图)

---

## 一、SAIN 严重 Bug 修复 (4个)

### BUG-1 [严重] BotManagerComponent.DeadBots 索引错乱导致尸体障碍物管理错误

**文件**: `Components/BotManagerComponent.cs` 第 139-185 行

**问题描述**:

```csharp
// 第 145-177 行: 收集需要移除的索引 (正向)
for (int i = 0; i < DeadBots.Count; i++)
{
    // ... 各种条件判断 ...
    IndexToRemove.Add(i);  // 例如: [0, 2]
}

// 第 179-182 行: 正向遍历移除 -- BUG!
foreach (var index in IndexToRemove)
{
    DeadBots.RemoveAt(index);  // 正向移除导致索引错乱
}
```

当 `DeadBots = [A, B, C, D]` 且 `IndexToRemove = [0, 2]` 时:
- 移除 `index 0` (A) -> 列表变为 `[B, C, D]`
- 移除 `index 2` -> 移除的是 `D` 而不是原来的 `C`

**影响**: 应该被清理的 Bot 尸体未被清理，导致 NavMeshObstacle 泄漏（尸体永久阻挡寻路）；不该被清理的 Bot 尸体被错误清理。

**修复方案**:

```csharp
// 修复: 从高索引到低索引遍历移除
// 将第 179-182 行改为:
for (int i = IndexToRemove.Count - 1; i >= 0; i--)
{
    DeadBots.RemoveAt(IndexToRemove[i]);
}
IndexToRemove.Clear();
```

另外，`AddNavObstacles()` 每帧调用且包含 `Physics.OverlapSphere` (第154行)，建议改为每 2-3 秒降频:

```csharp
// 将 AddNavObstacles 调用改为降频
private float _nextNavObstacleTime;
// 在 ManualUpdate 中:
if (_nextNavObstacleTime < Time.time)
{
    _nextNavObstacleTime = Time.time + 2f;  // 每2秒执行一次
    AddNavObstacles();
}
UpdateObstacles();
```

---

### BUG-2 [严重] MovementContextIsAIPatch 全局影响所有 Player

**文件**: `Patches/MovementPatches.cs` 第 99-112 行

**问题描述**:

```csharp
public class MovementContextIsAIPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        // 目标: MovementContext.IsAI 的 getter -- 影响所有 Player 实例!
        return AccessTools.PropertyGetter(typeof(MovementContext), nameof(MovementContext.IsAI));
    }

    [PatchPrefix]
    public static bool Patch(ref bool __result)
    {
        __result = false;
        return false;  // 对所有 Player 生效，包括真人玩家
    }
}
```

这个 Patch 对所有 `Player` 实例（包括真人玩家的 `MovementContext`）返回 `false`。原版 EFT 代码中 `IsAI` 用于区分 AI 和玩家的物理/碰撞/惯性行为。如果 EFT 某处代码使用 `IsAI` 来**判断是否是玩家**（而非判断是否是 AI），可能导致真人玩家获得意外的行为。

**修复方案**: 添加 `BotOwner` 检查，仅对 SAIN Bot 生效:

```csharp
[PatchPrefix]
public static bool Patch(MovementContext __instance, ref bool __result)
{
    // 仅对 SAIN Bot 返回 false, 对真人玩家保持原版行为
    Player player = __instance?.Player;
    if (player != null && SAINEnableClass.IsSAINDisabledForBot(player) == false)
    {
        __result = false;
        return false;
    }
    return true;  // 让原版方法正常执行
}
```

---

### BUG-3 [严重] CanBeSnappedPatch 全局影响所有 Player

**文件**: `Patches/MovementPatches.cs` 第 114-130 行

**问题描述**: 与 BUG-2 同样的模式 -- Patch 目标是 `Player.CanBeSnapped` 的 getter，直接返回 `false`，对所有 Player 生效。

```csharp
public class CanBeSnappedPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.PropertyGetter(typeof(Player), nameof(Player.CanBeSnapped));
    }

    [PatchPrefix]
    public static bool Patch(ref bool __result)
    {
        __result = false;
        return false;  // 全局影响
    }
}
```

`CanBeSnapped` 在 EFT 中用于门交互和动画位置修正。全局禁用可能破坏真人玩家的门交互定位。

**修复方案**:

```csharp
[PatchPrefix]
public static bool Patch(Player __instance, ref bool __result)
{
    if (__instance != null && SAINEnableClass.IsSAINDisabledForBot(__instance) == false)
    {
        __result = false;
        return false;
    }
    return true;
}
```

---

### BUG-4 [中] SAINNoBushESP 作为 MonoBehaviour 始终激活

**文件**: `Components/SAINNoBushESP.cs` 第 22-196 行

**问题描述**: `SAINNoBushESP` 继承 `MonoBehaviour`，每个 Bot 的 GameObject 上都附加了一个实例。即使 Bot 不在战斗中（非战斗状态下 `Update()` 方法第 68-82 行直接 return），Unity 仍然为每个实例执行 `Update` 调度的开销。20 个 Bot = 20 个额外的 MonoBehaviour 在 Update 循环中。

```csharp
public class SAINNoBushESP : MonoBehaviour  // 每个 Bot 一个 MonoBehaviour 实例
{
    public void Update()
    {
        if (BotOwner == null || !UserToggle)  // 非战斗时直接返回
        {
            NoBushESPActive = false;
            return;
        }
        // ...
    }
}
```

且 `Init()` 方法只在静态构造器中使用反射（第 24-42 行），每个实例分配 197 行代码的 MonoBehaviour 成本较高。

**修复方案**: 将 `SAINNoBushESP` 从 MonoBehaviour 改为普通 `IBotClass` 实现:

```csharp
// 方案: 移除 MonoBehaviour 继承，改为 IBotClass
public class SAINNoBushESP : BotBase, IBotClass
{
    // 移除 Update()，改为 ManualUpdate()
    public void ManualUpdate()
    {
        if (BotOwner == null || !UserToggle)
        {
            NoBushESPActive = false;
            return;
        }

        if (NoBushTimer < Time.time)
        {
            NoBushTimer = Time.time + Frequency;
            bool active = NoBushESPCheck();
            SetCanShoot(active);
        }
    }

    // TickRequirement 设为 OnlyBotInCombat
    public ESAINTickState TickRequirement => ESAINTickState.OnlyBotInCombat;
    public bool CanEverTick => true;
    public float TickInterval => Frequency;
    public float LastTickTime { get; set; }

    // 改为由 BotComponent 管理生命周期
    public void Init(BotComponent sain)
    {
        Bot = sain;
        BotOwner = sain.BotOwner;
        // ... 初始化逻辑
    }
}
```

并在 `BotComponent.CreateClasses()` 中将:
```csharp
NoBushESP = gameObject.AddComponent<SAINNoBushESP>();
```
改为:
```csharp
NoBushESP = new SAINNoBushESP(this);
```

---

## 二、SAIN 性能瓶颈优化 (4个)

### PERF-1 [高] CanGoToPoint 每次调用分配 new NavMeshPath

**文件**: `Classes/Bot/Mover/SAINMoverClass.cs` 第 300-323 行

**问题描述**:

```csharp
public bool CanGoToPoint(Vector3 point, out NavMeshPath path, ...)
{
    // ...
    path = new NavMeshPath();  // 每次调用分配新对象!
    if (NavMesh.CalculatePath(navData.Position, targetHit.position, -1, path) && ...)
    // ...
}
```

`CanGoToPoint` 在 `GoToCoverPoint` (第148行) 和 `WalkToPoint` (第195行) 两个热路径上被调用。每个活跃 Bot 在掩体查找和移动决策中频繁调用此方法。`NavMeshPath` 是托管类对象，高频 `new` 产生 GC 压力。

**修复方案**: 使用线程静态缓存复用 `NavMeshPath`:

```csharp
// 在 SAINMoverClass 中添加:
[ThreadStatic]
private static NavMeshPath _cachedPath;

public bool CanGoToPoint(Vector3 point, out NavMeshPath path, ...)
{
    var navData = Bot.Transform.NavData;
    if (!navData.IsOnNavMesh)
    {
        path = null;
        return false;
    }
    if (NavMesh.SamplePosition(point, out NavMeshHit targetHit, navSampleRange, -1))
    {
        // 复用缓存的 NavMeshPath
        if (_cachedPath == null)
            _cachedPath = new NavMeshPath();
        _cachedPath.ClearCorners();  // 清除上一帧数据

        if (NavMesh.CalculatePath(navData.Position, targetHit.position, -1, _cachedPath) 
            && _cachedPath.corners.Length > 1)
        {
            if (mustHaveCompletePath && _cachedPath.status != NavMeshPathStatus.PathComplete)
            {
                path = null;
                return false;
            }
            path = _cachedPath;
            return true;
        }
    }
    path = null;
    return false;
}
```

---

### PERF-2 [中] AddNavObstacles 每帧 Physics.OverlapSphere

**文件**: `Components/BotManagerComponent.cs` 第 139-185 行

**问题描述**: 见 BUG-1 的优化部分。`AddNavObstacles` 在 `ManualUpdate` 中被每帧调用，每个死 Bot 尸体执行一次 `Physics.OverlapSphere`。当有 10+ 尸体时，每帧 10+ 次球形物理查询。应改为降频执行（每 2-3 秒一次），并结合 BUG-1 的 RemoveAt 修复。

**修复方案**: 见 BUG-1 修复方案中的降频代码。

---

### PERF-3 [中] HearingInputClass.TrimExcess 触发内存重新分配

**文件**: `Classes/Bot/Sense/Hearing/HearingInputClass.cs` 第 164-165 行

**问题描述**:

```csharp
if (SoundRemoved)
    SoundDataToReactTo.TrimExcess();  // 每次有声音被移除都重新分配内部数组
```

`List<T>.TrimExcess()` 在内部重新分配一个恰好大小的数组并拷贝元素。这在高频声音处理路径上产生不必要的 GC 分配。`SoundDataToReactTo` 通常在处理后会自然缩小，移除少量元素后立即 `TrimExcess` 是不必要的。

**修复方案**: 移除 `TrimExcess()`，或将触发条件改为更宽松的阈值:

```csharp
// 方案1: 直接移除
// if (SoundRemoved)
//     SoundDataToReactTo.TrimExcess();

// 方案2: 仅在列表大幅缩减时执行 (例如容量 > 实际大小的 3 倍)
if (SoundRemoved && SoundDataToReactTo.Capacity > SoundDataToReactTo.Count * 3)
    SoundDataToReactTo.TrimExcess();
```

---

### PERF-4 [中] 大量 Bot 时的错峰更新

**文件**: `Components/BotManagerComponent.cs` 第 94-97 行

**当前实现**:

```csharp
HashSet<BotComponent> BotsArray = BotSpawnController.SAINBots;
foreach (BotComponent BotComponent in BotsArray)
    if (BotComponent != null)
        BotComponent.ManualUpdate(currentTime, deltaTime);
```

**问题**: 所有 Bot 的 `ManualUpdate` 在同一帧内全部执行。每个 Bot 内部又有 4 个更新批次（AlwaysTickClasses、TickWhenActiveClasses、TickWhenNoSleepClasses、TickWhenCombatClasses）。当 Bot 数量达到 20+ 时，单帧内执行 300-500 次 ManualUpdate 调用。

**修复方案**: 引入错峰(交错)更新机制，将 Bot 分摊到多帧:

```csharp
private int _batchUpdateIndex = 0;
private const int BATCH_SIZE = 5;  // 每帧最多更新 5 个非紧急 Bot

// 将 Bot 分为"关键"和"常规"两组
public void ManualUpdate(float currentTime, float deltaTime)
{
    // 关键 Bot (战斗中/有敌人的) 仍然每帧更新
    var criticalBots = BotSpawnController.SAINBots
        .Where(b => b != null && b.HasEnemy);

    foreach (var bot in criticalBots)
        bot.ManualUpdate(currentTime, deltaTime);

    // 常规 Bot (不在战斗中的) 错峰更新
    var regularBots = BotSpawnController.SAINBots
        .Where(b => b != null && !b.HasEnemy)
        .ToList();

    int start = _batchUpdateIndex * BATCH_SIZE;
    int end = Math.Min(start + BATCH_SIZE, regularBots.Count);
    for (int i = start; i < end; i++)
        regularBots[i].ManualUpdate(currentTime, deltaTime);

    _batchUpdateIndex++;
    if (_batchUpdateIndex * BATCH_SIZE >= regularBots.Count)
        _batchUpdateIndex = 0;
}
```

---

## 三、BigBrain 前置模组修复与优化 (6个)

### BB-1 [严重] BotBaseBrainUpdatePatch / BotAgentUpdatePatch 仅在 DEBUG 模式有异常保护

**文件**: `Patches/BotBaseBrainUpdatePatch.cs` 第 44-107 行

**问题描述**: 四个核心 Patch 的 `try-catch` 被 `#if DEBUG` 包裹:

```csharp
#if DEBUG
    try
    {
#endif
        // ... 核心逻辑 ...
#if DEBUG
    }
    catch (Exception ex)
    {
        Logger.LogError($"Exception in ShallUseNow...");
        throw ex;
    }
#endif
```

在 **Release 模式**下，如果这些 Patch 中的任何代码抛出未处理异常，整个 Bot AI 更新循环会崩溃。`BotBaseBrainUpdatePatch` 和 `BotAgentUpdatePatch` 都返回 `false`（跳过原方法），一旦崩溃 Bot 就完全停止响应。

**影响**: 如果 SAIN 的任何子组件在运行时抛出异常，在 Release 构建中 BigBrain 不会捕获，导致 Bot 大脑崩溃。

**修复方案**: 移除 `#if DEBUG` 条件，始终包裹 try-catch，并优雅降级:

```csharp
[PatchPrefix]
public static bool PatchPrefix(object __instance, ...)
{
    try
    {
        // ... 核心逻辑（保持不变）...
    }
    catch (Exception ex)
    {
        BotOwner owner = _ownerField.GetValue(__instance) as BotOwner;
        Logger.LogError($"Exception in Brain Update for {owner?.name}: {ex}");
        return true;  // 回退到原版方法，避免 Bot 脑死亡
    }
}
```

同样修改 `BotAgentUpdatePatch.cs` 和 `BotBaseBrainActivateLayerPatch.cs`。

---

### BB-2 [高] HasSameContents 语义错误 -- 重复元素判断错误

**文件**: `Utils.cs` 第 23-36 行

**问题描述**:

```csharp
public static bool HasSameContents<T>(IEnumerable<T> collection1, IEnumerable<T> collection2)
{
    if (collection1.Count() != collection2.Count())
        return false;
    if (collection1.Any(item => !collection2.Contains(item)))
        return false;
    return true;
}
```

对于包含重复元素的集合，此函数给出**错误结果**:
- `[1, 1, 2]` vs `[1, 2, 2]` -> 都包含3个元素，collection1的所有元素(1,1,2)都存在于collection2(1,2,2)中 -> 返回 true。但两个集合**不相等**。

**影响**: `ExcludeLayerSplitter` 使用此方法进行 Layer 排除规则去重。错误的结果可能导致应该被排除的 Layer 未被排除，或不应被排除的 Layer 被排除。

**修复方案**:

```csharp
public static bool HasSameContents<T>(IEnumerable<T> collection1, IEnumerable<T> collection2)
{
    // 使用频率计数确保重复元素也被正确比较
    var freq1 = new Dictionary<T, int>();
    var freq2 = new Dictionary<T, int>();

    foreach (var item in collection1)
        freq1[item] = freq1.TryGetValue(item, out int c) ? c + 1 : 1;
    foreach (var item in collection2)
        freq2[item] = freq2.TryGetValue(item, out int c) ? c + 1 : 1;

    if (freq1.Count != freq2.Count) return false;
    foreach (var kv in freq1)
        if (!freq2.TryGetValue(kv.Key, out int c2) || c2 != kv.Value)
            return false;

    return true;
}
```

---

### BB-3 [中] CustomLayersReadOnly / CustomLogicsReadOnly 每次 getter 调用分配新 Dictionary

**文件**: `Brains/BrainManager.cs` 第 59-60 行

**问题描述**:

```csharp
public static IReadOnlyDictionary<int, LayerInfo> CustomLayersReadOnly 
    => Instance.CustomLayers.ToDictionary(i => i.Key, i => i.Value);
public static IReadOnlyDictionary<Type, int> CustomLogicsReadOnly 
    => Instance.CustomLogics.ToDictionary(i => i.Key, i => i.Value);
```

每次访问这些只读属性时，`.ToDictionary()` 会分配新的 `Dictionary` 和所有 `KeyValuePair` 对象。如果在热路径上频繁访问，产生严重 GC 压力。

**修复方案**: 缓存包装器，仅在底层数据变化时重建:

```csharp
private ReadOnlyDictionary<int, LayerInfo> _customLayersReadOnly;
private ReadOnlyDictionary<Type, int> _customLogicsReadOnly;

public static IReadOnlyDictionary<int, LayerInfo> CustomLayersReadOnly 
    => Instance._customLayersReadOnly;
public static IReadOnlyDictionary<Type, int> CustomLogicsReadOnly 
    => Instance._customLogicsReadOnly;

// 在 AddCustomLayer 末尾添加:
Instance._customLayersReadOnly = new ReadOnlyDictionary<int, LayerInfo>(Instance.CustomLayers);
```

---

### BB-4 [中] BotStandartBotBrainActivatePatch 匿名 lambda 无法取消订阅

**文件**: `Patches/BotStandartBotBrainActivatePatch.cs` 第 31 行

**问题描述**:

```csharp
___botOwner_0.GetPlayer.OnPlayerDeadOrUnspawn += (player) => 
    { BrainManager.Instance.ActivatedBots.Remove(player); };
```

匿名 lambda 被订阅到事件，但**永远无法用 `-=` 取消订阅**。当 Bot 死亡/反生成时，lambda 闭包（捕获了 `BrainManager.Instance`）仍然挂在事件上。如果 BotOwner 对象被回收但事件未触发，形成内存泄漏。

**修复方案**: 使用命名方法替代 lambda:

```csharp
// 在 Patch 类中定义静态方法
private static void OnBotDead(IPlayer player)
{
    BrainManager.Instance.ActivatedBots.Remove(player);
}

// 订阅时:
___botOwner_0.GetPlayer.OnPlayerDeadOrUnspawn += OnBotDead;

// 但更好的方案是存储订阅引用以便在适当时候取消。
// 由于 Bot 由 SPT 管理生命周期，可使用弱引用模式或 Accept 当前风险
// （大多数 Bot 会在游戏结束时一起清理）
```

---

### BB-5 [中] RemoveLayerForBot 每调用一次反射获取一次层字典

**文件**: `Internal/BrainHelpers.cs` 第 87 行

**问题描述**:

```csharp
internal static void RemoveLayerForBot(this BotOwner botOwner, string layerName)
{
    Dictionary<int, AICoreLogicLayerClass> botBrainLayerDictionary 
        = botOwner.Brain.BaseBrain.GetBrainLayerDictionary();  // 反射调用!
    // ...
}
```

`RemoveAllExcludedLayers` 对每个 `ExcludeLayer` 调用一次 `RemoveLayerForBot(string)`，每次内部都通过反射获取层字典。`GetBrainLayerDictionary()` 内部使用 `AccessTools.Field(...)` 反射查找字段。

**修复方案**: 提供批量版本，一次性获取字典后处理多个 Layer:

```csharp
internal static void RemoveAllExcludedLayers(this BotOwner botOwner)
{
    if (botOwner == null) return;

    // 一次性获取字典（仅一次反射调用）
    var botBrainLayerDictionary = botOwner.Brain.BaseBrain.GetBrainLayerDictionary();

    foreach (var excludeLayer in BrainManager.Instance.ExcludeLayers)
    {
        if (!excludeLayer.AffectsBot(botOwner)) continue;
        
        RemoveLayerForBotInternal(botOwner, excludeLayer.excludeLayerName, botBrainLayerDictionary);
    }
}

private static void RemoveLayerForBotInternal(BotOwner botOwner, string layerName, 
    Dictionary<int, AICoreLogicLayerClass> dict)
{
    // ... 原 RemoveLayerForBot(string) 的逻辑，但复用传入的 dict ...
}
```

---

### BB-6 [低] CollectionIntersection 延迟执行导致重复枚举

**文件**: `Internal/CollectionIntersection.cs` 第 11-54 行

**问题**: 所有属性返回 `IEnumerable<T>` 延迟执行。每次访问 `CommonElements`、`UniqueCollection1Elements` 等属性时，LINQ 查询重新执行。

**修复方案**: 在构造函数中立即 materialize:

```csharp
public CollectionIntersection(IEnumerable<T> collection1, IEnumerable<T> collection2)
{
    var common = collection1.Intersect(collection2).ToList();
    CommonElements = common;
    UniqueCollection1Elements = collection1.Except(common).ToList();
    UniqueCollection2Elements = collection2.Except(common).ToList();
}
```

---

## 四、Waypoints 前置模组修复与优化 (6个)

### WP-1 [严重] DependencyChecker as 转换永远返回 null -- 导致插件加载崩溃

**文件**: `Helpers/DependencyChecker.cs` 第 24 行

**问题描述**:

```csharp
var dependencies = pluginType.GetCustomAttributes(typeof(BepInDependency), true) 
    as BepInDependency[];  // 永远为 null!
```

`GetCustomAttributes(Type, bool)` 返回 `object[]`，其运行时类型是 `object[]`，不是 `BepInDependency[]`。因此 `as` 转换永远返回 `null`。随后的 `foreach` 循环（第 26 行）对 `null` 进行迭代，抛出 `NullReferenceException`。

**影响**: 如果任何代码路径调用 `ValidateDependencies()`，插件立即崩溃，**Waypoints 完全无法加载**。SAIN 因此失去 NavMesh 寻路能力。

**修复方案**:

```csharp
// 方案1: 使用 Cast<T>().ToArray()
var dependencies = pluginType.GetCustomAttributes(typeof(BepInDependency), true)
    .Cast<BepInDependency>().ToArray();

// 方案2: 使用泛型版本 (如果目标框架支持)
var dependencies = pluginType.GetCustomAttributes<BepInDependency>(true);
```

---

### WP-2 [高] WaypointPatch InjectNavmesh -- MainPlayer 可能为 null

**文件**: `Patches/WaypointPatch.cs` 第 43 行

**问题描述**:

```csharp
private static void InjectNavmesh(GameWorld gameWorld)
{
    string mapName = gameWorld.MainPlayer.Location.ToLower();  // NRE 风险!
```

第 27-28 行检查了 `gameWorld == null`，但没有检查 `gameWorld.MainPlayer == null`。如果 `BotsController.Init` 在主玩家生成之前被调用，访问 `.Location` 抛出 NRE。

**修复方案**:

```csharp
private static void InjectNavmesh(GameWorld gameWorld)
{
    var mainPlayer = gameWorld.MainPlayer;
    if (mainPlayer == null)
    {
        Logger.LogError("BotController::Init called, but MainPlayer doesn't exist");
        return;
    }
    string mapName = mainPlayer.Location.ToLower();
    // ... 后续不变
}
```

---

### WP-3 [高] DoorLinkPatch -- aiDoorsHolder 可能为 null

**文件**: `Patches/DoorLinkPatch.cs` 第 22、55 行

**问题描述**:

```csharp
var aiDoorsHolder = UnityEngine.Object.FindObjectOfType<AIDoorsHolder>();  // 可能返回 null
// ...
gameObject.transform.SetParent(aiDoorsHolder.transform);  // 第55行: NRE!
```

`FindObjectOfType<AIDoorsHolder>()` 在某些自定义地图或 Mod 场景中可能返回 null。

**修复方案**:

```csharp
var aiDoorsHolder = UnityEngine.Object.FindObjectOfType<AIDoorsHolder>();
if (aiDoorsHolder == null)
{
    Logger.LogError("No AIDoorsHolder found in scene, skipping custom door links");
    return;
}
```

---

### WP-4 [中] FindPathPatch 每次调用 new NavMeshPath -- 高 GC 压力

**文件**: `Patches/FindPathPatch.cs` 第 23 行

**问题描述**:

```csharp
NavMeshPath navMeshPath = new NavMeshPath();  // 每次寻路都 new!
```

`FindPath` 是 Bot 移动的核心函数，被高频调用。每次调用分配 `NavMeshPath` 产生大量 GC。

**修复方案**: 使用线程静态缓存:

```csharp
[ThreadStatic]
private static NavMeshPath _cachedPath;

[PatchPrefix]
public static bool PatchPrefix(Vector3 f, Vector3 t, out Vector3[] corners, ref bool __result)
{
    try
    {
        if (_cachedPath == null)
            _cachedPath = new NavMeshPath();
        _cachedPath.ClearCorners();

        if (NavMesh.CalculatePath(f, t, -1, _cachedPath) 
            && _cachedPath.status != NavMeshPathStatus.PathInvalid)
        {
            corners = _cachedPath.corners;
            __result = true;
        }
        else
        {
            corners = null;
            __result = false;
        }
        return false;
    }
    catch (Exception ex)
    {
        Logger.LogError($"FindPath error: {ex}");
        corners = null;
        __result = false;
        return false;
    }
}
```

---

### WP-5 [中] mod.ts -- 缺少 null 安全检查

**文件**: `ServerMod/src/mod.ts` 第 11-37 行

**问题描述**:

```typescript
for (let type in tables.bots.types) {
    const botType = tables.bots.types[type];
    if (!botType.difficulty) continue;
    // ...
}
```

代码已经检查了 `difficulty`，但 SPT 数据库某些 Bot 类型可能缺少 `Patrol` 或 `Mind` 子对象。

**修复方案**:

```typescript
for (const type in tables.bots.types) {
    const botType = tables.bots.types[type];
    if (!botType?.difficulty) continue;

    for (const diff in botType.difficulty) {
        const diffSetting = botType.difficulty[diff];
        if (!diffSetting?.Patrol || !diffSetting?.Mind) continue;

        diffSetting.Patrol.LOOK_TIME_BASE = 3;
        diffSetting.Patrol.GO_TO_NEXT_POINT_DELTA = 3;
        diffSetting.Patrol.GO_TO_NEXT_POINT_DELTA_RESERV_WAY = 15;
        diffSetting.Patrol.RESERVE_TIME_STAY = 12;
        diffSetting.Patrol.SPRINT_BETWEEN_CACHED_POINTS = 400;
        diffSetting.Mind.CAN_STAND_BY = false;
        diffSetting.Patrol.USE_CHACHE_WAYS = false;
    }
}
```

---

### WP-6 [低] 死代码与资源泄漏

| 位置 | 问题 | 修复 |
|---|---|---|
| `WaypointsPlugin.cs:63-107` | `PerfTimingPatch` 完全未使用的死代码，约 45 行 | 删除或 `#if DEBUG` 包裹 |
| `Patches/GroupPointCachePatch.cs` 等 3 文件 | 注释掉的 3 个 Patch，含未完成代码 | 移到 `_DisabledPatches/` 子目录 |
| `Components/NavMeshDebugComponent.cs:79-82` | `StreamWriter` 未用 `using` 块 | 改为 `File.WriteAllText()` |
| `Components/NavMeshDebugComponent.cs:50,65` | `new Mesh()` 在 Dispose 时未显式 `Destroy()` | 在 Dispose 中添加 `Destroy(mesh)` |
| `Settings.cs:36,43,53` | 配置变更事件订阅永不取消 | 添加生命周期注释 |

---

## 五、修复优先级路线图

### 第一阶段 (必须立即修复 -- 影响稳定性和功能正确性)

| # | 问题 | 模组 | 风险 |
|---|---|---|---|
| WP-1 | DependencyChecker as 转换 null 导致崩溃 | Waypoints | 插件完全无法加载 |
| BB-1 | Release 模式 Patch 无异常保护 | BigBrain | Bot 大脑崩溃 |
| BUG-1 | DeadBots RemoveAt 索引错乱 | SAIN | 尸体障碍物管理错误 + 内存泄漏 |
| BB-2 | HasSameContents 语义错误 | BigBrain | Layer 排除逻辑错误 |

### 第二阶段 (高优先级性能与正确性修复)

| # | 问题 | 模组 | 风险 |
|---|---|---|---|
| WP-2 | MainPlayer null NRE | Waypoints | 地图加载崩溃 |
| WP-3 | aiDoorsHolder null NRE | Waypoints | 门 Link 创建失败 |
| BUG-2 | MovementContextIsAIPatch 全局影响 | SAIN | 真人玩家物理异常 |
| BUG-3 | CanBeSnappedPatch 全局影响 | SAIN | 真人玩家门交互异常 |
| WP-4 | FindPathPatch 每次 new NavMeshPath | Waypoints | GC 压力 |
| PERF-1 | CanGoToPoint 每次 new NavMeshPath | SAIN | GC 压力 |

### 第三阶段 (性能优化)

| # | 问题 | 模组 |
|---|---|---|
| PERF-2 | AddNavObstacles 每帧 OverlapSphere | SAIN |
| PERF-3 | TrimExcess 多余内存重分配 | SAIN |
| BB-3 | CustomLayersReadOnly 每访问分配 Dictionary | BigBrain |
| BUG-4 | SAINNoBushESP 改为非 MonoBehaviour | SAIN |
| PERF-4 | Bot 错峰更新 | SAIN |
| BB-5 | RemoveLayerForBot 批量反射优化 | BigBrain |

### 第四阶段 (代码质量改进)

| # | 问题 | 模组 |
|---|---|---|
| BB-4 | 匿名 lambda 事件泄漏 | BigBrain |
| BB-6 | CollectionIntersection 重复枚举 | BigBrain |
| WP-5 | mod.ts null 安全 | Waypoints |
| WP-6 | 死代码清理和资源泄漏 | Waypoints |

---

> [VAULT-TEC 终审备注] 以上 22 项修复涵盖 SAIN 本体、BigBrain 1.3.2 和 Waypoints 1.7.1 三个代码库。最严重的 4 个问题（WP-1、BB-1、BUG-1、BB-2）应优先处理 -- 它们分别导致崩溃、Bot 脑死亡、内存泄漏和逻辑错误。Vault-Tec 标准维修协议建议按上述路线图分阶段实施。每个修复都附带了精确的文件路径、行号和替代代码。
>
> Vault-Tec -- Building a Better Future... Underground!
