using EFT;
using Newtonsoft.Json;
using SAIN.Attributes;
using SAIN.Components.PlayerComponentSpace;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.Preset.GlobalSettings
{
    public struct HearingDelaySettings(float PeaceDelay, float ActiveEnemyDelay, float OtherEnemyDelay, float UnknownEnemyDelay, float InRandomizationMin = 0.75f, float InRandomizationMax = 1.25f)
    {
        public float AtPeace = PeaceDelay;
        public float ActiveEnemy = ActiveEnemyDelay;
        public float OtherEnemy_Known = OtherEnemyDelay;
        public float OtherEnemy_Unknown = UnknownEnemyDelay;
        public float RandomizationMin = InRandomizationMin;
        public float RandomizationMax = InRandomizationMax;
    }

    public class HearingSettings : SAINSettingsBase<HearingSettings>, ISAINSettings
    {
        static HearingSettings()
        {
            // Hearing Dispersion
            HEAR_DISPERSION_VALUES_Defaults = new Dictionary<SAINSoundType, float>()
            {
                { SAINSoundType.Shot, 17.5f },
                { SAINSoundType.SuppressedShot, 13.5f },
                { SAINSoundType.FootStep, 12.5f },
            };
            const float defaultDispersion = 12.5f;
            Helpers.ListHelpers.PopulateKeys(HEAR_DISPERSION_VALUES_Defaults, defaultDispersion);

            // Gunfire Hearing Distances
            HearingDistancesDefaults = new Dictionary<ECaliber, float>()
            {
                { ECaliber.Caliber9x18PM, 110f },
                { ECaliber.Caliber9x19PARA, 110f },
                { ECaliber.Caliber46x30, 120f },
                { ECaliber.Caliber9x21, 120f },
                { ECaliber.Caliber57x28, 120f },
                { ECaliber.Caliber762x25TT, 120f },
                { ECaliber.Caliber1143x23ACP, 115f },
                { ECaliber.Caliber9x33R, 125 },
                { ECaliber.Caliber545x39, 160 },
                { ECaliber.Caliber556x45NATO, 160 },
                { ECaliber.Caliber9x39, 160 },
                { ECaliber.Caliber762x35, 175 },
                { ECaliber.Caliber762x39, 175 },
                { ECaliber.Caliber366TKM, 175 },
                { ECaliber.Caliber762x51, 200f },
                { ECaliber.Caliber127x55, 200f },
                { ECaliber.Caliber762x54R, 225f },
                { ECaliber.Caliber86x70, 250f },
                { ECaliber.Caliber20g, 185 },
                { ECaliber.Caliber12g, 185 },
                { ECaliber.Caliber23x75, 210 },
                { ECaliber.Caliber26x75, 50 },
                { ECaliber.Caliber30x29, 50 },
                { ECaliber.Caliber40x46, 50 },
                { ECaliber.Caliber40mmRU, 50 },
                { ECaliber.Caliber127x108, 300 },
                { ECaliber.Caliber68x51, 200f },
                { ECaliber.Default, 125 },
            };
            const float defaultDistance = 125;
            Helpers.ListHelpers.PopulateKeys(HearingDistancesDefaults, defaultDistance);
        }
        
        [Name("开门声音范围")]
        [Description("Bot能听到开门声的最大范围。")]
        [Category("听觉距离")]
        [MinMax(0, 100, 1)]
        public float DOOR_OPEN_SOUND_RANGE = 40;

        [Name("踹门声音范围")]
        [Description("Bot能听到踹门声的最大范围。")]
        [Category("听觉距离")]
        [MinMax(0, 100, 1)]
        public float DOOR_KICK_SOUND_RANGE = 65;

        [Name("跳跃声音范围")]
        [Description("Bot能听到跳跃声的最大范围。")]
        [Category("听觉距离")]
        [MinMax(0, 100, 1)]
        public float JUMP_SOUND_RANGE = 65;

        [Name("跳跃声音间隔")]
        [MinMax(0.1f, 1f, 100f)]
        [Advanced]
        public float JUMP_SOUND_INTERVAL = 0.5f;

        [Name("雨中声音乘数-室外")]
        [Description("下雨时Bot听觉范围最多缩减到此乘数。取决于雨势强度，随雨量线性缩放。")]
        [Category("听觉距离")]
        [MinMax(0.01f, 1f, 1000f)]
        public float RAIN_SOUND_COEF_OUTSIDE = 0.5f;

        [Name("雨中声音乘数-室内")]
        [Description("下雨时Bot听觉范围最多缩减到此乘数。取决于雨势强度，随雨量线性缩放。")]
        [Category("听觉距离")]
        [MinMax(0.01f, 1f, 1000f)]
        public float RAIN_SOUND_COEF_INSIDE = 0.75f;

        [Name("最大脚步声听觉距离")]
        [Description("Bot能听到脚步声、冲刺、跳跃、转身、装备声等所有移动相关声音的最大范围(米)。此为理论最大值，实际范围因条件变化而不同。")]
        [Category("听觉距离")]
        [MinMax(10f, 150f, 100f)]
        public float MaxFootstepAudioDistance = 70f;

        [Name("无耳机时最大脚步声听觉距离")]
        [Description("Bot能听到脚步声、冲刺、跳跃、转身、装备声等所有移动相关声音的最大范围(米)无耳机时。此为理论最大值，实际范围因条件变化而不同。")]
        [Category("听觉距离")]
        [MinMax(10f, 150f, 100f)]
        public float MaxFootstepAudioDistanceNoHeadphones = 50f;

        [Name("听觉随机化与估算")]
        [Description(_dispersion_descr)]
        [Category("位置随机化")]
        [Advanced]
        [MinMax(1f, 100f, 1000f)]
        [DefaultDictionary(nameof(HEAR_DISPERSION_VALUES_Defaults))]
        public Dictionary<SAINSoundType, float> HEAR_DISPERSION_VALUES = new()
        {
            { SAINSoundType.Shot, 17.5f },
            { SAINSoundType.SuppressedShot, 13.5f },
            { SAINSoundType.FootStep, 12.5f },
        };

        [JsonIgnore]
        [Hidden]
        public static readonly Dictionary<SAINSoundType, float> HEAR_DISPERSION_VALUES_Defaults;

        [Name("未听到枪声的子弹飞过修正值")]
        [Description("当Bot有子弹飞过时，枪声源头的散布将增大X倍。例如值2=随机化位置距离为原计算的2倍。")]
        [Category("位置随机化")]
        [Advanced]
        [MinMax(1f, 10f, 100f)]
        public float HEAR_DISPERSION_BULLET_FELT_MOD = 2f;

        [Name("最小听觉随机偏差")]
        [Description("数值越高=随机偏差越大、位置预测越不准。Bot估算声源位置的最小偏差距离(米)。")]
        [Category("位置随机化")]
        [Advanced]
        [MinMax(0.0f, 2f, 1000f)]
        public float HEAR_DISPERSION_MIN = 0.5f;

        [Name("无随机化距离")]
        [Description("声音距离小于等于此值时Bot完美预测声源位置，无随机化或散布。值0=禁用。")]
        [Category("位置随机化")]
        [Advanced]
        [MinMax(0f, 50f, 1000f)]
        public float HEAR_DISPERSION_MIN_DISTANCE_THRESH = 10f;

        [Name("最大随机化距离")]
        [Description("估算位置与实际声源位置之间允许的最大偏差距离上限(米)。")]
        [Category("位置随机化")]
        [Advanced]
        [MinMax(10f, 250f, 1000f)]
        public float HEAR_DISPERSION_MAX_DISPERSION = 50f;

        [Name("听觉随机化角度-最大值")]
        [Description(_hear_angle_descr)]
        [Category("位置随机化")]
        [Advanced]
        [MinMax(0.1f, 3f, 1000f)]
        public float HEAR_DISPERSION_ANGLE_MULTI_MAX = 1.5f;

        [Name("听觉随机化角度-最小值")]
        [Description(_hear_angle_descr)]
        [Category("位置随机化")]
        [Advanced]
        [MinMax(0.1f, 3f, 1000f)]
        public float HEAR_DISPERSION_ANGLE_MULTI_MIN = 0.5f;

        [Name("地堡音频范围")]
        [Description("Bot和敌人不在同一地堡内时缩减听觉范围。")]
        [Category("听觉环境修正")]
        [Advanced]
        [MinMax(0.01f, 1f, 1000f)]
        public float BUNKER_REDUCTION_COEF = 0.2f;

        [Name("地堡楼层范围")]
        [Description("Bot和敌人同在地堡但不同楼层时缩减听觉范围。")]
        [Category("听觉环境修正")]
        [Advanced]
        [MinMax(0.01f, 1f, 100f)]
        public float BUNKER_ELEV_DIFF_COEF = 0.66f;

        [Name("枪声遮挡")]
        [Description("Bot头部与声源之间有障碍物时听觉范围缩减到此值。")]
        [Category("听觉环境修正")]
        [Advanced]
        [MinMax(0.01f, 1f, 1000f)]
        public float GUNSHOT_OCCLUSION_MOD = 0.8f;

        [Name("消音枪声遮挡")]
        [Description("Bot头部与声源之间有障碍物时听觉范围缩减到此值。")]
        [Category("听觉环境修正")]
        [Advanced]
        [MinMax(0.01f, 1f, 100f)]
        public float GUNSHOT_OCCLUSION_MOD_SUPP = 0.65f;

        [Name("脚步声遮挡")]
        [Description("Bot头部与声源之间有障碍物时听觉范围缩减到此值。")]
        [Category("听觉环境修正")]
        [Advanced]
        [MinMax(0.01f, 1f, 1000f)]
        public float FOOTSTEP_OCCLUSION_MOD = 0.6f;

        [Name("冲刺声遮挡")]
        [Description("Bot头部与声源之间有障碍物时听觉范围缩减到此值。")]
        [Category("听觉环境修正")]
        [Advanced]
        [MinMax(0.01f, 1f, 100f)]
        public float FOOTSTEP_OCCLUSION_MOD_SPRINT = 0.8f;

        [Name("其他声音遮挡")]
        [Description("Bot头部与声源之间有障碍物时听觉范围缩减到此值。")]
        [Category("听觉环境修正")]
        [Advanced]
        [MinMax(0.01f, 1f, 100f)]
        public float OTHER_OCCLUSION_MOD = 0.6f;

        [Name("室内外差异-枪声")]
        [Description("Bot与声源不在同一区域(室内/室外)时听觉范围缩减到此值。")]
        [Category("听觉环境修正")]
        [Advanced]
        [MinMax(0.01f, 1f, 1000f)]
        public float GUNSHOT_ENVIR_MOD = 0.65f;

        [Name("室内外差异-脚步/其他")]
        [Description("Bot与声源不在同一区域(室内/室外)时听觉范围缩减到此值。")]
        [Category("听觉环境修正")]
        [Advanced]
        [MinMax(0.01f, 1f, 100f)]
        public float FOOTSTEP_ENVIR_MOD = 0.7f;

        [Name("环境修正最小值")]
        [Description("")]
        [Category("听觉环境修正")]
        [DeveloperOption]
        [MinMax(0.01f, 1f, 1000f)]
        public float MIN_ENVIRONMENT_MOD = 0.05f;

        [Name("无耳机")]
        [Description("Bot未佩戴耳机时所有声音听觉范围缩减到此值。")]
        [Category("听觉修正")]
        [MinMax(0.01f, 1f, 1000f)]
        public float HEAR_MODIFIER_NO_EARS = 0.6f;

        [Name("重型头盔")]
        [Description("Bot佩戴重型头盔时所有声音听觉范围缩减到此值。")]
        [Category("听觉修正")]
        [MinMax(0.01f, 1f, 100f)]
        public float HEAR_MODIFIER_HEAVY_HELMET = 0.8f;

        [Name("濒死状态")]
        [Description("Bot濒死或重伤时所有声音听觉范围缩减到此值。")]
        [Category("听觉修正")]
        [MinMax(0.01f, 1f, 1000f)]
        public float HEAR_MODIFIER_DYING = 0.8f;

        [Name("冲刺中")]
        [Description("Bot正在冲刺时所有声音听觉范围缩减到此值。")]
        [Category("听觉修正")]
        [Advanced]
        [MinMax(0.01f, 1f, 1000f)]
        public float HEAR_MODIFIER_SPRINT = 0.85f;

        [Name("剧烈喘息")]
        [Description("Bot剧烈喘息时所有声音听觉范围缩减到此值。")]
        [Category("听觉修正")]
        [Advanced]
        [MinMax(0.01f, 1f, 1000f)]
        public float HEAR_MODIFIER_HEAVYBREATH = 0.65f;

        [Name("最低听觉修正值")]
        [Description("最终乘数不会低于此值。")]
        [Category("听觉修正")]
        [DeveloperOption]
        [MinMax(0.01f, 1f, 1000f)]
        public float HEAR_MODIFIER_MIN_CLAMP = 0.01f;

        [Name("最高听觉修正值")]
        [Description("最终乘数不会高于此值。")]
        [Category("听觉修正")]
        [DeveloperOption]
        [MinMax(1f, 5f, 1000f)]
        public float HEAR_MODIFIER_MAX_CLAMP = 5f;

        [Name("最低听觉修正距离")]
        [Description("距离小于此值的声音Bot有100%概率听到。")]
        [Category("听觉修正")]
        [Advanced]
        [MinMax(0f, 50f, 100f)]
        public float HEAR_MODIFIER_MAX_AFFECT_DIST = 3f;

        [Name("缩放起始距离-无耳机")]
        [Description("距离小于此值的声音Bot有100%概率听到。")]
        [Category("听觉概率")]
        [Advanced]
        [MinMax(0, 10, 100)]
        public float HEAR_CHANCE_MIN_DIST = 0.25f;

        [Name("缩放起始距离-有耳机")]
        [Description("距离小于此值的声音Bot有100%概率听到。")]
        [Category("听觉概率")]
        [Advanced]
        [MinMax(0, 100, 100)]
        public float HEAR_CHANCE_MIN_DIST_HEADPHONES = 1;

        [Name("中距离系数")]
        [Description("当声音与Bot之间的距离低于最大范围的该比例时，轻微提升听觉概率。")]
        [Category("听觉概率")]
        [Advanced]
        [MinMax(0.00f, 1f, 1000f)]
        public float HEAR_CHANCE_MIDRANGE_COEF = 0.66f;

        [Name("中距离最低概率-有耳机")]
        [Description("声音在中距离范围内时最低听觉概率提升此值。")]
        [Category("听觉概率")]
        [Advanced]
        [MinMax(0, 100, 1)]
        public float HEAR_CHANCE_MIDRANGE_MINCHANCE_HEADPHONES = 3;

        [Name("远距离最低概率-有耳机")]
        [Description("声音超出中距离范围时最低听觉概率提升此值。")]
        [Category("听觉概率")]
        [Advanced]
        [MinMax(0, 100, 1)]
        public float HEAR_CHANCE_LONGRANGE_MINCHANCE_HEADPHONES = 1;

        [Name("静止速度阈值")]
        [Description("Bot速度低于此值时轻微提升听觉概率。")]
        [Category("听觉概率")]
        [Advanced]
        [MinMax(0.0f, 1f, 1000f)]
        public float HEAR_CHANCE_NOTMOVING_VELOCITY = 0.05f;

        [Name("静止最低概率-无耳机")]
        [Description("Bot静止时非枪声类声音的最低听觉概率提升此百分比。")]
        [Category("听觉概率")]
        [Advanced]
        [MinMax(0, 100, 1)]
        public float HEAR_CHANCE_NOTMOVING_MINCHANCE = 2;

        [Name("静止最低概率-有耳机")]
        [Description("Bot静止时非枪声类声音的最低听觉概率提升此百分比。")]
        [Category("听觉概率")]
        [MinMax(0, 100, 1)]
        public float HEAR_CHANCE_NOTMOVING_MINCHANCE_HEADPHONES = 4;

        [Name("其他声音最低概率-有耳机")]
        [Description("声音类型非脚步声或枪声时最低听觉概率提升此百分比。")]
        [Category("听觉概率")]
        [Advanced]
        [MinMax(0, 100, 1)]
        public float HEAR_CHANCE_HEADPHONES_OTHERSOUNDS = 3;

        [Name("当前敌人最低概率-无耳机")]
        [Description("声音来源为Bot当前主要敌人时非枪声类声音的最低听觉概率提升此百分比。")]
        [Category("听觉概率")]
        [Advanced]
        [MinMax(0, 100, 1)]
        public float HEAR_CHANCE_CURRENTENEMY_MINCHANCE = 2;

        [Name("当前敌人最低概率-有耳机")]
        [Description("声音来源为Bot当前主要敌人时非枪声类声音的最低听觉概率提升此百分比。")]
        [Category("听觉概率")]
        [Advanced]
        [MinMax(0, 100, 1)]
        public float HEAR_CHANCE_CURRENTENEMY_MINCHANCE_HEADPHONES = 3;

        [MinMax(1f, 150f, 100f)]
        [Category("听觉距离")]
        [Name("舔包声音")]
        [Advanced]
        public float BaseSoundRange_Looting = 40f;

        [MinMax(1f, 150f, 100f)]
        [Category("听觉距离")]
        [Name("脚步声(急刹/转身)")]
        [Advanced]
        public float BaseSoundRange_MovementTurnSkid = 30f;

        [MinMax(1f, 150f, 100f)]
        [Category("听觉距离")]
        [Name("拔手雷/拉环声")]
        [Advanced]
        public float BaseSoundRange_GrenadePinDraw = 35f;

        [MinMax(1f, 150f, 100f)]
        [Category("听觉距离")]
        [Name("趴下声音")]
        [Advanced]
        public float BaseSoundRange_Prone = 50f;

        [MinMax(1f, 150f, 100f)]
        [Category("听觉距离")]
        [Name("治疗声音")]
        [Advanced]
        public float BaseSoundRange_Healing = 40f;

        [MinMax(1f, 150f, 100f)]
        [Category("听觉距离")]
        [Name("换弹声音")]
        [Advanced]
        public float BaseSoundRange_Reload = 30f;

        [MinMax(1f, 150f, 100f)]
        [Category("听觉距离")]
        [Name("手术声音")]
        [Advanced]
        public float BaseSoundRange_Surgery = 55f;

        [MinMax(1f, 150f, 100f)]
        [Category("听觉距离")]
        [Name("空仓声音")]
        [Advanced]
        public float BaseSoundRange_DryFire = 10f;

        [MinMax(1f, 150f, 100f)]
        [Category("听觉距离")]
        [Name("落地声音")]
        [Advanced]
        public float MaxSoundRange_FallLanding = 70;

        [MinMax(1f, 150f, 100f)]
        [Category("听觉距离")]
        [Name("开镜声音")]
        [Advanced]
        public float BaseSoundRange_AimingandGearRattle = 35f;

        [MinMax(1f, 150f, 100f)]
        [Category("听觉距离")]
        [Name("吃喝声音")]
        [Advanced]
        public float BaseSoundRange_EatDrink = 40f;

        [MinMax(1f, 150f, 100f)]
        [Category("听觉距离")]
        [Name("最大小队通讯范围-无耳机")]
        public float MaxRangeToReportEnemyActionNoHeadset = 50f;
        
        [Name("基础听觉延迟设置")]
        [MinMax(0.0f, 1f, 100f)]
        [Category("听觉延迟/反应延迟")]
        public HearingDelaySettings BaseHearingDelaySettings = new(0.5f, 0.1f, 0.25f, 0.66f);

        [Name("基于声音类型的听觉延迟/反应时间")]
        [Description("可选：如果在此定义了声音类型，将覆盖上面的基础听觉延迟。")]
        [MinMax(0.0f, 1f, 100f)]
        [Category("听觉延迟/反应延迟")]
        [Advanced]
        public Dictionary<SAINSoundType, HearingDelaySettings> HEARINGDELAY_SETTINGS = new() {
            { SAINSoundType.Shot, new HearingDelaySettings(0.25f, 0.1f, 0.2f, 0.6f) },
            { SAINSoundType.SuppressedShot, new HearingDelaySettings(0.3f, 0.1f, 0.25f, 0.65f) },
            { SAINSoundType.BulletImpact, new HearingDelaySettings(0.25f, 0.1f, 0.2f, 0.6f) },
        };

        public HearingDelaySettings GetDelaySettings(SAINSoundType soundType)
        {
            if (HEARINGDELAY_SETTINGS.ContainsKey(soundType))
            {
                return HEARINGDELAY_SETTINGS[soundType];
            }
            return BaseHearingDelaySettings;
        }

        [Name("全局枪声可听范围乘数")]
        [MinMax(0.1f, 2f, 100f)]
        [Category("听觉距离")]
        public float GunshotAudioMultiplier = 1f;

        [Name("全局脚步声可听范围乘数")]
        [MinMax(0.1f, 2f, 100f)]
        [Category("听觉距离")]
        public float FootstepAudioMultiplier = 1f;

        [Name("消音声音修正值")]
        [Description("使用消音器时枪声可听范围乘以此值。")]
        [MinMax(0.1f, 0.95f, 100f)]
        [Category("听觉距离")]
        public float SuppressorModifier = 0.6f;

        [Name("亚音速声音修正值")]
        [Description("使用消音器+亚音速弹药时枪声可听范围乘以此值。")]
        [MinMax(0.1f, 0.95f, 100f)]
        [Category("听觉距离")]
        public float SubsonicModifier = 0.33f;

        [Name("按弹药类型的听觉距离")]
        [Description("Bot听到各口径武器射击时的最大距离。")]
        [Category("听觉距离")]
        [MinMax(30f, 400f, 10f)]
        [Advanced]
        [DefaultDictionary(nameof(HearingDistancesDefaults))]
        public Dictionary<ECaliber, float> HearingDistances = new()
        {
            { ECaliber.Caliber9x18PM, 110f },
            { ECaliber.Caliber9x19PARA, 110f },
            { ECaliber.Caliber46x30, 120f },
            { ECaliber.Caliber9x21, 120f },
            { ECaliber.Caliber57x28, 120f },
            { ECaliber.Caliber762x25TT, 120f },
            { ECaliber.Caliber1143x23ACP, 115f },
            { ECaliber.Caliber9x33R, 125 },
            { ECaliber.Caliber545x39, 160 },
            { ECaliber.Caliber556x45NATO, 160 },
            { ECaliber.Caliber9x39, 160 },
            { ECaliber.Caliber762x35, 175 },
            { ECaliber.Caliber762x39, 175 },
            { ECaliber.Caliber366TKM, 175 },
            { ECaliber.Caliber762x51, 200f },
            { ECaliber.Caliber127x55, 200f },
            { ECaliber.Caliber762x54R, 225f },
            { ECaliber.Caliber86x70, 250f },
            { ECaliber.Caliber20g, 185 },
            { ECaliber.Caliber12g, 185 },
            { ECaliber.Caliber23x75, 210 },
            { ECaliber.Caliber26x75, 50 },
            { ECaliber.Caliber30x29, 50 },
            { ECaliber.Caliber40x46, 50 },
            { ECaliber.Caliber40mmRU, 50 },
            { ECaliber.Caliber127x108, 300 },
            { ECaliber.Caliber68x51, 200f },
            { ECaliber.Default, 125 },
        };

        [JsonIgnore]
        [Hidden]
        public static readonly Dictionary<ECaliber, float> HearingDistancesDefaults = new()
        {
            { ECaliber.Caliber9x18PM, 110f },
            { ECaliber.Caliber9x19PARA, 110f },
            { ECaliber.Caliber46x30, 120f },
            { ECaliber.Caliber9x21, 120f },
            { ECaliber.Caliber57x28, 120f },
            { ECaliber.Caliber762x25TT, 120f },
            { ECaliber.Caliber1143x23ACP, 115f },
            { ECaliber.Caliber9x33R, 125 },
            { ECaliber.Caliber545x39, 160 },
            { ECaliber.Caliber556x45NATO, 160 },
            { ECaliber.Caliber9x39, 160 },
            { ECaliber.Caliber762x35, 175 },
            { ECaliber.Caliber762x39, 175 },
            { ECaliber.Caliber366TKM, 175 },
            { ECaliber.Caliber762x51, 200f },
            { ECaliber.Caliber127x55, 200f },
            { ECaliber.Caliber762x54R, 225f },
            { ECaliber.Caliber86x70, 250f },
            { ECaliber.Caliber20g, 185 },
            { ECaliber.Caliber12g, 185 },
            { ECaliber.Caliber23x75, 210 },
            { ECaliber.Caliber26x75, 50 },
            { ECaliber.Caliber30x29, 50 },
            { ECaliber.Caliber40x46, 50 },
            { ECaliber.Caliber40mmRU, 50 },
            { ECaliber.Caliber127x108, 300 },
            { ECaliber.Caliber68x51, 200f },
            { ECaliber.Default, 125 },
        };

        public override void Init(List<ISAINSettings> list)
        {
            Helpers.ListHelpers.CloneEntries(HEAR_DISPERSION_VALUES_Defaults, HEAR_DISPERSION_VALUES);
            Helpers.ListHelpers.CloneEntries(HearingDistancesDefaults, HearingDistances);
            list.Add(this);
        }

        [JsonIgnore]
        [Hidden]
        private const string _dispersion_descr = "Higher = Less Randomization and more accuracy. " +
            "The distance to the sound's position, in meters, is divided by the number here. " +
            "Example: A unsuppressed gunshot is 150 meters away. And the dispersion value for unsuppressed gunfire is 20. So We divide 150 by 20 to result in 7.5, " +
            "so the randomized position that a bot thinks a gunshot came from is a position within 7.5 meters from the actual source of the gunshot. " +
            "Note: this randomized position must be somewhere that is walkable so that they can potentially be able the investigate it. " +
            "It is also not randomized in height to avoid bots having difficulty navigating to where they think a sound came from, " +
            "so imagine a flat plane around you that extends 7.5 meters away that includes all walkable space, " +
            "if you shoot - that bot 150 away will estimate you are somewhere within that 7.5 meter radius, on the same height level that you are on. " +
            "If there is no walkable space around you, it will find the closest walkable place.";

        [JsonIgnore]
        [Hidden]
        private const string _hear_angle_descr = "If a bot is looking at the source of a sound, they will be more accurate in their position prediction, up to the Minimum value here. " +
            "If it is directly behind them, randomization will be multiplied by the Maximumm value here. " +
            "It is a linear scale between these, so a sound directly to their right or left, will have the difference between the Maximumm and Minimum. " +
            "Setting both the Min and the Max to 1.0 will disable this system.";
    }
}