using SAIN.Attributes;
using SAIN.Components.RotationController;
using System.Collections.Generic;

namespace SAIN.Preset.GlobalSettings
{
    public class SteeringSettings : SAINSettingsBase<SteeringSettings>, ISAINSettings
    {
        [Name("最大路径长度")]
        [Description("Bot沿路径向敌人移动时检查视野的距离(米)。")]
        [Category("敌人路径可见度系统")]
        [MinMax(5f, 500f, 1)]
        [Advanced]
        public float MaxPathLengthPathVision = 75.0f;

        [Hidden]
        public float characterHeight = 1.5f;
        [Hidden]
        public float startHeight = 0.25f;

        [Name("路径节点堆叠高度")]
        [Description("每个路径节点上方生成X个检测点。")]
        [Category("敌人路径可见度系统")]
        [MinMax(2f, 6, 1)]
        [Advanced]
        public float GeneratePointStackHeight = 4;

        [Name("节点间距")]
        [Category("敌人路径可见度系统")]
        [MinMax(0.1f, 2, 1000)]
        [Advanced]
        public float DistanceBetweenPoints = 0.66f;

        [Name("随机Bot瞄准摇摆")]
        [Category("随机摇摆")]
        public bool RANDOMSWAY_TOGGLE = true;

        [Category("随机摇摆")]
        [Name("随机摇摆圆半径")]
        [MinMax(0f, 1f, 1000f)]
        [Advanced]
        public float RANDOMSWAY_CIRCLE_RADIUS = 0.02f;

        [Category("随机摇摆")]
        [Name("随机摇摆循环时长")]
        [MinMax(0f, 10f, 10f)]
        [Advanced]
        public float RANDOMSWAY_LOOP_DURATION = 4f;

        [Category("随机摇摆")]
        [Name("随机摇摆圆缩放")]
        [MinMax(0f, 1f, 1000f)]
        [Advanced]
        public float RANDOMSWAY_CIRCLE_SCALE = 0.015f;

        [Name("最小转向俯仰角")]
        [MinMax(-90f, -45f, 100f)]
        [Advanced]
        public float MIN_STEERING_PITCH = -65f;

        [Name("最后看到与最后已知位置距离阈值")]
        [Description("敌人最后已知位置距Bot最后看到位置在此距离内(米)时以最后看到位置为准。")]
        [Advanced]
        [MinMax(0f, 50f, 1000f)]
        public float STEER_LASTSEEN_TO_LASTKNOWN_DISTANCE = 2.5f;

        [Name("平滑转向设置")]
        [Hidden]
        public Dictionary<EBotLookMode, TurnSettings> SMOOTHING_BY_STATE = new() {
            { EBotLookMode.RandomLook, new TurnSettings(0.040f, 120f ) },
            { EBotLookMode.Peace, new TurnSettings(0.050f, 240) },
            { EBotLookMode.Combat, new TurnSettings(0.07f, 300f) },
            { EBotLookMode.CombatSprint, new TurnSettings(0.075f, 300f) },
            { EBotLookMode.CombatVisibleEnemy, new TurnSettings(0.090f, 360f) },
            { EBotLookMode.Aiming, new TurnSettings(0.10f, 360f ) },
        };

        [Name("收敛加速")]
        [MinMax(1f, 3f, 1000f)]
        [Advanced]
        public float ConvergenceBoost = 1.1f;   // Multiplier when far from target

        [Name("路径视野每任务最小命令数")]
        [Advanced]
        [MinMax(16, 2048, 1f)]
        public float PathVisionMinCommandsPerJob = 256f; // Minimum commands per job for path vision jobs

        public override void Init(List<ISAINSettings> list)
        {
            list.Add(this);
        }
    }
}