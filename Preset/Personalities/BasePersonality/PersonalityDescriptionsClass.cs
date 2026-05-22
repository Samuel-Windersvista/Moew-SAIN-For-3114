using System.Collections.Generic;

namespace SAIN.Preset.Personalities
{
    public static class PersonalityDescriptionsClass
    {
        public static void Import()
        {
        }

        public static Dictionary<EPersonality, string> PersonalityDescriptions { get; private set; } = new Dictionary<EPersonality, string>
        {
            {
                    EPersonality.Normal,
                    "一个普通的塔科夫玩家"
                },
            {
                    EPersonality.GigaChad,
                    "真正的顶级威胁。极度激进，通常穿戴高级装备。"
                },
            {
                    EPersonality.Wreckless,
                    "倾向于向敌人直线冲刺，经常对所有人咆哮——通常同时做这两件事。比超级猛男更具进攻性。"
                },
            {
                    EPersonality.SnappingTurtle,
                    "在老鼠与猛男之间取得平衡的玩家。会蹲坑阴你，但随时可能暴起冲锋。"
                },
            {
                    EPersonality.Chad,
                    "激进的玩家。通常穿戴高级装备，比普通更具进攻性。"
                },
            {
                    EPersonality.Rat,
                    "塔科夫的败类。很少主动寻找敌人，一旦去找，全程蹲着挪过去。"
                },
            {
                    EPersonality.Timmy,
                    "新手玩家，见什么怕什么。"
                },
            {
                    EPersonality.Coward,
                    "比普通玩家更懦弱被动。永远不会主动找敌人，会躲在柜子里直到危险消失。"
                },
        };
    }
}