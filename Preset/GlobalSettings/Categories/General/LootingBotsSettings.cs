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

        [Name("搜刮时威胁中断")]
        [Description("搜刮中检测到近距离敌人或被压制时自动停止搜刮。默认开启。")]
        [Category("搜刮安全")]
        public bool LOOTING_THREAT_INTERRUPT = true;

        [Name("搜刮前检查掩体")]
        [Description("搜刮前先找安全位置，无掩体且区域不安全时放弃搜刮。默认开启。")]
        [Category("搜刮安全")]
        public bool LOOTING_COVER_CHECK = true;
    }
}