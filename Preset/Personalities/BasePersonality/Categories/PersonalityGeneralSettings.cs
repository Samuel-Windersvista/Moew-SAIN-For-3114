using SAIN.Attributes;
using SAIN.Preset.GlobalSettings;

namespace SAIN.Preset.Personalities
{
    public class PersonalityGeneralSettings : SAINSettingsBase<PersonalityGeneralSettings>, ISAINSettings
    {
        [Name("攻击性乘数")]
        [Description("线性增减搜索时间和原地坚守时间。")]
        [MinMax(0.01f, 5f, 100)]
        public float AggressionMultiplier = 1f;

        [Name("踹开所有可能的门")]
        [Description("此个性Bot有敌人时只要可能就踹开门。")]
        public bool KickOpenAllDoors = false;
        
        [Name("近距交火-路径距离-开始")]
        [MinMax(0.0f, 50f, 100)]
        public float DOGFIGHT_PATH_DIST_START = 10;

        [Name("近距交火-上次看到时间-结束")]
        [MinMax(0.0f, 50f, 100)]
        public float DOGFIGHT_TIMESINCESEEN_START = 1;
        
        [Name("近距交火-路径距离-结束")]
        [MinMax(0.0f, 50f, 100)]
        public float DOGFIGHT_PATH_DIST_END = 15;

        [Name("近距交火-上次看到时间-结束")]
        [MinMax(0.0f, 60f, 100)]
        public float DOGFIGHT_TIMESINCESEEN_END = 8;

        [Name("原地坚守基础时间")]
        [Description("修正前的基础时间。Bot在掩体外被发现时坚守射击或还击的基础时间。")]
        [Advanced]
        [MinMax(0, 3f, 10)]
        public float HoldGroundBaseTime = 1f;

        [Name("原地坚守最小随机时间")]
        [Advanced]
        [MinMax(0f, 5f, 100)]
        public float HoldGroundMinRandom = 0.66f;

        [Name("原地坚守最大随机时间")]
        [Advanced]
        [MinMax(0f, 5f, 100)]
        public float HoldGroundMaxRandom = 1.5f;

        [Name("压制抗性")]
        [Description("值越高受压制影响越小。0=无抗性，1=完全免疫。最终抗性为个性和Bot类型抗性的中点。例如个性0.25+Bot类型0.75=0.5。")]
        [MinMax(0.0f, 1f, 100)]
        public float SuppressionResistance = 0f;


        [Name("敌人压制开关")]
        [Category("敌人压制")]
        public bool TARGET_SUPPRESS_TOGGLE = true;
        
        [Name("压制距离-近")]
        [Category("敌人压制")]
        [Description("敌人可见路径点距Bot认为的位置小于此值时无需检查角度直接压制。")]
        [MinMax(0f, 10f, 100f)]
        public float TARGET_SUPPRESS_DIST = 3f;

        [Name("压制距离-远")]
        [Category("敌人压制")]
        [Description("敌人可见路径点距Bot认为的位置小于此值时检查角度后可压制。")]
        [MinMax(0f, 30f, 100f)]
        public float TARGET_SUPPRESS_DIST_MAX = 12f;

        [Name("压制距离-远距离角度")]
        [Category("敌人压制")]
        [Description("敌人可见路径点到最后已知位置水平角度小于此值时可压制。")]
        [MinMax(0f, 180f, 1f)]
        public float MAX_TARGET_SUPPRESS_ANGLE = 45f;
        
        [Category("敌人压制")]
        [MinMax(0f, 180f, 10f)]
        public float TimeSinceSeenToSuppress = 3f;

        [Category("敌人压制")]
        [MinMax(0f, 180f, 10f)]
        public float TimeSinceShotAtToSuppress = 12f;

        [Category("敌人压制")]
        [MinMax(0f, 180f, 10f)]
        public float TimeSinceShotToSuppress = 12f;

        [Category("敌人狙击手反应")]
        [Description("Bot认为被狙击手射击时将始终冲刺寻找掩体。")]
        [Name("始终冲刺")]
        public bool ENEMYSNIPER_ALWAYS_SPRINT_COVER = true;

        [Category("敌人狙击手反应")]
        [Description("Bot认为被狙击手射击时搜寻狙击手将始终冲刺。")]
        [Name("始终冲刺")]
        public bool ENEMYSNIPER_ALWAYS_SPRINT_SEARCH = true;

        [Category("敌人狙击手反应")]
        [Name("被射击判定距离")]
        [Description("敌人在此距离外(米)向Bot射击时被视为狙击手。")]
        [MinMax(30f, 250f, 1f)]
        public float ENEMYSNIPER_DISTANCE = 85f;

        [Category("敌人狙击手反应")]
        [Name("不再视为狙击手的距离")]
        [Description("敌人在此距离内(米)时将不再被视为狙击手。")]
        [MinMax(30f, 250f, 1f)]
        public float ENEMYSNIPER_DISTANCE_END = 75f;

        [Name("手雷反应速度倍率")]
        [Description("对手雷的反应速度倍率。<1 更快反应，>1 更慢。Rat/Coward 应设 0.7，Timmy 应设 1.5。")]
        [Category("战斗行为")]
        [MinMax(0.5f, 2.0f)]
        public float GRENADE_REACTION_TIME_MODIFIER = 1.0f;

        [Name("手雷安全距离倍率")]
        [Description("躲避手雷时的安全距离倍率。Rat/Coward 应设 1.3(更远)，GigaChad 应设 0.6(更近)。")]
        [Category("战斗行为")]
        [MinMax(0.5f, 3.0f)]
        public float GRENADE_SAFE_DIST_MODIFIER = 1.0f;

        [Name("手雷硬扛概率")]
        [Description("完全不躲避手雷的概率。仅 GigaChad 建议设为 0.1。")]
        [Category("战斗行为")]
        [MinMax(0.0f, 0.5f)]
        public float GRENADE_IGNORE_CHANCE = 0.0f;
    }
}