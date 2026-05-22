using Newtonsoft.Json;
using SAIN.Attributes;
using SAIN.Preset.GlobalSettings;

namespace SAIN.Preset.BotSettings.SAINSettings.Categories
{
    public class SAINMoveSettings : SAINSettingsBase<SAINMoveSettings>, ISAINSettings
    {
        [Name("横移速度")]
        [Description("Bot在近距交火时的横移速度。")]
        [MinMax(0, 1, 100)]
        public float STRAFE_SPEED = 0.5f;

        [Name("可侧身")]
        [Description("Bot能否在探头及掩体外侧身？")]
        [Category("移动选项开关")]
        public bool LEAN_TOGGLE = true;

        [Name("掩体内可侧身")]
        [Description("Bot能否在掩体内侧身？")]
        [Category("移动选项开关")]
        public bool LEAN_INCOVER_TOGGLE = true;

        [Name("可跳跃")]
        [Description("Bot能否跳跃？")]
        [Category("移动选项开关")]
        public bool JUMP_TOGGLE = true;

        [Name("自动调整姿态")]
        [Description("Bot能否根据与敌人间的物体自动调整蹲伏高度？")]
        [Category("移动选项开关")]
        public bool AUTOCROUCH_TOGGLE = true;

        [Name("可趴下")]
        [Description("Bot能否趴下？")]
        [Category("移动选项开关")]
        public bool PRONE_TOGGLE = true;

        [Name("压制时可趴下")]
        [Description("Bot被压制时能否出于恐慌反应而趴下？")]
        [Category("移动选项开关")]
        public bool PRONE_SUPPRESS_TOGGLE = true;

        [Name("可翻越")]
        [Description("Bot能否翻越障碍物？")]
        [Category("移动选项开关")]
        public bool VAULT_TOGGLE = true;

        [Name("卡住时翻越")]
        [Description("Bot卡在地图几何体上时能否翻越脱困？")]
        [Category("移动选项开关")]
        public bool VAULT_UNSTUCK_TOGGLE = true;
    }
}