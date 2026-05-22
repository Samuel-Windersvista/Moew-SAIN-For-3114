using SAIN.Attributes;

namespace SAIN.Preset.Personalities
{
    public class PersonalityBehaviorSettings : SettingsGroupBase<PersonalityBehaviorSettings>, ISettingsGroup
    {
        [Name("通用")]
        public PersonalityGeneralSettings General = new();
        [Name("搜索")]
        public PersonalitySearchSettings Search = new();
        [Name("冲锋")]
        public PersonalityRushSettings Rush = new();
        [Name("掩体")]
        public PersonalityCoverSettings Cover = new();
        [Name("说话")]
        public PersonalityTalkSettings Talk = new();

        public override void InitList()
        {
            SettingsList.Clear();
            SettingsList.Add(Cover);
            SettingsList.Add(General);
            SettingsList.Add(Rush);
            SettingsList.Add(Search);
            SettingsList.Add(Talk);
        }
    }
}