using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings
{
    public class NotLookingSettings : SAINSettingsBase<NotLookingSettings>, ISAINSettings
    {
        [Name("Bot反应与精度变化开关-实验性")]
        [Section("Unseen Bot")]
        [Experimental]
        [Description("实验性：未看向Bot方向时Bot精度和视觉速度略微降低。背对Bot被发现并遭射击时会更不准且发现更慢。")]
        public bool NotLookingToggle = true;

        [Name("Bot反应与精度变化时间限制")]
        [Section("Unseen Bot")]
        [Experimental]
        [Description("Bot从背后射击你时散布降低效果最大持续时间。X秒后效果消失给你反击机会。")]
        [MinMax(0.5f, 20f, 100f)]
        [Advanced]
        public float NotLookingTimeLimit = 4f;

        [Name("Bot反应与精度变化角度")]
        [Section("Unseen Bot")]
        [Experimental]
        [Advanced]
        [Description("判定玩家是否看向Bot的最大角度。")]
        [MinMax(5f, 45f, 1f)]
        public float NotLookingAngle = 45f;

        [Name("未在视野内时Bot反应乘数")]
        [Section("Unseen Bot")]
        [Experimental]
        [Description("未看向Bot时他们发现你的视觉速度乘数。越高=反应时间越长。")]
        [MinMax(1f, 2f, 100f)]
        [Advanced]
        public float NotLookingVisionSpeedModifier = 1.1f;

        [Name("未在视野内时Bot精度与散布增量")]
        [Section("Unseen Bot")]
        [Experimental]
        [Description("玩家未看向Bot时Bot瞄准增加的额外随机散布。1=在原瞄准点周围1米球体内随机。越高=散布越大精度越低。")]
        [MinMax(0.1f, 1.5f, 100f)]
        [Advanced]
        public float NotLookingAccuracyAmount = 0.33f;
    }
}