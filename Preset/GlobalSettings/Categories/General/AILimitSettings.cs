using SAIN.Attributes;
using System.Collections.Generic;

namespace SAIN.Preset.GlobalSettings
{
    public class AILimitSettings : SAINSettingsBase<AILimitSettings>, ISAINSettings
    {
        [Name("AI对AI时限制SAIN功能-全局开关")]
        [Description("AI互斗且附近无玩家时禁用部分功能。用自由镜头观战可关闭。")]
        public bool LimitAIvsAIGlobal = true;

        [Name("AI限制更新频率")]
        [Description("检查所有玩家距离的间隔(秒)。")]
        [MinMax(1f, 5f, 10f)]
        public float AILimitUpdateFrequency = 3f;

        [Name("AI限制距离范围")]
        [Description("根据Bot与最近真人玩家的距离(米)分配不同等级的AI限制。")]
        [MinMax(150f, 600f, 1f)]
        public Dictionary<AILimitSetting, float> AILimitRanges = new()
        {
            { AILimitSetting.Far, 150f },
            { AILimitSetting.VeryFar, 250f },
            { AILimitSetting.Narnia, 400f },
        };

        [Name("限制AI对AI视觉")]
        [Description("Bot互斗且远离玩家时缩减视觉范围。")]
        public bool LimitAIvsAIVision = true;

        [Name("最大视觉距离范围")]
        [Description("根据AI限制等级定义Bot的最大视觉距离(米)。")]
        [MinMax(10f, 200f, 1f)]
        public Dictionary<AILimitSetting, float> MaxVisionRanges = new()
        {
            { AILimitSetting.Far, 200f },
            { AILimitSetting.VeryFar, 100f },
            { AILimitSetting.Narnia, 50f },
        };

        [Name("限制AI对AI听觉")]
        [Description("Bot互斗且远离玩家时缩减听觉范围。")]
        public bool LimitAIvsAIHearing = true;

        [Name("最大听觉距离范围")]
        [Description("根据AI限制等级定义Bot的最大听觉距离(米)。")]
        [MinMax(10f, 200f, 1f)]
        public Dictionary<AILimitSetting, float> MaxHearingRanges = new()
        {
            { AILimitSetting.Far, 100f },
            { AILimitSetting.VeryFar, 60f },
            { AILimitSetting.Narnia, 25f },
        };

    }
}