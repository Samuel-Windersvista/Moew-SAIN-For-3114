# SAIN 接管 Partisan 与邪教徒 — 实施方案

> **执行方式说明：** 本方案按任务（Task）分解，每个任务含独立验证步骤，可用 subagent-driven-development 逐任务派发执行。步骤使用 `- [ ]` 复选框跟踪。
> 前置文档：`docs/SAIN行为异常调查-Partisan与邪教徒.md`（根因分析，含全部反编译证据）

**目标：** 不走 `VanillaBosses`/`VanillaCultists` 退路，让 SAIN 完整、正确地接管 bossPartisan 与 sectantPriest/sectantWarrior：Partisan 恢复无个性限制的全能力行为（索敌/战斗/和平期游荡/布雷）；邪教徒在 SAIN 框架下复刻原版"潜伏→偷袭→转移"节奏且更智能。

**架构：** 三大杠杆——① 个性系统解锁（MindSettings）；② 大脑层治理（BigBrainHandler 移除/注入清单精确化）；③ SAIN 决策链扩展（BotDecisionManager 近战门控 + EnemyDecisionClass 邪教专属决策）。不改动 EFT 原版类，不新增对装备的假设（服务器侧手雷配置不在范围内）。

**技术栈：** C# / .NET Framework 4.7.1（SAIN.csproj）、DrakiaXYZ.BigBrain 1.3.2（BrainManager 层注入）、Harmony patch、SPT 3.11.4 客户端程序集。

## 全局约束

- 编译零错误：`dotnet build SAIN.sln` 必须通过（本仓库无单元测试框架，验证 = 编译 + 游戏内运行时检查）。
- 所有新行为必须可通过 F6 菜单开关/调参（沿用 `[Name]/[Category]/[Description]` 特性 + GlobalSettings 模式）。
- 禁止影响其他 bot 类型：所有邪教专属逻辑必须以 `WildSpawn.IsCultist(...)` 或 WildSpawnType 精确判断为闸门；Partisan 专属逻辑以 `bossPartisan` 为闸门。
- 层名字符串必须与反编译确认的 `Name()` 返回值**逐字符一致**（区分大小写，含 `&`、空格）。
- 不修改 `AIBrains.Bosses` 中 `BossPartisan` 的归属；不向 StrictExclusionList 添加任何类型。
- 装备/掉落（手雷有无）属服务器侧 SPT_Data，本方案不处理。

---

### Task 1: Partisan 个性解锁

**Files:**
- Modify: `Preset/GlobalSettings/Categories/MindSettings.cs:51`

**背景：** `PERS_BOSSES` 字典被 `Preset/Personalities/BasePersonality/PersonalityDictionary.cs:123-130` 的 `setBossPersonality()` 消费；字典中不存在的 Boss 会返回 `EPersonality.Normal`（全能力个性：可搜索、可转移掩体、无 Freeze 强制、搜索延迟正常）。移除 Partisan 条目即解除 Rat 强加的 Freeze/超长搜索延迟/禁冲刺限制。

- [ ] **Step 1: 删除强制 Rat 条目**

```csharp
// MindSettings.cs PERS_BOSSES 中删除此行：
{ WildSpawnType.bossPartisan, EPersonality.Rat},
```

- [ ] **Step 2: 编译验证**

Run: `dotnet build SAIN.sln`
Expected: 0 错误

- [ ] **Step 3: 游戏内验证**

Woods 图刷出 Partisan，用 SAIN Debug Overlay（或 BepInEx 日志）确认其个性不再为 Rat，听到远处枪声后不进入 Freeze 站桩，能进入 Search 决策。

---

### Task 2: Partisan 原版战斗层清理

**Files:**
- Modify: `Plugin/BigBrainHandler.cs:232-251`（`ToggleVanillaLayersForBosses`）

**背景：** Partisan 大脑 13 个原版层中，7 个战斗/潜行层（PrtFMN 110 / PrtPst 100 / PrtZrSvg 95 / PrtFight 90 / PrtBadTrg 85 / PrtMany 80 / PrtStalk 70）优先级全部高于 SAIN CombatSoloLayer(69)，只要原版 `Memory.HaveEnemy` 成立就夺走控制权，与 SAIN 决策形成"双层政府"。将它们加入移除列表后，战斗归 SAIN；**埋雷层（PartisanMine 60 / PartMineAll 50）、兜底层（HoldOrCoverT 40 / StayAtPos 11）、AvoidDanger/Malfunction 全部保留**——无敌人时 SAIN 层不激活，原版埋雷与驻守逻辑自然接管，绊雷功能恢复。

- [ ] **Step 1: 扩展移除列表**

```csharp
// BigBrainHandler.cs ToggleVanillaLayersForBosses() 的 LayersToToggle 中追加：
List<string> LayersToToggle = new List<string>
{
    "KnightFight",
    "BirdEyeFight",
    "BossBoarFight",
    "KojaniyB_Enemy",
    "Bully Layer",
    "KlnSolo",
    "KolontayFight",
    "KlnTrg",
    "BossSanitarFight",
    // Partisan 战斗/潜行层（3.11 新增 Boss，上游遗漏）
    "PrtFight",
    "PrtPst",
    "PrtStalk",
    "PrtMany",
    "PrtBadTrg",
    "PrtZrSvg",
    "PrtFMN",
};
```

注意：这些层名只存在于 BossPartisan 大脑，对其他 Boss 的大脑调用 `RemoveLayers` 为无操作，无副作用。

- [ ] **Step 2: 编译验证**（同 Task 1 Step 2）

- [ ] **Step 3: 游戏内验证**

与 Partisan 交火：确认其战斗行为由 SAIN 层驱动（Debug Overlay 显示 `SAIN : Combat Layer`），会找掩体/射击/搜索；脱离接触且无敌人后，确认其在雷点间移动布雷（若携带手雷）而非站桩。

---

### Task 3: Partisan 和平期游荡（兴趣点探索）

**Files:**
- Modify: `Plugin/BigBrainHandler.cs`（`addCustomLayersToBosses` 或新增 `addCustomLayersToPartisan`）

**背景：** fork 版 `Layers/Peace/PeacefulLayer.cs:29-41` 已含兴趣点探索分支（QG-1）：无 GoalEnemy 且存在 15m 外的兴趣点（交战/阵亡/枪声位置，由 `SAINInterestPointClass` 维护）时激活并 `WalkToPoint`。但该层从未注册给任何大脑。将其**仅以 45 优先级注册给 BossPartisan 大脑**：低于埋雷层（60/50，布雷优先）、高于 HoldOrCoverT(40)/StayAtPos(11)，实现"有雷布雷 → 无雷巡兴趣点 → 都不行才驻守"的和平期行为链。

- [ ] **Step 1: 新增 Partisan 专属注册方法**

```csharp
// BigBrainHandler.cs BrainAssignment 中新增：
private static void addCustomLayersToPartisan()
{
    List<string> brainList = new List<string>() { Brain.BossPartisan.ToString() };
    // 45: 低于 PartisanMine(60)/PartMineAll(50)，高于 HoldOrCoverT(40)/StayAtPos(11)
    BrainManager.AddCustomLayer(typeof(SAIN.Layers.Peace.PeacefulLayer), brainList, 45);
}
```

并在 `Init()` 中 `addCustomLayersToBosses();` 之后调用 `addCustomLayersToPartisan();`。

- [ ] **Step 2: 编译验证**

- [ ] **Step 3: 游戏内验证**

远处制造枪声（兴趣点生成），确认无直接敌人的 Partisan 离开出生点向兴趣区域移动（伏击者向战场靠拢的"更智能"表现）；确认兴趣点探索不干扰布雷（身上带雷时优先布雷）。

---

### Task 4: SectantWarrior 大脑接管修复（一行修复）

**Files:**
- Modify: `Preset/GlobalSettings/Categories/BigBrain/Brain.cs:123-141`（`AIBrains.Followers`）

**背景：** `BotBrains.cs:53` 已含 `Brain.SectantWarrior`（编辑器展示用），运行用的 `AIBrains.Followers` 遗漏 → 战士大脑既无 SAIN 层注入也无原版层清理，处于半接管混乱态。补一行即纳入与其他 Follower 一致的注入/清理管线。

- [ ] **Step 1: 补全列表**

```csharp
public static readonly List<Brain> Followers = new()
{
    Brain.FollowerBully,
    // ... 原有 15 项保持不变 ...
    Brain.FlBoarCl,
    Brain.FlBoarSt,
    Brain.SectantWarrior,   // 新增：修复上游遗漏
};
```

- [ ] **Step 2: 编译验证**

- [ ] **Step 3: 游戏内验证**

夜间 Customs 刷邪教，Debug Overlay 确认 sectantWarrior 大脑出现 `SAIN : Combat Layer` 等 SAIN 层（此前完全没有）。

---

### Task 5: 邪教徒原版战斗层清理

**Files:**
- Modify: `Plugin/BigBrainHandler.cs:232-273`（Boss 与 Follower 两个移除列表）

**背景：** 层名经反编译逐字确认。战士（Follower 列表）：`MeleeS_IN/MeleeS_OUT`（持刀冲刺，GClass150）、`SupShootSect_IN/SupShootSect_OUT`（伏击射击，GClass152）、`Run&Strike`（GClass151）、`Kill logic`（GClass59）。祭司（Boss 列表）：`GrenSuicide`（GClass148）、`R&H_IN/R&H_OUT`（逃跑隐匿，GClass149）。**保留**：AvoidDanger、Malfunction、Warn、Utility peace、StayAtPos、HoldOrCover、Khorovod——潜伏躲草丛/阴影的和平行为由这些层继续提供（用户认可的现有表现），SAIN 只接管战斗段。

- [ ] **Step 1: Boss 移除列表追加（祭司）**

```csharp
// ToggleVanillaLayersForBosses 的 LayersToToggle 追加：
"GrenSuicide",
"R&H_IN",
"R&H_OUT",
```

- [ ] **Step 2: Follower 移除列表追加（战士）**

```csharp
// ToggleVanillaLayersForFollowers 的 LayersToToggle 追加：
"MeleeS_IN",
"MeleeS_OUT",
"SupShootSect_IN",
"SupShootSect_OUT",
"Run&Strike",
"Kill logic",
```

- [ ] **Step 3: 编译验证**

- [ ] **Step 4: 游戏内验证**

确认邪教徒和平期仍躲草丛/阴影（Utility peace 存活）；受击后由 SAIN 层接管（Overlay 显示 SAIN 层名），不再出现原版层与 SAIN 层逐帧互抢的呆滞抖动。

---

### Task 6: 近战决策门控（持刀时先切枪）

**Files:**
- Modify: `Classes/Bot/Decision/BotDecisionManager.cs:192-196`

**背景：** 现逻辑 `if (BotOwner.WeaponManager.IsMelee) → MeleeAttack` 是无条件死路：邪教徒潜伏时持刀，一旦 SAIN 建立敌人必然进入持刀冲刺循环。增加门控：有可用主武器时优先切枪进入正常射击决策；仅当"敌人足够近"或"敌人未察觉（偷袭窗口）"时才允许持刀突袭——既保留邪教捅刀特色，又杜绝中距离滑铲死循环。Tagilla 无枪械，`TryChangeToMain` 失败自动回落原逻辑，不受影响。

- [ ] **Step 1: 修改决策分支**

```csharp
// BotDecisionManager.cs getDecision() 中，原：
//   if (BotOwner.WeaponManager.IsMelee)
//   { SetDecisions(ECombatDecision.MeleeAttack, ...); return; }
// 改为：
if (BotOwner.WeaponManager.IsMelee)
{
    bool enemyUnaware = !enemy.IsVisible && enemy.TimeSinceLastKnownUpdated > 3f;
    bool meleeViable = enemy.UnawareOfBot || enemyUnaware
        || enemy.RealDistance <= GlobalSettingsClass.Instance.General.Melee.MELEE_ENGAGE_MAX_DIST;
    if (!meleeViable && BotOwner.WeaponManager.Selector.CanChangeToMainWeapons)
    {
        BotOwner.WeaponManager.Selector.TryChangeToMain();
        // 不 return，落入下方正常射击决策链
    }
    else
    {
        SetDecisions(ECombatDecision.MeleeAttack, ESquadDecision.None, ESelfActionType.None, enemy);
        return;
    }
}
```

注：`Enemy` 类的可用属性以 `Classes/Bot/EnemyClasses/Enemy.cs` 实际定义为准（距离用 `RealDistance` 或 `Path.EnemyPathDistance`；未察觉判定可用 `enemy.Status.UnawareOfBot` 若存在，否则用 `TimeSinceSeen/TimeSinceLastKnownUpdated` 组合）。`CanChangeToMainWeapons`/`TryChangeToMain` 若该 EFT 版本签名不同，以 `WeaponManager.Selector` 反编译成员为准（参考 `DogFight.cs:44` 的 `TryChangeToMain()` 用法）。

- [ ] **Step 2: 新增 F6 设置**

```csharp
// Preset/GlobalSettings/Categories/General/ 下合适的设置类（如新建 MeleeSettings 或并入现有 Move/Shoot 类）：
[Name("近战交战最大距离")]
[Description("持有近战武器但主武器可用时，敌人距离小于此值才允许持刀冲锋(米)。")]
[MinMax(1f, 30f, 100f)]
public float MELEE_ENGAGE_MAX_DIST = 8f;
```

- [ ] **Step 3: 编译验证**

- [ ] **Step 4: 游戏内验证**

中距离（>15m）射击邪教徒：确认其切枪还击而非持刀冲锋；贴脸或背刺情境：确认持刀突袭仍会发生（特色保留）。

---

### Task 7: 邪教徒"打了就跑"决策（Hit-and-Run）

**Files:**
- Modify: `Classes/Bot/Decision/EnemyDecisionClass.cs`（`GetDecision`，第 43-205 行区域）
- Modify: `Preset/GlobalSettings/Categories/MindSettings.cs` 或新建 `CultistSettings`（F6 参数）

**背景：** 原版邪教节奏 = SupShootSect（伏击射击数发）→ 转移 → 再潜伏。SAIN 化复刻：在决策链中 `StandAndShoot` 判定**之前**插入邪教专属分支——被发现/交火超过窗口期后强制脱离（SeekCover 转移掩体并静默），转移完成后由既有 Search/潜行系统自然回到接近循环。因 SAIN 的 Search 带潜行（`SneakyBots`）、听觉定位与兴趣点，回圈速度比原版更快更聪明。

- [ ] **Step 1: 新增邪教决策分支**

```csharp
// EnemyDecisionClass.GetDecision() 内，shallStandAndShoot 之前插入：
bool shallHitAndRun = shallCultistHitAndRun(enemy, out reason);
if (shallHitAndRun)
{
    result = ECombatDecision.SeekCover;   // 脱离接触转移掩体
    return true;
}
```

```csharp
private float _cultistEngageStartTime = -1f;

private bool shallCultistHitAndRun(Enemy enemy, out string reason)
{
    reason = null;
    if (!Helpers.EnumValues.WildSpawn.IsCultist(Bot.Info.WildSpawnType))
        return false;
    var settings = GlobalSettingsClass.Instance.Mind;
    if (!settings.CULTIST_HIT_AND_RUN_ENABLED)
        return false;

    bool botIsSpotted = enemy.IsVisible || enemy.TimeSinceSeen < settings.CULTIST_HIT_AND_RUN_DISENGAGE_TIME;
    if (!botIsSpotted)
    {
        _cultistEngageStartTime = -1f;   // 未被发现：重置窗口，允许继续接近/伏击射击
        return false;
    }
    if (_cultistEngageStartTime < 0f)
        _cultistEngageStartTime = Time.time;
    if (Time.time - _cultistEngageStartTime > settings.CULTIST_HIT_AND_RUN_ENGAGE_WINDOW)
    {
        reason = "CultistHitAndRun";
        _cultistEngageStartTime = -1f;
        return true;
    }
    return false;
}
```

注：`Bot.Info.WildSpawnType` 的确切属性路径以 `Classes/Bot/Info/SAINBotInfoClass.cs` 为准；`IsCultist` 辅助方法已存在于 `Helpers/EnumValues.cs:47-49`。

- [ ] **Step 2: 新增 F6 设置**

```csharp
// MindSettings.cs 新增 Category("邪教徒战术")：
[Name("邪教打了就跑")]
[Description("邪教徒被发现交火超过窗口期后强制脱离转移，复刻原版伏击-转移节奏。")]
public bool CULTIST_HIT_AND_RUN_ENABLED = true;

[Name("交战窗口(秒)")]
[MinMax(0.5f, 15f, 100f)]
public float CULTIST_HIT_AND_RUN_ENGAGE_WINDOW = 3f;

[Name("脱离判定时间(秒)")]
[MinMax(0.5f, 10f, 100f)]
public float CULTIST_HIT_AND_RUN_DISENGAGE_TIME = 2f;
```

- [ ] **Step 3: 编译验证**

- [ ] **Step 4: 游戏内验证**

夜间图与邪教交火：确认战士还击约 3 秒后主动脱离转移掩体（不再死磕），随后潜行重新接近；祭司同等节奏；确认非邪教 bot（PMC/Scav/Boss）行为完全不受此分支影响。

---

### Task 8: 设置导出、文档与变更记录

**Files:**
- Modify: `SAIN原版与Moew兼容版差异对照表.md`（新增第九节）
- Modify: `CHANGELOG.md`
- Modify: `docs/SAIN行为异常调查-Partisan与邪教徒.md`（文末追加"已实施修复"状态）

- [ ] **Step 1:** 差异对照表新增本节全部改动条目（个性解锁/层清理×3/近战门控/Hit-and-Run/F6 新设置 3 项）。
- [ ] **Step 2:** CHANGELOG 增加版本条目（建议 v4.4.0）。
- [ ] **Step 3:** 调查报告文末标注各根因的修复状态与对应 Task 编号。

---

### Task 9: 集成验证（出厂检验清单）

- [ ] `dotnet build SAIN.sln` 零错误零新增警告
- [ ] Partisan：无敌人时布雷/兴趣点游荡；接触后 SAIN 战斗行为（掩体/射击/搜索）；全程无超过 30 秒的静止
- [ ] 邪教徒：和平期潜伏草丛；中距被击 → 切枪还击 → 窗口期后脱离转移 → 潜行回圈；贴脸/背刺 → 持刀突袭；无"原地滑铲循环"
- [ ] 回归：Reshala/Killa/PMC/Scav 行为与改动前一致（层移除清单的新增层名不存在于这些大脑，预期零影响，仍需抽查）
- [ ] F6：三项新设置可见可调，重启后持久化

---

## 自检记录（writing-plans 要求）

1. **需求覆盖：** Partisan 个性不限制 → Task 1；Partisan 索敌/布雷 → Task 1+2+3；邪教徒仿原版且更智能 → Task 4+5+6+7；不管装备问题 → 全局约束已声明排除。无缺口。
2. **占位符扫描：** Task 6/7 中有两处"以实际类定义为准"的适配说明——这是有意为之的 API 适配注释（EFT 混淆版本属性名需实现时核对），非空泛占位；其余步骤均含完整代码。
3. **类型一致性：** `Brain.SectantWarrior`、`ECombatDecision.SeekCover/MeleeAttack`、`WildSpawn.IsCultist`、`GlobalSettingsClass.Instance.Mind` 在任务间引用一致；新增设置名 `MELEE_ENGAGE_MAX_DIST`、`CULTIST_HIT_AND_RUN_*` 前后一致。

## 风险与备注

- **Task 3 依赖** fork 自有的 `SAINInterestPointClass` 已实现且稳定（v4.x 兴趣点探索功能）；若该功能被 F6 全局关闭，PeacefulLayer 对 Partisan 退化为不激活，回落 StayAtPos 驻守（可接受）。
- **Task 5 风险**：保留 `Warn`(GClass159, 优先级 100-120) 属未知行为层；若实测发现其在战斗中抢占 SAIN 层，将其追加进移除列表即可（一行改动）。
- **近战门控的 `meleeViable` 判定属性名**需对照 `Classes/Bot/EnemyClasses/` 实际 API 微调，逻辑骨架不变。
- 埋雷层激活仍要求原版 `Memory.HaveEnemy`（PartisanMine）或地图雷点（PartMineAll）；若实测无敌人时布雷仍不触发，下一步候选方案是在 SAIN 侧新建 MineLayer 直接调用 `BotOwner.MinesData`（`CacheAllInRadius`/`GetClosestsFromCache`/`StartPlant`，API 已反编译确认）——属增量增强，不在本期范围。
