using SAIN.Attributes;
using SAIN.Preset.GlobalSettings;

namespace SAIN.Preset.Personalities
{
    public class PersonalityTalkSettings : SAINSettingsBase<PersonalityTalkSettings>, ISAINSettings
    {
        [Name("可嘲讽叫骂")]
        [Description("嘿你...对就是你！听到了吗？")]
        public bool CanTaunt = false;

        [Name("可频繁嘲讽叫骂")]
        [Description("嘿混蛋！")]
        public bool FrequentTaunt = false;

        [Name("可不停嘲讽叫骂")]
        [Description("啊啊啊啊啊啊啊啊啊啊啊啊啊")]
        public bool ConstantTaunt = false;

        [Name("可回应敌人语音")]
        [Description("此个性是否会回应敌人的嘲讽叫骂。")]
        public bool CanRespondToEnemyVoice = true;

        [Name("嘲讽频率")]
        [Advanced]
        [MinMax(0.1f, 100f, 100f)]
        public float TauntFrequency = 15f;

        [Name("嘲讽概率")]
        [Advanced]
        [MinMax(0f, 100f, 1f)]
        public float TauntChance = 50f;

        [Name("嘲讽最大距离")]
        [Advanced]
        [MinMax(0.1f, 150f, 100f)]
        public float TauntMaxDistance = 50f;

        [Name("可假装死亡(稀有)")]
        [Advanced]
        public bool CanFakeDeathRare = false;

        [Name("假装死亡概率")]
        [Advanced]
        public float FakeDeathChance = 2f;

        [Name("可求饶")]
        [Advanced]
        public bool CanBegForLife = false;
    }
}