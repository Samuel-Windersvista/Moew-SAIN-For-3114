using System.Collections.Generic;
using UnityEngine;
using static SAIN.Editor.RectLayout;
using static SAIN.Editor.SAINLayout;

namespace SAIN.Editor
{
    internal class EditTabsClass
    {
        static EditTabsClass()
        {
            TabClasses = new Dictionary<EEditorTab, TabClass>
            {
                {
                    EEditorTab.Home, new TabClass
                    {
                        Name = "首页",
                        ToolTip = "选择预设并修改SAIN全局设置。",
                    }
                },
                {
                    EEditorTab.BotSettings, new TabClass
                    {
                        Name = "Bot设置",
                        ToolTip = "根据不同难度修改特定Bot类型的独立设置。难度由EFT在生成时决定，开局选择难度等级可影响。线上模式为所有难度混合。",
                    }
                },
                {
                    EEditorTab.Personalities, new TabClass
                    {
                        Name = "个性设置",
                        ToolTip = "修改各个个性的分配规则及其对Bot行为的影响。",
                    }
                },
                {
                    EEditorTab.EquipmentStealth, new TabClass
                    {
                        Name = "装备隐蔽值",
                        ToolTip = "修改特定装备提供的隐蔽度数值。",
                    }
                },
                {
                    EEditorTab.Advanced, new TabClass
                    {
                        Name = "高级选项",
                        ToolTip = "自行承担修改风险。在此启用额外的高级配置选项。",
                    }
                },
            };

            List<string> names = new();
            List<string> tooltips = new();
            foreach (var tab in TabClasses)
            {
                names.Add(tab.Value.Name);
                tooltips.Add(tab.Value.ToolTip);
            }
            Tabs = names.ToArray();
            TabTooltips = tooltips.ToArray();
        }

        private const float TabMenuHeight = 60f;
        private const float TabMenuVerticalMargin = 2f;

        public static EEditorTab TabSelectMenu(float minHeight = 30, float speed = 3, float closeSpeedMulti = 0.66f)
        {
            if (TabMenuRect == null || TabRects == null)
            {
                TabMenuRect = new Rect(0, ExitRect.height + TabMenuVerticalMargin, MainWindow.width, TabMenuHeight);
                TabRects = BuilderClass.HorizontalGridRects(TabMenuRect, Tabs.Length, minHeight);
            }

            string openTabString = BuilderClass.SelectionGridExpandHeight(TabMenuRect, Tabs, TabClasses[SelectedTab].Name, TabRects, minHeight, speed, closeSpeedMulti, TabTooltips);

            foreach (var tab in TabClasses)
            {
                if (tab.Value.Name == openTabString)
                {
                    SelectedTab = tab.Key;
                }
            }
            return SelectedTab;
        }

        private static Rect[] TabRects;
        public static Rect TabMenuRect;

        public static void BeginScrollView()
        {
            TabClasses[SelectedTab].Scroll = SAINLayout.BeginScrollView(TabClasses[SelectedTab].Scroll, MainWindow.width - 20f);
            BeginVertical();
        }

        public static void EndScrollView()
        {
            EndVertical();
            SAINLayout.EndScrollView();
        }

        public static bool IsTabSelected(EEditorTab tab)
        {
            return SelectedTab == tab;
        }

        public static EEditorTab SelectedTab = EEditorTab.Home;
        public static readonly string[] Tabs;
        public static readonly string[] TabTooltips;
        public static readonly Dictionary<EEditorTab, TabClass> TabClasses;
    }

    public sealed class TabClass
    {
        public string Name;
        public string ToolTip;
        public Vector2 Scroll = Vector2.zero;
    }

    public enum EEditorTab
    {
        Home,
        BotSettings,
        Personalities,
        EquipmentStealth,
        Advanced
    }
}