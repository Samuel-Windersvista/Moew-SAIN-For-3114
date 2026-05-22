using SAIN.Attributes;
using System.Collections.Generic;

namespace SAIN.Preset.GlobalSettings
{
    public class HitEffectSettings : SAINSettingsBase<HitEffectSettings>, ISAINSettings
    {
        [Name("新版中弹反应")]
        [Description("关闭则使用原版Bot中弹反应。关闭后以下设置均无效。")]
        public bool HIT_REACTION_TOGGLE = false;

        [Name("中弹效果乘数")]
        [Description("数值越高=中弹对Bot瞄准影响越大。")]
        [MinMax(0.01f, 5f, 100f)]
        public float DAMAGE_MANUAL_MODIFIER = 1f;

        [Name("使用中弹方向")]
        [Description("Instead of randomly calculating an angle to affect bot aim. " +
            "Use the direction they were hit from, so if they are shot in the right arm, their aim gets kicked to the right. " +
            "If they are shot in the leg, kick their aim down towards their leg.")]
        public bool USE_HIT_POINT_DIRECTION = true;

        [Name("中弹方向基础距离")]
        [MinMax(0.01f, 2f, 100f)]
        [Advanced]
        public float HIT_POINT_DIRECTION_BASE_DISTANCE = 0.5f;

        [Name("基础中弹效果最小角度")]
        [Description("若使用中弹方向开启则此项无效。")]
        [MinMax(1f, 60f, 10f)]
        [Advanced]
        public float DAMAGE_BASE_MIN_ANGLE = 7f;

        [Name("基础中弹效果最大角度")]
        [Description("若使用中弹方向开启则此项无效。")]
        [MinMax(1f, 90f, 10f)]
        [Advanced]
        public float DAMAGE_BASE_MAX_ANGLE = 10f;

        [Name("伤害基准值")]
        [Description("The amount of damage a bot received is divided by this number to produce a multiplier for how much to kick their aim. " +
            "So if the value here is 50, and a bot is shot by a bullet that does 100 damage, it will result in their hit reaction being 2x, or twice as impactful.")]
        [MinMax(10f, 100f, 1f)]
        [Advanced]
        public float DAMAGE_RECEIVED_BASELINE = 50;

        [Name("最小中弹伤害乘数")]
        [Description("")]
        [MinMax(0.01f, 1f, 100f)]
        [Advanced]
        public float DAMAGE_MIN_MOD = 0.2f;

        [Name("最大中弹伤害乘数")]
        [Description("")]
        [MinMax(1f, 10f, 100f)]
        [Advanced]
        public float DAMAGE_MAX_MOD = 3f;

        public override void Init(List<ISAINSettings> list)
        {
            list.Add(this);
        }
    }
}