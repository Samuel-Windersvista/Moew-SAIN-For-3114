# SAIN 性能与可维护性优化审查报告

## 1. 项目概述

- **仓库**：`E:\云文件\GitHub\Moew-SAIN-For-3114`
- **目标游戏**：Escape from Tarkov（SPT 3.11）
- **模组作用**：SAIN（Single Player Tarky AI）替换/扩展原生 AI 决策、感知、搜索、撤离等行为。
- **审查目的**：在不改变当前行为的前提下，识别性能、可维护性、反射兼容性与架构层面的问题，为后续迭代提供清单。

## 2. 检查范围与方法

- 静态阅读核心文件：
  - `Components\BotComponent.cs`
  - `Classes\Bot\SAINActivationClass.cs`
  - `Classes\Bot\Decision\BotDecisionManager.cs`
  - `Classes\Bot\Decision\SquadDecisionClass.cs`
  - `Classes\Bot\Sense\Hearing\HearingInputClass.cs`
  - `Classes\BotManager\BotHearingClass.cs`
  - `Layers\Extract\ExtractLayer.cs`
  - `Layers\Peace\PeacefulLayer.cs`
  - `Layers\Extract\SAINLootingBotsIntegration.cs`
  - `Layers\Extract\LootingBotsInterop.cs`
  - `Layers\Combat\Squad\CombatSquadLayer.cs`
- 搜索了所有 Harmony Patch 目标与 `LootingBots` 反射调用点。
- 未运行测试，未做运行时性能采样；结论基于代码结构与调用频率推断。

## 3. 主要发现

### 3.1 单 Bot 组件树过大，初始化链长

`BotComponent.CreateClasses()`（`BotComponent.cs:233-282`）一次性构造 30 多个子类。虽然使用 `try-catch` 包裹，但 `InitClasses()`（`BotComponent.cs:336-351`）采用“任一失败即返回 false 并 Dispose”的策略。在 SPT 频繁更新时，只要某个子类初始化异常，整个 bot 的 SAIN 就会被丢弃，回退到原生 AI。

```csharp
foreach (var botClass in BotClasses)
{
    try { botClass.Init(); }
    catch (Exception ex)
    {
        Logger.LogError($"Error When Initializing Class [{botClass}], Disposing... : {ex}");
        return false;
    }
}
```

**建议**：对非关键子类（如 `Talk`、`BotLight`）允许失败并标记禁用，而不是整体 Dispose；关键类才应导致 SAIN 失效。

### 3.2 Tick 分组与条件判断存在冗余

`BotComponent.ManualUpdate`（`BotComponent.cs:169-201`）维护四层 tick 分组：

- `AlwaysTickClasses`
- `TickWhenActiveClasses`
- `TickWhenNoSleepClasses`
- `TickWhenCombatClasses`

其中 `active`、`inStandBy`、`inCombat` 的判断在每一帧都会重新计算，并且部分子类（如 `SAINHearingSensorClass` 标记为 `OnlyNoSleep`）实际上还会自己内部做状态过滤。这种分层虽然清晰，但增加了每帧分支数量。

**建议**：考虑把 `active / standBy / combat` 变化事件化，仅在这些状态切换时重新构建 tick 列表，而不是每帧重复判断。

### 3.3 听觉系统每声源触发协程，GC 压力大

`BotHearingClass.PlayAISound`（`BotHearingClass.cs:65-89`）在每次播放 AI 音效时都会启动一个 `StartCoroutine(WaitDelayThenPlayDefaultBotEvent(...))`。该协程等待 0.1s 后再调用原生 `BotEventHandler.PlaySound`。

在高 bot 密度、枪声/脚步声密集的场景下，每秒可能产生数十到上百个协程，带来：

- 大量 `Coroutine`/`YieldInstruction` 分配；
- `WaitForSeconds` 对象分配（0.1s 短周期）；
- 原生 `PlaySound` 被批量延迟触发，增加同一帧处理压力。

```csharp
BotController.StartCoroutine(WaitDelayThenPlayDefaultBotEvent(soundType, playerComponent, position, range, volume));
```

**建议**：
1. 把音效延迟投递改为基于 `Time.time` 的队列，每帧统一批量处理；
2. 若必须保持原生事件触发时序，考虑使用对象池缓存 `WaitForSeconds` 或改用 `BotController.WaitForSeconds` 缓存实例。

### 3.4 听觉缓存处理存在高频分配与排序

`HearingInputClass` 维护 4 个 `List<AISoundData>` 缓存：`AISoundCachedEvents`、`...Gunshots`、`...Suppressed`、`...Conversations`。`ProcessAISoundCache` 中会对每个列表调用 `List.Sort`，且 `AISoundData` 是 struct（值类型），按距离排序会触发值类型比较装箱（取决于编译器优化）。

```csharp
Sounds.Sort((a, b) => a.PlayerDistance.CompareTo(b.PlayerDistance));
```

**建议**：
- 若 `AISoundData` 较大，可考虑改为 class；
- 使用 `Span<T>` 或手动冒泡/插入排序，避免 `List.Sort` 的委托分配；
- 按距离分组阈值处理时，未必需要全排序。

### 3.5 撤离-拾取集成状态机存在重复与不一致

`ExtractLayer.cs`（`Lines 134-168`）与 `PeacefulLayer.cs`（`Lines 153-181`）拥有完全相同的 `ExtractFromLoot()` 代码，各自持有独立的 `SAINLootingBotsIntegration` 实例。此外，`BotDecisionManager`（`BotDecisionManager.cs:41-50`）也懒加载了一个独立实例。

这意味着同一个 bot 可能同时存在 3 个 `SAINLootingBotsIntegration`：

- `BotDecisionManager` 中的实例负责 `TryTriggerPostCombatLoot`；
- `ExtractLayer` 与 `PeacefulLayer` 的实例分别负责 `UpdateLootingBotsInfo()`、`CheckStatus()`、`CheckLootingVigilance()`。

后果：

- `NetLootValue`、`FullInventory`、`FullOnLoot` 等状态被多处维护；
- `UpdateLootingBotsInfo()` 每 5 秒调用一次，但两个 Layer 可能在同一 tick 分别触发，导致重复反射调用；
- `FullOnLoot` 一旦置为 `true` 永不重置（`SAINLootingBotsIntegration.cs:34`），若 bot 丢弃高价值物品后仍按“满战利品”撤离。

**建议**：
- 将 `SAINLootingBotsIntegration` 提升为 `BotComponent` 单例，由 `BotComponent.ManualUpdate` 统一驱动；
- `FullOnLoot` 在库存价值下降或容量变化时允许回到 `false`；
- 移除 `ExtractLayer` 与 `PeacefulLayer` 中的重复代码。

### 3.6 战后拾取触发与设置强耦合

`BotDecisionManager.getDecision()`（`BotDecisionManager.cs:87-116`）中：

```csharp
if (GlobalSettingsClass.Instance.Mind.POST_COMBAT_RECOVERY && CombatEndTime > 0 && ...)
{
    // 处理治疗/换弹
    CombatEndTime = -1f;
}

if (CombatEndTime > 0 && Time.time - CombatEndTime > 10f)
{
    SAINLootingBotsIntegration?.TryTriggerPostCombatLoot();
    CombatEndTime = -1f;
}
```

而 `CombatEndTime` 只在 `SAINActivationClass.ManualUpdate`（`SAINActivationClass.cs:80-86`）中设置，并且被 `POST_COMBAT_RECOVERY` 开关包裹：

```csharp
if (_wasInCombat && !Bot.IsInCombat)
{
    if (GlobalSettingsClass.Instance.Mind.POST_COMBAT_RECOVERY)
    {
        Bot.Decision.DecisionManager.CombatEndTime = Time.time;
    }
}
```

**问题**：如果玩家关闭 `POST_COMBAT_RECOVERY`，`CombatEndTime` 永远不会被设置，那么 `TryTriggerPostCombatLoot()` 也永远不会执行。这是一个隐式依赖。

**建议**：战后拾取应拥有独立的计时器与开关，不应依赖治疗/换弹恢复开关。

### 3.7 反射互操作脆弱且无异常保护

`LootingBotsInterop.cs` 通过字符串程序集名称解析外部类型：

```csharp
_LootingBotsExternalType = Type.GetType("LootingBots.External, skwizzy.LootingBots");
```

并直接 `Invoke` 6 个方法：

```csharp
return (bool)_ForceBotToScanLootMethod.Invoke(null, [botOwner]);
```

**问题**：

- 字符串硬编码，LootingBots 未来若重命名程序集或命名空间，将静默失败；
- 未对 `Invoke` 使用 `try-catch`；方法签名一旦变化，`TargetParameterCountException`/`InvalidCastException` 会直接抛出到调用栈；
- `ModDetection.LootingBotsLoaded` 只检查插件是否加载（`Chainloader.PluginInfos.ContainsKey`），不检查反射解析是否成功；
- 价格缓存每 60 秒整体清空（`LootingBotsInterop.cs:178-182`），而非 LRU 淘汰。

**建议**：
- 对所有 `Invoke` 增加 `try-catch` 并记录 warning，失败时禁用该接口；
- 将 `ModDetection.LootingBotsLoaded` 与 `LootingBotsInterop.Init()` 统一为单一“可用”状态；
- 价格缓存改为带过期时间的 LRU 或滑动窗口。

### 3.8 决策频率固定 10Hz，缺少自适应

`BotDecisionManager` 固定每 0.1s 调用一次 `getDecision()`：

```csharp
private const float DECISION_FREQUENCY = 1f / 10;
```

对大多数非战斗 bot 而言，10Hz 过于频繁；对近距离交火 bot 而言，10Hz 又可能过慢。无法根据距离玩家、战斗状态、bot 数量动态调整。

**建议**：
- 引入距离/状态自适应频率：远距离或待机时降至 1-2Hz，近距离交火时提升到 20-30Hz；
- 或使用事件驱动：敌人出现、受伤、手雷等关键事件立即触发决策，而非等待下一 tick。

### 3.9 Harmony Patch 目标过多，EFT 更新维护成本高

SAIN 对 37 个以上的 EFT 原生类型进行了 Harmony 补丁，覆盖 `BotOwner`、`BotMover`、`Player`、`BotsGroup`、`BotMemoryClass`、`BotHearingSensor` 等核心类。其中部分补丁仅做 null-check（如 `FixItemTakerPatch`），大量补丁集中在 `EnemyInfo`、`Player`、`BotAimingClass`。

**问题**：

- SPT/EFT 版本升级时，任一目标方法签名变化都可能导致补丁失效；
- 过多前缀/后缀补丁增加了与其他 AI 模组（如 LootingBots）的冲突面；
- 仅做 null-guard 的补丁可以通过更通用的 `Finalizer` 或集中式 Null 检查减少重复。

**建议**：
- 对纯 null-guard 的补丁统一使用 `Finalizer` 处理异常，减少 Prefix 数量；
- 建立 Patch 清单文档，每次 SPT 升级时逐项核对。

### 3.10 `CombatSquadLayer` 缺少 `LootingOverwatch` 分支

`ESquadDecision.LootingOverwatch` 在 `SquadDecisionClass.shallLootingOverwatch()` 中被使用，但 `CombatSquadLayer.GetNextAction()` 的 `switch` 中没有对应 case，会落入 `default` 返回 `RegroupAction`。

```csharp
default:
    return new Action(typeof(RegroupAction), $"DEFAULT!");
```

这导致“拾取守望”决策实际表现为“重新集结”，既无法提供掩护，也容易让玩家看到 bot 发呆后突然跑向队长。

**建议**：
- 为 `LootingOverwatch` 实现专门的守望动作（如 `HoldPositionAction` 或 `SniperOverwatchAction`）；
- 若暂时不需要该决策，考虑从 enum 中移除，避免误导。

### 3.11 魔法数字与注释代码散落

代码中存在大量未命名的阈值（如 `25f`、`30f`、`15f`、`10f`、`0.5f`）以及大量被注释掉的调试代码（如 `SAINHearingSensorClass.CheckCalcGoal()` 整段注释、`BotActivationClass.CheckStandBy()` 中整段备用逻辑）。

**建议**：
- 将阈值提取为 `const` 字段并集中配置到 `GlobalSettings` 或 `PersonalitySettings`；
- 清理长期不用的注释代码，使用版本控制保留历史。

## 4. 优化建议汇总

| 优先级 | 建议 | 预估收益 |
|---|---|---|
| 高 | 把 `SAINLootingBotsIntegration` 提升为 `BotComponent` 单例，统一状态与更新 | 消除重复调用、避免状态不一致 |
| 高 | 听觉延迟协程改为基于 `Time.time` 的批量队列 | 显著降低 GC 与协程开销 |
| 高 | 为 `LootingBotsInterop.Invoke` 增加 try-catch 与统一可用状态 | 提高跨版本稳定性 |
| 高 | 为 `ESquadDecision.LootingOverwatch` 实现真正动作或移除 | 修复守望发呆/误跑行为 |
| 中 | 战后拾取触发与 `POST_COMBAT_RECOVERY` 解耦 | 避免设置关闭后功能失效 |
| 中 | 决策频率按距离/战斗状态自适应 | 降低远距 bot CPU 占用 |
| 中 | `FullOnLoot` 允许根据库存价值回落 | 避免错误撤离 |
| 中 | 对纯 null-guard 的 Harmony 补丁使用 `Finalizer` 统一处理 | 减少补丁数量与维护面 |
| 低 | 听觉缓存排序避免委托分配 | 小幅 GC 优化 |
| 低 | 清理魔法数字与注释代码 | 提高可读性 |

## 5. 风险等级汇总

| 编号 | 问题 | 风险等级 |
|---|---|---|
| S-1 | 单 bot 30+ 子类初始化链任一失败即整体 Dispose | 中 |
| S-2 | 每音效启动协程，GC/协程压力大 | 高 |
| S-3 | 听觉缓存排序与列表分配 | 中 |
| S-4 | `ExtractFromLoot` 在两层重复，多个 Integration 实例 | 高 |
| S-5 | `FullOnLoot` 单向不重置 | 中 |
| S-6 | 战后拾取依赖 `POST_COMBAT_RECOVERY` | 中 |
| S-7 | 反射互操作无异常保护，字符串硬编码 | 高 |
| S-8 | 决策频率固定 10Hz | 中 |
| S-9 | Harmony Patch 目标过多 | 中 |
| S-10 | `LootingOverwatch` 无对应动作 | 高 |
| S-11 | 魔法数字与注释代码 | 低 |

## 6. 结论

SAIN 是一个功能庞大但复杂度很高的 AI 框架。当前最大的可维护性风险集中在三方面：

1. **LootingBots 集成点分散且状态不一致**：`ExtractLayer`、`PeacefulLayer`、`BotDecisionManager` 各自持有实例，重复更新，容易导致“bot 发呆”“错误撤离”等玩家可见 bug。
2. **听觉/感知系统性能开销高**：每声源启动协程、每帧列表排序与分配，在 bot 密集场景下是显著的性能热点。
3. **反射互操作与 Harmony Patch 维护成本高**：缺少异常保护与统一状态，EFT/SPT 升级后极易出现静默失败或硬崩溃。

建议优先处理 S-4、S-7、S-10、S-2 四项，可显著提升稳定性与玩家体验；其余项作为长期清理项分批完成。

---

*报告生成时间：2026-06-23*
