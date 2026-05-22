using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings
{
    public class ElevationVisionSettings : SAINSettingsBase<ElevationVisionSettings>, ISAINSettings
    {
        [Name("启用高低差视觉")]
        public bool Enabled = true;

        [Name("高仰角角度范围")]
        [Description(
            "Bot视线到敌人角度差达到此值时完全应用高仰角修正。修正值随角度差平滑过渡。")]
        [MinMax(1f, 90f, 1f)]
        public float HighElevationMaxAngle = 60f;

        [Name("高仰角视觉修正值")]
        [Description(
            "敌人高于Bot且视角差>=高仰角角度时Bot发现速度乘以此值。越高=越慢。1.2=多用20%时间发现敌人。")]
        [MinMax(1f, 5f, 100f)]
        public float HighElevationVisionModifier = 2.5f;

        [Name("低俯角角度范围")]
        [Description(
            "Bot视线到敌人角度差达到此值时完全应用低俯角修正。修正值随角度差平滑过渡。")]
        [MinMax(1f, 90f, 1f)]
        public float LowElevationMaxAngle = 30f;

        [Name("低俯角视觉修正值")]
        [Description(
            "敌人低于Bot且视角差>=低俯角角度时Bot发现速度乘以此值。越低=越快。0.85=发现时间缩短15%。")]
        [MinMax(0.01f, 1f, 100f)]
        public float LowElevationVisionModifier = 0.75f;
    }
}