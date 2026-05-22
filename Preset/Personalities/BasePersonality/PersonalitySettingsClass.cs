using Newtonsoft.Json;
using SAIN.Attributes;
using SAIN.Preset.GlobalSettings;

namespace SAIN.Preset.Personalities
{
    public class PersonalitySettingsClass : SettingsGroupBase<PersonalitySettingsClass>
    {
        [JsonConstructor]
        public PersonalitySettingsClass()
        {
        }

        public PersonalitySettingsClass(EPersonality personality)
        {
            Name = personality.ToString();
            Description = PersonalityDescriptionsClass.PersonalityDescriptions[personality];
        }

        [Hidden]
        public string Name;
        [Hidden]
        public string Description;

        [Name("分配设置")]
        public PersonalityAssignmentSettings Assignment = new();
        [Name("行为设置")]
        public PersonalityBehaviorSettings Behavior = new();
        [Name("难度设置")]
        public DifficultySettings Difficulty = new();

        public override void Init()
        {
            InitList();
            CreateDefaults();
            Behavior.Init();
            Update();
        }

        public override void InitList()
        {
            SettingsList.Clear();
            SettingsList.Add(Assignment);
            SettingsList.Add(Difficulty);
        }
    }
}