# Changelog — Moew-SAIN-For-3114

> SPT 3.11.4 兼容版 SAIN 更新日志

---

## v4.3.0 (2026-05-31) — 战斗逻辑全面优化

> 基于对 SAIN 代码库的全面审计（三模型交叉审阅），识别并修复 10 项战斗逻辑问题。
> 审计报告: `docs/SAIN战斗逻辑综合分析报告.md` | 实施计划: `docs/SAIN第8章问题-实施计划.md`

### 决策系统修复

| ID | 描述 | 文件 |
|----|------|------|
| DEC-1 | MoveToEngage 决策激活 — `shallMoveToEngage()` 已有完整实现但从未被调用，Bot 超出有效射程后不会主动推进。已插入到 `GetDecision` 决策链中，被压制的 Bot 不受影响 | `EnemyDecisionClass.cs` |
| DEC-2 | Regroup 小队集结启用 — `shallRegroup()` 逻辑完整但调用被注释。无敌人时 >125m / 有敌人时 >50m 自动归队，敌人在视距内不触发。Bot 散开后不再永远流浪 | `SquadDecisionClass.cs` |

### 个性系统修正

| ID | 描述 | 文件 |
|----|------|------|
| PERS-1 | AggressionMultiplier 差异化激活 — 所有 8 种个性此值均为 1.0（无差异）。现按个性赋予 2.0(GigaChad)~0.3(Coward) 的差异化值，直接影响搜索速度/地面坚守时间/冻结时长 | `PersonalityDefaultsClass.cs` |

### 战斗行为改进

| ID | 描述 | 文件 |
|----|------|------|
| CMB-1 | 战后恢复窗口延长 — 10s→30s，手术中的 Bot 不会被提前中断恢复逻辑 | `BotDecisionManager.cs` |
| CMB-2 | 室外谨慎 Freeze — 谨慎型 Bot(Rat/SnappingTurtle) 室外听到敌人时，先寻找声音方向的掩体，跑过去再蹲守。无掩体则不触发。室内行为不变 | `EnemyDecisionClass.cs` |
| CMB-3 | 狗斗退出延迟 — 0.5s 最小持续时间锁，防止 Bot 在 10m 阈值边缘频繁切换 DogFight/常规战斗 | `DogFightDecisionClass.cs` |

### 感知与武器

| ID | 描述 | 文件 |
|----|------|------|
| PERC-1 | 枪声 10% 概率漏听 — 通过所有距离/修正检查后仍有 10% 概率忽略枪声，模拟注意力不集中或环境噪音遮蔽 | `HearingAnalysisClass.cs` |
| WPN-1 | 副武器 Holster 槽位统一 — SelfAction 武器切换从仅检查 SecondPrimaryWeapon 扩展为同时检查 Holster | `SelfActionDecisionClass.cs` |

### 手雷系统修复

| ID | 描述 | 文件 |
|----|------|------|
| GREN-1 | 手雷爆炸即时过期 — 手雷被销毁/爆炸后立即标记过期，不再仅依赖超时清理 | `GrenadeTrackerClass.cs` |

### 外部整合优化

| ID | 描述 | 文件 |
|----|------|------|
| LB-1 | LootingBots 武器切换概率分级 — Boss=40/20, Follower=50/25, Raider=60/30, PMC=70/35, Scav=80/40 | `EnableWeaponSwitchingPatch.cs` (LootingBots 外部) |
| LB-2 | LootingOverwatch 精确化 — 新增 `IsBotLooting` API，SAIN 优先通过反射调用精确检查队友拾取状态，不可用时回退启发式 | `External.cs` (LootingBots), `LootingBotsInterop.cs` (SAIN), `SquadDecisionClass.cs` |

### 代码清理

| ID | 描述 | 文件 |
|----|------|------|
| CLEAN-1 | 移除 Tagilla 自定义近战决策死代码（BSG 原生 AI 自行处理） | `BotDecisionManager.cs` |

### 统计

- **修改文件**: 9 (SAIN 7 + LootingBots 2)
- **0 编译错误**

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

### 武器隐蔽值扩展

| ID | 描述 | 文件 |
|----|------|------|
| PH0-1 | 装备隐蔽系统重构：删除硬编码方法 + 8 常量 + `initDefaults()`清空 | `AIGearModifierClass.cs`, `GearStealthValuesClass.cs` |
| PH1 | 武器听觉暴露：按武器类型注入移动噪音(0.90~1.35)，背挂加成 1.15 | `PlayerComponent.cs` |
| PH2 | 武器视觉暴露：按武器类型影响远距离可见度(1.00~0.55)，背挂加成 0.90 | `AIGearModifierClass.cs` |
| F6 | 武器暴露 F6 开关 + 装备隐蔽值"添加新条目"按钮 | `HearingSettings.cs`, `VisionDistanceSettings.cs`, `GUITabs.cs` |

### 手雷躲避系统 (Grenade Dodge)

> 设计文档: `docs/SAIN躲避手雷逻辑链改造方案.md` (v2.0) | 实施文档: `docs/SAIN躲避手雷逻辑链改造方案-实施文档.md`
>
> 激活了 SAIN 原有但从未生效的 `ECombatDecision.AvoidGrenade` 死代码，实现基于手雷落点的智能躲避系统，替代 EFT 原版的 `BewareGrenade`。

| ID | 描述 | 文件 |
|----|------|------|
| GD-1 | 新建 `DodgeGrenadeAction` — 核心躲避行为：根据手雷剩余时间+距离+类型执行分层策略（反向冲刺/掩体寻路/原地扑倒）。扇形采样 9 方向×3 距离级 NavMesh 安全点寻路，带掩体检测（碰撞体尺寸过滤 + 导航路径安全检查） | `DodgeGrenadeAction.cs` (新建) |
| GD-2 | 引信时间获取 — Harmony 反射读取 `Throwable._explosionTime`/`_fuseTime`/`_destroyTime` 私有字段，反射失败降级查表（6 种手雷类型映射）。碰炸手雷 (VOG) 直接归零，触发紧急扑倒 | `GrenadeController.cs` |
| GD-3 | 决策链注入 — `getDecision()` 最顶端（`enemy==null` 之前）插入手雷威胁检查，设为最高优先级。无敌人但有飞行中手雷时也能正确触发躲避 | `BotDecisionManager.cs` |
| GD-4 | 手雷类型分化 — 破片雷反向跑+掩体，闪光弹转身不看+后退，烟雾弹移出烟雾区。通过 `CollisionSounds` 枚举区分 | `GrenadeTrackerClass.cs` |
| GD-5 | 垂直楼层感知 — 多层建筑中 Y 轴差 >2m 且水平近时，用导航路径距离替代欧几里得距离判断威胁 | `DodgeGrenadeAction.cs` |
| GD-6 | 紧急反应 — 手雷距离 <8m 且正在接近（`GrenadeDistance` 持续缩小），即使 `CanReact=false` 也触发躲避 | `GrenadeTrackerClass.cs` |
| GD-7 | 三层降级策略 — SAIN 寻路成功 → 智能躲避 / 失败 → 原地扑倒 / Bot 特殊状态 → EFT 兜底。`GrenadeReactionClass.cs:120` 非敌人手雷保留 EFT 原生 | `DodgeGrenadeAction.cs` |
| GD-8 | Squad 协同 — 新增 `OnMemberSpottedGrenade` 事件，`CanReact` 触发时广播手雷落点到全体队友。30m 内队友暂停前进。报告 5s 过期自动清理 | `Squad.cs`, `GroupTalk.cs`, `SquadDecisionClass.cs` |
| GD-9 | 个性差异化 — 3 个新增可配置字段：`GRENADE_REACTION_TIME_MODIFIER`（反应倍率）、`GRENADE_SAFE_DIST_MODIFIER`（安全距离倍率）、`GRENADE_IGNORE_CHANCE`（硬扛概率）。GigaChad 可设 10% 概率不躲 | `PersonalityGeneralSettings.cs` |
| GD-10 | 全局开关 — `GrenadeSettings.ENABLED`。关闭后三个 `BewareGrenade` 调用入口全部分支回退到 EFT 原版 | `GrenadeSettings.cs` (新建), `GlobalSettingsClass.cs` |
| GD-11 | 威胁生命周期管理 — 最大 10s 存活限制（`MAX_THREAT_LIFETIME`），超时自动清除。手雷销毁后 `ManualUpdate` 自动清理 `DangerGrenade` 和决策状态，防止决策粘滞 | `GrenadeReactionClass.cs`, `GrenadeTrackerClass.cs` |
| GD-12 | 多手雷优先级 — `UpdateDangerGrenade()` 按距离×剩余时间评分选最紧急威胁。`EnemyGrenadesList` 中无效 tracker 自动清理 | `GrenadeReactionClass.cs` |
| GD-13 | `GrenadeThreatData` 数据结构 — 在检测层和决策层之间传递手雷威胁的标准化运行时数据 | `GrenadeThreatData.cs` (新建) |

**数据流**: `GrenadeController`(反射引信)→`GrenadeTrackerClass`(检测/个性)→`BotDecisionManager`(最高优先)→`SAINAvoidThreatLayer`(映射)→`DodgeGrenadeAction`(分层执行)→`Squad`(广播协同)

**改动规模**: 13 文件 / ~550 行新增代码 / 0 编译错误

### 统计

- **0 编译错误** / **0 编译警告**
- **DLL 大小**: 1,112,576 bytes

---

## 历史版本

参考 Solarint 原始仓库的 [Releases](https://github.com/Solarint/SAIN/releases) 获取 v4.1.x 及更早版本的更新日志。

> [VAULT-TEC 备注] Vault-Tec 不对任何因 AI 行为变化导致的阵亡、装备丢失或 raid 失败承担责任。Preparing for the Future!
