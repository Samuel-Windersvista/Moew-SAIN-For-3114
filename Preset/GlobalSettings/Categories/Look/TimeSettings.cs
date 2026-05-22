using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings
{
    public class TimeSettings : SAINSettingsBase<TimeSettings>, ISAINSettings
    {
        [Name("夜间视觉距离乘数")]
        [Category("夜间视觉效果")]
        [Description("夜间视觉距离缩减倍率。0.2=夜间为基础距离20%。")]
        [MinMax(0.01f, 1f, 1000f)]
        public float NightTimeVisionModifier = 0.2f;

        [Name("雪地夜间视觉距离乘数")]
        [Category("夜间视觉效果")]
        [Description("雪地夜间视觉距离缩减倍率。0.2=夜间为基础距离20%。")]
        [MinMax(0.01f, 1f, 1000f)]
        public float NightTimeVisionModifierSnow = 0.35f;

        [Name("视觉速度最大乘数")]
        [Category("夜间视觉效果")]
        [Description("Bot发现敌人速度最多缩减X倍取决于能见度和时间。越高=视力越差。")]
        [MinMax(1f, 10f, 1000f)]
        public float TIME_GAIN_SIGHT_SCALE_MAX = 3f;

        [Name("天气最低乘数")]
        [Category("天气视觉效果")]
        [Description("天气对视觉距离乘数不会低于此值。")]
        [MinMax(0.01f, 1f, 1000f)]
        [Advanced]
        public float VISION_WEATHER_MIN_COEF = 0.33f;

        [Name("天气最低视觉距离")]
        [Category("天气视觉效果")]
        [Description("基础视觉距离(经过天气计算、时间效果前)不会低于此值(米)。")]
        [MinMax(0f, 100f, 1000f)]
        [Advanced]
        public float VISION_WEATHER_MIN_DIST_METERS = 30f;

        [Name("雾天最低效果")]
        [Category("天气视觉效果-雾")]
        [Description("雾在X到1之间缩放取决于强度。0.4=最多降至40%视觉距离。")]
        [MinMax(0.01f, 1f, 1000f)]
        [Advanced]
        public float VISION_WEATHER_FOG_MAXCOEF = 0.4f;

        [Name("极小雨阈值")]
        [Category("天气视觉效果-雨")]
        [Description("雨量在0到1之间缩放。小于等于此值则使用下方乘数。")]
        [MinMax(0.01f, 1f, 1000f)]
        [Advanced]
        public float VISION_WEATHER_RAIN_SRINKLE_THRESH = 0.1f;

        [Name("极小雨缩放系数")]
        [Category("天气视觉效果-雨")]
        [Description("")]
        [MinMax(0.01f, 1f, 1000f)]
        [Advanced]
        public float VISION_WEATHER_RAIN_SRINKLE_COEF = 0.9f;

        [Name("小雨阈值")]
        [Category("天气视觉效果-雨")]
        [Description("雨量在0到1之间缩放。小于等于此值则使用下方乘数。")]
        [MinMax(0.01f, 1f, 1000f)]
        [Advanced]
        public float VISION_WEATHER_RAIN_LIGHT_THRESH = 0.35f;

        [Name("小雨缩放系数")]
        [Category("天气视觉效果-雨")]
        [Description("")]
        [MinMax(0.01f, 1f, 1000f)]
        [Advanced]
        public float VISION_WEATHER_RAIN_LIGHT_COEF = 0.65f;

        [Name("中雨阈值")]
        [Category("天气视觉效果-雨")]
        [Description("雨量在0到1之间缩放。小于等于此值则使用下方乘数。")]
        [MinMax(0.01f, 1f, 1000f)]
        [Advanced]
        public float VISION_WEATHER_RAIN_NORMAL_THRESH = 0.5f;

        [Name("中雨缩放系数")]
        [Category("天气视觉效果-雨")]
        [Description("")]
        [MinMax(0.01f, 1f, 1000f)]
        [Advanced]
        public float VISION_WEATHER_RAIN_NORMAL_COEF = 0.5f;

        [Name("大雨阈值")]
        [Category("天气视觉效果-雨")]
        [Description("雨量在0到1之间缩放。小于等于此值则使用下方乘数。")]
        [MinMax(0.01f, 1f, 1000f)]
        [Advanced]
        public float VISION_WEATHER_RAIN_HEAVY_THRESH = 0.75f;

        [Name("大雨缩放系数")]
        [Category("天气视觉效果-雨")]
        [Description("")]
        [MinMax(0.01f, 1f, 1000f)]
        [Advanced]
        public float VISION_WEATHER_RAIN_HEAVY_COEF = 0.45f;

        [Name("暴雨缩放系数")]
        [Category("天气视觉效果-雨")]
        [Description("雨量超过大雨阈值时使用此缩放系数。")]
        [MinMax(0.01f, 1f, 1000f)]
        [Advanced]
        public float VISION_WEATHER_RAIN_DOWNPOUR_COEF = 0.4f;

        [Name("晴天云量阈值")]
        [Category("天气视觉效果-云")]
        [Description("云量在0到1之间缩放。低于此值对视觉无影响。")]
        [MinMax(0.01f, 1f, 1000f)]
        [Advanced]
        public float VISION_WEATHER_NOCLOUDS_THRESH = 0.33f;

        [Name("多云阈值")]
        [Category("天气视觉效果-云")]
        [Description("云量在0到1之间缩放。低于此值视为多云，高于此值视为阴天。")]
        [MinMax(0.01f, 1f, 1000f)]
        [Advanced]
        public float VISION_WEATHER_CLOUDY_THRESH = 0.7f;

        [Name("多云缩放系数")]
        [Category("天气视觉效果-云")]
        [Description("多云时视觉缩减倍率。")]
        [MinMax(0.01f, 1f, 1000f)]
        [Advanced]
        public float VISION_WEATHER_CLOUDY_COEF = 0.8f;

        [Name("阴天缩放系数")]
        [Category("天气视觉效果-云")]
        [Description("阴天时视觉缩减倍率。")]
        [MinMax(0.01f, 1f, 1000f)]
        [Advanced]
        public float VISION_WEATHER_OVERCAST_COEF = 0.7f;

        [Name("黎明开始时刻")]
        [MinMax(5f, 8f, 10f)]
        [Advanced]
        public float HourDawnStart = 6f;

        [Name("黎明结束时刻")]
        [MinMax(6f, 9f, 10f)]
        [Advanced]
        public float HourDawnEnd = 8f;

        [Name("黄昏开始时刻")]
        [MinMax(19f, 22f, 10f)]
        [Advanced]
        public float HourDuskStart = 20f;

        [Name("黄昏结束时刻")]
        [MinMax(20f, 23f, 10f)]
        [Advanced]
        public float HourDuskEnd = 22f;
    }
}