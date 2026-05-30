# Changelog — Moew-SAIN-For-3114

> SPT 3.11.4 兼容版 SAIN 更新日志

---

## v4.2.0 (2026-05-31) — Moew 兼容版增强更新

### Bug 修复

| ID | 描述 | 文件 |
|----|------|------|
| BUG-1 | `IsBotDeafened` 比较方向 `<` → `>`。修复前近距离枪声震聋机制完全失效 | `HearingInputClass.cs` |
| BUG-2 | 对话声音 `ProcessSounds` 传入错误列表 → `AISoundCachedEvents_Conversations`。修复前 AI 不对脚步/换弹/语音反应 | `HearingInputClass.cs` |
| BUG-3 | `ratio` 计算添加括号 `(a-b)/(c-b)`。修复前运算符优先级错误导致定位精度梯度丧失 | `HearingDispersionClass.cs` |
| PERF-3 | `NoBushESP` 基类 `BotBase` → `BotComponentClassBase`，确保 IBotClass 生命周期正确注册 | `SAINNoBushESP.cs` |
| PERF-4 | 移除 `SoundDataToReactTo.TrimExcess()`，消除不必要的内存重新分配 | `HearingInputClass.cs` |

### 感知公平性改进

| ID | 描述 | 文件 |
|----|------|------|
| F1-1 | 被击中/压制时转向精度随距离梯度递减：30m 内精确 / 30-100m 线性增长 / 100m 外最大 60°误差。连续被压制每次缩减 25% 误差 | `SAINSteeringClass.cs`, `SAINMemoryClass.cs` |
| F1-2 | 弹着点定位三阶分级误差：<15m 线性 / 15-80m EaseInQuad 曲线 / 80m+ 极大随机。连续命中 40% 缩减分散 | `HearingInputClass.cs` |
| F1-3 | 倍镜视觉分级：`GetBotScopeMagnification` 读取瞄具倍率 → `CalcEffectiveVisionRange` 映射 60-400m 有效视距 → 超出视距后发现速度衰减至 1/20 | `EnemyGainSightClass.cs` |
| F1-4 | 高倍镜枪声定位难度 ×(1+scopeMag×0.1)，8x 镜→1.8x 定位难度 | `HearingDispersionClass.cs` |
| F1-5 | 开门前退后 0.5m，碰撞延迟到门打开 80% 后才移除，减少穿门现象 | `DoorOpener.cs` |

### 战斗行为增强

| ID | 描述 | 文件 |
|----|------|------|
| F2-1 | 主武器弹尽自动切副武器（包含 `SecondPrimaryWeapon`/`Holster` 槽位检查） | `SelfActionDecisionClass.cs`, `EnemyDecisionClass.cs` |
| F2-2 | 掩体内战术换弹：掩体内更积极保持满弹匣，开阔地有可见敌人时延期换弹优先找掩体 | `SelfActionDecisionClass.cs` |
| F2-3 | 小队成员阵亡反应：存活成员通过 `RecentTeammateDeath` 标记触发语音 (`OnDeath`) 反馈 | `Squad.cs`, `SAINMemoryClass.cs`, `SAINBotTalkClass.cs` |
| F2-4 | 战后自主恢复：战斗结束检测 → 按"治疗 > 换弹"优先级触发自行动作，10s 恢复窗口 | `SAINActivationClass.cs`, `BotDecisionManager.cs` |
| F2-5 | 击杀确认与补枪：敌人死亡后追踪 `LastKillTime`，保持 2s 瞄准确认 | `BotDecisionManager.cs`, `SAINEnemyController.cs` |
| F2-6 | 受伤跛行：腿部骨折→60% 移速禁止冲刺 / 重伤→75% 移速 / 决策层 `IsLegInjured` 禁止 RushEnemy | `SAINMoverClass.cs` |
| INT-1 | LootingBots 搜刮威胁中断：每 0.5s 检测 15m 内敌人/被压制→`PreventBotFromLooting(10s)` | `SAINLootingBotsIntegration.cs` |
| INT-2 | LootingBots 搜刮前安全就位：检查掩体状态，无掩体且安全→蹲下搜刮，不安全→阻止 30s | `SAINLootingBotsIntegration.cs` |

### 战术深度与外部整合

| ID | 描述 | 文件 |
|----|------|------|
| F3-1 | 闪光弹战术投掷：近距离室内敌人(3-15m)10%概率投掷，复用现有手雷逻辑 | `SAINEnum.cs`, `BotGrenadeManager.cs`, `EnemyDecisionClass.cs`, `ThrowGrenadeAction.cs` |
| F3-2 | 小队角色自动分配：狙击步枪→Sniper、冲锋枪/霰弹枪→Assault、机枪→Support | `Squad.cs`, `BotComponent.cs`, `ESquadRole.cs` |
| INT-3 | LootingBots 搜刮警戒轮换：新增 `ESquadDecision.LootingOverwatch`，队友搜刮时 25m 内其他成员警戒 | `SquadDecisionClass.cs`, `SAINEnum.cs` |
| INT-4 | LootingBots 战后搜刮触发：战后恢复完成后自动调用 `TryForceBotToScanLoot` | `SAINLootingBotsIntegration.cs`, `BotDecisionManager.cs` |
| INT-5 | LootingBots 物品估价缓存：`GetItemPriceCached` 60s TTL 缓存，减少反射调用 | `LootingBotsInterop.cs` |
| QG-1 | 兴趣点系统：记录战斗点/阵亡点/枪声点为兴趣点，120s 过期，10m 去重，和平状态探索 | `SAINInterestPointClass.cs` (新建), `BotComponent.cs`, `PeacefulLayer.cs` |
| QG-2 | 区域状态标记：`AreaStatus` 枚举（未知/已搜索/危险），20m 网格分区 | `SAINSearchClass.cs` |
| F3-3 | 冲刺限制：<10m/被压制/腿伤/濒死不冲刺，>100m 保留体力 | `SAINMoverClass.cs` |

### 氛围打磨

| ID | 描述 | 文件 |
|----|------|------|
| F4-1 | 战斗疲劳代理：`IsHeavySuppressed` 作为疲劳信号，强制禁止进攻行为 | `EnemyDecisionClass.cs` |
| F4-2 | 敌方武器识别：Sniper/DMR 近距阻止 Rush，SMG/Shotgun 远距加速 Rush；狙击手>50m 降速 15% 作规避机动 | `EnemyDecisionClass.cs`, `SAINMoverClass.cs` |
| F4-3 | 异常区域警觉：战斗中 30s 未听到敌人声音→强制保守决策 `canTakeAggressiveAction=false` | `EnemyDecisionClass.cs` |

### 统计

- **27 文件**（25 修改 + 2 新建）/ **+819 行** / **-30 行**
- **0 编译错误** / **0 编译警告**
- **DLL 大小**: 1,097,216 bytes

---

## 历史版本

参考 Solarint 原始仓库的 [Releases](https://github.com/Solarint/SAIN/releases) 获取 v4.1.x 及更早版本的更新日志。

> [VAULT-TEC 备注] Vault-Tec 不对任何因 AI 行为变化导致的阵亡、装备丢失或 raid 失败承担责任。Preparing for the Future!
