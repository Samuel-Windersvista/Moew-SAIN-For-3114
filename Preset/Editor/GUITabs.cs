using EFT.UI;
using SAIN.Attributes;
using SAIN.Components;
using SAIN.Editor.GUISections;
using SAIN.Helpers;
using SAIN.Plugin;
using SAIN.Preset;
using static SAIN.Editor.SAINLayout;

namespace SAIN.Editor
{
    public static class GUITabs
    {
        public static void CreateTabs(EEditorTab selectedTab)
        {
            EditTabsClass.BeginScrollView();
            switch (selectedTab)
            {
                case EEditorTab.Home:
                    Home(); break;

                case EEditorTab.BotSettings:
                    BotSettings(); break;

                case EEditorTab.Personalities:
                    Personality(); break;

                case EEditorTab.EquipmentStealth:
                    Stealth(); break;

                case EEditorTab.Advanced:
                    Advanced(); break;

                default: break;
            }
            EditTabsClass.EndScrollView();
        }

        public static void Home()
        {
            PresetSelection.PresetSelectionMenu();
            Space(20f);

            BotSettingsEditor.ShowAllSettingsGUI(
                SAINPlugin.LoadedPreset.GlobalSettings,
                out bool newEdit,
                "全局设置",
                $"SAIN/Presets/{SAINPlugin.LoadedPreset.Info.Name}",
                35f,
                out bool saved);

            if (saved)
            {
                SAINPresetClass.ExportAll(SAINPlugin.LoadedPreset);
                ConfigEditingTracker.Clear();
            }
        }

        public static void BotSettings()
        {
            BotSelectionClass.Menu();
        }

        public static void Personality()
        {
            BotPersonalityEditor.PersonalityMenu();
        }

        private static void Stealth()
        {
            BeginVertical();

            BeginHorizontal();
            if (ConfigEditingTracker.UnsavedChanges)
            {
                BuilderClass.Alert(
                    "点击保存以导出更改，若在游戏中则将更改应用到Bot。",
                    "你有未保存的修改！",
                    35f, ColorNames.DarkRed);
            }
            else
            {
                BuilderClass.Alert(null, null, 25f, null);
            }

            if (Button(
                "保存并导出",
                ConfigEditingTracker.GetUnsavedValuesString(),
                EUISoundType.InsuranceInsured,
                Height(25f)))
            {
                SAINPresetClass.ExportAll(SAINPlugin.LoadedPreset);
            }

            EndHorizontal();

            AttributesGUI.EditAllStealthValues(SAINPlugin.LoadedPreset.GearStealthValuesClass);
            EndVertical();
        }

        private static void ForceDecisions(int spacing)
        {
            Space(spacing);

            _forceDecisionMenuOpen = BuilderClass.ExpandableMenu("强制SAIN Bot决策", _forceDecisionMenuOpen);
            if (_forceDecisionMenuOpen)
            {
                Space(spacing);

                ForceSoloOpen = BuilderClass.ExpandableMenu("强制单人决策", ForceSoloOpen);
                if (ForceSoloOpen)
                {
                    Space(spacing / 2f);

                    if (Button("重置"))
                        SAINPlugin.ForceSoloDecision = ECombatDecision.None;

                    Space(spacing / 2f);

                    SAINPlugin.ForceSoloDecision = BuilderClass.SelectionGrid(
                        SAINPlugin.ForceSoloDecision,
                        EnumValues.GetEnum<ECombatDecision>());
                }

                Space(spacing);

                ForceSquadOpen = BuilderClass.ExpandableMenu("强制小队决策", ForceSquadOpen);
                if (ForceSquadOpen)
                {
                    Space(spacing / 2f);

                    if (Button("重置"))
                        SAINPlugin.ForceSquadDecision = ESquadDecision.None;

                    Space(spacing / 2f);

                    SAINPlugin.ForceSquadDecision =
                        BuilderClass.SelectionGrid(SAINPlugin.ForceSquadDecision,
                        EnumValues.GetEnum<ESquadDecision>());
                }

                Space(spacing);

                ForceSelfOpen = BuilderClass.ExpandableMenu("强制自身决策", ForceSelfOpen);
                if (ForceSelfOpen)
                {
                    Space(spacing / 2f);

                    if (Button("重置"))
                        SAINPlugin.ForceSelfDecision = ESelfActionType.None;

                    Space(spacing / 2f);

                    SAINPlugin.ForceSelfDecision = BuilderClass.SelectionGrid(
                        SAINPlugin.ForceSelfDecision,
                        EnumValues.GetEnum<ESelfActionType>());
                }
            }
        }

        public static void Advanced()
        {
            AttributesGUI.EditAllValuesInObj(PresetHandler.EditorDefaults, out bool newEdit);
            if (newEdit)
            {
                PresetHandler.ExportEditorDefaults();
            }

            if (!SAINPlugin.DebugMode)
            {
                return;
            }

            const int spacing = 4;
            ForceDecisions(spacing);
            ForceTalk(spacing);
        }

        private static void ForceTalk(int spacing)
        {
            Space(spacing);
            _forceTalkMenuOpen = BuilderClass.ExpandableMenu("强制Bot说出短语", _forceTalkMenuOpen);
            if (_forceTalkMenuOpen)
            {
                Space(5);
                _forceTagStatusToggle = Toggle(_forceTagStatusToggle, "为短语强制ETagStatus状态");
                if (_forceTagStatusToggle)
                {
                    ETagStatus[] statuses = EnumValues.GetEnum<ETagStatus>();
                    for (int i = 0; i < statuses.Length; i++)
                    {
                        if (Toggle(_forcedTagStatus == statuses[i], statuses[i].ToString()))
                        {
                            if (_forcedTagStatus != statuses[i])
                            {
                                _forcedTagStatus = statuses[i];
                            }
                        }
                    }
                }
                Space(5);
                _withGroupDelay = Toggle(_withGroupDelay, "应用组延迟？");
                Space(5);
                Label("说出短语");
                EPhraseTrigger[] triggers = EnumValues.GetEnum<EPhraseTrigger>();
                for (int i = 0; i < triggers.Length; i++)
                {
                    if (Button(triggers[i].ToString()))
                    {
                        if (BotManagerComponent.Instance?.Bots != null)
                        {
                            foreach (var bot in BotManagerComponent.Instance.Bots.Values)
                            {
                                if (bot != null)
                                {
                                    if (_forceTagStatusToggle)
                                    {
                                        bot.Talk.Say(triggers[i], _forcedTagStatus, _withGroupDelay);
                                    }
                                    else
                                    {
                                        bot.Talk.Say(triggers[i], null, _withGroupDelay);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static bool _withGroupDelay;
        private static bool _forceTagStatusToggle;
        private static ETagStatus _forcedTagStatus;
        private static bool _forceTalkMenuOpen;
        private static bool _forceDecisionMenuOpen;
        private static bool ForceSoloOpen;
        private static bool ForceSquadOpen;
        private static bool ForceSelfOpen;
    }
}