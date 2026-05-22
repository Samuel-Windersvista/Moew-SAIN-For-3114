using SAIN.Helpers;
using SAIN.Plugin;
using SAIN.Preset;
using System.Collections.Generic;
using static SAIN.Attributes.AttributesGUI;

namespace SAIN.Editor.GUISections
{
    public static class BotPersonalityEditor
    {
        public static void ClearCache()
        {
            ListHelpers.ClearCache(OpenPersMenus);
        }

        public static void PersonalityMenu()
        {
            string toolTip = $"将下方设定应用到所选个性。已编辑的值将导出到 SAIN/Presets/{SAINPlugin.LoadedPreset.Info.Name}/Personalities 文件夹";

            if (BuilderClass.SaveChanges(ConfigEditingTracker.GetUnsavedValuesString(), 35))
            {
                SAINPresetClass.ExportAll(SAINPlugin.LoadedPreset);
            }

            _selected = SelectPersonality(_selected, 35f, 4);
            if (_selected != EPersonality.None &&
                SAINPresetClass.Instance.PersonalityManager.PersonalityDictionary.TryGetValue(_selected, out var settings))
            {
                EditAllValuesInObj(settings, out bool newEdit, null, null, 1);
            }
            ;
        }

        public static EPersonality SelectPersonality(EPersonality selected, float height, int optionsPerLine)
        {
            if (_options.Count == 0)
            {
                _options.AddRange(SAINPresetClass.Instance.PersonalityManager.PersonalityDictionary.Keys);
            }
            return BuilderClass.SelectionGrid(selected, height, optionsPerLine, _options);
        }

        private static EPersonality _selected = EPersonality.None;
        public static bool PersonalitiesWereEdited => ConfigEditingTracker.UnsavedChanges;

        private static List<EPersonality> _options = new();

        private static readonly Dictionary<string, bool> OpenPersMenus = new();
    }
}