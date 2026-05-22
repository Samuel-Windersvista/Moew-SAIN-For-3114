using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings
{
    public class CoverSettings : SAINSettingsBase<CoverSettings>, ISAINSettings
    {
        [Name("最大掩体路径长度")]
        [Description("Bot搜索掩体时的最大路径长度。")]
        [MinMax(20f, 100f, 10f)]
        [Advanced]
        public float MaxCoverPathLength = 60f;

        [Name("切换掩体决策时间")]
        [Description("Bot在掩体间切换的决策间隔时间(秒)。")]
        [MinMax(1f, 30f, 1f)]
        [Advanced]
        public float ShiftCoverChangeDecisionTime = 6f;

        [Name("切换掩体-上次看到敌人时间")]
        [Description("自上次看到敌人以来需经过多久才能切换掩体(秒)。")]
        [MinMax(2f, 120f, 1f)]
        [Advanced]
        public float ShiftCoverTimeSinceSeen = 30f;

        [Name("切换掩体-敌人创建时间")]
        [Description("敌人创建后需经过多久才能切换掩体(秒)。")]
        [MinMax(1f, 120f, 1f)]
        [Advanced]
        public float ShiftCoverTimeSinceEnemyCreated = 30f;

        [Name("切换掩体-无敌人重置时间")]
        [Description("无敌人时重置掩体切换的等待时间(秒)。")]
        [MinMax(1f, 120f, 1f)]
        [Advanced]
        public float ShiftCoverNoEnemyResetTime = 10f;

        [Name("切换掩体-新掩体时间")]
        [Description("切换到新掩体需要的时间(秒)。")]
        [MinMax(1f, 120f, 1f)]
        [Advanced]
        public float ShiftCoverNewCoverTime = 10f;

        [Name("切换掩体-重置时间")]
        [Description("切换掩体的重置冷却时间(秒)。")]
        [MinMax(1f, 120f, 1f)]
        [Advanced]
        public float ShiftCoverResetTime = 10f;

        [Name("掩体最小高度")]
        [Description("掩体需达到的最低高度(米)。")]
        [MinMax(0.5f, 1.5f, 100f)]
        [Advanced]
        public float CoverMinHeight = 0.75f;

        [Name("掩体-与敌人最小距离")]
        [Description("掩体与敌人之间的最小距离(米)。")]
        [MinMax(0f, 30f, 1f)]
        [Advanced]
        public float CoverMinEnemyDistance = 8f;

        [Name("调试掩体搜索器")]
        [Description("启用掩体搜索调试模式。")]
        [Advanced]
        public bool DebugCoverFinder = false;
    }
}