using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings
{
    public class LootingBotsSettings : SAINSettingsBase<LootingBotsSettings>, ISAINSettings
    {
        [Name("基于战利品撤离")]
        public bool ExtractFromLoot = true;

        [Name("PMC最低战利品价值")]
        [MinMax(1f, 5000000, 1f)]
        public float MinLootValPMC = 500000;

        [Name("SCAV最低战利品价值")]
        [MinMax(1f, 5000000, 1f)]
        public float MinLootValSCAV = 200000;

        [Name("其他类型最低战利品价值")]
        [MinMax(1f, 5000000, 1f)]
        public float MinLootValOther = 350000;

        [Name("战利品价值例外阈值")]
        [Description("Bot装备价值>=此值时即使背包还有空间也会撤离。")]
        [MinMax(1f, 5000000, 1f)]
        public float MinLootValException = 1500000;
    }
}