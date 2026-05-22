using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings
{
    public class JokeSettings : SAINSettingsBase<JokeSettings>, ISAINSettings
    {
        [Name("随机作弊AI")]
        [Description("模拟线上体验！1%Bot为作弊者：跑得快无后坐力完美瞄准，全自动武器任何距离全自动，半自动武器最快射速。")]
        public bool RandomCheaters = false;

        [Name("随机加速挂概率")]
        [Description("启用随机作弊AI时被分配为加速挂的概率。")]
        [Percentage]
        public float RandomCheaterChance = 1f;
    }

}