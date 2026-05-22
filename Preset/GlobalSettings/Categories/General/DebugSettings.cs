using Newtonsoft.Json;
using SAIN.Attributes;
using System.Collections.Generic;

namespace SAIN.Preset.GlobalSettings
{
    public class DebugOverlaySettings : SAINSettingsBase<DebugOverlaySettings>
    {
        public bool Overlay_Info = true;
        public bool Overlay_Info_Expanded = false;
        public bool Overlay_Search = true;
        public bool Overlay_EnemyLists = false;
        public bool Overlay_EnemyInfo = true;
        public bool Overlay_EnemyInfo_Expanded = false;
        public bool Overlay_Decisions = false;
        public bool OverLay_AimInfo = false;
        public bool OverLay_AlwaysShowClosestHumanInfo = false;
        public bool OverLay_AlwaysShowMainPlayerInfo = false;
    }

    public class DebugGizmoSettings : SAINSettingsBase<DebugGizmoSettings>
    {
        [Name("绘制调试Gizmos")]
        public bool DrawDebugGizmos;

        [Name("绘制Transform Gizmos")]
        public bool DrawTransformGizmos;

        [Name("绘制玩家导航网格采样Gizmos")]
        public bool DrawNavMeshSamplingGizmos;

        [Name("绘制视线检查")]
        public bool DrawLineOfSightGizmos;

        [Name("绘制体积光Gizmos")]
        public bool DrawLightGizmos;

        [Name("绘制门链接")]
        public bool DrawDoorLinks;

        [Name("绘制后坐力Gizmos")]
        public bool DebugDrawRecoilGizmos = false;

        [Name("绘制瞄准Gizmos")]
        public bool DebugDrawAimGizmos = false;

        [Name("绘制盲角射线检测")]
        public bool DebugDrawBlindCorner = false;

        [Name("绘制压制点调试")]
        [Hidden]
        public bool DebugDrawProjectionPoints = false;

        [Name("绘制搜索窥视起终点Gizmos")]
        public bool DebugSearchGizmos = false;

        [Name("绘制路径安全测试器调试")]
        [Hidden]
        [JsonIgnore]
        public bool DebugDrawSafePaths = false;

        [Name("路径安全测试器")]
        [Hidden]
        [JsonIgnore]
        public bool DebugEnablePathTester = false;

        [Hidden]
        [JsonIgnore]
        public bool DebugMovementPlan = false;
    }

    public class DebugLogSettings : SAINSettingsBase<DebugLogSettings>
    {
        [Name("全局调试模式")]
        public bool GlobalDebugMode;

        [Name("全局性能分析模式")]
        [Description("启用Unity性能分析器函数采样。")]
        public bool GlobalProfilingToggle;

        [Name("测试Bot冲刺寻路")]
        public bool ForceBotsToRunAround;

        [Name("测试Bot爬行")]
        public bool ForceBotsToTryCrawl;

        [Name("测试手雷投掷")]
        public bool TestGrenadeThrow;

        [Name("绘制调试标签")]
        public bool DrawDebugLabels;

        [Name("调试外部")]
        public bool DebugExternal;

        [Name("调试后坐力计算")]
        public bool DebugRecoilCalculations = false;

        [Name("调试瞄准计算")]
        public bool DebugAimCalculations = false;

        [Name("调试听觉计算结果")]
        public bool DebugHearing = false;

        [Name("调试撤离")]
        public bool DebugExtract = false;

        [Name("收集并导出Bot层与大脑信息")]
        [Hidden]
        [JsonIgnore]
        public bool CollectBotLayerBrainInfo = false;
    }

    public class DebugSettings : SAINSettingsBase<DebugSettings>, ISAINSettings
    {
        public DebugSettings()
        {
            Instance = this;
        }

        public static DebugSettings Instance { get; private set; }

        public DebugLogSettings Logs = new();
        public DebugGizmoSettings Gizmos = new();
        public DebugOverlaySettings Overlay = new();

        public override void Init(List<ISAINSettings> list)
        {
            list.Add(Logs);
            list.Add(Gizmos);
            list.Add(Overlay);
        }
    }
}