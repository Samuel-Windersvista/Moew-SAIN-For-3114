using SAIN.Attributes;
using System.Collections.Generic;

namespace SAIN.Preset.GlobalSettings
{
    public class MoveSettings : SAINSettingsBase<MoveSettings>, ISAINSettings
    {
        [Name("可侧身-全局")]
        [Description("Bot能否在探头及掩体外侧身？")]
        [Category("移动选项开关")]
        public bool LEAN_TOGGLE = true;

        [Name("掩体内可侧身-全局")]
        [Description("Bot能否在掩体内侧身？")]
        [Category("移动选项开关")]
        public bool LEAN_INCOVER_TOGGLE = true;

        [Name("可跳跃-全局")]
        [Description("Bot能否跳跃？")]
        [Category("移动选项开关")]
        public bool JUMP_TOGGLE = true;

        [Name("自动调整姿态-全局")]
        [Description("Bot能否根据与敌人之间的物体自动调整蹲伏高度？")]
        [Category("移动选项开关")]
        public bool AUTOCROUCH_TOGGLE = true;

        [Name("可趴下-全局")]
        [Description("Bot能否趴下？")]
        [Category("移动选项开关")]
        public bool PRONE_TOGGLE = true;

        [Name("压制时可趴下-全局")]
        [Description("Bot被压制时能否出于恐慌反应而趴下？")]
        [Category("移动选项开关")]
        public bool PRONE_SUPPRESS_TOGGLE = true;

        [Name("可翻越-全局")]
        [Description("Bot能否翻越障碍物？")]
        [Category("移动选项开关")]
        public bool VAULT_TOGGLE = true;

        [Name("卡住时翻越-全局")]
        [Description("Bot卡在地图几何体上时能否翻越脱困？")]
        [Category("移动选项开关")]
        public bool VAULT_UNSTUCK_TOGGLE = true;

        [Name("强制恒定冲刺速度")]
        [Description("原版中Bot冲刺速度为恒定值。禁用后Bot将使用与玩家相同的移速计算。")]
        [Category("冲刺")]
        [Advanced]
        public bool EditSprintSpeed = false;

        [Name("未移动距离阈值")]
        [Description("Bot距上次位置多远(米平方)才被视为未移动。")]
        [Category("冲刺")]
        [Advanced]
        [MinMax(0.01f, 1.5f, 100f)]
        public float BotSprintNotMovingThreshold = 0.5f;

        [Name("未移动检查频率")]
        [Description("每X秒检查一次Bot是否移动。")]
        [Category("冲刺")]
        [Advanced]
        [MinMax(0.01f, 1.5f, 100f)]
        public float BotSprintNotMovingCheckFreq = 0.5f;

        [Name("未移动触发翻越时间")]
        [Description("Bot未移动时间超过此值时尝试翻越脱困。")]
        [Category("冲刺")]
        [Advanced]
        [MinMax(0.01f, 1.5f, 100f)]
        public float BOT_NOMOVE_TRYVAULT_TIME = 0.5f;

        [Name("未移动触发跳跃时间")]
        [Description("Bot未移动时间超过此值时尝试跳跃脱困。")]
        [Category("冲刺")]
        [Advanced]
        [MinMax(0.01f, 1.5f, 100f)]
        public float BOT_NOMOVE_TRYJUMP_TIME = 1f;

        [Name("未移动重算路径时间")]
        [Description("Bot未移动时间超过此值时重新计算目的地路径。")]
        [Category("冲刺")]
        [Advanced]
        [MinMax(0.01f, 3f, 100f)]
        public float BOT_NOMOVE_RECALC_TIME = 3f;

        [Name("路径转角-冲刺到达距离")]
        [Description("Bot距路径转角多远时认为已到达并导航到下一个转角。")]
        [Category("冲刺")]
        [Advanced]
        [MinMax(0.01f, 1f, 100f)]
        public float BotSprintCornerReachDist = 0.3f;

        [Name("行走转角到达距离")]
        [Description("Bot距路径转角多远时认为已到达并导航到下一个转角。")]
        [Advanced]
        [MinMax(0.01f, 1f, 100f)]
        public float BotWalkCornerReachDist = 0.1f;

        [Name("暂停冲刺的最大转角角度")]
        [Description("Bot沿路径接近转角时超过此角度将暂停冲刺转身，而非保持冲刺状态转弯。")]
        [Category("冲刺")]
        [Advanced]
        [MinMax(1f, 90f, 1f)]
        public float BotSprintCurrentCornerAngleMax = 25f;

        public override void Init(List<ISAINSettings> list)
        {
            list.Add(this);
        }
    }
}