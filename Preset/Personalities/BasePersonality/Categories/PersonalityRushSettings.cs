using SAIN.Attributes;
using SAIN.Preset.GlobalSettings;

namespace SAIN.Preset.Personalities
{
    public class PersonalityRushSettings : SAINSettingsBase<PersonalityRushSettings>, ISAINSettings
    {
        [Name("可冲锋正在治疗/换弹/拔雷的敌人")]
        public bool CanRushEnemyReloadHeal = false;

        [Name("可跳角突击")]
        [Description("此个性冲向敌人时能否跳跃出击？")]
        public bool CanJumpCorners = false;

        [Name("跳角突击概率")]
        [Description("Bot可以跳角突击时实际执行的概率。")]
        [Percentage()]
        public float JumpCornerChance = 60f;

        [Name("跳角突击时可连跳")]
        [Description("此Bot能在你身上打出集锦吗？")]
        public bool CanBunnyHop = false;

        [Name("连跳概率")]
        [Description("Bot可以连跳时实际执行的概率。")]
        [Percentage()]
        public float BunnyHopChance = 5f;
    }
}