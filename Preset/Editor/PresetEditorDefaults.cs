using SAIN.Attributes;
using SAIN.Plugin;
using SAIN.Preset.GlobalSettings;

namespace SAIN.Editor
{
    public class PresetEditorDefaults : SAINSettingsBase<PresetEditorDefaults>, ISAINSettings
    {
        public PresetEditorDefaults()
        {
            DefaultPreset = PresetHandler.DefaultPreset;
        }

        public PresetEditorDefaults(string selectedPreset)
        {
            SelectedCustomPreset = selectedPreset;
            DefaultPreset = PresetHandler.DefaultPreset;
        }

        [Category("通用")]
        [Name("显示高级Bot配置")]
        [Description("显示高级设置。大多数应保持默认，除非想修改特定内容。")]
        public bool AdvancedBotConfigs = false;

        [Category("通用")]
        [Name("显示开发者选项")]
        [Description("除非通过研读SAIN源码确切知道作用，否则不应修改。")]
        public bool DevBotConfigs = false;

        [Category("通用")]
        [Hidden]
        public SAINDifficulty SelectedDefaultPreset = SAINDifficulty.none;

        [Category("通用")]
        [Hidden]
        public string SelectedCustomPreset;

        [Category("通用")]
        [Hidden]
        public string DefaultPreset;

        [Name("GUI界面缩放")]
        [Category("GUI尺寸")]
        [MinMax(1f, 2f, 100f)]
        [DefaultFloat(1f)]
        public float ConfigScaling = 1f;

        [Name("配置项滑块")]
        [Category("GUI尺寸")]
        public bool SliderToggle = true;

        [Category("GUI尺寸")]
        [Name("配置项行高")]
        [Advanced]
        [MinMax(12f, 40f, 1f)]
        [DefaultFloat(20f)]
        public float ConfigEntryHeight = 20f;

        [Category("GUI尺寸")]
        [DeveloperOption]
        [SimpleValue]
        [MinMax(0.25f, 0.7f, 1000f)]
        [DefaultFloat(0.59f)]
        public float ConfigSliderWidth = 0.59f;

        [Category("GUI尺寸")]
        [DeveloperOption]
        [SimpleValue]
        [MinMax(0.02f, 0.1f, 1000f)]
        [DefaultFloat(0.045f)]
        public float ConfigResultsWidth = 0.045f;

        [Category("GUI尺寸")]
        [DeveloperOption]
        [SimpleValue]
        [MinMax(0.01f, 0.1f, 1000f)]
        [DefaultFloat(0.024f)]
        public float ConfigResetWidth = 0.024f;

        [Category("GUI尺寸")]
        [DeveloperOption]
        [SimpleValue]
        [MinMax(0f, 5f, 1f)]
        [DefaultFloat(2f)]
        public float SubList_Indent_Vertical = 2f;

        [Category("GUI尺寸")]
        [Name("子列表水平缩进")]
        [Advanced]
        [SimpleValue]
        [MinMax(0f, 100f, 1f)]
        [DefaultFloat(25f)]
        public float SubList_Indent_Horizontal = 25f;
    }
}