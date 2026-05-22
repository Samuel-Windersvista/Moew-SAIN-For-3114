using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings
{
    public class LightNVGSettings : SAINSettingsBase<LightNVGSettings>, ISAINSettings
    {
        [Name("手电筒开启能见度阈值")]
        [Description("能见度<=此比例时Bot会想打开手电筒。")]
        [Advanced]
        [MinMax(0.01f, 0.99f, 100f)]
        public float LightOnRatio = 0.33f;

        [Name("手电筒关闭能见度阈值")]
        [Description("Bot夜间开手电筒时若能见度>=此比例会想关掉。")]
        [Advanced]
        [MinMax(0.01f, 0.99f, 100f)]
        public float LightOffRatio = 0.66f;

        [Name("夜视仪开启能见度阈值")]
        [Description("能见度<=此比例时Bot会想打开夜视仪。")]
        [Advanced]
        [MinMax(0.01f, 0.99f, 100f)]
        public float NightVisionOnRatio = 0.33f;

        [Name("夜视仪关闭能见度阈值")]
        [Description("Bot夜间开夜视仪时若能见度>=此比例会想关掉。")]
        [Advanced]
        [MinMax(0.01f, 0.99f, 100f)]
        public float NightVisionOffRatio = 0.66f;
    }
}