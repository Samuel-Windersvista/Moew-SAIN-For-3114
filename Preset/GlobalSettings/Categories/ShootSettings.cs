using Newtonsoft.Json;
using SAIN.Attributes;
using System.Collections.Generic;

namespace SAIN.Preset.GlobalSettings
{
    // note for later: Need to find a way to remove the clunky duplicates of the dictionaries here, as its a hold over from a previous system of getting the default values for different config options.
    public class ShootSettings : SAINSettingsBase<ShootSettings>, ISAINSettings
    {
        [Name("基础射击间隔最小值")]
        [Category("通用")]
        [MinMax(0.0f, 2f, 100f)]
        public float MIN_FIRE_RATE_INTERVAL = 0.1f; // minimum time between shots

        [Name("基础射击间隔最大值")]
        [Category("通用")]
        [MinMax(0.0f, 8f, 100f)]
        public float MAX_FIRE_RATE_INTERVAL = 4f; // maximum time between shots

        [Name("全自动射击间隔乘数")]
        [Description("Bot处于全自动模式时射击间隔乘以此值。")]
        [Category("通用")]
        [MinMax(0.0f, 1f, 100f)]
        public float MAX_FIRE_RATE_COEF_FULLAUTO = 0.25f; // maximum time between shots

        [Name("射速随机化")]
        [Description("随机化Bot射击间隔使其表现不那么机械。0.25=射击间隔随机乘0.75-1.25。")]
        [Category("通用")]
        [MinMax(0.0f, 1f, 100f)]
        public float FIRERATE_RANDOMIZATION_COEF = 0.25f; // randomization coefficient for firerate

        [Name("Bot仅使用半自动")]
        [Description("开启后Bot在近/中距离无法使用全自动。")]
        [Category("通用")]
        public bool ONLY_SEMIAUTO_TOGGLE = false;

        [Name("全局后坐力乘数")]
        [Description("数值越高=后坐力越大。修改SAIN后坐力散布功能。1.5=单发后坐力1.5倍。")]
        [Category("Bot后坐力")]
        [MinMax(0.01f, 3f, 100f)]
        public float BOT_RECOIL_COEF = 0.5f;

        [Name("后坐力加减值")]
        [Description("线性加减最终后坐力结果。")]
        [Category("Bot后坐力")]
        [MinMax(-20f, 20f, 100f)]
        [Advanced]
        public float BOT_RECOIL_ADD = 0f;

        [Name("后坐力衰减系数")]
        [Description("控制Bot从后坐力中恢复的速度。数值越高=衰减越快。")]
        [Category("Bot后坐力")]
        [MinMax(0.01f, 1f, 100f)]
        [Advanced]
        public float BOT_RECOIL_DECAY_COEF = 0.3f;

        [Name("Bot武器后坐力基准值")]
        [Description("Bot武器后坐力除以此值计算每发射击视角旋转量。基准值100时，总后坐力250/100=2.5。数值越高=后坐力越小。")]
        [Category("Bot后坐力")]
        [MinMax(25f, 300f, 1f)]
        [Advanced]
        public float BOT_RECOIL_BASELINE = 100;

        [Name("Bot武器后坐力基准值-Realism模组")]
        [Description("Bot武器后坐力除以此值计算每发射击视角旋转量。Realism模组开启时使用此值。")]
        [Category("Bot后坐力")]
        [MinMax(25f, 300f, 1f)]
        [Advanced]
        public float BOT_RECOIL_BASELINE_REALISM = 125f;

        [Name("弹药射击可行性")]
        [Description(
            "数值越低越好。 " +
            "表示此弹药类型的射击可行性，影响半自动射速和全自动连射时长。" +
            "该值缩放后大约给射速带来正负20%的影响。" +
            "例如9x19在50米半自动时射速约快20%" +
            "，全自动时连射时长增加20%。"
            )]
        [Category("Bot武器控制")]
        [Percentage0to1(0.01f)]
        [Advanced]
        [DefaultDictionary(nameof(AmmoCaliberShootabilityDefaults))]
        public Dictionary<ECaliber, float> AmmoCaliberShootability = new()
        {
            { ECaliber.Caliber9x18PM, 0.225f },
            { ECaliber.Caliber9x19PARA, 0.275f },
            { ECaliber.Caliber46x30, 0.325f },
            { ECaliber.Caliber9x21, 0.325f },
            { ECaliber.Caliber57x28, 0.35f },
            { ECaliber.Caliber762x25TT, 0.425f },
            { ECaliber.Caliber1143x23ACP, 0.425f },
            { ECaliber.Caliber9x33R, 0.65f },
            { ECaliber.Caliber545x39, 0.525f },
            { ECaliber.Caliber556x45NATO, 0.525f },
            { ECaliber.Caliber9x39, 0.6f },
            { ECaliber.Caliber762x35, 0.575f },
            { ECaliber.Caliber762x39, 0.675f },
            { ECaliber.Caliber366TKM, 0.675f },
            { ECaliber.Caliber762x51, 0.725f },
            { ECaliber.Caliber127x55, 0.775f },
            { ECaliber.Caliber762x54R, 0.85f },
            { ECaliber.Caliber86x70, 1.0f },
            { ECaliber.Caliber20g, 0.7f },
            { ECaliber.Caliber12g, 0.725f },
            { ECaliber.Caliber23x75, 0.85f },
            { ECaliber.Caliber26x75, 1f },
            { ECaliber.Caliber30x29, 1f },
            { ECaliber.Caliber40x46, 1f },
            { ECaliber.Caliber40mmRU, 1f },
            { ECaliber.Caliber127x108, 0.5f },
            { ECaliber.Caliber68x51, 0.6f },
            { ECaliber.Default, 0.5f },
        };

        [JsonIgnore]
        [Hidden]
        public static readonly Dictionary<ECaliber, float> AmmoCaliberShootabilityDefaults = new()
        {
            { ECaliber.Caliber9x18PM, 0.225f },
            { ECaliber.Caliber9x19PARA, 0.275f },
            { ECaliber.Caliber46x30, 0.325f },
            { ECaliber.Caliber9x21, 0.325f },
            { ECaliber.Caliber57x28, 0.35f },
            { ECaliber.Caliber762x25TT, 0.425f },
            { ECaliber.Caliber1143x23ACP, 0.425f },
            { ECaliber.Caliber9x33R, 0.65f },
            { ECaliber.Caliber545x39, 0.525f },
            { ECaliber.Caliber556x45NATO, 0.525f },
            { ECaliber.Caliber9x39, 0.6f },
            { ECaliber.Caliber762x35, 0.575f },
            { ECaliber.Caliber762x39, 0.675f },
            { ECaliber.Caliber366TKM, 0.675f },
            { ECaliber.Caliber762x51, 0.725f },
            { ECaliber.Caliber127x55, 0.775f },
            { ECaliber.Caliber762x54R, 0.85f },
            { ECaliber.Caliber86x70, 1.0f },
            { ECaliber.Caliber20g, 0.7f },
            { ECaliber.Caliber12g, 0.725f },
            { ECaliber.Caliber23x75, 0.85f },
            { ECaliber.Caliber26x75, 1f },
            { ECaliber.Caliber30x29, 1f },
            { ECaliber.Caliber40x46, 1f },
            { ECaliber.Caliber40mmRU, 1f },
            { ECaliber.Caliber127x108, 0.5f },
            { ECaliber.Caliber68x51, 0.6f },
            { ECaliber.Default, 0.5f },
        };

        [Name("最大全自动距离")]
        [Description("Bot使用该口径全自动射击的最大距离。部分口径无全自动武器因此不使用。")]
        [Category("Bot武器控制")]
        [MinMax(10f, 150f)]
        [Advanced]
        [DefaultDictionary(nameof(AmmoCaliberFullAutoMaxDistancesDefaults))]
        public Dictionary<ECaliber, float> AmmoCaliberFullAutoMaxDistances = new()
        {
            { ECaliber.Caliber9x18PM, 80f },
            { ECaliber.Caliber9x19PARA, 80f },
            { ECaliber.Caliber46x30, 70f },
            { ECaliber.Caliber9x21, 70f },
            { ECaliber.Caliber57x28, 70f },
            { ECaliber.Caliber762x25TT, 70f },
            { ECaliber.Caliber1143x23ACP, 75f },
            { ECaliber.Caliber9x33R, 75f },
            { ECaliber.Caliber545x39, 65f },
            { ECaliber.Caliber556x45NATO, 65f },
            { ECaliber.Caliber9x39, 60f },
            { ECaliber.Caliber762x35, 55f },
            { ECaliber.Caliber762x39, 50f },
            { ECaliber.Caliber366TKM, 45f },
            { ECaliber.Caliber762x51, 45f },
            { ECaliber.Caliber127x55, 50f },
            { ECaliber.Caliber762x54R, 50f },
            { ECaliber.Caliber86x70, 40f },
            { ECaliber.Caliber20g, 30f },
            { ECaliber.Caliber12g, 30f },
            { ECaliber.Caliber23x75, 30f },
            { ECaliber.Caliber26x75, 30f },
            { ECaliber.Caliber30x29, 30f },
            { ECaliber.Caliber40x46, 30f },
            { ECaliber.Caliber40mmRU, 30f },
            { ECaliber.Caliber127x108, 30f },
            { ECaliber.Caliber68x51, 50f },
            { ECaliber.Default, 55f },
        };

        [JsonIgnore]
        [Hidden]
        public static readonly Dictionary<ECaliber, float> AmmoCaliberFullAutoMaxDistancesDefaults = new()
        {
            { ECaliber.Caliber9x18PM, 80f },
            { ECaliber.Caliber9x19PARA, 80f },
            { ECaliber.Caliber46x30, 70f },
            { ECaliber.Caliber9x21, 70f },
            { ECaliber.Caliber57x28, 70f },
            { ECaliber.Caliber762x25TT, 70f },
            { ECaliber.Caliber1143x23ACP, 75f },
            { ECaliber.Caliber9x33R, 75f },
            { ECaliber.Caliber545x39, 65f },
            { ECaliber.Caliber556x45NATO, 65f },
            { ECaliber.Caliber9x39, 60f },
            { ECaliber.Caliber762x35, 55f },
            { ECaliber.Caliber762x39, 50f },
            { ECaliber.Caliber366TKM, 45f },
            { ECaliber.Caliber762x51, 45f },
            { ECaliber.Caliber127x55, 50f },
            { ECaliber.Caliber762x54R, 50f },
            { ECaliber.Caliber86x70, 40f },
            { ECaliber.Caliber20g, 30f },
            { ECaliber.Caliber12g, 30f },
            { ECaliber.Caliber23x75, 30f },
            { ECaliber.Caliber26x75, 30f },
            { ECaliber.Caliber30x29, 30f },
            { ECaliber.Caliber40x46, 30f },
            { ECaliber.Caliber40mmRU, 30f },
            { ECaliber.Caliber127x108, 30f },
            { ECaliber.Caliber68x51, 50f },
            { ECaliber.Default, 55f },
        };

        [Name("武器射击可行性")]
        [Description(
            "数值越低越好。 " +
            "表示此武器类型的射击可行性，影响半自动射速和全自动连射时长。" +
            "该值缩放后大约给射速带来正负20%的影响。" +
            "例如冲锋枪在50米半自动时射速约快20%" +
            "，全自动时连射时长增加20%。"
            )]
        [Category("Bot武器控制")]
        [Percentage0to1(0.01f)]
        [Advanced]
        [DefaultDictionary(nameof(WeaponClassShootabilityDefaults))]
        public Dictionary<EWeaponClass, float> WeaponClassShootability = new()
        {
            { EWeaponClass.Default, 0.425f },
            { EWeaponClass.assaultCarbine, 0.5f },
            { EWeaponClass.assaultRifle, 0.5f },
            { EWeaponClass.machinegun, 0.15f },
            { EWeaponClass.smg, 0.25f },
            { EWeaponClass.pistol, 0.4f },
            { EWeaponClass.marksmanRifle, 0.75f },
            { EWeaponClass.sniperRifle, 1f },
            { EWeaponClass.shotgun, 0.75f },
            { EWeaponClass.grenadeLauncher, 1f },
            { EWeaponClass.specialWeapon, 1f },
        };

        [JsonIgnore]
        [Hidden]
        public static readonly Dictionary<EWeaponClass, float> WeaponClassShootabilityDefaults = new()
        {
            { EWeaponClass.Default, 0.425f },
            { EWeaponClass.assaultCarbine, 0.5f },
            { EWeaponClass.assaultRifle, 0.5f },
            { EWeaponClass.machinegun, 0.15f },
            { EWeaponClass.smg, 0.25f },
            { EWeaponClass.pistol, 0.4f },
            { EWeaponClass.marksmanRifle, 0.75f },
            { EWeaponClass.sniperRifle, 1f },
            { EWeaponClass.shotgun, 0.75f },
            { EWeaponClass.grenadeLauncher, 1f },
            { EWeaponClass.specialWeapon, 1f },
        };

        [Name("武器射速等待时间")]
        [Description(
            "数值越高越好。 " +
            "每米距离的射击等待时间。" +
            "该值除以到目标的距离得到射击间隔。" +
            "例如设为100：目标距离50米，则射击间隔50/100=0.5秒。" +
            "此值随后被射击可行性乘数修正，得到最终射速。"
            )]
        [MinMax(30f, 250f, 1f)]
        [Category("Bot武器控制")]
        [Advanced]
        [DefaultDictionary(nameof(WeaponPerMeterDefaults))]
        public Dictionary<EWeaponClass, float> WeaponPerMeter = new()
        {
            { EWeaponClass.Default, 130f },
            { EWeaponClass.assaultCarbine, 150 },
            { EWeaponClass.assaultRifle, 140 },
            { EWeaponClass.machinegun, 180 },
            { EWeaponClass.smg, 160 },
            { EWeaponClass.pistol, 75 },
            { EWeaponClass.marksmanRifle, 85 },
            { EWeaponClass.sniperRifle, 60 },
            { EWeaponClass.shotgun, 80 },
            { EWeaponClass.grenadeLauncher, 90 },
            { EWeaponClass.specialWeapon, 80 },
        };

        [JsonIgnore]
        [Hidden]
        public static readonly Dictionary<EWeaponClass, float> WeaponPerMeterDefaults = new()
        {
            { EWeaponClass.Default, 130f },
            { EWeaponClass.assaultCarbine, 150 },
            { EWeaponClass.assaultRifle, 140 },
            { EWeaponClass.machinegun, 180 },
            { EWeaponClass.smg, 160 },
            { EWeaponClass.pistol, 75 },
            { EWeaponClass.marksmanRifle, 85 },
            { EWeaponClass.sniperRifle, 60 },
            { EWeaponClass.shotgun, 80 },
            { EWeaponClass.grenadeLauncher, 90 },
            { EWeaponClass.specialWeapon, 80 },
        };

        [Name("Bot首选射击距离")]
        [Description(
            "Bot对特定武器类型的偏好射击距离。 " +
            "超出此距离时Bot会尝试拉近距离。"
            )]
        [MinMax(10f, 250f, 1f)]
        [Category("Bot武器控制")]
        [Advanced]
        [DefaultDictionary(nameof(EngagementDistanceDefaults))]
        public Dictionary<EWeaponClass, float> EngagementDistance = new()
        {
            { EWeaponClass.Default, 125f },
            { EWeaponClass.assaultCarbine, 125f },
            { EWeaponClass.assaultRifle, 150f },
            { EWeaponClass.machinegun, 125f },
            { EWeaponClass.smg, 70f },
            { EWeaponClass.pistol, 50f },
            { EWeaponClass.marksmanRifle, 175f },
            { EWeaponClass.sniperRifle, 300f },
            { EWeaponClass.shotgun, 50f },
            { EWeaponClass.grenadeLauncher, 100f },
            { EWeaponClass.specialWeapon, 100f },
        };

        [JsonIgnore]
        [Hidden]
        public static readonly Dictionary<EWeaponClass, float> EngagementDistanceDefaults = new()
        {
            { EWeaponClass.Default, 125f },
            { EWeaponClass.assaultCarbine, 125f },
            { EWeaponClass.assaultRifle, 150f },
            { EWeaponClass.machinegun, 125f },
            { EWeaponClass.smg, 70f },
            { EWeaponClass.pistol, 50f },
            { EWeaponClass.marksmanRifle, 175f },
            { EWeaponClass.sniperRifle, 300f },
            { EWeaponClass.shotgun, 50f },
            { EWeaponClass.grenadeLauncher, 100f },
            { EWeaponClass.specialWeapon, 100f },
        };

        [JsonIgnore]
        [Hidden]
        private const string Shootability = "影响武器射击可行性计算，它决定Bot该以多快速度射击、连射多久、以及全自动切换到半自动的距离判定。";

        private const string Scaling = "影响该类型对Bot武器射击可行性的作用程度。数值越高=影响越大。例如将后坐力缩放设为0，后坐力将完全不影响Bot的射速。";

        [Name("武器类型缩放")]
        [Description(Shootability + Scaling)]
        [Category("Bot武器控制")]
        [Advanced]
        [Percentage01to99]
        public float WeaponClassScaling = 0.25f;

        [Name("后坐力缩放")]
        [Description(Shootability + Scaling)]
        [Category("Bot武器控制")]
        [Advanced]
        [Percentage01to99]
        public float RecoilScaling = 0.35f;

        [Name("人机工效缩放")]
        [Description(Shootability + Scaling)]
        [Category("Bot武器控制")]
        [Advanced]
        [Percentage01to99]
        public float ErgoScaling = 0.08f;

        [Name("口径缩放")]
        [Description(Shootability + Scaling)]
        [Category("Bot武器控制")]
        [Advanced]
        [Percentage01to99]
        public float AmmoCaliberScaling = 0.3f;

        [Name("武器熟练度缩放")]
        [Description(Shootability + Scaling)]
        [Category("Bot武器控制")]
        [Advanced]
        [Percentage01to99]
        public float WeaponProficiencyScaling = 0.3f;

        [Name("难度缩放")]
        [Description(Shootability + Scaling)]
        [Category("Bot武器控制")]
        [Advanced]
        [Percentage01to99]
        public float DifficultyScaling = 0.3f;

        public override void Init(List<ISAINSettings> list)
        {
            list.Add(this);
        }
    }
}