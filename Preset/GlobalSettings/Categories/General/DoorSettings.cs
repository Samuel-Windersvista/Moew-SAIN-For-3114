using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings
{
    public class DoorSettings : SAINSettingsBase<DoorSettings>, ISAINSettings
    {
        //[Name("SAIN Door Handling")]
        //[Description("WIP")]
        //public bool NewDoorOpening = true;

        [Name("禁用开门动画")]
        [Description("Bot自动开门而非卡在动画中。加载Fika模组则忽略且始终禁用。")]
        public bool NoDoorAnimations = true;

        [Name("始终推开房门")]
        [Description("仅在禁用开门动画时生效。Bot始终推开门避免卡住。偶尔门会鬼畜但极大改善导航。")]
        public bool InvertDoors = true;

        [Name("禁用所有门")]
        [Description("门太难做了全关掉。仅对可正常开关的门生效。")]
        public bool DisableAllDoors = false;
    }

}