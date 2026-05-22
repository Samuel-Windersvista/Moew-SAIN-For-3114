using Newtonsoft.Json;
using SAIN.Attributes;
using SAIN.Preset.GlobalSettings;

namespace SAIN.Preset.BotSettings.SAINSettings.Categories
{
    public class SAINAimingSettings : SAINSettingsBase<SAINAimingSettings>, ISAINSettings
    {
        [Category("瞄准目标")]
        [Name("始终瞄准躯干中央")]
        [Description("强制此Bot类型瞄准躯干中央。")]
        public bool AimCenterMass = true;

        [Name("可爆头瞄准")]
        [Category("瞄准目标")]
        public bool AimForHead = false;

        [Category("瞄准目标")]
        [Name("爆头瞄准-百分比概率")]
        [Percentage]
        public float AimForHeadChance = 33f;

        [Category("瞄准时间")]
        [Name("距离瞄准时间乘数")]
        [Description("根据距离乘Bot瞄准时间。越高=越远瞄准越慢。")]
        [MinMax(0.1f, 5f, 100f)]
        public float DistanceAimTimeMultiplier = 1f;

        [Category("瞄准时间")]
        [Name("角度瞄准时间乘数")]
        [Description("根据所需转角乘Bot瞄准时间。越高=转向越大瞄准越慢。")]
        [MinMax(0.1f, 5f, 100f)]
        public float AngleAimTimeMultiplier = 1f;

        [Category("瞄准时间")]
        [Name("快速近战反应")]
        [Description("设置此Bot在近距离是否拥有更快反应和瞄准速度。")]
        public bool FasterCQBReactions = true;

        [Category("瞄准时间")]
        [Name("快速近战反应最大距离")]
        [Description("Max distance a bot can react faster for Faster CQB Reactions. Scales with distance." +
            "Example: If Max distance is set to 20 meters, and an enemy is 10 meters away. they will react 2x as fast as usual, " +
            "or if an enemy is 15 meters away, they will react 1.5x as fast as usual. " +
            "If the enemy is at 20 meters or further, nothing will happen.")]
        [NameAndDescription(
            "Faster CQB Reactions Max Distance",
            "Max distance a bot can react faster for Faster CQB Reactions. Scales with distance.")]
        [MinMax(5f, 100f)]
        public float FasterCQBReactionsDistance = 30f;

        [Category("瞄准时间")]
        [Name("快速近战反应最低速度")]
        [Description("Bot反应并射击的绝对最低速度(秒)。")]
        [MinMax(0.05f, 0.75f, 100f)]
        public float FasterCQBReactionsMinimum = 0.33f;

        //[Name("Accuracy Spread Multiplier")]
        //[Description("Higher = less accurate. Modifies a bot's base accuracy and spread. 1.5 = 1.5x higher accuracy spread")]
        //[MinMax(0.1f, 10f, 10f)]
        //public float AccuracySpreadMulti = 1f;

        [Category("瞄准时间")]
        [Name("瞄准时间最大提升值")]
        [Description("原版EFT配置值：越低=越好。Bot随时间提升瞄准精度的上限。0.25=瞄准偏移乘0.25倍。")]
        [MinMax(0.01f, 0.99f, 100f)]
        [Advanced]
        [CopyValue]
        public float MAX_AIMING_UPGRADE_BY_TIME = 0.25f;

        [Category("散布修正")]
        [Name("无视散布的距离")]
        [Description("原版EFT配置值：敌人距离小于此值则忽略散布。")]
        [MinMax(0.1f, 30f, 100f)]
        [Advanced]
        public float DIST_TO_SHOOT_NO_OFFSET = 3f;

        [Category("散布修正")]
        [Name("散布乘数-移动中(原版EFT)")]
        [MinMax(0.1f, 6f, 100f)]
        [Advanced]
        public float COEF_IF_MOVE = 1.5f;

        [Category("瞄准时间")]
        [Name("瞄准时间乘数-移动中(原版EFT)")]
        [Hidden]
        [JsonIgnore]
        public float TIME_COEF_IF_MOVE = 1.5f;

        [Category("瞄准时间")]
        [Name("最大瞄准时间")]
        [Description("原版EFT配置值：Bot完成瞄准并开始射击的最大时间上限。")]
        [MinMax(0.01f, 4f, 1000f)]
        [Advanced]
        [CopyValue]
        public float MAX_AIM_TIME = 2f;

        [Hidden]
        [JsonIgnore]
        public int AIMING_TYPE = 1;

        //[Name("Friendly Fire Spherecast Size")]
        //[Description("")]
        //[MinMax(0f, 0.5f, 100f)]
        //[Advanced]
        //public float SHPERE_FRIENDY_FIRE_SIZE = 0.15f;

        [Hidden]
        public float DAMAGE_TO_DISCARD_AIM_0_100 = 100;

        [Category("瞄准时间")]
        [NameAndDescription(
            "VANILLA EFT CONFIG VALUE : Hit Reaction Recovery Time",
            "How much time it takes to recover a bot's aim when they get hit by a bullet")]
        [MinMax(0.1f, 0.99f, 100f)]
        [Advanced]
        public float BASE_HIT_AFFECTION_DELAY_SEC = 0.65f;

        [Category("瞄准时间")]
        [Name("中弹瞄准时间惩罚-最小值(原版EFT)")]
        [MinMax(0f, 1f, 100f)]
        [Advanced]
        public float MIN_TIME_DISCARD_AIM_SEC = 0.5f;

        [Category("瞄准时间")]
        [Name("中弹瞄准时间惩罚-最大值(原版EFT)")]
        [MinMax(0f, 2f, 100f)]
        [Advanced]
        public float MAX_TIME_DISCARD_AIM_SEC = 1.5f;

        [Hidden]
        [JsonIgnore]
        public float ANY_PART_SHOOT_TIME = 2f;

        [Category("瞄准时间")]
        [Name("首次接敌反应延迟(原版EFT)")]
        [MinMax(0f, 1f, 100f)]
        [Advanced]
        public float FIRST_CONTACT_ADD_SEC = 0.2f;

        [Hidden]
        [JsonIgnore]
        public float FIRST_CONTACT_ADD_CHANCE_100 = 100f;

        [Hidden]
        [JsonIgnore]
        public float OFFSET_RECAL_ANYWAY_TIME = 30f;

        //[Hidden]
        //[JsonIgnore]
        //public float RECALC_SQR_DIST = 2f * 2f;
        public override void Apply(BotSettingsComponents settings)
        {
            settings.Aiming.MAX_AIMING_UPGRADE_BY_TIME = MAX_AIMING_UPGRADE_BY_TIME;
            settings.Aiming.DIST_TO_SHOOT_NO_OFFSET = DIST_TO_SHOOT_NO_OFFSET;
            settings.Aiming.COEF_IF_MOVE = COEF_IF_MOVE;
            settings.Aiming.TIME_COEF_IF_MOVE = TIME_COEF_IF_MOVE;
            settings.Aiming.MAX_AIM_TIME = MAX_AIM_TIME;
            settings.Aiming.AIMING_TYPE = AIMING_TYPE;
            settings.Aiming.DAMAGE_TO_DISCARD_AIM_0_100 = DAMAGE_TO_DISCARD_AIM_0_100;
            settings.Aiming.BASE_HIT_AFFECTION_DELAY_SEC = BASE_HIT_AFFECTION_DELAY_SEC;
            settings.Aiming.MIN_TIME_DISCARD_AIM_SEC = MIN_TIME_DISCARD_AIM_SEC;
            settings.Aiming.MAX_TIME_DISCARD_AIM_SEC = MAX_TIME_DISCARD_AIM_SEC;
            settings.Aiming.ANY_PART_SHOOT_TIME = ANY_PART_SHOOT_TIME;
            settings.Aiming.FIRST_CONTACT_ADD_SEC = FIRST_CONTACT_ADD_SEC;
            settings.Aiming.FIRST_CONTACT_ADD_CHANCE_100 = FIRST_CONTACT_ADD_CHANCE_100;
            settings.Aiming.OFFSET_RECAL_ANYWAY_TIME = OFFSET_RECAL_ANYWAY_TIME;
        }
    }
}