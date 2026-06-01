using EFT;
using SAIN.BotController.Classes;
using SAIN.Components;
using SAIN.Preset.GlobalSettings;
using UnityEngine;
using UnityEngine.PlayerLoop;
using static RootMotion.FinalIK.AimPoser;

namespace SAIN.SAINComponent.SubComponents
{
    public class GrenadeTrackerClass
    {
        public GrenadeTrackerClass(BotComponent bot, Grenade grenade, Vector3 dangerPoint, float reactionTime, float remainingTime)
        {
            Bot = bot;
            ReactionTime = reactionTime;
            DangerPoint = dangerPoint;
            Grenade = grenade;
            RemainingTime = remainingTime;
            _threatSetTime = Time.time;
            _lastGrenadeDistance = (grenade.transform.position - bot.Position).magnitude;
            if (_lastGrenadeDistance < 10f)
            {
                setSpotted();
            }
        }

        public void CheckHeardGrenadeCollision(float maxRange)
        {
            if (_spotted)
            {
                return;
            }
            maxRange *= 0.75f;
            if (GrenadeDistance < maxRange)
            {
                setSpotted();
            }
        }

        private readonly BotComponent Bot;
        private BotOwner BotOwner => Bot.BotOwner;

        public float GrenadeDistance { get; private set; }

        public float RemainingTime { get; set; }
        private float _threatSetTime;
        private float _lastGrenadeDistance = float.MaxValue;

        public void Update()
        {
            if (BotOwner == null || BotOwner.IsDead || Grenade == null || _sentToBot)
            {
                return;
            }

            if (!_sentToBot && CanReact)
            {
                _sentToBot = true;

                // 全局开关检查
                if (!GlobalSettingsClass.Instance.Grenade.ENABLED)
                {
                    BotOwner.BewareGrenade.AddGrenadeDanger(DangerPoint, Grenade);
                    return;
                }

                var collisionSound = Grenade.GrenadeSettings.CollisionSound;
                bool isFrag = collisionSound == GrenadeSettings.CollisionSounds.frag;
                var trigger = isFrag ? EPhraseTrigger.OnEnemyGrenade : EPhraseTrigger.Look;
                Bot.Talk.GroupSay(trigger, ETagStatus.Combat, false, 100);

                // 通过 SAIN 决策系统处理
                GrenadeThreatData data = BuildThreatData();
                Bot.Decision.DecisionManager.SetAvoidGrenade(data);

                // Squad 广播手雷威胁
                var squad = Bot.Squad?.SquadInfo;
                if (squad != null)
                {
                    bool isSmoke = data.IsSmoke;
                    bool isFlash = data.IsFlash;
                    squad.OnMemberSpottedGrenade?.Invoke(DangerPoint, isSmoke, isFlash, Bot);
                }

                return;
            }

            if (_spotted)
            {
                return;
            }

            // 距离更新 + 紧急反应检测
            GrenadeDistance = (Grenade.transform.position - BotOwner.Position).magnitude;

            checkEmergencyReact();

            if (GrenadeDistance < 3f)
            {
                setSpotted();
                return;
            }

            if (_nextCheckRaycastTime < Time.time)
            {
                _nextCheckRaycastTime = Time.time + 0.05f;
                if (checkVisibility())
                {
                    setSpotted();
                }
            }
        }

        private bool _sentToBot;

        private void setSpotted()
        {
            if (!_spotted)
            {
                _timeSpotted = Time.time;
                _spotted = true;
            }
        }

        private bool checkVisibility()
        {
            Vector3 lookPoint = Bot.Transform.WeaponRoot;
            Vector3 lookDir = Bot.LookDirection;

            Vector3 grenadePos = Grenade.transform.position + (Vector3.up * 0.05f);
            Vector3 grenadeDir = grenadePos - lookPoint;
            if (Vector3.Dot(lookDir, grenadeDir.normalized) < 0.25f)
            {
                return false; // Not looking in the right direction
            }
            return !Physics.Raycast(lookPoint, grenadeDir, 1f, LayerMaskClass.HighPolyWithTerrainMaskAI);
        }

        public void UpdateGrenadeDanger(Vector3 Danger, float newRemainingTime = -1f)
        {
            DangerPoint = Danger;
            if (newRemainingTime > 0f)
                RemainingTime = newRemainingTime;

            if (_sentToBot && !_updated)
            {
                _updated = true;
                if (GlobalSettingsClass.Instance.Grenade.ENABLED)
                {
                    Bot.Decision.DecisionManager.UpdateGrenadeDangerPoint(Danger);
                }
                else
                {
                    BotOwner.BewareGrenade.AddGrenadeDanger(Danger, Grenade);
                }
            }
        }

        public GrenadeThreatData BuildThreatData()
        {
            var collisionSound = Grenade.GrenadeSettings.CollisionSound;
            return new GrenadeThreatData
            {
                DangerPoint = DangerPoint,
                IsSmoke = collisionSound == GrenadeSettings.CollisionSounds.smoke,
                IsFlash = collisionSound == GrenadeSettings.CollisionSounds.stun,
                IsImpact = RemainingTime <= 0f,
                RemainingTime = RemainingTime,
                SetTime = Time.time,
                LastUpdateTime = Time.time,
                DistanceToBot = GrenadeDistance,
                Grenade = Grenade
            };
        }

        public bool HasExpired()
        {
            return Time.time - _threatSetTime > GlobalSettingsClass.Instance.Grenade.MAX_THREAT_LIFETIME
                || (Grenade != null && Grenade.gameObject == null); // Grenade destroyed/exploded
        }

        private void checkEmergencyReact()
        {
            float emergencyDist = GlobalSettingsClass.Instance.Grenade.EMERGENCY_REACT_DISTANCE;
            if (GrenadeDistance < emergencyDist && IsGrenadeClosingIn())
            {
                setSpotted();
                ReactionTime = 0f;
            }
            _lastGrenadeDistance = GrenadeDistance;
        }

        private bool IsGrenadeClosingIn()
        {
            return GrenadeDistance < _lastGrenadeDistance;
        }

        private bool _updated;

        private float _timeSpotted { get; set; }
        public float TimeSinceSpotted => _spotted ? Time.time - _timeSpotted : 0f;
        public Grenade Grenade { get; private set; }
        public Vector3 DangerPoint { get; set; }
        private bool _spotted { get; set; }
        public bool CanReact => _spotted && TimeSinceSpotted > ReactionTime;

        private float ReactionTime;
        private float _nextCheckRaycastTime;
    }
}