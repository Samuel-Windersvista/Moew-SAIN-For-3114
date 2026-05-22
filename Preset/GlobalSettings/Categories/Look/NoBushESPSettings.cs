using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings
{
    public class NoBushESPSettings : SAINSettingsBase<NoBushESPSettings>, ISAINSettings
    {
        [Name("防草丛透视")]
        [Description("为Bot添加额外视觉检查防止透过植被看到或射中玩家。")]
        public bool NoBushESPToggle = true;

        [Name("防草丛透视增强射线")]
        [Description("实验性：提高精度并增加额外检查。")]
        public bool NoBushESPEnhanced = false;

        [Name("防草丛透视增强射线频率/秒")]
        [Description("实验性：检查植被遮挡视野的频率。")]
        [MinMax(0f, 1f, 100f)]
        [Advanced]
        public float NoBushESPFrequency = 0.1f;

        [Name("防草丛透视增强射线比率")]
        [Description("实验性：提高精度。设置可见/不可见身体部位比例。0.75=75%部位可见才不被阻挡。")]
        [MinMax(0.1f, 1f, 100f)]
        [Advanced]
        public float NoBushESPEnhancedRatio = 0.75f;

        [Name("防草丛透视调试")]
        [Advanced]
        public bool NoBushESPDebugMode = false;
    }
}