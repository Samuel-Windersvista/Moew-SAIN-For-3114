# SAIN 第8章问题 —— 实施计划

> **版本**: 基于 SAIN 战斗逻辑综合分析报告（2026-05-31 修订版）
> **优先级分级**: P0=严重阻塞 / P1=中等 / P2=低优先级 / P3=信息性

---

## 问题总览

| 编号 | 优先级 | 标题 | 类型 | 工作量 |
|:----:|:------:|------|:----:|:------:|
| 8.1 | P0 | MoveToEngage 决策未集成 | 代码修复 | S |
| 8.12 | P0 | AggressionMultiplier 全为 1.0 | 配置/代码 | S |
| 8.2 | — | 无弹药副武器检查（已修复） | 已修复 | — |
| 8.3 | P1 | 战后恢复仅 10s 窗口 | 代码修复 | S |
| 8.4 | P1 | Freeze 行为仅限室内 | 功能扩展 | M |
| 8.5 | P1 | 脚步声/枪声判定模型不统一 | 设计修改 | M |
| 8.6 | P1 | 武器切换概率无 Bot 类型分级 | 配置修改 | S |
| 8.11 | P1 | CreepOnEnemy 未使用 | 代码清理/实现 | M |
| 8.13 | P2 | SelfAction Holster 槽位遗漏 | 代码修复 | S |
| 8.14 | P2 | 关键函数体被注释掉 | 代码清理 | S |
| 8.7 | P2 | 压制仅子弹飞过触发 | 功能扩展 | M |
| 8.8 | P2 | LootingOverwatch 检测粗糙 | 代码改进 | S |
| 8.9 | P2 | 手雷威胁仅时间过期 | 代码修复 | S |
| 8.10 | P2 | 狗斗切换可能抽搐 | 代码修复 | S |
| 8.15 | P3 | Boss/Follower/Goons 层优先级差异 | 文档 | — |
| 8.16 | P3 | 拾取价值撤离联动未描述 | 文档 | — |
| 8.17 | P3 | 头盔武器切换概率遗漏 | 文档 | — |
| 8.18 | P3 | LootingBots 层移除影响 | 文档 | — |
| 8.19 | P3 | LootingBotsInfo 更新周期不匹配 | 文档 | — |

---

## P0 — 严重阻塞

### 8.1 MoveToEngage 决策未集成

**位置**: `Classes/Bot/Decision/EnemyDecisionClass.cs`

**问题**: `shallMoveToEngage()` 方法（第 439 行）已完整实现，`CombatSoloLayer` 已有 `case ECombatDecision.MoveToEngage` 路由，但 `GetDecision()`（第 40-189 行）中从未调用。

**实施步骤**:
1. 打开 `EnemyDecisionClass.GetDecision()`
2. 在 `shallStandAndShoot` 返回 `false` 之后、`shallShootDistantEnemy` 之前，插入:
```csharp
// 当敌人超出有效射程时，推进交火距离
bool shallEngage = shallMoveToEngage(enemy);
#if DEBUG
if (SAINPlugin.DebugMode) DecisionReasons.AppendLine($"2b. Shall MoveToEngage: [{shallEngage}]");
#endif
if (shallEngage)
{
    result = ECombatDecision.MoveToEngage;
    return true;
}
```
3. 编译验证
4. 游戏中测试：Bot 面对超出射程的敌人是否会向前推进

**预期效果**: Bot 在敌人超出有效射程时不再原地发呆，而是主动推进到可交火距离。

---

### 8.12 AggressionMultiplier 全为 1.0

**位置**: `Preset/Personalities/BasePersonality/PersonalityDefaultsClass.cs`

**问题**: 所有 8 种命名个性的 `AggressionMultiplier` 均为 `1.0`，导致通过该乘数缩放的计时器在个性间无差异。

**影响范围**:
- `TimeBeforeSearch = SearchBaseTime / AggressionMultiplier`
- `HoldGroundDelay = HoldGroundBaseTime / AggressionMultiplier`
- `FreezeDuration = Random(10, 120) / AggressionMultiplier`

**实施步骤**:
1. 打开 `PersonalityDefaultsClass.cs`
2. 为每种个性设置差异化值:

| 个性 | 建议 AggressionMultiplier | 修改行 |
|------|:----------------------:|:------:|
| GigaChad | 2.0 | 第 55 行 |
| Wreckless | 1.8 | 第 120 行 |
| Chad | 1.4 | 第 254 行 |
| SnappingTurtle | 1.2 | 第 188 行 |
| Normal | 1.0 | 第 529 行 |
| Rat | 0.6 | 第 322 行 |
| Timmy | 0.4 | 第 396 行 |
| Coward | 0.3 | 第 473 行 |

3. 同时减少 `SearchBaseTime` 的差异量（因为现在 AggressionMultiplier 会自动缩放），避免双重缩放导致行为极端化
4. 编译并测试各性行为差异是否仍保持合理范围

**预期效果**: 搜索时间、HoldGround 时间、Freeze 时间等所有关联计时器统一缩放，个性间攻击性差异更加协调一致。

---

## P1 — 中等优先级

### 8.3 战后恢复仅 10s 窗口

**位置**: `Classes/Bot/Decision/BotDecisionManager.cs` 第 123 行

**问题**: `Time.time - CombatEndTime < 10f` 硬编码窗口，手术（最长 60s）可能被截断。

**实施步骤**:
1. 在 `GlobalSettings` 中添加配置项 `PostCombatRecoveryDuration`（默认 30f）
2. 将第 123 行的硬编码 `10f` 替换为配置值
3. 或者更简单的修复: 在 `CombatEndTime` 块中，如果 `CurrentSelfDecision == ESelfActionType.Surgery` 则不清除 `CombatEndTime`

**预期效果**: Bot 在战斗结束后有足够时间完成手术等长程治疗。

---

### 8.4 Freeze 行为仅限室内

**位置**: `EnemyDecisionClass.shallFreezeAndWait` 第 232 行

**问题**: `!Bot.Memory.Location.IsIndoors` → 直接返回 false，室外无谨慎行为。

**实施步骤**:
1. 在 `PersonalitySearchSettings` 中添加 `OutdoorFreezeBehavior`（默认 false 保持兼容）
2. 若启用，室外 Freeze 使用更短的持续时间（室内 Freeze 的 50%）和更严格的距离条件（< 40m 替代 < 70m）
3. 对 Rat/Timmy/Coward 个性考虑开启此功能

**预期效果**: 谨慎型 Bot 在室外也能有基本的伏击/蹲守行为。

---

### 8.5 脚步声/枪声判定模型不统一

**位置**: `HearingAnalysisClass.DoIDetectFootsteps` vs `CheckIfSoundHeard`

**问题**: 枪声用确定性阈值，脚步声用概率模型，两种判定方式不一致。

**实施步骤**:
1. 探讨是否值得统一。两种模型的差异化可能是**有意设计**——枪声在物理上是确定性的（你能听到或听不到），而脚步声的检测更受注意力影响
2. 若决定统一: 在 `CheckIfSoundHeard` 中对枪声也应用概率衰减（远距离概率性听到而非以 FinalRange 一刀切）
3. 添加 `HEAR_CHANCE_GUNSHOT_FAR` 配置项控制远距离枪声的概率性

**预期效果**: 听觉行为更一致，远距离枪声仍有小概率被捕捉。

---

### 8.6 武器切换概率无 Bot 类型分级

**位置**: `LootingBots/Patches/EnableWeaponSwitchingPatch.cs`（LootingBots 外部代码库）

**问题**: 所有启用拾取的 Bot 统一 80%/40% 武器切换概率，Boss 和 Scav 无区别。

**实施步骤** (需在 LootingBots 侧修改):
1. 在 `EnableWeaponSwitchingPatch.PatchPostfix` 中添加 `___wildSpawnType_0` 的判断逻辑
2. 建议分级:

| Bot 类型 | CHANCE_TO_CHANGE_WEAPON | CHANCE_TO_CHANGE_WEAPON_WITH_HELMET |
|---------|:---------------------:|:--------------------------------:|
| Boss | 40 | 20 |
| Follower | 50 | 25 |
| Raider/Rogue | 60 | 30 |
| PMC | 70 | 35 |
| Scav | 80 | 40 |

3. 确保修改不影响 Bot 的拾取行为（仅影响武器切换概率）

**预期效果**: Boss/精英 AI 更稳定地使用主武器，Scav 保持灵活切换。

---

### 8.11 CreepOnEnemy 未使用

**位置**: `SAINEnum.cs` 第 19 行

**问题**: `ECombatDecision.CreepOnEnemy` 枚举定义存在但无任何决策路径使用。

**实施步骤** (二选一):
- **方案A（清理）**: 从枚举中移除 `CreepOnEnemy`，同步清理所有 switch 中的引用
- **方案B（实现）**: 实现室外谨慎接近行为。类似 Freeze 但在室外可用：Bot 以低速、低姿态向敌人的已知位置移动，频繁停下来检查四周。可提供给 Rat/Timmy/Coward 在室外听到敌人时使用

**推荐**: 方案B — 实现后可解决 8.4（Freeze 仅限室内）的局限性。

**预期效果**: 谨慎型 Bot 在室外有合理的近敌行为。

---

## P2 — 低优先级

### 8.13 SelfAction Holster 槽位遗漏

**位置**: `SelfActionDecisionClass.CheckDoReload` 第 80 行

**问题**: `WEAPON_SWAP_ON_DRY` 仅检查 `EquipmentSlot.SecondPrimaryWeapon`，遗漏 Holster。

**实施步骤**:
```csharp
// 将第 80-97 行中的:
if (weaponManager.info.TryGetValue(EquipmentSlot.SecondPrimaryWeapon, out var secondInfo) && ...)
// 改为循环检查两个槽位:
private static readonly EquipmentSlot[] _swapSlots = { EquipmentSlot.SecondPrimaryWeapon, EquipmentSlot.Holster };
// 然后在 WEAPON_SWAP_ON_DRY 分支中遍历检查
```
参考 `EnemyDecisionClass` 第 19-23 行已有的 `_weaponSlotsToCheck` 实现。

---

### 8.14 关键函数体被注释掉

三处注释掉的代码需要处理:

| 位置 | 内容 | 建议 |
|------|------|------|
| `BotDecisionManager` 第 168-177 | Tagilla 近战决策 | 添加 TODO 注释说明是否计划恢复 |
| `SquadDecisionClass` 第 48 | Regroup 小队集结 | 考虑恢复或彻底删除 |
| `SAINHearingSensorClass` 第 111-118 | CheckCalcGoal | 确认移除目标计算是否有意为之 |

---

### 8.7 压制仅子弹飞过触发

**位置**: `SAINBotSuppressClass.CheckAddSuppression`

**问题**: 可听到的枪声不产生任何压制效果。

**实施步骤**:
1. 在 `SAINHearingSensorClass.ReactToHeardSound` 中添加枪声压制调用:
```csharp
if (sound.IsGunShot)
{
    float distantSuppression = getSuppNum(sound.Enemy) * 0.15f; // 15% 的飞过压制量
    Bot.Suppression.CheckAddSuppression(sound.Enemy, sound.PlayerDistance, distantSuppression);
}
```
2. 添加配置开关 `SUPP_FROM_GUNSHOT_AUDIO` 控制此行为

**预期效果**: 附近密集枪声会积累微量压制，模拟战斗心理压力。

---

### 8.8 LootingOverwatch 检测粗糙

**位置**: `SquadDecisionClass.shallLootingOverwatch` 第 116-135 行

**问题**: 使用 "不在战斗 + 没移动 + < 25m" 启发式推断队友在拾取。

**实施步骤** (推荐方案):
1. 在 LootingBots 的 `External.cs` 中新增 `IsBotLooting(BotOwner bot)` API:
```csharp
public static bool IsBotLooting(BotOwner bot)
{
    if (GetLootingBrain(bot, out LootingBrain lootingBrain))
        return lootingBrain.IsBotLooting;
    return false;
}
```
2. 在 SAIN 侧的 `LootingBotsInterop` 中添加对应方法
3. 修改 `shallLootingOverwatch` 使用精确的 `IsBotLooting` 检查

---

### 8.9 手雷威胁仅时间过期

**位置**: `GrenadeTrackerClass.HasExpired`

**问题**: 手雷爆炸后若未触发 DestroyEvent，威胁持续到超时。

**实施步骤**:
1. 监听手雷的 `DestroyEvent` 或 `OnExplosionEvent`
2. 添加爆炸后立即过期的逻辑（但保留一小段延迟如 0.5s 确保躲避行为完成）

---

### 8.10 狗斗切换可能抽搐

**位置**: `DogFightDecisionClass`

**问题**: 进入/退出阈值（10m/15m）在边缘波动时可能快速切换。

**实施步骤**:
1. 添加退出最小持续时间锁：进入 DogFight 后至少持续 0.5s 才能退出
2. 或者拉大退出阈值差距：进入 10m / 退出 20m

---

## P3 — 信息性 / 文档

以下问题属于文档完善或信息性发现，不涉及可执行代码变更：

| 编号 | 内容 |
|:----:|------|
| 8.15 | Boss/Follower/Goons 使用不同的层优先级（70/69/64/62 vs 20/22/24），在分析特殊敌人行为时需注意 |
| 8.16 | SAINLootingBotsIntegration 的拾取价值撤离联动（`FullOnLoot`）是完整的功能闭环 |
| 8.17 | 头盔武器切换概率 40% 已在 LootingBots 中实现，文档应记录 |
| 8.18 | LootingBots 移除了原版 `Utility peace` 和 `LootPatrol` 层 |
| 8.19 | `UpdateLootingBotsInfo()` 5s 更新 vs `CheckLootingVigilance()` 0.5s 检查，存在 5s 数据延迟 |

---

## 实施建议排序

**第一轮** (立即修复):
1. 8.12 AggressionMultiplier 差异化配置
2. 8.1 MoveToEngage 集成调用

**第二轮** (短期):
3. 8.3 战后恢复窗口延长
4. 8.13 SelfAction Holster 槽位统一
5. 8.6 武器切换概率分级（需 LootingBots 配合）

**第三轮** (可选增强):
6. 8.4 室外谨慎行为
7. 8.11 CreepOnEnemy 实现（可作为 8.4 的方案）
8. 8.7 枪声压制
9. 8.8 LootingOverwatch 精确化
10. 8.9/8.10 边缘情况修复

**第四轮** (清理):
11. 8.14 注释代码决策确认
12. 8.5 听觉模型统一（需设计讨论）

---

> *Vault-Tec 工程评估: 第一轮的两项修复（AggressionMultiplier + MoveToEngage）投入最小、收益最大，建议优先执行。预期这两个修复将显著改善 Bot 的个性差异感知和远距离交战主动性。*
>
> *—— Vault-Tec -- Preparing for the Future!*
