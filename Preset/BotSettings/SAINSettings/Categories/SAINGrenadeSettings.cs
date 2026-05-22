using Newtonsoft.Json;
using SAIN.Attributes;
using SAIN.Preset.GlobalSettings;

namespace SAIN.Preset.BotSettings.SAINSettings.Categories
{
    public class SAINGrenadeSettings : SAINSettingsBase<SAINGrenadeSettings>, ISAINSettings
    {
        public bool CanThrowWhileSprinting = false;

        [NameAndDescription(
            "可向可见敌人投掷",
            "控制Bot是否向可见敌人直接投掷手雷。")]
        public bool CAN_THROW_STRAIGHT_CONTACT = false;

        [NameAndDescription(
            "看到敌人后投掷延迟",
            "自上次看到敌人后需多久才能考虑投掷手雷。")]
        [MinMax(0.0f, 30f, 100f)]
        public float TimeSinceSeenBeforeThrow = 4f;

        [NameAndDescription(
            "下次投掷间隔-最小值",
            "Bot投掷另一颗手雷前需等待的最短时间。")]
        [MinMax(3f, 30f, 100f)]
        public float ThrowGrenadeFrequency = 5f;

        [NameAndDescription(
            "下次投掷间隔-最大值",
            "Bot投掷另一颗手雷前需等待的最短时间。")]
        [MinMax(3f, 60f, 100f)]
        public float ThrowGrenadeFrequency_MAX = 10f;

        [NameAndDescription(
            "投掷目标与友方最小距离",
            "友方距投掷目标多近时Bot停止投掷手雷(米)。")]
        [MinMax(0.01f, 30f, 100f)]
        public float MinFriendlyDistance = 8f;

        [NameAndDescription(
            "敌人距Bot最小距离",
            "敌人距Bot多近时不会尝试投掷手雷(米)。")]
        [MinMax(0.01f, 30f, 100f)]
        public float MinEnemyDistance = 8f;

        [NameAndDescription(
            "手雷投掷散布",
            "Bot投掷目标位置的随机化距离(米)。")]
        [MinMax(0f, 5f, 100f)]
        [CopyValue]
        public float GrenadePrecision = 0.25f;

        [Percentage0to1]
        [Advanced]
        [Hidden]
        [JsonIgnore]
        public float MIN_THROW_DIST_PERCENT_0_1 = 0.5f;

        [Hidden]
        [JsonIgnore]
        public float CHANCE_TO_NOTIFY_ENEMY_GR_100 = 100f;

        [Hidden]
        [JsonIgnore]
        public float DELTA_GRENADE_START_TIME = 0.0f;

        [Hidden]
        [JsonIgnore]
        public int BEWARE_TYPE = 2;

        [Hidden]
        [JsonIgnore]
        public float DELTA_NEXT_ATTEMPT = 4f;

        public override void Apply(BotSettingsComponents settings)
        {
            settings.Grenade.GrenadePrecision = GrenadePrecision;
            settings.Grenade.CAN_THROW_STRAIGHT_CONTACT = CAN_THROW_STRAIGHT_CONTACT;
            settings.Grenade.DELTA_NEXT_ATTEMPT = ThrowGrenadeFrequency;
            settings.Grenade.CHANCE_TO_NOTIFY_ENEMY_GR_100 = 100f;
            settings.Grenade.MIN_THROW_DIST_PERCENT_0_1 = MIN_THROW_DIST_PERCENT_0_1;
            settings.Grenade.MIN_DIST_NOT_TO_THROW = MinEnemyDistance;
            settings.Grenade.DELTA_GRENADE_START_TIME = DELTA_GRENADE_START_TIME;
            settings.Grenade.BEWARE_TYPE = BEWARE_TYPE;
        }
    }
}