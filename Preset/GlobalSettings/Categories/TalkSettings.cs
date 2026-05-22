using SAIN.Attributes;
using System.Collections.Generic;

namespace SAIN.Preset.GlobalSettings
{
    public class TalkSettings : SAINSettingsBase<TalkSettings>, ISAINSettings
    {
        [Category("和平状态说话")]
        [Name("多话的Scav")]
        [Description("和平状态下Scav会互相交谈发出噪音暴露位置。")]
        public bool TalkativeScavs = true;

        [Category("和平状态说话")]
        [Name("多话的PMC")]
        [Description("和平状态下PMC会互相交谈发出噪音暴露位置。")]
        public bool TalkativePMCs = false;

        [Category("和平状态说话")]
        [Name("多话的Raider与Rogue")]
        [Description("和平状态下Raider和Rogue会互相交谈发出噪音暴露位置。")]
        public bool TalkativeRaidersRogues = true;

        [Category("和平状态说话")]
        [Name("多话的Boss")]
        [Description("和平状态下Boss和守卫会互相交谈发出噪音暴露位置。")]
        public bool TalkativeBosses = true;

        [Category("和平状态说话")]
        [Name("多话的Goon小队")]
        [Description("和平状态下Goon小队会互相交谈发出噪音暴露位置。")]
        public bool TalkativeGoons = false;

        [Name("人类玩家回应概率")]
        [Description("回应友方人类玩家语音的百分比概率。")]
        [Category("友方回应")]
        [Percentage]
        public float FriendlyReponseChance = 85f;

        [Name("AI回应概率")]
        [Description("回应友方AI语音的百分比概率。")]
        [Category("友方回应")]
        [Percentage]
        public float FriendlyReponseChanceAI = 80f;

        [Name("人类玩家回应最大距离")]
        [Category("友方回应")]
        [Percentage]
        public float FriendlyReponseDistance = 65f;

        [Name("AI回应最大距离")]
        [Category("友方回应")]
        [Percentage]
        public float FriendlyReponseDistanceAI = 35f;

        [Name("回应频率")]
        [Description("2=每2秒检查一次。")]
        [Category("友方回应")]
        [MinMax(0.5f, 10f)]
        public float FriendlyResponseFrequencyLimit = 1f;

        [Name("回应延迟随机化最小值")]
        [Category("友方回应")]
        [MinMax(0.25f, 3f)]
        public float FriendlyResponseMinRandomDelay = 0.33f;

        [Name("回应延迟随机化最大值")]
        [Category("友方回应")]
        [MinMax(0.25f, 3f)]
        public float FriendlyResponseMaxRandomDelay = 0.75f;

        [Name("原版Bot说话")]
        [Description("禁用所有SAIN对Bot说话的处理。不再有小队闲聊和安静Bot，完全禁用SAIN的Bot语音处理。")]
        public bool DisableBotTalkPatching = false;

        public override void Init(List<ISAINSettings> list)
        {
            list.Add(this);
        }
    }
}