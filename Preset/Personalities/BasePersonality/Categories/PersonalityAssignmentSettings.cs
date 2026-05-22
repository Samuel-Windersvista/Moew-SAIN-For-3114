using EFT;
using Newtonsoft.Json;
using SAIN.Attributes;
using SAIN.Preset.GlobalSettings;
using System.Collections.Generic;

namespace SAIN.Preset.Personalities
{
    public class PersonalityAssignmentSettings : SAINSettingsBase<PersonalityAssignmentSettings>, ISAINSettings
    {
        [JsonIgnore]
        [Hidden]
        private const string PowerLevelDescription = " Power level is a combined number that takes into account " +
            "armor, the class of that armor, " +
            "the attachments a bot has on their weapon, " +
            "whether they have a faceshield, " +
            "and the weapon class that is currently used by a bot." +
            " Power Level usually falls within 0 to 250 on average, and almost never goes above 500";

        [Name("启用该个性")]
        [Description("启用或禁用该个性。若全局设置中启用了全Chad/全GigaChad/全Rat则忽略。")]
        public bool Enabled = true;

        [NameAndDescription("可被随机分配", "该个性可无视Bot属性、战斗力、玩家等级等条件随机分配给任何Bot的百分比概率。")]
        public bool CanBeRandomlyAssigned = true;

        [NameAndDescription("随机分配概率", "如果该个性可被随机分配，此为其实际发生的概率。")]
        [MinMax(0, 100)]
        public float RandomlyAssignedChance = 3;

        [NameAndDescription("最低等级", "Bot有资格获得此个性的最低等级。")]
        [Percentage]
        public float MinLevel = 0;

        [NameAndDescription("最高等级", "Bot有资格获得此个性的最高等级。")]
        [Percentage]
        public float MaxLevel = 100;

        [Name("战斗力缩放起点")]
        [Description("Bot战斗力>=此值时开始有概率被分配此个性。")]
        [MinMax(0, 1000, 1)]
        public float PowerLevelScaleStart = 0f;

        [Name("战斗力缩放终点")]
        [Description("Bot战斗力>=此值时获得完整百分比分配概率。")]
        [MinMax(0, 1000, 1)]
        public float PowerLevelScaleEnd = 500f;

        [Name("反向缩放")]
        [Description("战斗力越低，被分配概率越高。")]
        public bool InverseScale = false;

        [NameAndDescription("战斗力最低值", "Bot使用此个性的最低战斗力要求。" + PowerLevelDescription)]
        [MinMax(0, 1000, 1)]
        public float PowerLevelMin = 0;

        [NameAndDescription("战斗力最高值", "Bot使用此个性的最高战斗力范围。" + PowerLevelDescription)]
        [MinMax(0, 1000, 1)]
        public float PowerLevelMax = 800;

        [Name("满足条件时的最大概率")]
        [Description("If the bot meets all conditions for this personality, this is the chance the personality will actually be assigned. " +
            "The percentage chance to be assigned scales if a bots power level falls between Power Level Scale Start and Power Level Scale End, " +
            "so if they fall right in the middle, and the value here is 60%, they will have a 30% chance to be assigned.")]
        [MinMax(0, 100, 1)]
        public float MaxChanceIfMeetRequirements = 50;

        [Name("允许的Bot类型")]
        [Advanced]
        [Hidden]
        public List<WildSpawnType> AllowedTypes = new();
    }
}