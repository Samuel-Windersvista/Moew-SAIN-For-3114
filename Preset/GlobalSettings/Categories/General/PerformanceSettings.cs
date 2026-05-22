using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings
{
    public class PerformanceSettings : SAINSettingsBase<PerformanceSettings>, ISAINSettings
    {
        [Name("性能模式")]
        [Description("限制掩体搜索以最大化性能。降低部分射线检测频率。如果CPU受限，可挽回SAIN消耗的部分帧率。可能导致Bot寻找掩体时间过长。")]
        public bool PerformanceMode = false;
    }
}