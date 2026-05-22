using SAIN.Attributes;
using SAIN.Preset.GlobalSettings;

namespace SAIN.Preset.Personalities
{
    public class PersonalitySearchSettings : SAINSettingsBase<PersonalitySearchSettings>, ISAINSettings
    {
        [Name("会搜索敌人")]
        [Advanced]
        public bool WillSearchForEnemy = true;

        [Name("会通过声音搜索")]
        [Advanced]
        public bool WillSearchFromAudio = true;

        [Name("和平状态听到敌人后的行为")]
        [Description("Bot听到敌人动静且之前处于和平状态(无目标、巡逻中)时的反应。")]
        public EHeardFromPeaceBehavior HeardFromPeaceBehavior = EHeardFromPeaceBehavior.Freeze;

        [Name("会追赶远距离枪声")]
        [Description("此个性是否会听到并与非针对自己的远距离枪声交战？")]
        public bool WillChaseDistantGunshots = true;

        [Name("忽略远距离声音阈值")]
        [Description("声音超过此距离视为追逐远距离枪声。若追逐关闭则忽略(除非向自己射击)。")]
        public float AudioStraightDistanceToIgnore = 100f;

        [Name("开始搜索基础时间")]
        [Description("修正前的基础时间，此个性通常开始搜索敌人的时间。")]
        [MinMax(0.1f, 500f)]
        public float SearchBaseTime = 40;

        [Name("搜索等待乘数")]
        [Description("线性增减Bot搜索时暂停等待的时间。")]
        [MinMax(0.01f, 5f, 100)]
        public float SearchWaitMultiplier = 1f;

        [Name("搜索时冲刺概率")]
        [Percentage]
        public float SprintWhileSearchChance = 25f;

        [Name("潜行模式")]
        [Advanced]
        public bool Sneaky = false;

        [Name("潜行速度")]
        [Percentage0to1]
        [Advanced]
        public float SneakySpeed = 1f;

        [Name("潜行姿态")]
        [Percentage0to1]
        [Advanced]
        public float SneakyPose = 1f;
        
        [Name("转角减速")]
        [Advanced]
        public bool SlowAtCorners = true;
    }
}