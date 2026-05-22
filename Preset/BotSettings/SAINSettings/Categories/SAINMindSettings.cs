using SAIN.Attributes;
using SAIN.Preset.GlobalSettings;

namespace SAIN.Preset.BotSettings.SAINSettings.Categories
{
    public class SAINMindSettings : SAINSettingsBase<SAINMindSettings>, ISAINSettings
    {
        [Category("个性")]
        [Name("攻击性乘数")]
        [Description("Bot丢失目标后搜索敌人的速度和谨慎程度。越高=越具攻击性。")]
        [MinMax(0.01f, 3f, 10f)]
        public float Aggression = 1f;

        [Category("武器控制")]
        [Name("武器熟练度")]
        [Description("Bot使用各种武器的能力，影响后坐力、射速和连发长度。越高=越强。")]
        [Percentage01to99]
        public float WeaponProficiency = 0.5f;

        [Name("压制抗性")]
        [Description("值越高受压制影响越小。0=无抗性，1=完全免疫。最终抗性为个性和Bot类型抗性的中点。例如个性0.25+Bot类型0.75=0.5。")]
        [MinMax(0.0f, 1f, 100)]
        public float SuppressionResistance = 0f;

        [Category("说话")]
        [Name("说话频率")]
        [Description("检查Bot是否想说话的频率。越高=说话间隔越长。")]
        [MinMax(0f, 30f)]
        public float TalkFrequency = 1f;

        [Category("说话")]
        [Name("Bot可说话")]
        public bool CanTalk = true;

        [Category("说话")]
        [Name("Bot嘲讽")]
        public bool BotTaunts = true;

        [Category("说话")]
        [Name("小队说话")]
        public bool SquadTalk = true;

        [Category("说话")]
        [Name("小队说话频率。越高=说话间隔越长。")]
        [MinMax(0f, 60f)]
        public float SquadMemberTalkFreq = 3f;

        [Category("说话")]
        [Name("队长说话频率。越高=说话间隔越长。")]
        [MinMax(0f, 60f)]
        public float SquadLeadTalkFreq = 3f;

        [Category("撤离")]
        [Name("启用撤离")]
        public bool EnableExtracts = true;

        [Category("撤离")]
        [Name("撤离前最大战局进度百分比")]
        [Description("Bot决定撤离前的最大可能时间。基于总战局时间计算。60分钟总时间剩6分钟=10%。")]
        [MinMax(0f, 100f)]
        public float MaxExtractPercentage = 30f;

        [Category("撤离")]
        [Name("撤离前最小战局进度百分比")]
        [Description("The longest possible time before this bot can decide to move to extract. Based on total raid timer and time remaining. 60 min total raid time with 6 minutes remaining would be 10 percent")]
        [MinMax(0f, 100f)]
        public float MinExtractPercentage = 5f;

        public override void Apply(BotSettingsComponents settings)
        {
            settings.Mind.UNDER_FIRE_PERIOD = 5f;
            settings.Mind.CHANCE_FUCK_YOU_ON_CONTACT_100 = 0f;
            //settings.Mind.PART_PERCENT_TO_HEAL = 0.6f;
            //settings.Mind.SURGE_KIT_ONLY_SAFE_CONTAINER = false;
            settings.Mind.FOOD_DRINK_DELAY_SEC = 240f;
            settings.Mind.CAN_USE_MEDS = true;
            settings.Mind.CAN_USE_FOOD_DRINK = true;
            settings.Mind.HIT_DELAY_WHEN_PEACE = 0.4f;
            settings.Mind.HIT_DELAY_WHEN_HAVE_SMT = 0.1f;
        }
    }
}