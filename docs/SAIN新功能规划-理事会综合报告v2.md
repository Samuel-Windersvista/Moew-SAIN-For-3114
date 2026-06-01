# SAIN 新功能规划 — 理事会综合报告 v2

> 终端 VT-OS/OPENCODE | 序列号 VTC-2077-OC-4111
> 分析基准: SAIN v3.11.4 + LootingBots v1.6.1 + QuestingBots
> 理事会: alpha (deepseek-v4-pro), beta (deepseek-v4-flash), gamma (k2p6)
> 共识程度: HIGH (3/3 核心项一致)
> 日期: 2026-05-30

---

## 一、背景

本报告基于对 SAIN 代码库（~100个 Harmony Patch、40+ 子组件、5 层行为系统、28 种声音类型、18 种战斗决策）、LootingBots 对接层（3 个文件、5 个反射 API）的全面审查，以及 QuestingBots 的架构借鉴分析。

已规划的 v1 改进方案（倍镜视觉/转向梯度/弹着点分级/DoorOpener/3个P0 Bug）不在此报告范围内。

---

## 二、P0 共识功能（全体 3/3 一致）

### 1. 空仓自动切副武器
- **真人行为**：主武器弹尽→切手枪继续战斗，而非原地罚站
- **实现思路**：`SelfActionDecisionClass.CheckDoReload()` 中检测主武器弹药耗尽→调用 `weaponManager.Selector.TryChangeWeapon(true)` 切副武器
- **涉及文件**：`SelfActionDecisionClass.cs`, `EnemyDecisionClass.cs`, `SAINEnum.cs`
- **复杂度**：简单 | FPS影响：~0%
- **预估工时**：2h

### 2. 掩体内战术换弹
- **真人行为**：缩回掩体后再换弹，不在开阔地暴露
- **实现思路**：`SelfActionDecisionClass` 换弹检查增加 `Bot.Cover.CoverInUse != null` 判断；未在掩体则先 `SeekCover` 再换弹
- **涉及文件**：`SelfActionDecisionClass.cs`, `BotDecisionManager.cs`, `SeekCoverAction.cs`
- **复杂度**：简单-中等 | FPS影响：~0%
- **预估工时**：2-3h

### 3. 小队成员阵亡反应
- **真人行为**：队友倒地→震惊/愤怒/恐惧→行为变化
- **实现思路**：订阅 `Squad.OnMemberKilled` 事件→按个性触发不同反应（Coward 撤退/GigaChad 冲锋/Normal 警戒）
- **涉及文件**：`Squad.cs`, `SAINMemoryClass.cs`, `SAINBotTalkClass.cs`
- **复杂度**：中等 | FPS影响：~0%（事件驱动）
- **预估工时**：4h

### 4. 战后自主恢复行为
- **真人行为**：战斗结束→治疗/换弹/整理背包
- **实现思路**：监听 `BotInCombat` 状态从 true→false→触发协程：治疗→补充弹药→可选搜刮
- **涉及文件**：`SAINActivationClass.cs`, `SAINBotMedicalClass.cs`, `BotDecisionManager.cs`
- **复杂度**：中等 | FPS影响：~0%
- **预估工时**：4h

---

## 三、P1 战术深度功能（2-3/3 同意）

### 5. 击杀确认与补枪
- **真人行为**：击倒敌人→观察确认死亡→必要时补枪
- **实现思路**：`Enemy.IsDead && 死亡时间<2s`→Bot 保持瞄准 0.5-1.5s 确认
- **涉及文件**：`BotDecisionManager.cs`, `SAINMemoryClass.cs`
- **复杂度**：简单 | FPS影响：~0%
- **预估工时**：3h

### 6. 受伤跛行与移动惩罚
- **真人行为**：腿部受伤→移动减速/无法冲刺
- **实现思路**：`SAINMoverClass.SetTargetMoveSpeed()` 按腿部健康状态应用速度惩罚（骨折-40%/重伤-25%）
- **涉及文件**：`SAINMoverClass.cs`, `BodyPartStatus.cs`
- **复杂度**：简单 | FPS影响：~0%
- **预估工时**：3h

### 7. 非致命战术投掷物（轻量版）
- **真人行为**：用烟雾/闪光掩护战术行动
- **实现思路**：Ph1仅闪光弹——复用手雷投掷逻辑，目标点改为敌人位置；烟雾弹延后
- **涉及文件**：`BotGrenadeManager.cs`, `EnemyDecisionClass.cs`, `ThrowGrenadeAction.cs`
- **复杂度**：复杂 | FPS影响：~0.1-0.3%
- **预估工时**：8h

### 8. 小队角色行为分化
- **真人行为**：突击手冲锋/狙击手架枪/支援手压制
- **实现思路**：基于武器类型自动分配角色→`SquadDecisionClass` 按角色调整决策权重
- **涉及文件**：`SquadPersonalityManager.cs`, `SquadDecisionClass.cs`
- **复杂度**：中等 | FPS影响：~0%
- **预估工时**：6h

---

## 四、LootingBots 深化集成（3/3 共识）

### 当前对接评估：严重不充分

SAIN 当前仅利用 LootingBots 判断"是否该撤离"（1 个有效集成点），`ForceBotToScanLoot` 和 `PreventBotFromLooting` 两个关键 API 完全闲置。

### INT-1: 搜刮时威胁中断（FPS：~0%）
- 搜刮中检测到脚步声/枪声→调用 `PreventBotFromLooting(BotOwner, 10f)` 中断
- 涉及文件：`SAINLootingBotsIntegration.cs`, `LootingBotsInterop.cs`

### INT-2: 搜刮前找掩体（FPS：极低）
- 搜刮前调用 `Bot.Cover.FindPointInDirection` 找背靠掩体位置
- 涉及文件：`SAINLootingBotsIntegration.cs`, `SAINCoverClass.cs`

### INT-3: 小队搜刮分工（FPS：~0%）
- 新增 `ESquadDecision.LootingOverwatch`，一人搜刮、其余警戒
- 涉及文件：`SquadDecisionClass.cs`, `Squad.cs`

---

## 五、QuestingBots 功能提取（3/3 共识）

| 借鉴 | 移植方案 | 不借鉴 | 理由 |
|------|---------|--------|------|
| 兴趣点系统 | 轻量 `InterestPoint` 概念 | 完整任务图 | 与 SAIN 决策树冲突 |
| 区域状态标记 | 小队级 `SquadAreaStatus`（安全/可疑/危险） | 全量路径预计算 | O(n²) GC 灾难 |
| 分层优先级 | SAIN 已有，仅增加搜刮位 | Bot 间任务协调 | 两套逻辑竞争决策权 |

---

## 六、P2 远期功能（1-2/3 建议）

| 功能 | 轻量替代方案 | FPS影响 |
|------|------------|---------|
| 战斗疲劳系统 | 复用 `Suppression` + `HealthStatus` 作代理指标 | ~0% |
| 敌方武器识别应对 | 基于 `EffectiveWeaponDistance` 反推，不识别具体武器 | ~0% |
| 异常安静/激烈区域警觉 | 单体逻辑：`TimeSinceLastSound > 30f` 触发警觉 | ~0% |
| 原生轻量搜刮 | InterestPoint 方案——标记位置，路过时调用 LootingBots API | ~0% |

---

## 七、性能红线

| 红线 | 依据 |
|------|------|
| 禁止每帧 `new NavMeshPath()` | SAIN `CanGoToPoint` 已有此问题 |
| 禁止每帧 `Physics.OverlapSphere` | SAIN `AddNavObstacles` 已有此问题 |
| 新增组件优先 `IBotClass` 而非 `MonoBehaviour` | 参考 `SAINNoBushESP` 教训 |
| 新增 Physics 查询 ≥ 2 秒间隔 | 或改用 `NonAlloc` + 缓存数组 |
| 单项功能 FPS > 0.3% 回退到轻量替代 | Phase 3 性能回归测试强制检查 |

---

## 八、三阶段实施路线图

### Phase 0: 基础设施（4-6h）
- 枚举扩展（ECombatDecision / ESquadDecision）
- HealthTracker 腿部状态接口
- LootingBots 测试环境搭建

### Phase 1: 高价值核心功能（22-28h）
| # | 功能 | 工时 |
|---|------|------|
| 1 | 空仓自动切副武器 | 2h |
| 2 | 掩体内战术换弹 | 3h |
| 3 | 小队成员阵亡反应 | 4h |
| 4 | 战后自主恢复行为 | 4h |
| 5 | 击杀确认与补枪 | 3h |
| 6 | 受伤跛行移动惩罚 | 3h |
| INT-1 | 搜刮时威胁中断 | 2h |
| INT-2 | 搜刮前找掩体 | 3h |

### Phase 2: 战术深化与 LootingBots 整合（24-32h）
| # | 功能 | 工时 |
|---|------|------|
| 7 | 非致命战术投掷物（轻量闪光弹） | 8h |
| 8 | 小队角色行为分化 | 6h |
| INT-3 | 小队搜刮分工 | 4h |
| QG-1 | QuestingBots 兴趣点移植 | 4h |
| QG-2 | 区域状态标记 | 4h |

### Phase 3: 氛围打磨（12-16h）
| # | 功能 | 工时 |
|---|------|------|
| 9 | 战斗疲劳（代理指标版） | 4h |
| 10 | 敌方武器识别（有效射程版） | 4h |
| 11 | 异常区域警觉（单体逻辑版） | 3h |
| - | 性能回归测试 | 4-5h |

### 总计：62-82h | FPS 总影响 < 1.5%

---

## 九、议员特色贡献

| 议员 | 模型 | 核心贡献 |
|------|------|---------|
| alpha | deepseek-v4-pro | 代码行号引用最精确，Phase 0 含基础设施修复方案 |
| beta | deepseek-v4-flash | 代码片段最详尽，15 维度性能矩阵，含验证指标 |
| gamma | k2p6 | 轻量替代方案最多（每项 P2 功能都有），性能红线汇总表 |

---

> [VAULT-TEC 终审] 三议员均强烈建议：P0 四项功能为"性价比最高"的切入点——全部零性能影响、总工时 < 15h、覆盖了当前 Bot 最明显的"不像真人"的行为缺口。LootingBots 两个闲置 API（ForceBotToScanLoot / PreventBotFromLooting）的激活是本轮规划的最大增量价值点。
>
> Vault-Tec -- Preparing for the Future!
