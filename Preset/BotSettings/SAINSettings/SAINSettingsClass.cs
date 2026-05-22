using SAIN.Preset.BotSettings.SAINSettings.Categories;
using SAIN.Preset.GlobalSettings;
using SAIN.Attributes;
using SAIN.Preset.Personalities;

namespace SAIN.Preset.BotSettings.SAINSettings
{
    public class SAINSettingsClass : SettingsGroupBase<SAINSettingsClass>
    {
        [Name("难度设置")]
        public DifficultySettings Difficulty = new();
        [Name("核心设置")]
        public SAINCoreSettings Core = new();
        [Name("瞄准设置")]
        public SAINAimingSettings Aiming = new();
        [Name("Boss设置")]
        public SAINBossSettings Boss = new();
        [Name("变更设置")]
        public SAINChangeSettings Change = new();
        [Name("手雷设置")]
        public SAINGrenadeSettings Grenade = new();
        [Name("听觉设置")]
        public SAINHearingSettings Hearing = new();
        [Name("布阵设置")]
        public SAINLaySettings Lay = new();
        [Name("视觉设置")]
        public SAINLookSettings Look = new();
        [Name("思维设置")]
        public SAINMindSettings Mind = new();
        [Name("移动设置")]
        public SAINMoveSettings Move = new();
        [Name("巡逻设置")]
        public SAINPatrolSettings Patrol = new();
        [Name("散布设置")]
        public SAINScatterSettings Scattering = new();
        [Name("射击设置")]
        public SAINShootSettings Shoot = new();

        public override void InitList()
        {
            SettingsList.Clear();
            SettingsList.Add(Difficulty);
            SettingsList.Add(Core);
            SettingsList.Add(Aiming);
            SettingsList.Add(Boss);
            SettingsList.Add(Change);
            SettingsList.Add(Grenade);
            SettingsList.Add(Hearing);
            SettingsList.Add(Lay);
            SettingsList.Add(Look);
            SettingsList.Add(Mind);
            SettingsList.Add(Patrol);
            SettingsList.Add(Scattering);
            SettingsList.Add(Shoot);
        }
    }
}