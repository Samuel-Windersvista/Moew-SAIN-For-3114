using EFT;
using UnityEngine;

namespace SAIN.SAINComponent.SubComponents
{
    /// <summary>
    /// 手雷威胁运行时数据，在 GrenadeTrackerClass 和 BotDecisionManager 之间传递。
    /// </summary>
    public class GrenadeThreatData
    {
        /// <summary>手雷落点/当前位置</summary>
        public Vector3 DangerPoint;

        /// <summary>是否为烟雾弹</summary>
        public bool IsSmoke;

        /// <summary>是否为闪光弹</summary>
        public bool IsFlash;

        /// <summary>是否为碰炸手雷（VOG）</summary>
        public bool IsImpact;

        /// <summary>到爆炸的剩余时间（秒）。-1 表示未知，0 表示碰炸/即将爆炸。</summary>
        public float RemainingTime = -1f;

        /// <summary>威胁设置的时间戳（Time.time）</summary>
        public float SetTime;

        /// <summary>最后一次更新 DangerPoint 的时间</summary>
        public float LastUpdateTime;

        /// <summary>Bot 到手雷落点的直线距离</summary>
        public float DistanceToBot;

        /// <summary>威胁是否仍然有效（手雷未销毁）</summary>
        public bool IsValid => Grenade != null;

        /// <summary>关联的手雷引用。手雷销毁后为 null。</summary>
        public Grenade Grenade;

        /// <summary>是否已通过 Squad 广播给队友</summary>
        public bool SquadBroadcasted;
    }
}
