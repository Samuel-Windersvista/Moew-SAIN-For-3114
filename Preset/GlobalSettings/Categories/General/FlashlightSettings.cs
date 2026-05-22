using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings
{
    public class FlashlightSettings : SAINSettingsBase<FlashlightSettings>, ISAINSettings
    {
        [Name("致盲效果强度")]
        [MinMax(0.25f, 10f, 100f)]
        public float DazzleEffectiveness = 3f;

        [Name("最大致盲距离")]
        [MinMax(0f, 60f)]
        public float MaxDazzleRange = 40f;

        [Name("黑暗建筑中允许开灯")]
        public bool AllowLightOnForDarkBuildings = true;

        [Name("无敌人时关闭手电-PMC")]
        public bool TurnLightOffNoEnemyPMC = true;

        [Name("无敌人时关闭手电-Scav")]
        public bool TurnLightOffNoEnemySCAV = false;

        [Name("无敌人时关闭手电-Goons")]
        public bool TurnLightOffNoEnemyGOONS = true;

        [Name("无敌人时关闭手电-Boss")]
        public bool TurnLightOffNoEnemyBOSS = false;

        [Name("无敌人时关闭手电-随从")]
        public bool TurnLightOffNoEnemyFOLLOWER = false;

        [Name("无敌人时关闭手电-突袭者/流浪者")]
        public bool TurnLightOffNoEnemyRAIDERROGUE = false;

        [Name("调试手电")]
        [Advanced]
        public bool DebugFlash = false;

        [Name("搞笑模式")]
        public bool SillyMode = false;
    }
}