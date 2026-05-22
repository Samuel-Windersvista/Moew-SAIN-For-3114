using Newtonsoft.Json;
using SAIN.Attributes;
using SAIN.Preset.GlobalSettings;

namespace SAIN.Preset.Personalities
{
    public class PersonalityCoverSettings : SAINSettingsBase<PersonalityCoverSettings>, ISAINSettings
    {
        [JsonConstructor]
        public PersonalityCoverSettings() { }

        public PersonalityCoverSettings(bool createDefaults)
        {

        }

        [Name("可切换掩体位置")]
        [Description("Bot能否在战斗中切换掩体位置。")]
        [Advanced]
        public bool CanShiftCoverPosition = true;

        [Name("切换掩体时间乘数")]
        [Description("切换掩体所需时间的倍率。")]
        [Advanced]
        public float ShiftCoverTimeMultiplier = 1f;

        [Name("无敌人时移向掩体速度")]
        [Description("无敌人状态下移向掩体的移动速度倍率。")]
        [Percentage0to1]
        [Advanced]
        public float MoveToCoverNoEnemySpeed = 1f;

        [Name("无敌人时移向掩体姿态")]
        [Description("无敌人状态下移向掩体的姿态倍率。")]
        [Percentage0to1]
        [Advanced]
        public float MoveToCoverNoEnemyPose = 1f;

        [Name("有敌人时移向掩体速度")]
        [Description("有敌人状态下移向掩体的移动速度倍率。")]
        [Percentage0to1]
        [Advanced]
        public float MoveToCoverHasEnemySpeed = 1f;

        [Name("有敌人时移向掩体姿态")]
        [Description("有敌人状态下移向掩体的姿态倍率。")]
        [Percentage0to1]
        [Advanced]
        public float MoveToCoverHasEnemyPose = 1f;
    }
}