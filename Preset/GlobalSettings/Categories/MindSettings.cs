using EFT;
using Newtonsoft.Json;
using SAIN.Attributes;
using SAIN.Models.Enums;
using SAIN.SAINComponent.Classes.WeaponFunction;
using System.Collections.Generic;

namespace SAIN.Preset.GlobalSettings.Categories
{
    public class MindSettings : SAINSettingsBase<MindSettings>, ISAINSettings
    {
        public override void Update()
        {
        }
        
        [Name("敌人压制开关")]
        [Category("敌人压制")]
        public bool TARGET_SUPPRESS_TOGGLE = true;

        [Name("强制所有Bot使用单一个性")]
        [Description("所有生成的SAIN Bot将全部被分配为选定的个性(如设为开启)。")]
        [Category("个性")]
        public Dictionary<EPersonality, bool> ForcePersonality = new()
        {
            { EPersonality.Wreckless, false},
            { EPersonality.GigaChad, false },
            { EPersonality.Chad, false },
            { EPersonality.SnappingTurtle, false},
            { EPersonality.Rat, false },
            { EPersonality.Coward, false },
            { EPersonality.Timmy, false},
            { EPersonality.Normal, false},
        };

        [Name("Boss个性")]
        [Description("设置Boss始终使用的个性。")]
        [Category("个性")]
        [Hidden]
        public Dictionary<WildSpawnType, EPersonality> PERS_BOSSES = new() {
            { WildSpawnType.bossKilla, EPersonality.Wreckless},
            { WildSpawnType.bossTagilla, EPersonality.Wreckless},
            { WildSpawnType.bossKolontay, EPersonality.Wreckless},

            { WildSpawnType.bossKnight, EPersonality.GigaChad},
            { WildSpawnType.followerBigPipe, EPersonality.GigaChad},

            { WildSpawnType.followerBirdEye, EPersonality.SnappingTurtle},
            { WildSpawnType.bossGluhar, EPersonality.SnappingTurtle},

            { WildSpawnType.bossKojaniy, EPersonality.Rat},
            { WildSpawnType.bossPartisan, EPersonality.Rat},

            { WildSpawnType.bossBully, EPersonality.Coward},
            { WildSpawnType.bossSanitar, EPersonality.Coward},
            { WildSpawnType.bossBoar, EPersonality.Coward},
        };

        [MinMax(0.1f, 5f, 100f)]
        [Category("个性")]
        [Name("全局攻击性")]
        [Description("数值越高=Bot越具攻击性，搜索敌人等待时间越短。2倍=等待时间减半。")]
        public float GlobalAggression = 1f;

        [Name("Bot可使用潜行搜索")]
        [Description("Bot认为自己未被听到且未在交战时可在建筑物内潜行搜索敌人。")]
        [Category("个性")]
        public bool SneakyBots = true;

        [Name("仅潜行型个性可潜行")]
        [Description("仅允许潜行型个性(老鼠、伏击龟)在搜索敌人时潜行。上方禁用则忽略。")]
        [Category("个性")]
        public bool OnlySneakyPersonalitiesSneaky = true;

        [Name("潜行生效最大距离")]
        [Description("启用潜行搜索时距搜索目的地多远Bot开始潜行。")]
        [Category("个性")]
        [Advanced]
        [MinMax(5f, 200f, 10f)]
        public float MaximumDistanceToBeSneaky = 80f;

        [Name("Bot压制系统")]
        [Description("控制Bot是否会被压制。关闭后以下选项均无效。")]
        [Category("压制")]
        public bool SUPP_TOGGLE = true;

        [Name("压制距离缩放起点")]
        [Description("子弹与Bot头部之间完全压制效果的距离(米)。")]
        [Category("压制")]
        [MinMax(1f, 30f, 100f)]
        [Advanced]
        public float SUPP_DISTANCE_SCALE_START = 4f;

        [Name("压制距离缩放终点")]
        [Description("子弹与Bot头部被视为压制火力的最大距离(米)，在终点和起点间线性缩放。")]
        [Category("压制")]
        [MinMax(1f, 30f, 100f)]
        [Advanced]
        public float SUPP_DISTANCE_SCALE_END = 10f;

        [Name("压制距离增强距离")]
        [Description("子弹与Bot头部距离小于此值(米)时增强压制效果。")]
        [Category("压制")]
        [MinMax(0f, 5f, 100f)]
        [Advanced]
        public float SUPP_DISTANCE_AMP_DIST = 0.5f;

        [Name("压制距离增强倍率")]
        [Description("子弹与Bot头部距离小于增强距离时压制效果乘以此倍率。")]
        [Category("压制")]
        [MinMax(1f, 3f, 100f)]
        [Advanced]
        public float SUPP_DISTANCE_AMP_AMOUNT = 1.5f;

        [Name("被射击最大判定距离")]
        [Description("子弹与Bot头部被视为活跃敌人火力下的最大距离。")]
        [MinMax(0.1f, 20f, 100f)]
        [Category("压制")]
        [Advanced]
        public float MaxUnderFireDistance = 2f;

        [Hidden]
        [Name("压制状态")]
        [Description("配置各级压制状态。")]
        [MinMax(0.01f, 10f, 100f)]
        [Category("压制")]
        [Advanced]
        public Dictionary<ESuppressionState, SuppressionConfig> SUPPRESSION_STATES = new()
        {
            {ESuppressionState.Light, new SuppressionConfig {
                Threshold = 1f,
                PrecisionSpeedCoef = 0.8f,
                AccuracySpeedCoef = 1.2f,
                GainSightCoef = 0.9f,
                ScatteringCoef = 1.35f,
                VisibleDistCoef = 0.85f,
                HearingDistCoef = 0.8f,
                }
            },
            {ESuppressionState.Medium, new SuppressionConfig {
                Threshold = 6f,
                PrecisionSpeedCoef = 0.75f,
                AccuracySpeedCoef = 1.5f,
                GainSightCoef = 0.75f,
                ScatteringCoef = 1.75f,
                VisibleDistCoef = 0.6f,
                HearingDistCoef = 0.6f,
                }
            },
            {ESuppressionState.Heavy, new SuppressionConfig {
                Threshold = 15f,
                PrecisionSpeedCoef = 0.5f,
                AccuracySpeedCoef = 2f,
                GainSightCoef = 0.65f,
                ScatteringCoef = 2.5f,
                VisibleDistCoef = 0.5f,
                HearingDistCoef = 0.4f,
                }
            },
            {ESuppressionState.Extreme, new SuppressionConfig {
                Threshold = 25f,
                PrecisionSpeedCoef = 0.25f,
                AccuracySpeedCoef = 3f,
                GainSightCoef = 0.5f,
                ScatteringCoef = 3f,
                VisibleDistCoef = 0.33f,
                HearingDistCoef = 0.25f,
                }
            },
        };

        [Name("压制量乘数")]
        [Description("线性增减Bot单发子弹获得的压制点数。越高=越易被压制。")]
        [MinMax(0.01f, 5f, 100f)]
        [Category("压制")]
        public float SUPP_AMOUNT_MULTI = 1f;

        [Name("压制强度乘数")]
        [Description("线性增减压制对Bot属性的影响强度。越高=影响越大。")]
        [MinMax(0.01f, 5f, 100f)]
        [Category("压制")]
        public float SUPP_STRENGTH_MULTI = 1f;

        [Advanced]
        [Name("衰减每Tick量")]
        [Description("每次更新Tick移除的压制值。")]
        [MinMax(0.01f, 5f, 100f)]
        [Category("压制")]
        public float SUP_DECAY_AMOUNT = 0.25f;

        [Advanced]
        [Name("衰减Tick频率")]
        [Description("每秒衰减Tick频率。0.25=每秒4次。")]
        [MinMax(0.01f, 1f, 100f)]
        [Category("压制")]
        public float SUP_DECAY_FREQ = 0.25f;

        [Advanced]
        [Name("状态更新Tick频率")]
        [Description("每秒检查压制状态频率。0.5=每秒2次。")]
        [MinMax(0.01f, 1f, 100f)]
        [Category("压制")]
        public float SUP_CHECK_FREQ = 0.5f;

        [Advanced]
        [Name("各口径压制量")]
        [Description("对Bot旁飞过的每发子弹，压制计数器增加此值(持续线性衰减)。")]
        [MinMax(0.1f, 20f, 100f)]
        [Category("压制")]
        [DefaultDictionary(nameof(SUPP_AMOUNTS_DEFAULT))]
        public Dictionary<ECaliber, float> SUPP_AMOUNTS = new()
        {
            { ECaliber.Caliber9x18PM, 1f },
            { ECaliber.Caliber9x19PARA, 1.1f },
            { ECaliber.Caliber46x30, 1.2f },
            { ECaliber.Caliber9x21, 1.25f },
            { ECaliber.Caliber57x28, 1.3f },
            { ECaliber.Caliber762x25TT, 1.4f },
            { ECaliber.Caliber1143x23ACP, 1.5f },
            { ECaliber.Caliber9x33R, 1.5f },
            { ECaliber.Caliber545x39, 2.1f },
            { ECaliber.Caliber556x45NATO, 2f },
            { ECaliber.Caliber9x39, 2.5f },
            { ECaliber.Caliber762x35, 2.4f },
            { ECaliber.Caliber762x39, 2.5f },
            { ECaliber.Caliber366TKM, 2.5f },
            { ECaliber.Caliber68x51, 2.5f },
            { ECaliber.Caliber762x51, 2.65f },
            { ECaliber.Caliber127x55, 2.7f },
            { ECaliber.Caliber762x54R, 2.75f },
            { ECaliber.Caliber20g, 3f },
            { ECaliber.Caliber12g, 3f },
            { ECaliber.Caliber23x75, 3f },
            { ECaliber.Caliber26x75, 3f },
            { ECaliber.Caliber30x29, 3f },
            { ECaliber.Caliber40x46, 3f },
            { ECaliber.Caliber40mmRU, 3f },
            { ECaliber.Caliber86x70, 5f },
            { ECaliber.Caliber127x108, 5f },
            { ECaliber.Default, 2f },
        };

        [JsonIgnore]
        [Hidden]
        public static readonly Dictionary<ECaliber, float> SUPP_AMOUNTS_DEFAULT = new()
        {
            { ECaliber.Caliber9x18PM, 1f },
            { ECaliber.Caliber9x19PARA, 1.1f },
            { ECaliber.Caliber46x30, 1.2f },
            { ECaliber.Caliber9x21, 1.25f },
            { ECaliber.Caliber57x28, 1.3f },
            { ECaliber.Caliber762x25TT, 1.4f },
            { ECaliber.Caliber1143x23ACP, 1.5f },
            { ECaliber.Caliber9x33R, 1.5f },
            { ECaliber.Caliber545x39, 2.1f },
            { ECaliber.Caliber556x45NATO, 2f },
            { ECaliber.Caliber9x39, 2.5f },
            { ECaliber.Caliber762x35, 2.4f },
            { ECaliber.Caliber762x39, 2.5f },
            { ECaliber.Caliber366TKM, 2.5f },
            { ECaliber.Caliber68x51, 2.5f },
            { ECaliber.Caliber762x51, 2.65f },
            { ECaliber.Caliber127x55, 2.7f },
            { ECaliber.Caliber762x54R, 2.75f },
            { ECaliber.Caliber86x70, 5f },
            { ECaliber.Caliber20g, 3f },
            { ECaliber.Caliber12g, 3f },
            { ECaliber.Caliber23x75, 3f },
            { ECaliber.Caliber26x75, 3f },
            { ECaliber.Caliber30x29, 3f },
            { ECaliber.Caliber40x46, 3f },
            { ECaliber.Caliber40mmRU, 3f },
            { ECaliber.Caliber127x108, 5f },
            { ECaliber.Default, 2f },
        };

        [Advanced]
        [Name("最大压制值")]
        [Description("压制值上限。")]
        [MinMax(0.01f, 50f, 100f)]
        [Category("压制")]
        public float SUPP_MAX_NUM = 30f;

        public override void Init(List<ISAINSettings> list)
        {
            list.Add(this);
        }
    }
}