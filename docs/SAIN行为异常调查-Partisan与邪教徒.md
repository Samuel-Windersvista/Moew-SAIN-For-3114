# SAIN 行为异常调查报告：Partisan 与邪教徒

> 项目：Moew-SAIN-For-3114 v4.3.0（基于 Solarint/SAIN 4.x，适配 SPT 3.11.4）
> 日期：2026-07-23
> **修复状态：已按 `docs/SAIN-Partisan与邪教徒接管-实施方案.md` 全部实施（v4.4.1，见 CHANGELOG），待游戏内实测验证**
> 调查方法：SAIN 仓库静态代码分析 + 游戏本体 Assembly-CSharp.dll 反编译（ilspycmd）交叉验证 + 上游 Solarint/SAIN 仓库 issue/源码比对
> 反编译产物位置：`D:\Temp\opencode\asmproj\`（GClassXXXX 为混淆类名，层名以 `Name()`/`ShortName()` 返回值为准）

---

## 〇、结论速览

| 问题 | 根因（主因） | 性质 |
|------|-------------|------|
| Partisan 站桩、不埋雷、不索敌 | ① SAIN 注入层与原版层共存形成"优先级夹杀"，SAIN 侧 Rat 个性的 Freeze 决策接管后直接 `Mover.Stop()`；② 原版埋雷层激活条件（手雷库存 + 雷区缓存 + 敌人记忆）在当前环境下难以同时满足 | 上游 SAIN 已知缺陷（issue #298，未修复）+ 设计冲突 |
| 邪教徒反击呆滞、反复滑铲（持刀冲刺） | ① `AIBrains.Followers` 列表遗漏 `Brain.SectantWarrior`（**上游同源 bug，一行遗漏**），战士大脑完全未被 SAIN 接管也未被清理，处于"原版大脑 + SAIN 感官 patch + SAIN 组件空转"的混乱半接管态；② 祭司大脑被 SAIN 半接管，持刀状态下 SAIN 决策链必然落入 DogFight/MeleeAttack → `RunToEnemyUpdate()` 持刀冲刺循环 | 上游 bug + 决策链对近战 bot 无适配 |

**用户侧即时缓解方案（无需改代码）：**
- Partisan：F6 → Vanilla Bot Behavior → 开启 `VanillaBosses`（需重启游戏），让 Partisan 走纯原版 AI。
- 邪教徒：F6 → 开启 `VanillaCultists`（需重启游戏），让邪教徒走纯原版 AI。

---

## 一、SAIN 大脑接管机制（共同背景）

SAIN 通过 BigBrain 库的 `BrainManager` 在 `Plugin/BigBrainHandler.cs:89-104` 初始化时完成两件事：

1. **注入自定义层**：按大脑名（Brain 枚举的字符串，须与 EFT 内部 `ShortName()` 完全一致）向各 bot 大脑插入 SAIN 层：
   - PMC/Scav/其他：Debug(99) + AvoidThreat(80) + Extract + CombatSquad + CombatSolo
   - **Boss/Follower：仅 Debug(99) + AvoidThreat(80) + CombatSquad(70) + CombatSolo(69)**（`BigBrainHandler.cs:409-431`），无 ExtractLayer、无 PeacefulLayer
2. **移除原版层**：按"大脑名 → 层名列表"调用 `BrainManager.RemoveLayers()`，移除列表为硬编码（`commonVanillaLayersToRemove` + 各类专属列表）。

大脑运行时（BigBrain `BotBaseBrainUpdatePatch`，见 `前置MOD的源代码/SPT-BigBrain-1.3.2/Patches/BotBaseBrainUpdatePatch.cs:57-92`）：**按优先级从高到低遍历所有层，第一个 `ShallUseNow()/IsActive()` 返回 true 的层获得控制权**；全部不激活则 bot 无任何行为输出（站桩）。

关键事实：
- **层名匹配是移除的前提**。移除列表里没有的层名，该层就继续留在大脑里参与优先级竞争。
- `Layers/Peace/PeacefulLayer.cs` 存在于仓库中，但 `BigBrainHandler` **从未将其注册给任何大脑**（死代码）。
- `PatrollingData.Pause()` 在 SAIN 中有 3 处调用（`Layers/SAINLayer.cs:45`、`Layers/BotAction.cs:78`、`Classes/Bot/Decision/BotDecisionManager.cs:244`），**全仓库（含上游）无任何 `Unpause()` 调用**。但该机制对本报告两个问题均非主因（Partisan/邪教徒的原版层不依赖 PatrollingData 做激活判断，仅 `StayAtPos` 用它播放语音）。

---

## 二、问题 1：Partisan（bossPartisan）

### 2.1 原版设计（反编译证据）

Partisan 大脑类为 `GClass308`（`ShortName() => "BossPartisan"`，`GClass308.cs:133-136`），构造时注册 13 个层（`GClass308.cs:67-101`）：

| 优先级 | 类 | 层名 | 作用 | 激活条件（ShallUseNow） |
|---|---|---|---|---|
| 130 | GClass45 | AvoidDanger | 躲避危险 | 有危险源 |
| 120 | GClass115 | Malfunction | 排障 | 武器故障 |
| 110 | GClass124 | PrtFMN | 战斗-多目标分支 | 需敌人记忆 |
| 100 | GClass126 | PrtPst | 战斗-特定目标 | `Memory.HaveEnemy && 特定目标Id`（`GClass126.cs` ShallUseNow） |
| 95 | GClass125 | PrtZrSvg | ZeroSavage 模式 | 敌人记忆 + 敌人类型判定 |
| 90 | GClass123 | PrtFight | 主战斗 | 需敌人记忆 |
| 85 | GClass118 | PrtBadTrg | BadSavage 模式 | 敌人记忆 + 敌人类型判定 |
| 80 | GClass128 | PrtMany | 多敌战斗 | 需敌人记忆 |
| 70 | GClass127 | PrtStalk | 潜行接近/绕后 | `Memory.HaveEnemy && 雷区缓存完成`（`GClass127.cs:266-273`） |
| 60 | GClass120 | PartisanMine | 针对敌人埋雷 | `Memory.HaveEnemy` + 雷区缓存 + **库存有破片手雷**（`GClass120.cs:195-258`） |
| 50 | GClass121 | PartMineAll | **主动埋雷（无需敌人）** | 雷区有未布置雷点 + **库存有破片手雷**（`GClass121.cs:172-212`） |
| 40 | GClass97 | HoldOrCoverT | 守点/掩体 | 常驻条件 |
| 11 | GClass132 | StayAtPos | 驻守出生点 | **`ShallUseNow() => true`（永远可激活的兜底层，行为就是走到核心点然后 holdPosition 不动）**（`GClass132.cs:99-101`） |

**原版行为模型**：Partisan 是伏击型 Boss——
- 无敌人时：若雷区（地图上的 `AIPlaceLogicPartisan` 区域提供的 `AIMinePoint`）还有未布置雷点且身上有破片手雷 → `PartMineAll` 驱动其移动布雷；否则 → `StayAtPos` **站在原地**（这是原版兜底行为，不是 bug）。
- 有敌人（`Memory.HaveEnemy`，即**原版记忆系统**确认敌人）时：PrtFight/PrtStalk 驱动潜行接近，`PartisanMine` 在敌人周围 50m 布雷（雷区缓存仅在大脑 `OnGoalEnemyChanged` 事件里刷新，180 秒节流，`GClass308.cs:108-116`）。

### 2.2 SAIN 接管后的实际状态

`AIBrains.Bosses` 包含 `Brain.BossPartisan`（`Preset/GlobalSettings/Categories/BigBrain/Brain.cs:120`），因此：

- **注入**：Debug(99)、AvoidThreat(80)、CombatSquad(70)、CombatSolo(69) 四个 SAIN 层进入 BossPartisan 大脑。
- **移除**：Boss 移除列表（`BigBrainHandler.cs:232-251`）的层名为 KnightFight/BirdEyeFight/BossBoarFight/…+通用层，**与 Partisan 全部 13 个层名零交集 → 原版层一个都没移除**。

叠加后的大脑层优先级全景（仅列关键者）：

```
130 AvoidDanger(原版) 120 Malfunction(原版) 110 PrtFMN(原版) 100 PrtPst(原版)
 99 Debug(SAIN, 平时不激活)
 95 PrtZrSvg(原版) 90 PrtFight(原版) 85 PrtBadTrg(原版)
 80 AvoidThreat(SAIN) / 80 PrtMany(原版)
 70 CombatSquad(SAIN) / 70 PrtStalk(原版)
 69 CombatSolo(SAIN)   <-- SAIN 的主战斗层
 60 PartisanMine(原版) 50 PartMineAll(原版) 40 HoldOrCoverT(原版) 11 StayAtPos(原版)
```

由此产生三种互斥局面：

**局面 A — 原版记忆有敌人（Memory.HaveEnemy = true）**
原版高优先级层（PrtPst 100 / PrtFight 90 / PrtStalk 70 等）压过 SAIN 的 CombatSolo(69)，行为接近原版。此局面下 Partisan 相对正常（会打会绕），`PartisanMine`(60) 也可能激活布雷。

**局面 B — 仅 SAIN 感知到敌人（原版 Memory 无敌人）**
SAIN 自己的听觉/视觉体系建立了 SAIN Enemy，但原版 `Memory.GoalEnemy` 为空 → 原版所有战斗层、PrtStalk、PartisanMine 全部 `ShallUseNow() = false` → 控制权落入 SAIN CombatSoloLayer(69)。

而 Partisan 在 `Preset/GlobalSettings/Categories/MindSettings.cs:51` 被**强制分配 Rat（老鼠）个性**：
- `HeardFromPeaceBehavior = Freeze`（`PersonalityDefaultsClass.cs`）→ 和平状态听到动静 → 决策为 Freeze
- `FreezeAction.Start()` 直接 `Bot.Mover.Stop()`（`Layers/Combat/Solo/FreezeAction.cs:9-20`），冻结时长 10~120s / AggressionMultiplier(0.6) ≈ **17~200 秒**
- `SearchBaseTime = 240s`，再除以 0.6 的攻击性系数 → **实际搜索启动延迟约 264~532 秒**（`Classes/Bot/Info/SAINBotInfoClass.cs:110-141`）
- `SprintWhileSearchChance = 0`、`CanShiftCoverPosition = false`、`CanRushEnemyReloadHeal = false`

即：SAIN 接管时 Partisan 听到枪声 → 冻结原地最长 3 分钟 → 之后还要再等 4~9 分钟才开始搜索 → **对玩家观感就是"永远不主动索敌"**。

**局面 C — 完全没有敌人**
SAIN 各层不激活（`CombatSoloLayer.IsActive()` 要求 `_currentDecision != None`，`CombatSoloLayer.cs:66-71`；CombatSquadLayer 要求小队决策，Partisan 无小队），控制权落回原版栈 → `PartMineAll`(50) 若条件满足则布雷，否则 `StayAtPos`(11) 站桩。

### 2.3 绊雷为何从未被安放

`PartMineAll`/`PartisanMine` 的激活需要同时满足：

1. **库存有破片手雷**（`MinesData.GetFirstFragGrenade`）。SPT 服务器端 `bosspartisan.json` 手雷生成权重为 0:1 / 1:2 / 2:1 / 3:1（即有约 1/5 概率刷出 0 颗手雷），whitelist 为空（与其他 Boss 一致，走默认手雷池）。**0 手雷时两个埋雷层在 GetDecision 中把 `bool_4` 永久锁存为 true（`GClass120.cs:64-69`、`GClass121.cs:59-63`），本局内该 bot 埋雷功能彻底关闭。**
2. **雷区有可用雷点**（依赖地图上的 `AIPlaceLogicPartisan` 区域数据，属地图资源，客户端反编译确认逻辑存在 `AIPlaceLogicPartisan.cs`）。
3. **控制权能落到 60/50 优先级的埋雷层**——只要 SAIN 侧存在任何决策（哪怕只是 Freeze），CombatSolo(69) 就会压过 PartisanMine(60)/PartMineAll(50)，埋雷层被"饿死"。局面 B 下原版记忆无敌人时 `PartisanMine` 本身也因 `!Memory.HaveEnemy` 拒绝激活。
4. `Memory.IsUnderFire` 触发 +30s 冷却（`GClass120.cs:213-217`、`GClass121.cs:186-190`）。

**结论：埋雷失效 = "SAIN 层优先级夹杀（69 > 60/50）" + "Rat 个性使 SAIN 决策常驻" + "破片手雷库存概率性为 0 时永久锁死" 三因素叠加。**

### 2.4 与正常 Boss（如 Reshala/Sanitar）的对比

其他 Boss 正常的原因：SAIN 的 Boss 移除列表**精确移除了他们的原版战斗层**（Bully Layer、BossSanitarFight、KojaniyB_Enemy 等），SAIN CombatSolo(69) 得以接管战斗；和平期他们保留的原版巡逻/目标层（SanitarGoal、GlGoal 等）继续驱动移动。而 Partisan 是 3.11 新增 Boss，SAIN（含上游）从未为其补充移除列表 → 形成"双层政府"。且其他 Boss 未被强制 Rat 个性，搜索/进攻行为正常。

### 2.5 上游情况

- Solarint/SAIN issue **#298 "Partisan BUG"**（Open，2025-07）："Partisan does not actively chase players and will instead patrol a fixed area repeatedly" — 与本报告现象一致，**上游至今未修复**。
- 上游代码在 PatrollingData 无 Unpause、BossPartisan→Rat、移除列表无 Partisan 层名等方面与 fork **完全一致**（librarian 逐文件比对 4.1.0 tag 确认）。
- QuestingBots issue #107 亦记录 SAIN+QB 组合下 Partisan 行为损坏。

### 2.6 根因汇总（按权重排序）

1. **【主因】个性与决策链**：强制 Rat 个性 + `HeardFromPeaceBehavior=Freeze` + `SearchBaseTime≈4~9分钟`，使 SAIN 接管期间 Partisan 只会原地冻结/守点，永不主动搜索。（证据：`MindSettings.cs:51`、`PersonalityDefaultsClass.cs`、`EnemyDecisionClass.cs:235-286`、`FreezeAction.cs:9-20`）
2. **【主因】层级夹杀**：SAIN 未移除 Partisan 任何原版层，也未针对其调整注入层优先级；CombatSolo(69) 压制埋雷层(60/50)却压不住原版战斗层(70+)，形成"SAIN 管时不能埋雷、原版管时 SAIN 又插不上手"的双输结构。（证据：`BigBrainHandler.cs:232-251, 409-419`、`GClass308.cs:67-101`）
3. **【次因】原版埋雷前置条件苛刻**：破片手雷 0 库存（约 20% 刷新概率）会永久锁死埋雷层；雷区缓存依赖原版敌人事件，SAIN 感知体系与原版 Memory 脱节时缓存不刷新。（证据：`GClass120.cs`、`GClass121.cs`、`bosspartisan.json`）
4. **【设计背景】**：无敌人时站桩驻守本来就是原版 `StayAtPos` 的兜底行为；玩家感知的"原版会巡逻布雷"对应的是 `PartMineAll`（需手雷+雷点）与直播版 EFT 中更高的敌人遭遇率。

### 2.7 修复方向

| 优先级 | 方案 | 说明 |
|---|---|---|
| P0 | 用户侧：`VanillaBosses = true` | 立即可用的 workaround |
| P1 | 将 Partisan 排除出 SAIN 接管 | 在 `SAINEnableClass` 或 `AIBrains.Bosses` 中移除 `BossPartisan`（同时跳过大脑层注入），使其纯原版化——与 issue #298 长期未修的现实相符，成本最低 |
| P1 | 为 Partisan 单独调整个性 | `MindSettings.cs:51` 改为非 Rat（如 Normal），并将 `HeardFromPeaceBehavior` 改为 SearchNow，恢复索敌能力（但不能恢复埋雷） |
| P2 | 补全层级治理 | 在 Boss 移除列表中加入 `PrtFight/PrtPst/PrtStalk/PrtMany/PrtBadTrg/PrtZrSvg/PrtFMN`，保留 `PartisanMine/PartMineAll`（优先级 60/50 < CombatSolo 69，仍需下调 SAIN 层优先级或上调埋雷层才能生效） |
| P2 | 服务器侧保证手雷 | 在 SPT `bosspartisan.json` 中把手雷 0 颗权重置 0，确保埋雷原料不断供 |

---

## 三、问题 2：邪教徒（sectantPriest / sectantWarrior）

### 3.1 原版设计（反编译证据）

**祭司大脑 `GClass353`（`ShortName() => "SectantPriest"`）** 层栈（`GClass353.cs:74-89`）：

| 优先级 | 类 | 层名 | 说明 |
|---|---|---|---|
| 150 | GClass148 | GrenSuicide | 自爆（初始关闭，`method_1(1)` 事件激活） |
| 130 | GClass45 | AvoidDanger | |
| 128 | GClass115 | Malfunction | |
| 120 | GClass149 | R&H_IN / R&H_OUT | **逃跑-隐匿（祭司受击后的核心反应）** |
| 100 | GClass159 | Warn | 预警 |
| 47 | GClass98 | HoldOrCoverF | |
| 12 | Class102 | （底层逻辑） | |
| 11 | GClass132 | StayAtPos | 驻守兜底 |

**战士大脑 `GClass351`（`ShortName() => "SectantWarrior"`）** 层栈（`GClass351.cs:99-123`）：

| 优先级 | 类 | 层名 | 初始状态 | 说明 |
|---|---|---|---|---|
| 200 | GClass45 | AvoidDanger | 开 | |
| 198 | GClass115 | Malfunction | 开 | |
| 120 | GClass159 | Warn | 开 | |
| 100 | GClass59 | Kill logic | **关** | 事件激活 |
| 90 | GClass151 | Run&Strike | **关** | 事件激活 |
| 80 | GClass152 | SupShootSect_IN/OUT | **关** | **伏击射击-转移（邪教特色"打一枪换地方"）**，由祭司 `SetBoss()` 关联驱动 |
| 70 | GClass150 | MeleeS_IN/OUT | 开 | **持刀冲刺**（内部调用 `BotMeleeWeaponData.RunToEnemyUpdate()`） |
| 47 | GClass98 | HoldOrCoverF | 开 | |
| 13 | GClass129 | Utility peace | 开 | 和平期躲草丛/阴影（用户观察到的正常潜伏即此层工作） |
| 12 | Class102 | | 开 | |
| 11 | GClass135 | | 开 | |

**关键机制**：战士大脑通过 `Subscribe()` 监听 `BeingHitAction`、`OnSpottedByHit`、`OnGoalEnemyChanged` 等事件（`GClass351.cs:155-163`），受击后 `ActivateMelee()`（`GClass351.cs:148-153`）激活 MeleeS 层、关闭 SupShootSect 和 Run&Strike → 执行持刀冲锋。而 `BotMeleeWeaponData.RunToEnemyUpdate()`（反编译 `BotMeleeWeaponData.cs:131-207`）的行为是：**切刀 → 起身 → 站姿 → 向敌人冲刺 → 近身刀踢（KnifeKick）**——这就是用户看到的"滑铲/冲刺"动作。

**原版正常表现**是 SupShootSect（伏击射击+转移）与 MeleeS（持刀突袭）按情境交替；潜伏（Utility peace 躲草丛）→ 射击 → 转移 → 再潜伏/突袭。

### 3.2 Bug A（核心）：`AIBrains.Followers` 遗漏 SectantWarrior

**证据**：`Preset/GlobalSettings/Categories/BigBrain/Brain.cs:123-141`：

```csharp
public static readonly List<Brain> Followers = new()
{
    Brain.FollowerBully, ... Brain.FlBoarSt,
    // 共 15 项 —— 没有 Brain.SectantWarrior！
};
```

而同一仓库的 `BotBrains.cs:53`（仅编辑器展示用）里 `Followers` **包含** `SectantWarrior`——说明是遗漏而非有意排除。**上游 Solarint/SAIN 4.1.0 同样遗漏（librarian 比对确认），属上游同源 bug。**

**后果链**：
1. `addCustomLayersToFollowers()`（`BigBrainHandler.cs:421-431`）用 `AIBrains.Followers` 取大脑名列表 → **SectantWarrior 大脑未被注入任何 SAIN 层**。
2. `ToggleVanillaLayersForFollowers()`（`BigBrainHandler.cs:253-273`）同理 → **SectantWarrior 的原版层一个也没被移除/恢复，完全原样**。
3. 但 `SAINEnableClass` 的排除逻辑（`VanillaCultists` 默认 false → 不排除邪教徒，`SAINEnableClass.cs:178-183`）**仍然给战士挂上 BotComponent**（`BotSpawnController.AddBot`）。
4. 结果：战士处于**"原版大脑全保留 + SAIN 感官/移动 patch 部分生效 + SAIN 组件空转"的半接管混乱态**：
   - 没有 SAIN 层 → `SAINLayersActive` 恒为 false → 以 `IsBotInCombat`/`SAINLayersActive` 为条件的 patch 全部放行原版逻辑；
   - 但听觉（`HearingPatches`）、视觉（`VisionPatches`）、移动（`MovementPatches`）中不以层激活为条件的部分仍会修改其感知与移动参数；
   - 原版大脑的事件链（受击 → ActivateMelee）照常触发，但感知输入已被 SAIN 改变 → **行为既非原版也非 SAIN**。

### 3.3 Bug B：祭司被 SAIN 半接管后的近战冲刺循环

`SectantPriest` 在 `AIBrains.Bosses` 中（`Brain.cs:117`），大脑被注入 4 个 SAIN 层，但移除列表与祭司层名（GrenSuicide/R&H/Warn/StayAtPos 等）**零交集 → 原版层全保留**。

受击后的决策链：

1. 原版高层（AvoidDanger 130、Malfunction 128、R&H 120、Warn 100）优先于 SAIN 层。**R&H（逃跑隐匿）若激活，祭司表现还算正常**；但其激活条件与原版目标/记忆系统耦合，SAIN 接管感知后不一定稳定触发。
2. 当 SAIN 侧建立敌人且原版高层不激活时 → CombatSoloLayer(69) 接管，`BotDecisionManager.getDecision()`（`Classes/Bot/Decision/BotDecisionManager.cs:187-196`）：
   ```csharp
   if (Bot.Decision.DogFightDecision.DogFightActive) → DogFight
   if (BotOwner.WeaponManager.IsMelee)             → MeleeAttack
   ```
   邪教徒潜伏接近时**手持刀具**（原版设计就是持刀摸近下毒），`IsMelee = true` → **决策必然落入 MeleeAttack/DogFight**。
3. `DogFight.DogFightMove()`（`Classes/Bot/Mover/DogFight.cs:35-47`）与 `MeleeAttackAction` 都调用 `BotOwner.WeaponManager.Melee.RunToEnemyUpdate()`。
4. SAIN 的 `RunToEnemyUpdatePatch`（`Patches/FixPatches.cs:13-117`）在 SAIN 层激活时接管该方法，核心动作仍是 `bot.Mover.RunToPoint(goalEnemy.CurrPosition, sprint: High)` + 近身刀踢——**持刀冲刺循环**。
5. DogFight 触发阈值宽松（路径距离 ≤10m、1 秒内见过敌人，`DogFightDecisionClass.cs:96-101`），近身缠斗中条件持续满足 → **反复冲刺-刀踢-再冲刺**，即用户观察到的"总是在滑铲、行为呆滞"。
6. 祭司本应有的"开枪-逃跑-再潜伏"（R&H）与指挥战士的群体行为，因 SAIN 决策链对近战持械 bot 没有任何分支适配（没有"持刀时先切枪"或"打了就跑"的逻辑）而完全丢失。

### 3.4 为什么"躲草丛/阴影"这部分是正常的

潜伏行为由**原版和平层**（战士的 Utility peace 13 / GClass135 11，祭司的 StayAtPos 11 等）驱动。无敌人时 SAIN 层不激活，这些原版层照常工作——与用户观察一致。问题只出在**受击后的战斗反应段**。

### 3.5 与正常 bot 的对比

PMC/Scav 被 SAIN 接管时：注入 SAIN 层 + **精确移除** Pmc/AssaultHaveEnemy/Pursuit 等原版层 → 单一决策源，行为一致。Boss（如 Killa/Sanitar）同理。邪教徒是**唯一一类"战士完全漏接管、祭司半接管且原版层零清理"的 bot**——这正是其表现与其他 bot 截然不同的结构性原因。

### 3.6 根因汇总（按权重排序）

1. **【主因 / 战士】`AIBrains.Followers` 遗漏 `Brain.SectantWarrior`**（`Brain.cs:123-141`）——一行遗漏导致战士大脑既无 SAIN 层也无原版层清理，半接管混乱态。上游同源 bug。
2. **【主因 / 祭司】SAIN 决策链对近战持械 bot 无适配**：`IsMelee → MeleeAttack/DogFight → RunToEnemyUpdate` 是强制路径（`BotDecisionManager.cs:187-196`、`DogFight.cs:35-47`），邪教徒持刀即被判死循环冲刺；原版 R&H/SupShootSect 的"射击-转移-潜伏"节奏被旁路。
3. **【次因】层级零清理 + 优先级竞争**：祭司大脑内 SAIN 层(69-99)与原版层(100-150)共存，激活条件此消彼长，行为逐帧切换不稳定（"呆滞"观感的来源之一）。
4. **【次因】群体联动断裂**：原版邪教体系依赖祭司 `SetBoss()` 向战士分发 SupShootSect 配置（`GClass351.cs:165-172`）；祭司被 SAIN 接管后其大脑事件流改变，战士的伏击射击层可能永远无法正确初始化，只剩 MeleeS 常驻 → 全体邪教只剩"持刀冲锋"一种反应。

### 3.7 修复方向

| 优先级 | 方案 | 说明 |
|---|---|---|
| P0 | 用户侧：`VanillaCultists = true` | 立即可用的 workaround，邪教徒完全走原版 |
| P1 | **一行修复**：`Brain.cs` 的 `AIBrains.Followers` 增加 `Brain.SectantWarrior` | 使战士获得与祭司一致的 SAIN 接管；注意需同时评估移除列表 |
| P1 | 近战 bot 决策适配 | 在 `BotDecisionManager.getDecision()` 的 `IsMelee` 分支前增加"有远程武器则切枪"（`Selector.TryChangeToMain()`）判断，或为邪教徒 WildSpawnType 增加专属决策：射击后撤离（复刻 SupShootSect 节奏） |
| P2 | 补全移除列表 | Boss/Follower 移除列表加入 `SupShootSect_IN/OUT`、`MeleeS_IN/OUT`、`Run&Strike`、`Warn`、`R&H_IN/OUT`、`GrenSuicide`、`Utility peace`、`Khorovod` 等邪教专属层名，消除双层政府 |
| P2 | 或整体排除 | 若短期不做适配，将两类邪教徒加入排除逻辑（等效内置 VanillaCultists），避免半接管态 |

---

## 四、验证状态与遗留假设

| 结论 | 验证程度 |
|---|---|
| SAIN 层注入/移除机制、优先级数值 | 代码直接确认（SAIN 源 + BigBrain 源 + 反编译） |
| Partisan 原版层栈与激活条件 | 反编译直接确认（GClass308/120/121/126/127/132） |
| SectantWarrior 遗漏导致的半接管态 | 代码直接确认（Brain.cs + BigBrainHandler.cs + SAINEnableClass.cs） |
| IsMelee → 冲刺决策链 | 代码直接确认（BotDecisionManager/DogFight/MeleeAttackAction/FixPatches + 反编译 BotMeleeWeaponData） |
| Rat 个性 Freeze/搜索延迟数值 | 代码直接确认 |
| 手雷 0 库存锁死埋雷层 | 反编译 + SPT 配置确认逻辑存在；**实际刷新概率需运行时验证** |
| 地图雷区（AIMinePoint）数据是否在 SPT 各地图完整 | **未验证**（需运行时或地图资源检查） |
| 原版 Memory 与 SAIN 感知在具体交战中的先后关系 | **静态推断**，建议用 BepInEx 运行时日志（Debug 模式）确认局面 A/B/C 的实际分布 |

> 建议的下一步：在 DEBUG 构建下开启 SAIN 的决策日志（`SAINPlugin.DebugMode` + ForceSoloDecision），实地观察 Partisan 的 `CurrentCombatDecision` 与活跃层名，可一次性确认局面 B（Freeze 接管）是否为实机主因。

---

## 五、参考来源

- 本仓库：`Plugin/BigBrainHandler.cs`、`Plugin/SAINEnableClass.cs`、`Classes/Bot/Decision/BotDecisionManager.cs`、`Classes/Bot/Mover/DogFight.cs`、`Layers/Combat/Solo/CombatSoloLayer.cs`、`Layers/Combat/Solo/FreezeAction.cs`、`Layers/Combat/Solo/MeleeAttackAction.cs`、`Patches/FixPatches.cs`、`Preset/GlobalSettings/Categories/BigBrain/Brain.cs`、`Preset/GlobalSettings/Categories/MindSettings.cs`
- 游戏本体反编译（SPT 3.11.4 Assembly-CSharp.dll）：`GClass308/120/121/126/127/132/148/149/150/151/152/351/353`、`BotMeleeWeaponData.cs`、`AIPlaceLogicPartisan.cs`
- SPT 服务器数据：`SPT_Data/Server/database/bots/types/bosspartisan.json`、`sectantpriest.json`、`sectantwarrior.json`
- 上游：Solarint/SAIN 4.1.0 源码比对；issue [#298](https://github.com/Solarint/SAIN/issues/298)（Partisan，Open）；issue [#53](https://github.com/Solarint/SAIN/issues/53)（邪教徒强度，Closed）；QuestingBots issue [#107](https://github.com/dwesterwick/SPTQuestingBots/issues/107)
