using EFT.UI;
using SAIN.Attributes;
using SAIN.Plugin;
using System.Collections.Generic;
using System.Text;
using static SAIN.Editor.SAINLayout;

namespace SAIN.Editor.GUISections
{
    public static class BotSettingsEditor
    {
        private static readonly StringBuilder _stringBuilder = new();

        public static void ShowAllSettingsGUI(object settings, out bool wasEdited, string name, string savePath, float height, out bool Saved)
        {
            BeginHorizontal();

            Box(name, Height(height));

            Space(10);

            Label("搜索", Width(125f), Height(height));

            var container = SettingsContainers.GetContainer(settings.GetType(), name);
            container.SearchPattern = TextField(
                container.SearchPattern,
                null,
                Width(250),
                Height(height));

            if (Button(
                "清除",
                EUISoundType.MenuContextMenu,
                Width(80),
                Height(height)))
            {
                container.SearchPattern = string.Empty;
            }

            Space(10);

            if (ConfigEditingTracker.UnsavedChanges)
            {
                BuilderClass.Alert(
                    "点击保存以导出更改，若在游戏中则将更改应用到Bot。",
                    "你有未保存的修改！",
                    height, ColorNames.DarkRed);
            }
            else
            {
                BuilderClass.Alert(null, null, height, null);
            }

            Saved = Button(
                "保存并导出",
                ConfigEditingTracker.GetUnsavedValuesString(),
                EUISoundType.InsuranceInsured,
                Height(height));

            EndHorizontal();

            container.Scroll = BeginScrollView(container.Scroll);

            CategoryOpenable(container.Categories, settings, out wasEdited, container.SearchPattern);

            EndScrollView();
        }

        public static bool CheckIfOpen(SettingsContainer container, float height = 30f)
        {
            BeginHorizontal();
            container.Open = BuilderClass.ExpandableMenu(container.Name, container.Open, null, height);
            if (Button("清除", "清除此菜单中的已选选项",
                EFT.UI.EUISoundType.MenuDropdownSelect,
                Width(100), Height(height)))
            {
                container.SelectedCategories.Clear();
                foreach (var category in container.Categories)
                {
                    category.SelectedList.Clear();
                }
            }
            EndHorizontal();
            return container.Open;
        }

        public static bool WasEdited;

        private static void CategoryOpenable(List<Category> categories, object settingsObject, out bool wasEdited, string search = null)
        {
            wasEdited = false;
            foreach (var categoryClass in categories)
            {
                if (categoryClass.OptionCount(out int notUsed) == 0)
                {
                    continue;
                }

                var attributes = categoryClass.CategoryInfo;
                object categoryObject = categoryClass.GetValue(settingsObject);

                BeginHorizontal(30);

                bool open = true;
                if (string.IsNullOrEmpty(search))
                {
                    categoryClass.Open = BuilderClass.ExpandableMenu(
                        attributes.Name, categoryClass.Open, attributes.Description, EntryConfig.EntryHeight);
                    open = categoryClass.Open;
                }
                else
                {
                    Box(attributes.Name, attributes.Description, Height(PresetHandler.EditorDefaults.ConfigEntryHeight));
                }

                EndHorizontal(30);

                if (open)
                {
                    AttributesGUI.EditAllValuesInObj(categoryClass, categoryObject, out bool newEdit, search);
                    if (newEdit)
                    {
                        wasEdited = true;
                    }
                }
            }
        }

        private static readonly GUIEntryConfig EntryConfig = new();
    }
}