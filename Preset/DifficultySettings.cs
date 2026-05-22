using SAIN.Attributes;
using System.Collections.Generic;

namespace SAIN.Preset.GlobalSettings
{
    public class DifficultySettings : SAINSettingsBase<DifficultySettings>, ISAINSettings
    {
        [Name("视觉距离乘数")]
        [Description("数值越高=难度越大。")]
        [DifficultyModAttribute]
        public float VisibleDistCoef = 1f;

        [Name("视觉速度乘数")]
        [Description("数值越高=难度越大。2意味着Bot发现敌人速度快2倍。")]
        [DifficultyModAttribute]
        public float GainSightCoef = 1f;

        [Name("散布乘数")]
        [Description("数值越低=散布越小，难度越大。")]
        [DifficultyModAttribute]
        public float ScatteringCoef = 1f;

        [Name("听觉距离乘数")]
        [Description("数值越高=难度越大。")]
        [DifficultyModAttribute]
        public float HearingDistanceCoef = 1f;

        [Name("攻击性乘数")]
        [Description("数值越高=难度越大。影响Bot进入搜索前的等待时间和发现敌人时原地坚守还击的时间。")]
        [DifficultyModAttribute]
        public float AggressionCoef = 1f;

        [Name("精确瞄准速度乘数")]
        [Description("数值越高=难度越大。")]
        [DifficultyModAttribute]
        public float PRECISION_SPEED_COEF = 1f;

        [Name("精度速度乘数")]
        [Description("数值越低=难度越大。影响Bot瞄准时完成射击对准所需的时间。")]
        [DifficultyModAttribute]
        public float ACCURACY_SPEED_COEF = 1f;

        public override void Init(List<ISAINSettings> list)
        {
            list.Add(this);
        }
    }
}