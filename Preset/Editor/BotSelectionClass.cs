using EFT.UI;
using SAIN.Attributes;
using SAIN.Editor.Util;
using SAIN.Plugin;
using SAIN.Preset;
using SAIN.Preset.BotSettings.SAINSettings;
using System;
using System.Collections.Generic;
using UnityEngine;
using static SAIN.Editor.SAINLayout;

namespace SAIN.Editor
{
    public static class BotSelectionClass
    {
        static BotSelectionClass()
        {
            List<string> sections = new();
            foreach (var type in BotTypeDefinitions.BotTypes.Values)
            {
                if (!sections.Contains(type.Section))
                {
                    sections.Add(type.Section);
                }
            }

            Sections = sections.ToArray();
            SectionOpens = new bool[Sections.Length];
        }

        private static GUIStyle botTypeSectionStyle;
        private static bool[] SectionOpens;

        public static void Menu()
        {
            BeginHorizontal();
            FlexibleSpace();
            string toolTip = $"将下方设定应用到已选Bot类型。已编辑的值将导出到 SAIN/Presets/{SAINPlugin.LoadedPreset.Info.Name}/BotSettings 文件夹";
            if (BuilderClass.SaveChanges(ConfigEditingTracker.GetUnsavedValuesString(), 35f))
            {
                SAINPresetClass.ExportAll(SAINPlugin.LoadedPreset);
            }
            FlexibleSpace();
            EndHorizontal();
            BeginHorizontal();
            FlexibleSpace();
            Space(3);
            float sectionWidth = 1850f / Sections.Length;
            for (int i = 0; i < Sections.Length; i++)
            {
                BeginVertical();
                if (botTypeSectionStyle == null)
                {
                    botTypeSectionStyle = new GUIStyle(GetStyle(Style.toggle))
                    {
                        alignment = TextAnchor.MiddleLeft,
                        padding = new RectOffset(5, 5, 0, 0),
                        margin = new RectOffset(5, 5, 0, 0),
                        border = new RectOffset(5, 5, 0, 0),
                        fontStyle = FontStyle.Bold
                    };
                }
                string section = Sections[i];
                SectionOpens[i] = Toggle(SectionOpens[i], new GUIContent(section), botTypeSectionStyle, EUISoundType.MenuDropdown, Height(35), Width(sectionWidth));
                if (SectionOpens[i])
                {
                    ModifyLists.AddOrRemove(SelectedBotTypes, section, 27.5f, sectionWidth);
                }
                EndVertical();
            }
            FlexibleSpace();
            EndHorizontal();
            Space(3f);
            if (Button("清除Bot类型", "清除所有已选Bot类型", EUISoundType.ButtonBottomBarClick))
            {
                SelectedBotTypes.Clear();
            }
            Space(3f);
            BeginHorizontal();
            Label("难度", "选择你想要修改的难度。", Height(25));
            Space(3f);
            ModifyLists.AddOrRemove(SelectedDifficulties, out bool newEdit, 4, 1200f, 35f);
            Space(3f);
            if (Button("清除难度", "清除所有已选难度", null, Height(25f), Width(150f)))
            {
                SelectedDifficulties.Clear();
            }
            EndHorizontal();
            Space(5);
            SelectProperties();
        }

        public static readonly string[] Sections;

        private static readonly List<BotType> SelectedBotTypes = new();

        public static readonly BotDifficulty[] BotDifficultyOptions = [BotDifficulty.easy, BotDifficulty.normal, BotDifficulty.hard, BotDifficulty.impossible];
        public static readonly List<BotDifficulty> SelectedDifficulties = new();

        public static bool BotSettingsWereEdited;

        private static GUIEntryConfig entryConfig;

        private static void SelectProperties()
        {
            if (SelectedBotTypes.Count == 0 || SelectedDifficulties.Count == 0)
            {
                if (SelectedBotTypes.Count == 0)
                {
                    Box("未选择任何Bot类型，请在上方至少选择一个。");
                }
                else
                {
                    Box("未选择任何难度，请在上方至少选择一个。");
                }
                return;
            }

            var container = SettingsContainers.GetContainer(typeof(SAINSettingsClass), "选择要编辑的选项");
            string search = BuilderClass.SearchBox(container);

            try
            {
                foreach (var category in container.Categories)
                {
                    var toggleStyle = GetStyle(Style.toggle);
                    var oldAlignment = toggleStyle.alignment;
                    toggleStyle.alignment = TextAnchor.MiddleLeft;
                    category.CategoryInfo.MenuOpen = Toggle(category.CategoryInfo.MenuOpen, category.CategoryInfo.Name, null, Height(PresetHandler.EditorDefaults.ConfigEntryHeight));
                    toggleStyle.alignment = oldAlignment;
                    if (!category.CategoryInfo.MenuOpen)
                    {
                        continue;
                    }
                    // Get the fields in this category
                    for (int i = 0; i < category.FieldAttributesList.Count; i++)
                    {
                        var fieldAtt = category.FieldAttributesList[i];
                        // Check if the user is searching
                        if (!string.IsNullOrEmpty(search) && !fieldAtt.Name.ToLower().Contains(search))
                        {
                            continue;
                        }
                        BeginHorizontal();
                        Space(30f);
                        toggleStyle.alignment = TextAnchor.MiddleLeft;
                        fieldAtt.MenuOpen = Toggle(fieldAtt.MenuOpen, fieldAtt.Name, fieldAtt.Description, null, Height(PresetHandler.EditorDefaults.ConfigEntryHeight), Width(500f));
                        toggleStyle.alignment = oldAlignment;
                        EndHorizontal();
                        if (!fieldAtt.MenuOpen)
                        {
                            continue;
                        }
                        for (int k = 0; k < SelectedBotTypes.Count; k++)
                        {
                            var bot = SelectedBotTypes[k];
                            if (SAINPlugin.LoadedPreset.BotSettings.SAINSettings.TryGetValue(bot.WildSpawnType, out var settings))
                            {
                                if (entryConfig == null)
                                {
                                    entryConfig = new GUIEntryConfig();
                                }

                                for (int t = 0; t < SelectedDifficulties.Count; t++)
                                {
                                    var difficulty = SelectedDifficulties[t];
                                    if (settings.Settings.TryGetValue(difficulty, out var SAINSettings))
                                    {
                                        BeginHorizontal();
                                        Space(60);
                                        object categoryValue = category.GetValue(SAINSettings);
                                        object value = fieldAtt.GetValue(categoryValue);
                                        Label($"{bot.Name} : {difficulty}", Height(PresetHandler.EditorDefaults.ConfigEntryHeight), Width(200));
                                        value = AttributesGUI.EditFloatBoolInt(ref value, categoryValue, fieldAtt, entryConfig, 0, out bool newEdit, false, false);
                                        if (newEdit)
                                            ConfigEditingTracker.Add(fieldAtt.Name, value);
                                        fieldAtt.SetValue(categoryValue, value);
                                        EndHorizontal();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }
    }
}