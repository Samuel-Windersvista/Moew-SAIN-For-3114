using EFT.UI;
using SAIN.Plugin;
using SAIN.Preset;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using static SAIN.Editor.SAINLayout;
using JsonUtility = SAIN.Helpers.JsonUtility;

namespace SAIN.Editor.GUISections
{
    public static class PresetSelection
    {
        private static readonly List<SAINPresetDefinition> defaultPresets = SAINDifficultyClass.DefaultPresetDefinitions.Values.ToList();

        private const float PRESET_LABEL_HEIGHT = 55f;
        private const float PRESET_OPTION_HEIGHT = 25f;
        private const float PRESET_OPTION_WIDTH = 500;
        private const float PRESET_BASE_OPTION_WIDTH = 150f;
        private const float PRESET_ALERT_HEIGHT = 30f;

        public static void PresetSelectionMenu()
        {
            SAINPresetDefinition selectedPreset = SAINPlugin.LoadedPreset.Info;
            checkCreateWarning(selectedPreset);

            /////
            BeginHorizontal();

            baseSelectionOptions();
            selectedPreset = selectDefault(selectedPreset);
            selectedPreset = selectCustom(selectedPreset);
            checkCreateNew();
            if (checkDeletePreset())
            {
                selectedPreset = SAINPresetClass.Instance.Info;
            }
            FlexibleSpace();

            EndHorizontal();
            /////

            if (selectedPreset.Name != SAINPlugin.LoadedPreset.Info.Name)
            {
                PresetHandler.InitPresetFromDefinition(selectedPreset);
            }
        }

        private static void checkCreateWarning(SAINPresetDefinition selectedPreset)
        {
            string sainPresetV = selectedPreset.SAINPresetVersion;
            if (string.IsNullOrEmpty(sainPresetV))
            {
                sainPresetV = selectedPreset.SAINVersion;
            }
            GUIContent content = new(
                        $"警告: 所选预设版本为: [{sainPresetV}], " +
                        $"但当前SAIN预设版本为: [{AssemblyInfoClass.SAINPresetVersion}] (SAIN版本 [{AssemblyInfoClass.SAINVersion}])。由于SAIN的更新，默认Bot配置值可能不正确。这不代表游戏坏了，只是Bot行为可能不如预期。");

            Rect rect = GUILayoutUtility.GetRect(content, GetStyle(Style.alert), Height(PRESET_ALERT_HEIGHT));
            if (selectedPreset.IsCustom && sainPresetV != AssemblyInfoClass.SAINPresetVersion)
            {
                GUI.Box(rect, content, GetStyle(Style.alert));
            }
            else
            {
                GUI.Box(rect, new GUIContent(""), GetStyle(Style.blankbox));
            }
        }

        private static void baseSelectionOptions()
        {
            BeginVertical();
            Box("预设", "选择已安装的SAIN设置预设", Height(PRESET_LABEL_HEIGHT), Width(PRESET_BASE_OPTION_WIDTH));
            if (Button("刷新", "刷新已安装的预设列表", EUISoundType.ButtonClick, Height(PRESET_LABEL_HEIGHT), Width(PRESET_BASE_OPTION_WIDTH)))
            {
                PresetHandler.LoadCustomPresetOptions();
            }

            _makeNewPresetMenuToggle = Toggle(
                _makeNewPresetMenuToggle,
                new GUIContent("创建新预设"),
                EUISoundType.ButtonClick,
                Height(PRESET_LABEL_HEIGHT), Width(PRESET_BASE_OPTION_WIDTH));

            EndVertical();
        }

        private static SAINPresetDefinition selectDefault(SAINPresetDefinition selectedPreset)
        {
            BeginVertical();
            Label("默认预设", Width(PRESET_OPTION_WIDTH));

            for (int i = 0; i < defaultPresets.Count; i++)
            {
                var preset = defaultPresets[i];
                if (SAINDifficultyClass.DefaultPresetDefinitions.TryGetKey(preset, out var sainDifficulty))
                {
                    bool selected = SAINPlugin.EditorDefaults.SelectedDefaultPreset == sainDifficulty;

                    if (Toggle(
                        selected,
                        $"{preset.Name}",
                        preset.Description,
                        EUISoundType.MenuCheckBox,
                        Height(PRESET_OPTION_HEIGHT), Width(PRESET_OPTION_WIDTH)
                        ))
                    {
                        if (!selected)
                        {
                            SAINPlugin.EditorDefaults.SelectedDefaultPreset = sainDifficulty;
                            selectedPreset = preset;
                        }
                    }
                }
            }
            EndVertical();
            return selectedPreset;
        }

        private static SAINPresetDefinition selectCustom(SAINPresetDefinition selectedPreset)
        {
            BeginVertical();
            Label("自定义预设", Width(PRESET_OPTION_WIDTH));
            for (int i = 0; i < PresetHandler.CustomPresetOptions.Count; i++)
            {
                var preset = PresetHandler.CustomPresetOptions[i];
                if (preset.IsCustom == true)
                {
                    bool selected = SAINPlugin.EditorDefaults.SelectedDefaultPreset == SAINDifficulty.none
                        && selectedPreset.Name == preset.Name;

                    if (Toggle(
                        selected,
                        $"{preset.Name}",
                        preset.Description,
                        EUISoundType.MenuCheckBox,
                        Height(PRESET_OPTION_HEIGHT), Width(PRESET_OPTION_WIDTH)
                        ))
                    {
                        if (!selected)
                        {
                            selectedPreset = preset;
                        }
                    }
                }
            }
            EndVertical();
            return selectedPreset;
        }

        private static void checkCreateNew()
        {
            if (_makeNewPresetMenuToggle)
            {
                BeginVertical();

                BeginHorizontal();
                Space(25);
                SAINPresetDefinition info = SAINPlugin.LoadedPreset.Info;
                if (info.CanEditName && Button("保存信息", "更新当前预设的名称、描述和创建者。", EFT.UI.EUISoundType.InsuranceInsured, Height(30f)))
                {
                    string oldName = info.Name;
                    var newInfo = info.Clone();

                    newInfo.Name = NewName;
                    newInfo.Description = NewDescription;
                    newInfo.Creator = NewCreator;

                    JsonUtility.DeletePreset(info);

                    PresetHandler.SavePresetDefinition(newInfo);
                    PresetHandler.InitPresetFromDefinition(newInfo, true);
                    PresetHandler.LoadCustomPresetOptions();
                }
                if (Button("保存新预设", EFT.UI.EUISoundType.InsuranceInsured, Height(30f)))
                {
                    SAINPresetDefinition newPreset = SAINPlugin.LoadedPreset.Info.Clone();

                    newPreset.Name = NewName;
                    newPreset.Description = NewDescription;
                    newPreset.Creator = NewCreator;
                    newPreset.SAINVersion = AssemblyInfoClass.SAINPresetVersion;
                    newPreset.DateCreated = DateTime.Today.ToString();

                    PresetHandler.SavePresetDefinition(newPreset);
                    PresetHandler.InitPresetFromDefinition(newPreset, true);
                }
                Space(25);
                EndHorizontal();

                Space(3);

                NewName = LabeledTextField(NewName, "名称");
                NewDescription = LabeledTextField(NewDescription, "描述");
                NewCreator = LabeledTextField(NewCreator, "创建者");

                EndVertical();
            }
        }

        private static bool checkDeletePreset()
        {
            if (SAINPresetClass.Instance.Info.IsCustom)
            {
                BeginVertical();
                _deletePresetConfirmation1 = Toggle(_deletePresetConfirmation1, "删除选中的预设", null, Height(30), Width(250f));
                if (_deletePresetConfirmation1)
                {
                    _deletePresetConfirmation2 = Toggle(_deletePresetConfirmation2, "确认删除？", null, Height(30), Width(250f));
                    if (_deletePresetConfirmation2)
                    {
                        if (Button($"确认删除 {SAINPresetClass.Instance.Info.Name} ？", Height(60), Width(250f)))
                        {
                            var deletedInfo = SAINPresetClass.Instance.Info;
                            PresetHandler.loadDefault();
                            JsonUtility.DeletePreset(deletedInfo);
                            Sounds.PlaySound(EUISoundType.MalfunctionExamined);
                            PresetHandler.LoadCustomPresetOptions();
                            _deletePresetConfirmation2 = false;
                            _deletePresetConfirmation1 = false;
                            return true;
                        }
                    }
                }
                EndVertical();
            }
            return false;
        }

        private static bool _deletePresetConfirmation1 = false;
        private static bool _deletePresetConfirmation2 = false;

        private static string LabeledTextField(string value, string label)
        {
            BeginHorizontal();
            Box(label, Width(125f), Height(PresetHandler.EditorDefaults.ConfigEntryHeight));
            value = TextField(value, null, Width(350f), Height(PresetHandler.EditorDefaults.ConfigEntryHeight));
            EndHorizontal();

            return Regex.Replace(value, @"[^\w \-]", "");
        }

        private static bool _makeNewPresetMenuToggle;

        private static string NewName = "在此输入名称";
        private static string NewDescription = "在此输入描述";
        private static string NewCreator = "在此输入您的名字";
    }
}