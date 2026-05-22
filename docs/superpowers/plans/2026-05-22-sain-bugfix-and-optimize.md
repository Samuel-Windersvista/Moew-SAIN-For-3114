# SAIN + BigBrain + Waypoints Bug修复与性能优化实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 修复 SAIN、BigBrain、Waypoints 三大代码库中的 20 个 Bug/性能问题，按 4 阶段优先级实施。

**Architecture:** 20 个任务按代码库和严重程度分组为 4 个阶段。每个任务修改 1-3 个文件，包含精确的替换代码。Phase 1 (严重崩溃/逻辑错误) -> Phase 2 (正确性) -> Phase 3 (性能) -> Phase 4 (代码质量)。

**Tech Stack:** C# .NET Framework 4.7.1 (Harmony Patch), TypeScript (SPT ServerMod)

**前置参考文档:** `BUG修复与性能优化方案.md` -- 包含每个问题的详细分析和完整修复代码。

---

## Phase 1：严重 Bug 修复 (4 tasks)

### Task 1: Waypoints -- 修复 DependencyChecker as 转换崩溃 [WP-1]

**Files:**
- Modify: `前置MOD的源代码/SPT-Waypoints-1.7.1/Helpers/DependencyChecker.cs:24`

- [ ] **Step 1: 修复 as 转换**

将第 24 行：
```csharp
var dependencies = pluginType.GetCustomAttributes(typeof(BepInDependency), true) as BepInDependency[];
```
改为：
```csharp
var dependencies = pluginType.GetCustomAttributes(typeof(BepInDependency), true)
    .Cast<BepInDependency>().ToArray();
```

- [ ] **Step 2: 编译验证**

Run: `dotnet build 前置MOD的源代码/SPT-Waypoints-1.7.1/DrakiaXYZ-Waypoints.sln`
Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add 前置MOD的源代码/SPT-Waypoints-1.7.1/Helpers/DependencyChecker.cs
git commit -m "fix(waypoints): fix DependencyChecker as-cast always returning null causing plugin crash"
```

---

### Task 2: BigBrain -- 为 4 个核心 Patch 添加 Release 模式异常保护 [BB-1]

**Files:**
- Modify: `前置MOD的源代码/SPT-BigBrain-1.3.2/Patches/BotBaseBrainUpdatePatch.cs:42-108`
- Modify: `前置MOD的源代码/SPT-BigBrain-1.3.2/Patches/BotAgentUpdatePatch.cs:30-66`
- Modify: `前置MOD的源代码/SPT-BigBrain-1.3.2/Patches/BotBaseBrainActivateLayerPatch.cs:42-63`
- Modify: `前置MOD的源代码/SPT-BigBrain-1.3.2/Patches/BotBrainCreateLogicNodePatch.cs:35-51`

- [ ] **Step 1: 修复 BotBaseBrainUpdatePatch**

移除 `#if DEBUG` 条件，将整个方法体包裹在 `try-catch` 中，异常时 fallback。将第 42-108 行改为：

```csharp
[PatchPrefix]
public static bool PatchPrefix(object __instance, AILogicActionResultStruct prevResult, ref AILogicActionResultStruct? __result)
{
    try
    {
        List<AICoreLogicLayerClass> activeLayerList = _activeLayerListField.GetValue(__instance) as List<AICoreLogicLayerClass>;
        AICoreLogicLayerClass activeLayer = _activeLayerGetter.Invoke(__instance, null) as AICoreLogicLayerClass;

        if (activeLayerList == null)
        {
            __result = null;
            return false;
        }

        foreach (AICoreLogicLayerClass layer in activeLayerList)
        {
            if (layer.ShallUseNow())
            {
                if (layer != activeLayer)
                {
                    if (activeLayer is CustomLayerWrapper customActiveLayer)
                    {
                        customActiveLayer.Stop();
                    }

                    activeLayer = layer;
                    _activeLayerSetter.Invoke(__instance, new object[] { layer });
                    Action<AICoreLogicLayerClass> action = _onLayerChangedToField.GetValue(__instance) as Action<AICoreLogicLayerClass>;
                    if (action != null)
                    {
                        action(activeLayer);
                    }

                    if (activeLayer is CustomLayerWrapper customNewLayer)
                    {
                        customNewLayer.Start();
                    }
                }

                __result = activeLayer.Update(new AILogicActionResultStruct?(prevResult));
                return false;
            }
        }

        __result = null;
        return false;
    }
    catch (Exception ex)
    {
        BotOwner owner = _ownerField.GetValue(__instance) as BotOwner;
        Logger.LogError($"Exception in Brain Update for {owner?.name}: {ex}");
        return true; // fallback to original method
    }
}
```

- [ ] **Step 2: 修复 BotAgentUpdatePatch**

同样移除 `#if DEBUG`，方法体包裹 try-catch，异常时 `return true`：
```csharp
[PatchPrefix]
public static bool PatchPrefix(object __instance, AILogicActionResultStruct prevResult)
{
    try
    {
        // ... 原有核心逻辑保持不变 ...
    }
    catch (Exception ex)
    {
        Logger.LogError($"Exception in Agent Update: {ex}");
        return true; // fallback to original method
    }
}
```

- [ ] **Step 3: 修复 BotBaseBrainActivateLayerPatch**

同样包裹 try-catch，增加 null 检查：
```csharp
[PatchPrefix]
public static bool PatchPrefix(object __instance, ref object __result)
{
    try
    {
        // ... 原有逻辑，但在 activeLayerList 处增加 null 检查后 return true ...
    }
    catch (Exception ex)
    {
        Logger.LogError($"Exception in ActivateLayer: {ex}");
        return true;
    }
}
```

- [ ] **Step 4: 修复 BotBrainCreateLogicNodePatch**

同样包裹 try-catch，增加 CustomLogicList 索引越界检查：
```csharp
[PatchPrefix]
public static bool PatchPrefix(BotLogicDecision decision, ref object __result, ref bool __state)
{
    try
    {
        int adjustedIndex = (int)decision - BrainManager.START_LOGIC_ID;
        if (adjustedIndex < 0 || adjustedIndex >= BrainManager.Instance.CustomLogicList.Count)
            return true;

        Type logicType = BrainManager.Instance.CustomLogicList[adjustedIndex];
        // ... 原有逻辑 ...
    }
    catch (Exception ex)
    {
        Logger.LogError($"Exception in CreateLogicNode: {ex}");
        return true;
    }
}
```

- [ ] **Step 5: 编译验证**

Run: `dotnet build 前置MOD的源代码/SPT-BigBrain-1.3.2/DrakiaXYZ-BigBrain.sln`
Expected: 0 errors.

- [ ] **Step 6: Commit**

```bash
git add 前置MOD的源代码/SPT-BigBrain-1.3.2/Patches/
git commit -m "fix(bigbrain): add Release-mode exception protection to all 4 core Patches; fallback to vanilla on error"
```

---

### Task 3: SAIN -- 修复 DeadBots RemoveAt 索引错乱 + AddNavObstacles 降频 [BUG-1 + PERF-2]

**Files:**
- Modify: `Components/BotManagerComponent.cs:139-186`

- [ ] **Step 1: 修复 RemoveAt 从高到低遍历**

将第 179-182 行：
```csharp
foreach (var index in IndexToRemove)
{
    DeadBots.RemoveAt(index);
}
```
改为：
```csharp
for (int i = IndexToRemove.Count - 1; i >= 0; i--)
{
    DeadBots.RemoveAt(IndexToRemove[i]);
}
```

- [ ] **Step 2: 为 AddNavObstacles 添加降频**

在 `ManualUpdate` 方法中找到 `AddNavObstacles()` 调用，将其包裹在频率控制中。在类中添加字段：
```csharp
private float _nextNavObstacleTime;
```
将调用处改为：
```csharp
if (_nextNavObstacleTime < Time.time)
{
    _nextNavObstacleTime = Time.time + 2f; // 每2秒执行一次
    AddNavObstacles();
}
UpdateObstacles();
```

- [ ] **Step 3: 编译验证**

Run: `dotnet build SAIN.sln`
Expected: 0 errors.

- [ ] **Step 4: Commit**

```bash
git add Components/BotManagerComponent.cs
git commit -m "fix(sain): fix DeadBots RemoveAt index corruption; throttle AddNavObstacles to 2s interval"
```

---

### Task 4: BigBrain -- 修复 HasSameContents 重复元素语义错误 [BB-2]

**Files:**
- Modify: `前置MOD的源代码/SPT-BigBrain-1.3.2/Utils.cs:23-36`

- [ ] **Step 1: 改为频率计数实现**

将第 23-36 行整个方法替换为：
```csharp
public static bool HasSameContents<T>(IEnumerable<T> collection1, IEnumerable<T> collection2)
{
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

- [ ] **Step 2: 编译验证**

Run: `dotnet build 前置MOD的源代码/SPT-BigBrain-1.3.2/DrakiaXYZ-BigBrain.sln`
Expected: 0 errors.

- [ ] **Step 3: Commit**

```bash
git add 前置MOD的源代码/SPT-BigBrain-1.3.2/Utils.cs
git commit -m "fix(bigbrain): fix HasSameContents returning wrong result for collections with duplicates"
```

---

## Phase 2：高优先级正确性修复 (6 tasks)

### Task 5: Waypoints -- MainPlayer null 保护 [WP-2]

**Files:**
- Modify: `前置MOD的源代码/SPT-Waypoints-1.7.1/Patches/WaypointPatch.cs:40-43`

- [ ] **Step 1: 添加 MainPlayer null 检查**

在 `InjectNavmesh` 方法开头（第 43 行之前）插入：
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
    // ... 后续代码不变 ...
```

- [ ] **Step 2: Commit**

```bash
git add 前置MOD的源代码/SPT-Waypoints-1.7.1/Patches/WaypointPatch.cs
git commit -m "fix(waypoints): add MainPlayer null guard in InjectNavmesh to prevent NRE"
```

---

### Task 6: Waypoints -- DoorLinkPatch aiDoorsHolder null 保护 [WP-3]

**Files:**
- Modify: `前置MOD的源代码/SPT-Waypoints-1.7.1/Patches/DoorLinkPatch.cs:22,55`
- Modify: `前置MOD的源代码/SPT-Waypoints-1.7.1/Patches/DoorLinkPatch.cs:115-116` (voxel null check)

- [ ] **Step 1: 添加 aiDoorsHolder null 检查**

在第 22 行之后（FindObjectOfType 调用之后）插入：
```csharp
var aiDoorsHolder = UnityEngine.Object.FindObjectOfType<AIDoorsHolder>();
if (aiDoorsHolder == null)
{
    Logger.LogError("No AIDoorsHolder found in scene, skipping custom door links");
    return;
}
```

- [ ] **Step 2: 添加 voxel 和 DoorLinksIds null 检查**

在第 115-116 行，将：
```csharp
NavGraphVoxelSimple voxel = coversData.GetVoxelSafeByIndexes(x, y, z);
if (voxel.DoorLinks == null)
```
改为：
```csharp
NavGraphVoxelSimple voxel = coversData.GetVoxelSafeByIndexes(x, y, z);
if (voxel == null) continue;
if (voxel.DoorLinks == null) voxel.DoorLinks = new List<NavMeshDoorLink>();
if (voxel.DoorLinksIds == null) voxel.DoorLinksIds = new List<int>();
```

- [ ] **Step 3: Commit**

```bash
git add 前置MOD的源代码/SPT-Waypoints-1.7.1/Patches/DoorLinkPatch.cs
git commit -m "fix(waypoints): add null guards for aiDoorsHolder and voxel in DoorLinkPatch"
```

---

### Task 7: SAIN -- 修复 MovementContextIsAIPatch 和 CanBeSnappedPatch 全局影响 [BUG-2 + BUG-3]

**Files:**
- Modify: `Patches/MovementPatches.cs:99-112` (MovementContextIsAIPatch)
- Modify: `Patches/MovementPatches.cs:114-130` (CanBeSnappedPatch)

- [ ] **Step 1: 修复 MovementContextIsAIPatch -- 仅对 SAIN Bot 生效**

将第 106-111 行的 Patch 方法改为：
```csharp
[PatchPrefix]
public static bool Patch(MovementContext __instance, ref bool __result)
{
    Player player = __instance?.Player;
    if (player != null && SAINEnableClass.IsSAINDisabledForBot(player) == false)
    {
        __result = false;
        return false;  // 仅对 SAIN Bot 生效
    }
    return true;  // 真人玩家让原版方法正常执行
}
```

- [ ] **Step 2: 修复 CanBeSnappedPatch -- 仅对 SAIN Bot 生效**

将第 124-129 行的 Patch 方法改为：
```csharp
[PatchPrefix]
public static bool Patch(Player __instance, ref bool __result)
{
    if (__instance != null && SAINEnableClass.IsSAINDisabledForBot(__instance) == false)
    {
        __result = false;
        return false;  // 仅对 SAIN Bot 生效
    }
    return true;  // 真人玩家让原版方法正常执行
}
```

- [ ] **Step 3: Commit**

```bash
git add Patches/MovementPatches.cs
git commit -m "fix(sain): scope MovementContextIsAI and CanBeSnapped patches to SAIN bots only"
```

---

### Task 8: Waypoints -- FindPathPatch NavMeshPath 缓存避免 GC [WP-4]

**Files:**
- Modify: `前置MOD的源代码/SPT-Waypoints-1.7.1/Patches/FindPathPatch.cs` (全文)

- [ ] **Step 1: 添加 ThreadStatic 缓存 + try-catch**

将整个文件替换为：
```csharp
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

namespace DrakiaXYZ.Waypoints.Patches
{
    internal class FindPathPatch : ModulePatch
    {
        [ThreadStatic]
        private static NavMeshPath _cachedPath;

        protected override MethodBase GetTargetMethod()
        {
            Type targetType = PatchConstants.EftTypes.First(type => type.GetMethod("FindPath") != null);
            return AccessTools.Method(targetType, "FindPath");
        }

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
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add 前置MOD的源代码/SPT-Waypoints-1.7.1/Patches/FindPathPatch.cs
git commit -m "perf(waypoints): cache NavMeshPath via ThreadStatic in FindPathPatch to eliminate GC allocation per call"
```

---

### Task 9: SAIN -- CanGoToPoint NavMeshPath 缓存避免 GC [PERF-1]

**Files:**
- Modify: `Classes/Bot/Mover/SAINMoverClass.cs:300-323`

- [ ] **Step 1: 添加 ThreadStatic 缓存**

在 `SAINMoverClass` 类中添加字段：
```csharp
[ThreadStatic]
private static NavMeshPath _cachedGoToPath;
```

将 `CanGoToPoint` 方法中第 310 行：
```csharp
path = new NavMeshPath();
```
改为：
```csharp
if (_cachedGoToPath == null)
    _cachedGoToPath = new NavMeshPath();
_cachedGoToPath.ClearCorners();
path = _cachedGoToPath;
```

- [ ] **Step 2: Commit**

```bash
git add Classes/Bot/Mover/SAINMoverClass.cs
git commit -m "perf(sain): cache NavMeshPath via ThreadStatic in CanGoToPoint to eliminate GC allocation"
```

---

## Phase 3：性能优化 (6 tasks)

### Task 10: SAIN -- 移除 HearingInputClass.TrimExcess [PERF-3]

**Files:**
- Modify: `Classes/Bot/Sense/Hearing/HearingInputClass.cs:164-165`

- [ ] **Step 1: 移除或条件化 TrimExcess**

将第 164-165 行：
```csharp
if (SoundRemoved)
    SoundDataToReactTo.TrimExcess();
```
改为：
```csharp
// 仅在容量远大于实际大小时执行
if (SoundRemoved && SoundDataToReactTo.Capacity > SoundDataToReactTo.Count * 3)
    SoundDataToReactTo.TrimExcess();
```

- [ ] **Step 2: Commit**

```bash
git add Classes/Bot/Sense/Hearing/HearingInputClass.cs
git commit -m "perf(sain): throttle TrimExcess on SoundDataToReactTo to avoid excessive memory reallocation"
```

---

### Task 11: BigBrain -- CustomLayersReadOnly/CustomLogicsReadOnly 缓存 [BB-3]

**Files:**
- Modify: `前置MOD的源代码/SPT-BigBrain-1.3.2/Brains/BrainManager.cs:51-62`

- [ ] **Step 1: 添加 ReadOnlyDictionary 包装器字段**

在 `BrainManager` 类中添加（第 56 行之后）：
```csharp
private ReadOnlyDictionary<int, LayerInfo> _customLayersReadOnly;
private ReadOnlyDictionary<Type, int> _customLogicsReadOnly;
```

将第 59-60 行：
```csharp
public static IReadOnlyDictionary<int, LayerInfo> CustomLayersReadOnly => Instance.CustomLayers.ToDictionary(i => i.Key, i => i.Value);
public static IReadOnlyDictionary<Type, int> CustomLogicsReadOnly => Instance.CustomLogics.ToDictionary(i => i.Key, i => i.Value);
```
改为：
```csharp
public static IReadOnlyDictionary<int, LayerInfo> CustomLayersReadOnly => Instance._customLayersReadOnly;
public static IReadOnlyDictionary<Type, int> CustomLogicsReadOnly => Instance._customLogicsReadOnly;
```

在 `AddCustomLayer` 方法末尾添加重建逻辑：
```csharp
Instance._customLayersReadOnly = new ReadOnlyDictionary<int, LayerInfo>(Instance.CustomLayers);
```

在构造函数末尾初始化：
```csharp
private BrainManager()
{
    _customLayersReadOnly = new ReadOnlyDictionary<int, LayerInfo>(CustomLayers);
    _customLogicsReadOnly = new ReadOnlyDictionary<Type, int>(CustomLogics);
}
```

- [ ] **Step 2: 编译验证**

Run: `dotnet build 前置MOD的源代码/SPT-BigBrain-1.3.2/DrakiaXYZ-BigBrain.sln`
Expected: 0 errors.

- [ ] **Step 3: Commit**

```bash
git add 前置MOD的源代码/SPT-BigBrain-1.3.2/Brains/BrainManager.cs
git commit -m "perf(bigbrain): cache CustomLayersReadOnly/CustomLogicsReadOnly as ReadOnlyDictionary to avoid per-access ToDictionary allocation"
```

---

### Task 12: SAIN -- 将 SAINNoBushESP 从 MonoBehaviour 改为 IBotClass [BUG-4]

**Files:**
- Modify: `Components/SAINNoBushESP.cs` (全文重写)
- Modify: `Components/BotComponent.cs:235` (修改创建方式)

- [ ] **Step 1: 重写 SAINNoBushESP 为 IBotClass**

将 `Components/SAINNoBushESP.cs` 全文替换为：

```csharp
using EFT;
using HarmonyLib;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SAIN.Components
{
    public class SAINNoBushESP : BotBase, IBotClass
    {
        static SAINNoBushESP()
        {
            Type botType = typeof(BotOwner);
            Type memoryType = AccessTools.Field(botType, "Memory").FieldType;
            GoalEnemyProp = AccessTools.Property(memoryType, "GoalEnemy");
            IsVisibleProp = AccessTools.Property(GoalEnemyProp.PropertyType, "IsVisible");
            Type shootDataType = AccessTools.Property(botType, "ShootData").PropertyType;
            CanShootByState = AccessTools.PropertySetter(shootDataType, "CanShootByState");
        }

        private static readonly PropertyInfo GoalEnemyProp;
        private static readonly PropertyInfo IsVisibleProp;
        private static readonly MethodInfo CanShootByState;
        private static LayerMask NoBushMask = 0;

        public SAINNoBushESP(BotComponent sain) : base(sain)
        {
            TickRequirement = ESAINTickState.OnlyBotInCombat;
        }

        public override void Init()
        {
            if (NoBushMask == 0)
            {
                NoBushMask = LayerMaskClass.HighPolyWithTerrainMaskAI 
                    | (1 << LayerMask.NameToLayer("PlayerSpiritAura"));
            }
            base.Init();
        }

        public override void ManualUpdate()
        {
            if (!UserToggle) return;

            if (NoBushTimer < Time.time)
            {
                NoBushTimer = Time.time + Frequency;
                bool active = CheckNoBushESP();
                ApplyNoBushESP(active);
            }
        }

        // ESAINTickState 由构造函数设置
        public bool CanEverTick => true;
        public float TickInterval => Frequency;
        public float LastTickTime { get; set; }

        private bool NoBushESPActive = false;
        private float NoBushTimer = 0f;
        private Vector3 HeadPosition => BotOwner.LookSensor._headPoint;

        private static NoBushESPSettings Settings => SAINPlugin.LoadedPreset?.GlobalSettings?.Look?.NoBushESP;
        private static bool UserToggle => Settings?.NoBushESPToggle ?? false;
        private static bool EnhancedChecks => Settings?.NoBushESPEnhanced ?? false;
        private static float EnhancedRatio => Settings?.NoBushESPEnhancedRatio ?? 0.5f;
        private static float Frequency => Settings?.NoBushESPFrequency ?? 0.1f;
        private static bool DebugMode => Settings?.NoBushESPDebugMode ?? false;

        public bool CheckNoBushESP()
        {
            Enemy sainEnemy = Bot?.GoalEnemy;
            var enemy = sainEnemy?.EnemyInfo ?? BotOwner?.Memory?.GoalEnemy;
            if (enemy != null && (enemy.IsVisible || enemy.CanShoot))
            {
                IPlayer person = enemy.Person;
                if (person != null && !person.IsAI)
                {
                    return EnhancedChecks ? CheckEnhanced(person) : CheckSimple(person);
                }
            }
            return false;
        }

        private bool CheckSimple(IPlayer player)
        {
            Vector3 partPos = player.MainParts[BodyPartType.body].Position;
            return RayCast(partPos, HeadPosition);
        }

        private bool CheckEnhanced(IPlayer player)
        {
            int hitCount = 0;
            int partCount = player.MainParts.Count;
            Vector3 start = HeadPosition;
            foreach (var part in player.MainParts)
            {
                if (RayCast(part.Value.Position, start)) hitCount++;
            }
            float ratio = (float)hitCount / partCount;
            return ratio >= EnhancedRatio;
        }

        private static bool RayCast(Vector3 end, Vector3 start)
        {
            Vector3 direction = end - start;
            if (Physics.Raycast(start, direction.normalized, out var hit, direction.magnitude, NoBushMask))
            {
                GameObject hitObject = hit.transform?.parent?.gameObject;
                if (hitObject != null)
                {
                    string hitName = hitObject.name?.ToLower();
                    foreach (string exclusion in ExclusionList)
                    {
                        if (hitName.Contains(exclusion)) return true;
                    }
                }
            }
            return false;
        }

        public void ApplyNoBushESP(bool blockShoot)
        {
            NoBushESPActive = blockShoot;
            if (!blockShoot) return;

            var enemy = BotOwner?.Memory?.GoalEnemy;
            if (enemy != null)
            {
                enemy.SetCanShoot(false);
                enemy.SetVisible(false);

                if (BotOwner.AimingManager.CurrentAiming is BotAimingClass aimData 
                    && aimData.aimStatus_0 != AimStatus.NoTarget)
                {
                    aimData.aimStatus_0 = AimStatus.NoTarget;
                }

                var vision = Bot?.EnemyController.GetEnemy(enemy.ProfileId, false)?.Vision;
                if (vision != null)
                {
                    vision.UpdateVisibleState(Time.time, true);
                }
            }
        }

        private static readonly List<string> ExclusionList = new() 
        { 
            "filbert", "fibert", "tree", "pine", "plant", "birch", "collider", 
            "timber", "spruce", "bush", "metal", "wood", "grass" 
        };

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
```

- [ ] **Step 2: 修改 BotComponent.CreateClasses**

在 `Components/BotComponent.cs` 第 235 行附近，将：
```csharp
NoBushESP = gameObject.AddComponent<SAINNoBushESP>();
```
改为：
```csharp
NoBushESP = new SAINNoBushESP(this);
```

同时在 `BotComponent.Dispose` 中移除 `Destroy(NoBushESP)` 调用（因为不再是 UnityEngine.Object）。

- [ ] **Step 3: 编译验证**

Run: `dotnet build SAIN.sln`
Expected: 0 errors.

- [ ] **Step 4: Commit**

```bash
git add Components/SAINNoBushESP.cs Components/BotComponent.cs
git commit -m "refactor(sain): convert SAINNoBushESP from MonoBehaviour to IBotClass to eliminate per-bot Unity overhead"
```

---

### Task 13: SAIN -- Bot 错峰更新 [PERF-4]

**Files:**
- Modify: `Components/BotManagerComponent.cs:86-98`

- [ ] **Step 1: 实现错峰更新**

在 `BotManagerComponent` 类中添加字段：
```csharp
private int _batchUpdateIndex = 0;
private const int BATCH_SIZE = 8;
```

将 `ManualUpdate` 方法中的 Bot 遍历逻辑改为：

```csharp
public void ManualUpdate(float currentTime, float deltaTime)
{
    BotSpawnController.ManualUpdate(currentTime, deltaTime);
    BotExtractManager.Update(currentTime, deltaTime);
    TimeVision.Update(currentTime, deltaTime);
    WeatherVision.Update(currentTime, deltaTime);
    BotSquads.Update(currentTime, deltaTime);

    // 有敌人的 Bot 每帧更新 (关键)
    HashSet<BotComponent> bots = BotSpawnController.SAINBots;
    foreach (BotComponent bot in bots)
    {
        if (bot != null && bot.HasEnemy)
            bot.ManualUpdate(currentTime, deltaTime);
    }

    // 无敌人的 Bot 错峰更新 (分摊到多帧)
    var idleBots = new List<BotComponent>();
    foreach (BotComponent bot in bots)
    {
        if (bot != null && !bot.HasEnemy)
            idleBots.Add(bot);
    }

    if (idleBots.Count > 0)
    {
        int start = _batchUpdateIndex % Math.Max(1, (idleBots.Count + BATCH_SIZE - 1) / BATCH_SIZE) * BATCH_SIZE;
        int end = Math.Min(start + BATCH_SIZE, idleBots.Count);
        for (int i = start; i < end; i++)
            idleBots[i].ManualUpdate(currentTime, deltaTime);
        _batchUpdateIndex++;
        if (_batchUpdateIndex >= int.MaxValue / 2) _batchUpdateIndex = 0;
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Components/BotManagerComponent.cs
git commit -m "perf(sain): stagger idle bot updates across frames to reduce single-frame CPU spike"
```

---

### Task 14: BigBrain -- RemoveLayerForBot 批量反射优化 [BB-5]

**Files:**
- Modify: `前置MOD的源代码/SPT-BigBrain-1.3.2/Internal/BrainHelpers.cs:41-123`

- [ ] **Step 1: 批量版本重构**

添加内部方法，复用字典引用避免每层重复反射获取。在 `RemoveAllExcludedLayers` 方法中一次性获取字典：

```csharp
internal static void RemoveAllExcludedLayers(this BotOwner botOwner)
{
    if (botOwner == null)
        throw new ArgumentNullException(nameof(botOwner));

    // 一次性获取字典（仅一次反射调用）
    var botBrainLayerDictionary = botOwner.Brain.BaseBrain.GetBrainLayerDictionary();

    foreach (BrainManager.ExcludeLayerInfo excludeLayer in BrainManager.Instance.ExcludeLayers)
    {
        if (!excludeLayer.AffectsBot(botOwner)) continue;
        botOwner.RemoveLayerForBotInternal(excludeLayer.excludeLayerName, botBrainLayerDictionary);
    }
}

private static void RemoveLayerForBotInternal(this BotOwner botOwner, string layerName, 
    Dictionary<int, AICoreLogicLayerClass> botBrainLayerDictionary)
{
    if (layerName == null) throw new ArgumentNullException(nameof(layerName));

    int layerIndexToRemove = -1;
    foreach (int index in botBrainLayerDictionary.Keys)
    {
        if (botBrainLayerDictionary[index].Name() != layerName) continue;

        layerIndexToRemove = index;
        botOwner.Brain.BaseBrain.method_3(index);
        
        if (botOwner.Brain.BaseBrain.method_2(botBrainLayerDictionary[index]))
            throw new InvalidOperationException($"Could not remove brain layer '{layerName}'");

        BrainManager.Instance.ExcludedLayers.Add(
            new BrainManager.ExcludedLayerInfo(botOwner, botBrainLayerDictionary[index], 
            botOwner.Brain.BaseBrain.ShortName(), index));
        break;
    }

    if (layerIndexToRemove > -1)
        botBrainLayerDictionary.Remove(layerIndexToRemove);
}
```

保留原 `RemoveLayerForBot(this BotOwner, string)` 方法签名但内部委托给 `RemoveLayerForBotInternal`。

- [ ] **Step 2: Commit**

```bash
git add 前置MOD的源代码/SPT-BigBrain-1.3.2/Internal/BrainHelpers.cs
git commit -m "perf(bigbrain): batch RemoveAllExcludedLayers to avoid repeated reflection-based dict lookups"
```

---

## Phase 4：代码质量改进 (4 tasks)

### Task 15: BigBrain -- 修复匿名 lambda 事件泄漏 [BB-4]

**Files:**
- Modify: `前置MOD的源代码/SPT-BigBrain-1.3.2/Patches/BotStandartBotBrainActivatePatch.cs:31`

- [ ] **Step 1: 改为命名方法**

在 Patch 类中添加静态方法：
```csharp
private static void OnBotDead(IPlayer player)
{
    BrainManager.Instance.ActivatedBots.Remove(player);
}
```

将第 31 行：
```csharp
___botOwner_0.GetPlayer.OnPlayerDeadOrUnspawn += (player) => { BrainManager.Instance.ActivatedBots.Remove(player); };
```
改为：
```csharp
___botOwner_0.GetPlayer.OnPlayerDeadOrUnspawn += OnBotDead;
```

- [ ] **Step 2: Commit**

```bash
git add 前置MOD的源代码/SPT-BigBrain-1.3.2/Patches/BotStandartBotBrainActivatePatch.cs
git commit -m "fix(bigbrain): replace anonymous lambda with named method to enable proper event unsubscription"
```

---

### Task 16: BigBrain -- CollectionIntersection 立即 materialize [BB-6]

**Files:**
- Modify: `前置MOD的源代码/SPT-BigBrain-1.3.2/Internal/CollectionIntersection.cs:11-54`

- [ ] **Step 1: 构造函数中 ToList**

将类的构造函数逻辑改为在构造时立即 materialize。将原 `HasCommonAndUniqueElements` 等延迟属性改为构造函数中 `.ToList()` 后存储：

```csharp
public CollectionIntersection(IEnumerable<T> collection1, IEnumerable<T> collection2)
{
    var common = collection1.Intersect(collection2).ToList();
    CommonElements = common;
    UniqueCollection1Elements = collection1.Except(common).ToList();
    UniqueCollection2Elements = collection2.Except(common).ToList();
}
```

将对应的属性从计算的 `IEnumerable<T>` 改为存储的 `IReadOnlyList<T>` 类型。

- [ ] **Step 2: Commit**

```bash
git add 前置MOD的源代码/SPT-BigBrain-1.3.2/Internal/CollectionIntersection.cs
git commit -m "perf(bigbrain): materialize CollectionIntersection results in constructor to avoid repeated LINQ enumeration"
```

---

### Task 17: Waypoints -- mod.ts null 安全 [WP-5]

**Files:**
- Modify: `前置MOD的源代码/SPT-Waypoints-1.7.1/ServerMod/src/mod.ts:11-37`

- [ ] **Step 1: 添加可选链和 Patrol/Mind null 检查**

将整个巡逻参数设置循环替换为：
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

- [ ] **Step 2: 编译验证**

Run: `npm run build` in `前置MOD的源代码/SPT-Waypoints-1.7.1/ServerMod/`
Expected: 0 errors.

- [ ] **Step 3: Commit**

```bash
git add 前置MOD的源代码/SPT-Waypoints-1.7.1/ServerMod/src/mod.ts
git commit -m "fix(waypoints): add optional chaining and null guards to mod.ts bot type iteration"
```

---

### Task 18: Waypoints -- 死代码清理和资源泄漏修复 [WP-6]

**Files:**
- Modify: `前置MOD的源代码/SPT-Waypoints-1.7.1/WaypointsPlugin.cs:63-107` (删除 PerfTimingPatch)
- Modify: `前置MOD的源代码/SPT-Waypoints-1.7.1/Components/NavMeshDebugComponent.cs:79-82` (StreamWriter -> File.WriteAllText)
- Modify: `前置MOD的源代码/SPT-Waypoints-1.7.1/Components/NavMeshDebugComponent.cs:20-25` (Dispose 中添加 Destroy(mesh))

- [ ] **Step 1: 删除 PerfTimingPatch 死代码**

在 `WaypointsPlugin.cs` 中删除第 63-107 行的 `PerfTimingPatch` 类定义（包含所有内嵌类和注释）。

- [ ] **Step 2: 修复 NavMeshDebugComponent StreamWriter 泄漏**

将第 79-82 行：
```csharp
StreamWriter streamWriter = new StreamWriter(meshFilename);
streamWriter.Write(jsonString);
streamWriter.Flush();
streamWriter.Close();
```
改为：
```csharp
File.WriteAllText(meshFilename, jsonString);
```

- [ ] **Step 3: 修复 Mesh 资源未销毁**

在 `NavMeshDebugComponent.Dispose` 方法中（第 20-25 行附近），在 `gameObjects.ForEach(Destroy)` 之前添加：
```csharp
foreach (var go in gameObjects)
{
    var mf = go.GetComponent<MeshFilter>();
    if (mf?.mesh != null) Destroy(mf.mesh);
}
```

- [ ] **Step 4: Commit**

```bash
git add 前置MOD的源代码/SPT-Waypoints-1.7.1/WaypointsPlugin.cs
git add 前置MOD的源代码/SPT-Waypoints-1.7.1/Components/NavMeshDebugComponent.cs
git commit -m "chore(waypoints): remove dead PerfTimingPatch code; fix StreamWriter leak; add mesh cleanup in Dispose"
```

---

## 实施顺序与依赖关系

```
Phase 1 (可并行)
  Task 1 (Waypoints) ──┐
  Task 2 (BigBrain)  ──┤  无相互依赖，可同时执行
  Task 3 (SAIN)      ──┤
  Task 4 (BigBrain)  ──┘

Phase 2 (可并行)
  Task 5 (Waypoints) ──┐
  Task 6 (Waypoints) ──┤
  Task 7 (SAIN)      ──┤  无相互依赖
  Task 8 (Waypoints) ──┤
  Task 9 (SAIN)      ──┘

Phase 3 (Task 12 依赖 Phase 2 完成)
  Task 10 (SAIN)     ──┐
  Task 11 (BigBrain) ──┤  无相互依赖
  Task 12 (SAIN)     ──┤  (Task 12 涉及架构变更，应先确保 Phase 1-2 修复正确)
  Task 13 (SAIN)     ──┤
  Task 14 (BigBrain) ──┘

Phase 4 (可并行)
  Task 15 (BigBrain) ──┐
  Task 16 (BigBrain) ──┤  无相互依赖
  Task 17 (Waypoints) ──┤
  Task 18 (Waypoints) ──┘
```

---

## 验证清单

所有 Phase 完成后执行:

- [ ] **编译验证**: `dotnet build SAIN.sln` + BigBrain.sln + Waypoints.sln 全部通过
- [ ] **TypeScript 编译**: `npm run build` 在 Waypoints ServerMod 目录成功
- [ ] **运行时加载**: 将编译的 dll 放入 SPT `BepInEx/plugins/`，启动游戏确认 F6 GUI 可用
- [ ] **Bot 行为验证**: 进入自定义地图，确认 Bot 正常战斗、移动、寻路
- [ ] **长期稳定性**: 完整一局游戏中观察日志无异常

---

> [VAULT-TEC 备注] 本计划共 18 个任务覆盖 22 项修复。Phase 1 的 4 个任务是关键路径 -- 修复崩溃和逻辑错误。所有任务都包含精确的文件路径、行号和替换代码。执行时建议使用 `@fixer` 子代理并行处理相同 Phase 内无依赖的任务。
>
> Vault-Tec -- Preparing for the Future!
