using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings
{
    public class VanillaBotSettings : SAINSettingsBase<VanillaBotSettings>, ISAINSettings
    {
        [Name("原版Scav")]
        [Description("需重启游戏。非玩家Scav使用原版AI行为。")]
        public bool VanillaScavs = false;

        [Name("原版Boss")]
        [Description("需重启游戏。除Goon小队外的Boss使用原版AI行为。")]
        public bool VanillaBosses = false;

        [Name("原版Boss随从")]
        [Description("需重启游戏。除Goon小队外的Boss随从使用原版AI行为。")]
        public bool VanillaFollowers = false;

        [Name("原版Goon小队")]
        [Description("需重启游戏。Goon小队使用原版行为。禁用特别定制的个性编辑。")]
        public bool VanillaGoons = false;

        [Name("原版Bloodhounds")]
        [Description("需重启游戏。")]
        public bool VanillaBloodHounds = false;

        [Name("原版Rogue")]
        [Description("需重启游戏。")]
        public bool VanillaRogues = false;

        [Name("原版邪教徒")]
        [Description("需重启游戏。")]
        public bool VanillaCultists = false;
    }
}