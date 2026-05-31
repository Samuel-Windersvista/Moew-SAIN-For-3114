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

        [Name("倍镜影响视觉距离")]
        [Description("无镜AI远距极难发现玩家，有镜AI按倍率扩展有效视距。默认开启。")]
        [Category("倍镜视觉")]
        public bool SCOPE_VISION_ENABLED = true;

        [Name("倍镜最大有效视距")]
        [Description("8倍镜AI的有效视距上限(米)。")]
        [Category("倍镜视觉")]
        [MinMax(100f, 600f, 50f)]
        [Advanced]
        public float SCOPE_MAX_RANGE = 400f;

        [Name("武器轮廓影响视觉隐蔽")]
        [Description("长枪管/重型武器让AI在远距离更容易发现你。默认开启。")]
        [Category("武器暴露")]
        public bool WEAPON_VISUAL_STEALTH_ENABLED = true;
    }
}