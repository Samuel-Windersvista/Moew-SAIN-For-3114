using SAIN.Attributes;
using System.Collections.Generic;

namespace SAIN.Preset.GlobalSettings
{
    public class VisionSpeedSettings : SAINSettingsBase<VisionSpeedSettings>, ISAINSettings
    {
        public ElevationVisionSettings Elevation = new();

        public MovementVisibilitySettings Movement = new();

        public PartsVisibilitySettings PartsVisibility = new();

        public PeripheralVisionSettings Peripheral = new();

        public PoseVisibilitySettings Pose = new();

        public ThirdPartySettings ThirdParty = new();

        public override void Init(List<ISAINSettings> list)
        {
            list.Add(this);
            list.Add(Elevation);
            list.Add(Movement);
            list.Add(PartsVisibility);
            list.Add(Peripheral);
            list.Add(Pose);
            list.Add(ThirdParty);
        }
    }

    public class PeripheralVisionSettings : SAINSettingsBase<PeripheralVisionSettings>, ISAINSettings
    {
        public string Description =
            "Adds additional vision speed reduction to targets in a bot's peripheral vision." +
            "Scales with the angle from their look direction.";

        [Name("启用周边视野")]
        public bool Enabled = true;

        [Name("周边视野起始角度")]
        [MinMax(5f, 60f, 1f)]
        [Advanced]
        public float PERIPHERAL_VISION_START_ANGLE = 30;

        [Name("周边视野最大缩减系数")]
        [MinMax(1f, 3f, 100f)]
        [Advanced]
        public float PERIPHERAL_VISION_MAX_REDUCTION_COEF = 2f;
    }

    public class ThirdPartySettings : SAINSettingsBase<ThirdPartySettings>, ISAINSettings
    {
        public string Description =
            "When an enemy is a certain angle away from their active enemies last known position, " +
            "this will reduce their vision speed of that target up to the maximum set amount.";

        [Name("启用第三方视野")]
        public bool Enabled = true;

        [Name("第三方视野起始角度")]
        [MinMax(5f, 60f, 1f)]
        [Advanced]
        public float THIRDPARTY_VISION_START_ANGLE = 30;

        [Name("第三方视野最大系数")]
        [MinMax(1f, 3f, 100f)]
        [Advanced]
        public float THIRDPARTY_VISION_MAX_COEF = 1.5f;
    }

    public class PartsVisibilitySettings : SAINSettingsBase<PartsVisibilitySettings>, ISAINSettings
    {
        public string Description =
            "Scales vision speed based on the number of body parts that are within line of sight to their enemy. " +
            "Only applies to Non-AI targets.";

        [Name("启用部位可见度")]
        public bool Enabled = true;

        [Name("部位可见最大系数")]
        [MinMax(1f, 3f, 100f)]
        [Advanced]
        public float PARTS_VISIBLE_MAX_COEF = 2f;

        [Name("部位可见最小系数")]
        [MinMax(0.25f, 1f, 100f)]
        [Advanced]
        public float PARTS_VISIBLE_MIN_COEF = 0.9f;
    }

    public class MovementVisibilitySettings : SAINSettingsBase<MovementVisibilitySettings>, ISAINSettings
    {
        public string Description =
            "Scales vision speed based on the movement speed of their enemy. " +
            "Faster movement = faster vision speed.";

        [Name("启用移动可见度")]
        public bool Enabled = true;

        [Name("移动视觉修正值")]
        [Description(
            "Bot发现移动中玩家的速度乘以此值。越低=辨识越快。0.66=原来10秒缩短为约6.6秒。")]
        [MinMax(0.01f, 1f, 100f)]
        [Advanced]
        public float MOVEMENT_VISION_MULTIPLIER = 0.5f;
    }

    public class PoseVisibilitySettings : SAINSettingsBase<PoseVisibilitySettings>, ISAINSettings
    {
        public string Description =
            "Scales vision speed based on the pose of their enemy. " +
            "Only applies to Non-AI targets.";

        [Name("启用姿态可见度")]
        public bool Enabled = true;

        [Name("趴下视觉速度系数")]
        [MinMax(1f, 3f, 100f)]
        [Advanced]
        public float PRONE_VISION_SPEED_COEF = 1.75f;

        [Name("蹲下视觉速度系数")]
        [MinMax(1f, 3f, 100f)]
        [Advanced]
        public float DUCK_VISION_SPEED_COEF = 1.25f;
    }
}