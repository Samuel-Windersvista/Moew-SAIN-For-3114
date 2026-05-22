using SAIN.Attributes;
using System.Collections.Generic;

namespace SAIN.Preset.GlobalSettings
{
    public class SquadTalkSettings : SAINSettingsBase<SquadTalkSettings>, ISAINSettings
    {
        [Name("换弹喊话概率")]
        [Description("条件满足时Bot实际说出语音的概率。")]
        [Percentage]
        public float _reportReloadingChance = 33f;

        [Name("换弹喊话频率")]
        [Description("4=每4秒一次。")]
        [MinMax(0.1f, 20f, 100f)]
        public float _reportReloadingFreq = 4f;

        [Name("丢失敌人视野喊话概率")]
        [Description("条件满足时Bot实际说出语音的概率。")]
        [Percentage]
        public float _reportLostVisualChance = 40f;

        [Name("发现蹲坑敌人喊话概率")]
        [Description("条件满足时Bot实际说出语音的概率。")]
        [Percentage]
        public float _reportRatChance = 33f;

        [Name("发现蹲坑敌人-上次看到敌人时间")]
        [Description("")]
        [MinMax(1f, 120f, 1f)]
        public float _reportRatTimeSinceSeen = 60f;

        [Name("敌人交谈喊话概率")]
        [Description("条件满足时Bot实际说出语音的概率。")]
        [Percentage]
        public float _reportEnemyConversationChance = 10f;

        [Name("敌人交谈最大距离")]
        [Description("")]
        [MinMax(1f, 120f, 1f)]
        public float _reportEnemyMaxDist = 70f;

        [Name("敌人血量状态喊话概率")]
        [Description("条件满足时Bot实际说出语音的概率。")]
        [Percentage]
        public float _reportEnemyHealthChance = 40f;

        [Name("敌人血量状态喊话频率")]
        [Description("8=每8秒一次。")]
        [MinMax(0.1f, 20f, 100f)]
        public float _reportEnemyHealthFreq = 8f;

        [Name("击杀敌人喊话概率")]
        [Description("条件满足时Bot实际说出语音的概率。")]
        [Percentage]
        public float _reportEnemyKilledChance = 60f;

        [Name("队长确认击杀概率")]
        [Description("队员报告击杀敌人时队长确认并称赞的概率。")]
        [Percentage]
        public float _reportEnemyKilledSquadLeadChance = 60f;

        [Name("毒舌队长")]
        [Description("旧Bug变成特性。友方队员被杀时队长会大喊干得好。")]
        public bool _reportEnemyKilledToxicSquadLeader = false;

        [Name("友方近距判定")]
        [Description("Bot认为可以与之交谈的友方距离。")]
        [MinMax(1f, 120f, 1f)]
        public float _friendCloseDist = 40f;

        [Name("友方阵亡喊话概率")]
        [Description("条件满足时Bot实际说出语音的概率。")]
        [Percentage]
        public float _reportFriendKilledChance = 60f;

        [Name("撤退喊话概率")]
        [Description("条件满足时Bot实际说出语音的概率。")]
        [Percentage]
        public float _talkRetreatChance = 60f;

        [Name("撤退喊话频率")]
        [Description("10=每10秒一次。")]
        [MinMax(0.1f, 20f, 100f)]
        public float _talkRetreatFreq = 10f;

        [Hidden]
        public EPhraseTrigger _talkRetreatTrigger = EPhraseTrigger.CoverMe;

        [Hidden]
        public ETagStatus _talkRetreatMask = ETagStatus.Combat;

        [Name("撤退喊话组延迟")]
        [Description("组延迟=小队成员共享语音冷却防止刷屏。")]
        public bool _talkRetreatGroupDelay = true;

        [Name("被射击喊话概率")]
        [Description("条件满足时Bot实际说出语音的概率。")]
        [Percentage]
        public float _underFireNeedHelpChance = 45f;

        [Hidden]
        public EPhraseTrigger _underFireNeedHelpTrigger = EPhraseTrigger.NeedHelp;

        [Hidden]
        public ETagStatus _underFireNeedHelpMask = ETagStatus.Combat;

        [Name("求助喊话延迟")]
        [Description("组延迟=小队成员共享语音冷却防止刷屏。")]
        public bool _underFireNeedHelpGroupDelay = true;

        [Name("被射击喊话频率")]
        [Description("10=每10秒一次。")]
        [MinMax(0.1f, 20f, 100f)]
        public float _underFireNeedHelpFreq = 1f;

        [Name("求助喊话概率")]
        [Description("条件满足时Bot实际说出语音的概率。")]
        [Percentage]
        public float _enemyNeedHelpChance = 40f;

        [Name("听到动静喊话概率")]
        [Description("条件满足时Bot实际说出语音的概率。")]
        [Percentage]
        public float _hearNoiseChance = 50f;

        [Name("报告动静最大距离")]
        [Description("报告非枪声动静给小队成员的最大距离。")]
        [MinMax(1f, 120f, 1f)]
        public float _hearNoiseMaxDist = 70f;

        [Name("听到动静喊话频率")]
        [Description("10=每10秒一次。")]
        [MinMax(0.1f, 60f, 100f)]
        public float _hearNoiseFreq = 30f;

        [Name("敌人位置喊话概率")]
        [Description("条件满足时Bot实际说出语音的概率。")]
        [Percentage]
        public float _enemyLocationTalkChance = 70f;

        [Name("敌人位置喊话-上次看到时间")]
        [Description("Time Since Seen = 自上次看到或听到敌人以来的时间。")]
        [MinMax(0.1f, 10f, 100f)]
        public float _enemyLocationTalkTimeSinceSeen = 3f;

        [Name("敌人位置喊话频率")]
        [Description("10=每10秒一次。")]
        [MinMax(0.1f, 20f, 100f)]
        public float _enemyLocationTalkFreq = 1f;

        [Name("敌人位置喊话-后方角度范围")]
        [MinMax(1f, 90f, 1f)]
        [Advanced]
        public float _enemyLocationBehindAngle = 90f;

        [Name("敌人位置喊话-侧面角度范围")]
        [MinMax(1f, 90f, 1f)]
        [Advanced]
        public float _enemyLocationSideAngle = 45f;

        [Name("敌人位置喊话-前方角度范围")]
        [MinMax(1f, 90f, 1f)]
        [Advanced]
        public float _enemyLocationFrontAngle = 90f;

        public override void Init(List<ISAINSettings> list)
        {
            list.Add(this);
        }
    }
}