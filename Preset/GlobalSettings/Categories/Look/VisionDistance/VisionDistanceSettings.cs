using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings
{
    public class VisionDistanceSettings : SAINSettingsBase<VisionDistanceSettings>, ISAINSettings
    {
        [Name("移动视觉距离修正值")]
        [Description(
            "Bot发现移动中玩家的距离乘以此值。越高=越远。1.75=最大速度时看到的距离1.75倍。随玩家速度缩放。")]
        [MinMax(1f, 3f, 100f)]
        public float MovementDistanceModifier = 1.5f;
    }
}