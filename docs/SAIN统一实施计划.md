# SAIN 统一实施计划

> 合并来源: SAIN改进方案-v1 + SAIN新功能规划-理事会综合报告v2
> 版本: Moew-SAIN-For-3114
> 原则: 增量改进现有架构，不替换核心逻辑
> 日期: 2026-05-30

---

## 一、计划概览

本计划将 v1 改进方案（Bug 修复 + 感知公平性改进）和 v2 理事会报告（新功能 + LootingBots 整合）合并为统一的四阶段实施路线。

| 阶段 | 内容 | 项数 | 预估工时 | FPS 影响 |
|------|------|------|---------|---------|
| Phase 0 | Bug 修复 + 性能基础 | 7 | 8-12h | 回收 0.5-1% |
| Phase 1 | 感知公平性改进（v1 设计改进） | 5 | 18-26h | +0.3% |
| Phase 2 | 战斗行为增强（v2 核心功能） | 8 | 22-30h | +0.1% |
| Phase 3 | 战术深度与整合 | 8 | 28-38h | +0.5% |
| Phase 4 | 氛围打磨与性能验证 | 4 | 11-12h | +0.05% |
| **合计** | | **32** | **86-118h** | **< 1.2%** |

---

## 二、Phase 0: Bug 修复与性能基础

> 优先级: 最高。这些是地基——不修则后续功能建立在流沙上。

### BUG-1: IsBotDeafened 比较方向错误
- **文件**: `Classes\Bot\Sense\Hearing\HearingInputClass.cs:36`
- **问题**: `_BotDeafedTime < Time.time` 应为 `>`
- **影响**: 近距离震聋机制完全失效
- **修复**: 一行改动，`<` 改 `>`
- **工时**: 0.1h

### BUG-2: 对话声音传入错误列表 **[关键 — INT-1 前置依赖]**
- **文件**: `Classes\Bot\Sense\Hearing\HearingInputClass.cs:146`
- **问题**: `ProcessSounds(AISoundCachedEvents, ...)` 传入通用列表，应为 `_Conversations`
- **影响**: AI 完全不对语音/脚步/换弹等非枪声做出反应。**直接影响 INT-1**：搜刮时只能被枪声打断，听不到摸近的脚步声和换弹声
- **修复**: 一行改动
- **工时**: 0.1h

### BUG-3: HearingDispersion ratio 缺少括号
- **文件**: `Classes\Bot\Sense\Hearing\HearingDispersionClass.cs:30`
- **问题**: 运算符优先级错误，实际计算丢失了渐变效果
- **影响**: 听觉定位精度梯度丧失，除极近距离外全部 clamp 到最大值
- **修复**: 一行加括号
- **工时**: 0.1h

### BUG-4: DeadBots RemoveAt 索引错乱
- **文件**: `Components\BotManagerComponent.cs:178-183`
- **问题**: 正向顺序移除导致索引错误
- **影响**: 尸体 NavMeshObstacle 管理混乱，NavMesh 污染
- **修复**: 改为从高索引到低索引遍历
- **工时**: 0.5h

### PERF-1: CanGoToPoint NavMeshPath 缓存
- **文件**: `Classes\Bot\Mover\SAINMoverClass.cs` CanGoToPoint 方法
- **问题**: 每次调用 `new NavMeshPath()` 产生 GC 分配
- **修复**: 引入全局 NavMeshPath 池复用
- **工时**: 1.5h

### PERF-2: AddNavObstacles 降频
- **文件**: `Components\BotManagerComponent.cs` AddNavObstacles 方法
- **问题**: 每个尸体每帧 `Physics.OverlapSphere`
- **修复**: 改为协程，每 2-3 秒检测一次
- **工时**: 1h

### PERF-3: SAINNoBushESP 改 IBotClass
- **文件**: `Classes\Bot\SAINNoBushESP.cs`
- **问题**: 作为 MonoBehaviour，每个 Bot 始终参与 Unity Update 循环
- **修复**: 改为普通 `IBotClass`，纳入 `TickWhenCombatClasses` 批次
- **工时**: 2h

### PERF-4: TrimExcess 移除
- **文件**: `Classes\Bot\Sense\Hearing\HearingInputClass.cs`
- **问题**: `SoundDataToReactTo.TrimExcess()` 触发不必要的内存重新分配
- **修复**: 移除 TrimExcess 调用
- **工时**: 0.5h

> Phase 0 合计: 7 项 | 5.5-8h | FPS: 回收 0.5-1%

---

## 三、Phase 1: 感知公平性改进（v1 设计改进）

> 优先级: 高。解决"AI 千里眼/顺风耳"的不公平感知问题。

### F1-1: 转向系统 — 被击中/压制时梯度角度误差
- **真人行为**: 远距离被击中只能判断大致方向，连续被击中逐步定位
- **实现思路**: `SAINSteeringClass.LookToUnderFirePos/LookToLastHitPos` 中引入距离梯度误差（30m 内精确→80m 外最大 60°误差），BotMemory 新增连续压制计数缩减误差
- **涉及文件**: `SAINSteeringClass.cs`, `BotMemory.cs`
- **复杂度**: 中等 | FPS: ~0% | 工时: 6h

### F1-2: 听觉定位 — bulletImpacted 距离分级误差
- **真人行为**: 弹着点是听觉线索，远距离定位极不精确
- **实现思路**: 15m 内线性→15-80m 曲线过渡→80m+ 极大随机分散；新增 ImpactMemory 追踪连续命中缩减分散
- **涉及文件**: `HearingInputClass.cs` bulletImpacted 方法
- **复杂度**: 中等 | FPS: ~0% | 工时: 5h

### F1-3: 视觉系统 — 倍镜分级有效视距
- **真人行为**: 无镜 AI 远距极难发现玩家，有镜 AI 按倍率扩展有效视距
- **实现思路**: 新增 `GetBotScopeMagnification` 读取瞄具倍率→`CalcEffectiveVisionRange` 映射 60-400m 有效视距→超出视距后发现速度大幅衰减（降至 1/20）
- **涉及文件**: `EnemyGainSightClass.cs`（新增两个方法）
- **复杂度**: 中等 | FPS: ~0.1% | 工时: 6h
- **依赖**: 需实现 GetBotScopeMagnification（EFT 武器附件 API 查询）

### F1-4: 声音定位 — getBaseDispersion 倍率系数
- **真人行为**: 高倍镜狙击枪声更难精确定位
- **实现思路**: `HearingDispersionClass.getBaseDispersion` 中，枪声类型 + 高倍镜敌人 → 定位难度 ×1.8
- **涉及文件**: `HearingDispersionClass.cs`
- **复杂度**: 简单 | FPS: ~0% | 工时: 2h
- **依赖**: F1-3 的 GetBotScopeMagnification

### F1-5: DoorOpener — 退后等门物理完成
- **真人行为**: 开门后等门真正打开再通过，而非穿门
- **实现思路**: 门交互前退后 0.5m→估算门动画时间→门打开 80% 后才移除碰撞
- **涉及文件**: `DoorOpener.cs`
- **复杂度**: 简单-中等 | FPS: ~0% | 工时: 3h

> Phase 1 合计: 5 项 | 22h | FPS: +0.1%

---

## 四、Phase 2: 战斗行为增强（v2 P0 核心功能）

> 优先级: 高。覆盖 Bot 最明显的"不像真人"的行为缺口，全部零到极低性能影响。

### F2-1: 空仓自动切副武器
- **真人行为**: 主武器打空→拔手枪继续战斗
- **实现思路**: `SelfActionDecisionClass.CheckDoReload()` 中，弹药耗尽 + 副武器可用→调用 `weaponManager.Selector.TryChangeWeapon(true)`
- **涉及文件**: `SelfActionDecisionClass.cs`, `EnemyDecisionClass.cs`, `SAINEnum.cs`
- **复杂度**: 简单 | FPS: ~0% | 工时: 2h

### F2-2: 掩体内战术换弹
- **真人行为**: 先找掩体再换弹
- **实现思路**: `SelfActionDecisionClass` 换弹检查增加 `Bot.Cover.CoverInUse != null` 判断；未在掩体→先 `SeekCover` 再换弹
- **涉及文件**: `SelfActionDecisionClass.cs`, `BotDecisionManager.cs`
- **复杂度**: 简单-中等 | FPS: ~0% | 工时: 3h

### F2-3: 小队成员阵亡反应
- **真人行为**: 队友倒地→情绪反应→行为变化
- **实现思路**: 订阅 `Squad.OnMemberKilled` 事件→按个性差异化（Coward 撤退/GigaChad 冲锋/Normal 警戒）+ 触发语音
- **涉及文件**: `Squad.cs`, `SAINMemoryClass.cs`, `SAINBotTalkClass.cs`
- **复杂度**: 中等 | FPS: ~0%（事件驱动） | 工时: 4h

### F2-4: 战后自主恢复行为
- **真人行为**: 战斗结束→治疗/换弹/整理背包
- **实现思路**: 监听 `BotInCombat` 状态变化→触发恢复协程：治疗→补充弹药→可选搜刮
- **涉及文件**: `SAINActivationClass.cs`, `SAINBotMedicalClass.cs`, `BotDecisionManager.cs`
- **复杂度**: 中等 | FPS: ~0% | 工时: 4h

### F2-5: 击杀确认与补枪
- **真人行为**: 击倒敌人→观察确认→必要时补枪
- **实现思路**: 敌人死亡时间 < 2s→Bot 保持瞄准 0.5-1.5s 确认；近距可配置概率补射
- **涉及文件**: `BotDecisionManager.cs`, `SAINMemoryClass.cs`
- **复杂度**: 简单 | FPS: ~0% | 工时: 3h

### F2-6: 受伤跛行与移动惩罚
- **真人行为**: 腿部受伤→移动减速/无法冲刺
- **实现思路**: `SAINMoverClass.SetTargetMoveSpeed()` 按腿部健康状态应用速度惩罚（骨折-40%、重伤-25%）；决策层面降低 RushEnemy 倾向
- **涉及文件**: `SAINMoverClass.cs`, `BotDecisionManager.cs`
- **复杂度**: 简单 | FPS: ~0% | 工时: 3h

### INT-1: LootingBots 搜刮时威胁中断 **[依赖 BUG-2]**
- **对接**: 搜刮中检测到脚步声/枪声/敌人→调用 `PreventBotFromLooting(BotOwner, 10f)` 中断
- **前置条件**: BUG-2 必须先修复——修复前 AI 听不到脚步/换弹/语音，威胁检测范围严重缩水
- **涉及文件**: `SAINLootingBotsIntegration.cs`, `LootingBotsInterop.cs`
- **复杂度**: 简单 | FPS: ~0% | 工时: 2h

### INT-2: LootingBots 搜刮前找掩体
- **对接**: 搜刮前调用 `Bot.Cover.FindPointInDirection` 找背靠掩体位置→就位后再允许搜刮
- **涉及文件**: `SAINLootingBotsIntegration.cs`, `SAINCoverClass.cs`
- **复杂度**: 中等 | FPS: ~0% | 工时: 3h

> Phase 2 合计: 8 项 | 24h | FPS: +0.1%

---

## 五、Phase 3: 战术深度与外部整合

> 优先级: 中。显著提升战术多样性和模组协同效果。

### F3-1: 非致命战术投掷物（轻量版：仅闪光弹）
- **真人行为**: 攻房前扔闪
- **实现思路**: 复用手雷投掷逻辑，目标点改为敌人位置；闪光弹条件：敌人室内、距离 < 15m、有 LOS
- **涉及文件**: `BotGrenadeManager.cs`, `EnemyDecisionClass.cs`, `ThrowGrenadeAction.cs`, `SAINEnum.cs`
- **复杂度**: 复杂 | FPS: ~0.15% | 工时: 8h

### F3-2: 小队角色行为分化
- **真人行为**: 突击手冲锋/狙击手架枪/支援手压制
- **实现思路**: 基于武器类型自动分配角色（Assault/Support/Sniper/Medic）→`SquadDecisionClass` 按角色权重调整决策
- **涉及文件**: `SquadPersonalityManager.cs`, `SquadDecisionClass.cs`
- **复杂度**: 中等 | FPS: ~0% | 工时: 6h

### INT-3: LootingBots 小队搜刮分工
- **对接**: 新增 `ESquadDecision.LootingOverwatch` → 一人搜刮、其余警戒
- **涉及文件**: `SquadDecisionClass.cs`, `Squad.cs`
- **复杂度**: 中等 | FPS: ~0% | 工时: 4h

### INT-4: LootingBots 战后主动搜刮触发
- **对接**: 战后恢复完成后，调用 `ForceBotToScanLoot(BotOwner)` 引导 LootingBots 搜刮
- **涉及文件**: `SAINLootingBotsIntegration.cs`, `BotDecisionManager.cs`
- **复杂度**: 简单 | FPS: ~0% | 工时: 2h

### INT-5: LootingBots 物品估价缓存
- **对接**: `GetItemPrice` 增加 Dictionary 缓存（60s 过期），避免重复反射调用
- **涉及文件**: `LootingBotsInterop.cs`
- **复杂度**: 简单 | FPS: 正向优化 | 工时: 1h

### QG-1: QuestingBots 兴趣点移植
- **借鉴**: 战后将交战位置/阵亡位置标记为 `InterestPoint`，和平状态下 Bot 巡逻时经过可触发搜刮/警戒
- **涉及文件**: 新增 `SAINInterestPointClass.cs`, `PeacefulLayer.cs`
- **复杂度**: 中等 | FPS: ~0% | 工时: 4h

### QG-2: 区域状态标记
- **借鉴**: 小队级 `SquadAreaStatus`（安全/可疑/危险），影响搜索行为
- **涉及文件**: `Squad.cs`, `SAINSearchClass.cs`
- **复杂度**: 简单 | FPS: ~0% | 工时: 3h

### F3-3: 冲刺限制逻辑移植
- **借鉴**: QuestingBots 的冲刺条件判断（太近不冲、被压制不冲、受伤不冲）
- **涉及文件**: `SAINMoverClass.cs`
- **复杂度**: 简单 | FPS: ~0% | 工时: 1.5h

> Phase 3 合计: 8 项 | 29.5h | FPS: +0.15%

---

## 六、Phase 4: 氛围打磨与性能验证

> 优先级: 低。锦上添花，全部使用轻量替代方案。
> 注: 搜刮功能由 LootingBots 完整负责，SAIN 通过 INT-4（Phase 3）在合适时机触发 `ForceBotToScanLoot` 即可，不再重复实现。

### F4-1: 战斗疲劳（代理指标版）
- **真人行为**: 长时间交火后更保守
- **实现**: 复用 `Suppression.IsHeavySuppressed` 临时降低 20% 侵略性，零新增状态追踪
- **涉及文件**: `EnemyDecisionClass.cs`
- **复杂度**: 简单 | FPS: ~0% | 工时: 2h

### F4-2: 敌方武器识别（有效射程版）
- **真人行为**: 针对敌方武器类型调整战术
- **实现**: 基于已有 `EffectiveWeaponDistance` 反推（≥100m 为长枪→增加规避机动），不识别具体武器
- **涉及文件**: `EnemyDecisionClass.cs`, `SAINMoverClass.cs`
- **复杂度**: 简单 | FPS: ~0% | 工时: 3h

### F4-3: 异常区域警觉（单体逻辑版）
- **真人行为**: 长时间没听到声音→警觉
- **实现**: 单体逻辑 `Bot.Hearing.TimeSinceLastSound > 30f && Bot.IsInCombat` 触发警觉
- **涉及文件**: `EnemyDecisionClass.cs`
- **复杂度**: 简单 | FPS: ~0% | 工时: 2h

### F4-4: 性能回归测试
- 20 Bot 场景下对比全部功能开/关的 FPS 差异
- 单项功能 FPS > 0.3% 必须回退到轻量替代方案
- **工时**: 4-5h

> Phase 4 合计: 4 项 | 11-12h | FPS: +0.05%

---

## 七、依赖关系图

```
Phase 0 (Bug修复)
  ├─ BUG-2 (对话声音) ────→ INT-1 (搜刮威胁中断)  [关键依赖]
  ├─ BUG-1/3/4 + PERF-1~4
  └─→ Phase 1 (感知公平性)
        ├─→ F1-3 (倍镜视觉) ──→ F1-4 (声音倍率系数)
        ├─→ F1-2 (弹着点分级)
        ├─→ F1-1 (转向梯度)
        └─→ F1-5 (DoorOpener)
              └─→ Phase 2 (战斗行为)
                    ├─→ F2-1~F2-6 (核心功能) ──→ Phase 3 (战术深度)
                    └─→ INT-1~INT-2 (LootingBots 整合)
                                                    └─→ Phase 4 (氛围打磨)
```

**关键依赖**:
- **BUG-2 → INT-1**: 对话声音修复是搜刮威胁中断的前置条件。BUG-2 修复前 AI 听不到脚步/换弹/语音，INT-1 的威胁检测只能覆盖枪声，功能废一半
- F1-3（倍镜视觉）→ F1-4（声音倍率）: 声音倍率依赖视觉倍镜的 `GetBotScopeMagnification`
- Phase 2 Int-1/2 → Phase 3 Int-3/4: LootingBots 基础整合先行，小队分工后置
- INT-4 已覆盖搜刮触发需求，搜刮行为全部由 LootingBots 负责

---

## 八、性能红线

| 红线 | 来源 |
|------|------|
| 禁止每帧 `new NavMeshPath()` | SAIN CanGoToPoint 已有此问题（Phase 0 修复） |
| 禁止每帧 `Physics.OverlapSphere` | SAIN AddNavObstacles 已有此问题（Phase 0 修复） |
| 新增组件优先 `IBotClass` 而非 `MonoBehaviour` | 参考 SAINNoBushESP 教训（Phase 0 修复） |
| 新增 Physics 查询 ≥ 2 秒间隔 | 或改用 NonAlloc + 缓存数组 |
| 单项功能 FPS > 0.3% 回退到轻量替代 | Phase 4 回归测试强制检查 |
| LootingBots 交互仅通过已有反射桥接 | 不可引入对 LootingBots 程序集的硬依赖 |

---

## 九、F6 GUI 配置项规划

以下功能需要暴露到 SAIN F6 预设编辑器中：

| 功能 | 配置项 | 类型 | 默认值 |
|------|--------|------|--------|
| F1-1 | `SteerAccuracyGradientEnabled` | bool | true |
| F1-1 | `SteerMaxAngleError` | float | 60f |
| F1-3 | `ScopeVisionEnabled` | bool | true |
| F1-3 | `ScopeMaxEffectiveRange` | float | 400f |
| F2-1 | `WeaponSwapOnDryEnabled` | bool | true |
| F2-3 | `SquadDeathReactionEnabled` | bool | true |
| F2-5 | `KillConfirmEnabled` | bool | true |
| F2-6 | `InjuryMovePenaltyEnabled` | bool | true |
| F3-1 | `TacticalThrowablesEnabled` | bool | false（默认关闭，实验性） |
| F3-2 | `SquadRoleEnabled` | bool | true |
| INT-1 | `LootingThreatInterruptEnabled` | bool | true |
| INT-2 | `LootingCoverBeforeEnabled` | bool | true |
| INT-3 | `LootingOverwatchEnabled` | bool | true |

---

## 十、总结

| 维度 | 数据 |
|------|------|
| 总项数 | 32 |
| 总工时 | 86-118h |
| FPS 净影响 | < 1.2%（含 Phase 0 回收的 0.5-1%） |
| 涉及文件 | ~38 个（含新建 ~5 个） |
| 新增枚举值 | ~10 个（ECombatDecision / ESquadDecision / ESelfActionType） |
| 新增配置项 | ~12 个（F6 GUI） |
| 外部依赖 | LootingBots v1.6.1（反射桥接，非硬依赖；搜刮行为完全由 LootingBots 负责） |

> [VAULT-TEC 备注] Phase 0 的 Bug 修复是整个计划的基石，必须先行。Phase 1 的感知公平性改进直接解决社区反馈最强烈的"AI 千里眼"问题。Phase 2 的战斗行为增强覆盖了 Bot 最明显的"不像真人"的行为漏洞。Phase 3-4 的战术深度和氛围打磨可按实际开发进度灵活调整优先级。
>
> Vault-Tec -- Preparing for the Future!
