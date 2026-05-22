using SAIN.Attributes;
using System.Collections.Generic;

namespace SAIN.Preset.GlobalSettings
{
    public class GlobalDifficultySettings : SAINSettingsBase<GlobalDifficultySettings>, ISAINSettings
    {
        [Name("视觉距离乘数")]
        [Description("数值越高=难度越大。")]
        [DifficultyModAttribute]
        public float VisibleDistCoef = 1f;

        [Name("视觉速度乘数")]
        [Description("数值越低=难度越大。")]
        [DifficultyModAttribute]
        public float GainSightCoef = 1f;

        [Name("散布乘数")]
        [Description("数值越低=散布越小，难度越大。")]
        [DifficultyModAttribute]
        public float ScatteringCoef = 1f;

        //[Name("Scatter Priority Multiplier")]
        //[Description("Lower is more difficult.")]
        //[DifficultyModAttribute]
        //public float PriorityScatteringCoef = 1f;

        [Name("听觉距离乘数")]
        [Description("数值越高=难度越大。")]
        [DifficultyModAttribute]
        public float HearingDistanceCoef = 1f;

        [Name("攻击性乘数")]
        [Description("数值越高=难度越大。")]
        [DifficultyModAttribute]
        public float AggressionCoef = 1f;

        [Name("精确瞄准速度乘数")]
        [Description("数值越低=难度越大。")]
        [DifficultyModAttribute]
        public float PrecisionSpeedCoef = 1f;

        [Name("精度速度乘数")]
        [Description("数值越低=难度越大。")]
        [DifficultyModAttribute]
        public float AccuracySpeedCoef = 1f;

        public override void Init(List<ISAINSettings> list)
        {
            list.Add(this);
        }
    }
}