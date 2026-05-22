using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings
{
    public class ExtractSettings : SAINSettingsBase<ExtractSettings>, ISAINSettings
    {
        [Name("SAIN撤离行为")]
        [Description("需重启游戏。禁用原版Bot撤离行为改用SAIN决策系统。")]
        public bool SAIN_EXTRACT_TOGGLE = false;
    }
}