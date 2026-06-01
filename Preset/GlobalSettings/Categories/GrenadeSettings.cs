using SAIN.Attributes;
using System.Collections.Generic;

namespace SAIN.Preset.GlobalSettings
{
    public class GrenadeSettings : SAINSettingsBase<GrenadeSettings>, ISAINSettings
    {
        [Name("启用 SAIN 手雷躲避")]
        [Description("关闭后手雷躲避完全恢复 EFT 原版 BewareGrenade 行为。")]
        [Category("手雷躲避")]
        public bool ENABLED = true;

        [Name("手雷安全距离")]
        [Description("认为破片手雷无威胁的最短逃跑距离（米），Bot 会尝试跑出此距离。")]
        [Category("手雷躲避")]
        [MinMax(5f, 30f)]
        public float FRAG_SAFE_DISTANCE = 15f;

        [Name("掩体缩减距离")]
        [Description("当逃跑方向存在掩体遮挡时，安全距离缩短至此值（米）。")]
        [Category("手雷躲避")]
        [MinMax(3f, 15f)]
        public float COVER_REDUCED_DISTANCE = 8f;

        [Name("垂直楼层过滤")]
        [Description("如果手雷在不同楼层且导航路径不可达，是否忽略威胁。")]
        [Category("手雷躲避")]
        public bool VERTICAL_FLOOR_FILTER = true;

        [Name("紧急反应距离")]
        [Description("即使 CanReact=false，距离小于此值的正在接近的手雷仍会触发紧急躲避。")]
        [Category("手雷躲避")]
        [MinMax(3f, 15f)]
        public float EMERGENCY_REACT_DISTANCE = 8f;

        [Name("最大威胁生命周期")]
        [Description("手雷威胁超过此秒数后自动清除（防止卡死位置的手雷导致无限躲避）。")]
        [Category("手雷躲避")]
        [MinMax(5f, 30f)]
        public float MAX_THREAT_LIFETIME = 10f;

        [Name("有效掩体最小尺寸")]
        [Description("Raycast 命中的碰撞体边界盒小于此尺寸不计为有效掩体。")]
        [Category("手雷躲避")]
        [MinMax(0.2f, 2f)]
        public float MIN_COVER_SIZE = 0.5f;

        [Name("掩体最小高度")]
        [Description("Raycast 命中的碰撞体高度低于此值不计为有效掩体。")]
        [Category("手雷躲避")]
        [MinMax(0.2f, 2f)]
        public float MIN_COVER_HEIGHT = 1.0f;

        public override void Init(List<ISAINSettings> list)
        {
            list.Add(this);
        }
    }
}
