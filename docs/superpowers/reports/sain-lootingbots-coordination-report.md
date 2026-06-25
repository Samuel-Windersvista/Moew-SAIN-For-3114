# SAIN 与 LootingBots 协同问题专项报告

## 1. 背景与用户现象

- **LootingBots**：由 skwizzy 开发，负责控制 AI bot 搜刮尸体/容器、评估物品价值、管理背包容量。
- **SAIN**：控制 AI 的战斗、搜索、撤离、小队协同等高级行为。
- **用户反馈现象**：
  - bot 在拾取物品时出现“发呆”——既不对附近枪声/敌人作出反应，也不继续搜刮或移动；
  - bot 在搜刮过程中被攻击时反应迟缓；
  - 小队成员搜刮时，其他 bot 没有提供有效掩护，而是乱跑或呆站。

本报告聚焦这两个模组之间的接口、状态共享、战斗/搜刮切换逻辑，定位协同失效的根因。

## 2. 集成架构

### 2.1 SAIN 对 LootingBots 的反射接口

SAIN 通过 `Layers\Extract\LootingBotsInterop.cs` 反射调用 LootingBots 的外部 API：

```csharp
_LootingBotsExternalType = Type.GetType("LootingBots.External, skwizzy.LootingBots");
```

解析出的 6 个方法：

| 方法 | 作用 |
|---|---|
| `ForceBotToScanLoot(BotOwner)` | 强制 bot 扫描附近战利品 |
| `PreventBotFromLooting(BotOwner, float)` | 在指定秒数内阻止搜刮 |
| `CheckIfInventoryFull(BotOwner)` | 背包是否已满 |
| `GetNetLootValue(BotOwner)` | 当前搜刮到的物品总价值 |
| `GetItemPrice(LootItem)` | 单件物品估价 |
| `IsBotLooting(BotOwner)` | 是否正在搜刮 |

### 2.2 每个 bot 的多重集成实例

SAIN 中至少有三处会创建 `SAINLootingBotsIntegration`：

1. `BotDecisionManager`（`BotDecisionManager.cs:41-50`）——用于战后拾取触发；
2. `ExtractLayer.ExtractFromLoot()`（`ExtractLayer.cs:144-146`）——用于“满战利品撤离”；
3. `PeacefulLayer.ExtractFromLoot()`（`PeacefulLayer.cs:163-165`）——同样用于“满战利品撤离”。

三处均为独立实例，各自维护自己的 `NetLootValue`、`FullInventory`、`FullOnLoot`、`UpdateInfoTimer` 等状态。

## 3. 关键交互点分析

### 3.1 战后拾取触发

`BotDecisionManager.getDecision()`（`BotDecisionManager.cs:87-116`）在 `enemy == null` 时检查战后恢复与战后拾取：

```csharp
if (GlobalSettingsClass.Instance.Mind.POST_COMBAT_RECOVERY && CombatEndTime > 0 && ...)
{
    // 治疗 / 换弹
    CombatEndTime = -1f;
}

if (CombatEndTime > 0 && Time.time - CombatEndTime > 10f)
{
    SAINLootingBotsIntegration?.TryTriggerPostCombatLoot();
    CombatEndTime = -1f;
}
```

`CombatEndTime` 只在 `SAINActivationClass.cs:80-86` 设置，且被 `POST_COMBAT_RECOVERY` 开关包裹：

```csharp
if (_wasInCombat && !Bot.IsInCombat)
{
    if (GlobalSettingsClass.Instance.Mind.POST_COMBAT_RECOVERY)
    {
        Bot.Decision.DecisionManager.CombatEndTime = Time.time;
    }
}
```

**根因 1**：如果用户关闭 `POST_COMBAT_RECOVERY`，`CombatEndTime` 永不为正，导致 `TryTriggerPostCombatLoot()` 永远不会执行。bot 战斗结束后不会主动搜刮。

### 3.2 搜刮中威胁感知与中断

`SAINLootingBotsIntegration.CheckLootingVigilance()`（`SAINLootingBotsIntegration.cs:96-119`）每 0.5 秒检查一次：

```csharp
bool hasThreat = false;
if (SAIN?.GoalEnemy != null && SAIN.GoalEnemy.RealDistance < 15f)
    hasThreat = true;
if (SAIN?.Suppression?.IsSuppressed == true)
    hasThreat = true;

if (hasThreat)
{
    LootingBots.LootingBotsInterop.TryPreventBotFromLooting(BotOwner, 10f);
    Logger.LogInfo($"SAIN: Interrupted looting for {BotOwner.name} — threat detected");
}
```

**问题**：

- 只检查**可见敌人**或**已被压制**状态；
- 对**听到的枪声/脚步声**、**子弹飞过头顶**、**队友报告敌人**等听觉事件不直接触发中断；
- `TryPreventBotFromLooting` 只是向 LootingBots 请求“未来 10 秒内不要搜刮”，若 bot 已经处于拾取动画中，LootingBots 可能无法立即取消动作。

因此，当敌人在 bot 开始拾取动作后才出现，bot 可能继续完成动画，表现为“反应慢/发呆”。

### 3.3 安全搜刮位置检查

`TryEnsureSafeLootingPosition()`（`SAINLootingBotsIntegration.cs:121-144`）在 bot 准备搜刮前调用：

```csharp
if (SAIN?.Cover?.CoverInUse != null)
    return true;

bool areaIsSafe = SAIN?.GoalEnemy == null;
if (areaIsSafe)
{
    SAIN?.BotOwner?.SetPose(0f); // 蹲下
    return true;
}

LootingBots.LootingBotsInterop.TryPreventBotFromLooting(BotOwner, 30f);
return false;
```

**问题**：

- 仅以 `GoalEnemy == null` 判断区域安全，未检查最近听到的声音、未确认子弹来源方向；
- `SetPose(0f)` 改变姿态，但没有阻止 LootingBots 继续执行搜刮；
- 如果调用方未正确处理 `false` 返回值，bot 仍可能开始搜刮。

### 3.4 小队拾取守望

`SquadDecisionClass.shallLootingOverwatch()`（`SquadDecisionClass.cs:116-142`）：

```csharp
private bool shallLootingOverwatch()
{
    if (!ModDetection.LootingBotsLoaded) return false;
    if (Bot.GoalEnemy != null) return false;

    foreach (var member in Bot.Squad.Members.Values)
    {
        if (member == Bot) continue;
        if (member?.BotOwner == null || member.BotOwner.IsDead) continue;
        float dist = Vector3.Distance(Bot.Position, member.Position);
        if (dist >= 25f) continue;

        if (LootingBots.LootingBotsInterop.IsBotLooting(member.BotOwner))
            return true;

        if (!member.IsInCombat && member?.Mover?.Moving == false)
            return true;
    }
    return false;
}
```

**根因 2（发呆）**：fallback 条件 `!member.IsInCombat && member?.Mover?.Moving == false` 过于宽泛。只要队友**不在战斗且静止**，无论其是否真的在搜刮，当前 bot 都会进入 `LootingOverwatch` 状态。

**根因 3（乱跑）**：`CombatSquadLayer.GetNextAction()`（`CombatSquadLayer.cs:13-43`）对 `ESquadDecision.LootingOverwatch` 没有 case，会落入 `default`：

```csharp
default:
    return new Action(typeof(RegroupAction), $"DEFAULT!");
```

于是“守望”实际变成了“向队长重新集结”，玩家看到的就是 bot 呆站一会儿又突然跑开。

### 3.5 战斗状态切换与 LootingBots 无互锁

SAIN 的战斗状态由 `BotComponent.ManualUpdate`（`BotComponent.cs:193`）决定：

```csharp
bool inCombat = active && !inStandBy && SAINLayersActive && GoalEnemy != null;
BotActivation.SetInCombat(inCombat);
```

`GoalEnemy` 由视觉/听觉事件驱动添加。LootingBots 没有任何 API 能告诉 SAIN“请等我完成这次拾取再切战斗”。

结果是：

- 当 bot 正在拾取动画中，`GoalEnemy` 被设置 → `inCombat` 立即变为 true；
- SAIN 的战斗层接管 movement/aim/shoot，但 LootingBots 可能仍在控制手部/交互；
- 若两个层同时尝试控制 bot，就会产生卡顿、发呆、不射击。

### 3.6 反射接口的空引用风险

`shallLootingOverwatch()` 使用 `ModDetection.LootingBotsLoaded` 判断 LootingBots 是否存在：

```csharp
if (!ModDetection.LootingBotsLoaded) return false;
```

该属性只检查插件 key 是否存在。如果 `LootingBotsInterop.Init()` 反射解析失败（例如 LootingBots 更新了类型名），`_IsBotLootingMethod` 将为 null，但 `IsBotLooting()` 仍会调用 `Init()` 后执行 `Invoke(null, [botOwner])`，导致 `NullReferenceException`。

## 4. 协同问题清单

| 编号 | 问题 | 风险等级 | 表现 |
|---|---|---|---|
| C-1 | 反射 `Invoke()` 无 try-catch，签名变化即崩溃 | 高 | 版本更新后 bot 行为异常或报错 |
| C-2 | `shallLootingOverwatch` fallback 把静止队友误判为搜刮 | 高 | bot 对非搜刮队友发呆守望 |
| C-3 | `ModDetection.LootingBotsLoaded` 与反射解析状态不一致 | 高 | 可能 NPE |
| C-4 | 战后拾取依赖 `POST_COMBAT_RECOVERY` | 中 | 关闭恢复后不再战后搜刮 |
| C-5 | 三个 `SAINLootingBotsIntegration` 实例状态不一致 | 中 | 重复反射调用、状态漂移 |
| C-6 | `CombatSquadLayer` 无 `LootingOverwatch` 动作 | 高 | 守望变集结，bot 乱跑 |
| C-7 | `CheckLootingVigilance` 只检查可见敌人/压制，忽略听觉 | 中 | 被偷袭/侧翼时反应慢 |
| C-8 | 无 LootingBots→SAIN 的“我正在搜刮”信号 | 中 | 搜刮动画中被强制切战斗 |
| C-9 | 插件 key 字符串多处硬编码 | 低 | 维护风险 |
| C-10 | 价格缓存全量清空 | 低 | GC/性能微影响 |
| C-11 | `FullOnLoot` 单向不重置 | 低 | 丢弃物品后仍按满包撤离 |
| C-12 | `TryEnsureSafeLootingPosition` 安全判断过于简单 | 中 | 危险区域开始搜刮 |

## 5. 根因链：bot 发呆 / 战斗反应慢

### 5.1 发呆链

```
队友静止（或死亡后未移除）
    ↓
shallLootingOverwatch() fallback 命中
    ↓
ESquadDecision.LootingOverwatch
    ↓
CombatSquadLayer 无对应 case → default RegroupAction
    ↓
bot 跑向队长 / 找不到队长时原地 idle
```

同时，若当前 bot 自己正在搜刮，SAIN 的战斗层尚未激活，`CheckLootingVigilance` 又只关心 15m 内可见敌人，对**听到但还没看见的敌人**不会主动取消搜刮，于是出现“听到枪声还在摸包”的发呆。

### 5.2 战斗反应慢链

```
敌人开火 → SAIN 听觉系统处理 → enemy 被加入
    ↓
GoalEnemy != null → BotInCombat = true
    ↓
BotDecisionManager 下次 tick（最慢 0.1s 后）选择 combat decision
    ↓
若此时 LootingBots 仍在控制拾取动画，两个系统竞争 bot 控制权
    ↓
动画/移动/瞄准冲突 → 看起来“反应慢”或“卡住”
```

## 6. 修复建议

### 6.1 立即处理（高优先级）

1. **统一并单例化 `SAINLootingBotsIntegration`**
   - 在 `BotComponent` 中创建唯一实例，由 `BotComponent.ManualUpdate` 统一调用 `Update()`；
   - `ExtractLayer` 与 `PeacefulLayer` 只读取 `Bot.SAINLootingBotsIntegration.FullOnLoot`，不再自己创建实例。

2. **修复 `shallLootingOverwatch()` fallback**
   - 移除 `!member.IsInCombat && !member.Mover.Moving` 兜底；
   - 若 `IsBotLooting` 返回 false，则不进入守望；
   - 添加队友距离/视野限制，只在队友真实搜刮且在当前 bot 可见范围内才守望。

3. **为 `LootingOverwatch` 实现真正动作**
   - 新增 `LootingOverwatchAction`，行为：
     - 面向队友方向；
     - 寻找附近掩体并蹲伏；
     - 保持警戒，发现敌人立即切换战斗；
   - 在 `CombatSquadLayer.GetNextAction()` 中增加 case。

4. **反射接口增加异常保护**
   - 在 `LootingBotsInterop` 每个 `Invoke` 处加 `try-catch`；
   - 任一方法调用失败后设置全局“不可用”标志，本局不再尝试；
   - 统一用 `LootingBotsInterop.IsAvailable` 替代 `ModDetection.LootingBotsLoaded`。

### 6.2 中优先级

5. **扩展 `CheckLootingVigilance()` 威胁来源**
   - 除了 `GoalEnemy < 15m` 和 `IsSuppressed`，还应监听：
     - 最近听到的枪声方向与距离；
     - 子弹飞过头顶事件；
     - 队友报告的敌人位置。
   - 威胁出现时立即调用 `TryPreventBotFromLooting` 并尝试取消当前交互。

6. **战后拾取与 `POST_COMBAT_RECOVERY` 解耦**
   - 在 `SAINActivationClass` 中设置独立的 `LootCombatEndTime`；
   - 在全局设置中增加“战后自动搜刮”独立开关。

7. **增加 LootingBots→SAIN 的“搜刮中”事件**
   - 若 LootingBots 侧能触发事件，SAIN 可在收到事件时：
     - 临时降低听觉优先级？（不推荐，因为会失去突然袭击反应）
     - 或标记 `IsLooting`，在 `CheckLootingVigilance` 中更快中断；
   - 更安全的做法：让 SAIN 在切战斗前调用 `TryPreventBotFromLooting` 并等待一帧，减少动画冲突。

8. **改进 `TryEnsureSafeLootingPosition()` 安全判断**
   - 检查最近 5 秒内听到的危险声音；
   - 检查附近是否有已知敌人（即使不可见）；
   - 检查是否处于开阔地带（无掩体且距离最近掩体过远）。

### 6.3 低优先级

9. 将 `me.skwizzy.lootingbots` 插件 key 提取为常量；
10. 价格缓存改为 LRU；
11. `FullOnLoot` 在物品价值下降/背包容量变化时允许重置。

## 7. 测试与验证建议

- **单元/静态验证**：
  - 确认 `LootingBotsInterop` 在类型解析失败时返回 `false` 并不再调用 `Invoke`；
  - 确认 `shallLootingOverwatch` 在队友静止但不搜刮时返回 `false`。
- **游戏内验证**：
  - 关闭 `POST_COMBAT_RECOVERY`，观察 bot 战后是否仍触发搜刮；
  - 在 bot 开始拾取后从侧翼开枪，观察 bot 是否能在 0.5s 内中断；
  - 观察小队成员搜刮时，其他 bot 是否就地警戒而非跑向队长。
- **日志验证**：
  - 开启 DEBUG 后检查 `SAIN: Interrupted looting for ...` 出现时机是否与威胁出现一致。

## 8. 结论

SAIN 与 LootingBots 的协同问题主要集中在：

1. **状态不共享**：SAIN 的“守望/撤离/战后拾取”逻辑各自持有 LootingBots 集成实例，导致状态漂移；
2. **战斗与搜刮无互锁**：SAIN 一旦检测到敌人就会立即接管 bot，而 LootingBots 可能仍在执行拾取动画；
3. **守望决策动作缺失**：`LootingOverwatch`  enum 存在但无实际动作，导致 bot 发呆或乱跑；
4. **威胁感知范围窄**：搜刮中断只检查极近距离可见敌人，忽略了听觉和侧翼威胁。

建议按 6.1 节立即处理高优先级项，可显著改善 bot 搜刮时的发呆与战斗反应问题；中优先级项作为下一迭代补充。

---

*报告生成时间：2026-06-23*
