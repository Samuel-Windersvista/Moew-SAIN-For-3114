using EFT;
using HarmonyLib;
using SAIN.Components;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.SAINComponent.SubComponents;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.WeaponFunction
{
    public class GrenadeVelocityTracker : MonoBehaviour
    {
        public Vector3 Velocity { get; private set; }
        public float VelocityMagnitude { get; private set; }

        private const float GRENADE_UPDATE_FREQUENCY = 0.5f;

        public void Awake()
        {
            _grenade = this.GetComponent<Grenade>();
            _grenade.DestroyEvent += GrenadeDestroyed;
            _rigidBody = (Rigidbody)_rigidBodyField.GetValue(_grenade);
        }

        public void Update()
        {
            if (_grenade == null) return;
            if (_rigidBody == null)
            {
                GrenadeDestroyed(_grenade);
                return;
            }
            if (_nextUpdateTime < Time.time)
            {
                _nextUpdateTime = Time.time + GRENADE_UPDATE_FREQUENCY;
                Velocity = _rigidBody.velocity;
                VelocityMagnitude = Velocity.magnitude;
                //Logger.LogInfo($"Grenade {_grenade.Id} Velocity [{Velocity}] Magnitude: [{VelocityMagnitude}]");
            }
        }

        private Rigidbody _rigidBody;
        private Grenade _grenade;

        static GrenadeVelocityTracker()
        {
            _rigidBodyField = AccessTools.Field(typeof(Throwable), "Rigidbody");
        }

        private void GrenadeDestroyed(Throwable grenade)
        {
            if (grenade != null)
            {
                grenade.DestroyEvent -= GrenadeDestroyed;
            }
            Destroy(this);
        }

        private static FieldInfo _rigidBodyField;
        private float _nextUpdateTime;
    }

    public class GrenadeReactionClass : BotSubClass<BotGrenadeManager>, IBotClass
    {
        public GrenadeTrackerClass DangerGrenade { get; private set; }
        public Vector3? GrenadeDangerPoint => DangerGrenade?.DangerPoint;
        public Dictionary<Throwable, GrenadeTrackerClass> EnemyGrenadesList { get; private set; } = [];

        public GrenadeReactionClass(BotGrenadeManager ThrowWeapItemClass) : base(ThrowWeapItemClass)
        {
        }

        public override void Init()
        {
            var grenadeController = BotManagerComponent.Instance.GrenadeController;
            grenadeController.OnGrenadeCollision += GrenadeCollision;
            grenadeController.OnGrenadeThrown += EnemyGrenadeThrown;
            grenadeController.OnGrenadeDangerUpdated += GrenadeDangerUpdated;
            base.Init();
        }

        public override void ManualUpdate()
        {
            // 清理已完成/超时的 tracker
            var toRemove = new List<Throwable>();
            foreach (var kvp in EnemyGrenadesList)
            {
                if (kvp.Value?.Grenade == null || kvp.Value.HasExpired())
                    toRemove.Add(kvp.Key);
                else
                    kvp.Value.Update();
            }
            foreach (var key in toRemove)
                EnemyGrenadesList.Remove(key);
            
            // 更新 DangerGrenade 为最紧急威胁
            UpdateDangerGrenade();
            
            base.ManualUpdate();
        }

        public override void Dispose()
        {
            var grenadeController = BotManagerComponent.Instance.GrenadeController;
            grenadeController.OnGrenadeCollision -= GrenadeCollision;
            grenadeController.OnGrenadeThrown -= EnemyGrenadeThrown;
            grenadeController.OnGrenadeDangerUpdated -= GrenadeDangerUpdated;

            foreach (var tracker in EnemyGrenadesList.Values)
                if (tracker?.Grenade != null)
                    tracker.Grenade.DestroyEvent -= RemoveGrenade;
            EnemyGrenadesList.Clear();
            base.Dispose();
        }

        public void EnemyGrenadeThrown(Grenade grenade, Vector3 dangerPoint, string profileId)
        {
            if (Bot == null || profileId == Bot.ProfileId || !Bot.BotActive)
            {
                return;
            }
            Enemy enemy = Bot.EnemyController.GetEnemy(profileId, false);
            if (enemy != null &&
                enemy.RealDistance <= MAX_ENEMY_GRENADE_DIST_TOCARE)
            {
                EnemyGrenadesList.Add(grenade, new GrenadeTrackerClass(Bot, grenade, dangerPoint, GetReactionTime(), -1f));
                grenade.DestroyEvent += RemoveGrenade;
                return;
            }
            BotOwner.BewareGrenade.AddGrenadeDanger(dangerPoint, grenade);
        }

        private const float MAX_ENEMY_GRENADE_DIST_TOCARE = 125;

        private void GrenadeCollision(Grenade grenade, float maxRange)
        {
            if (EnemyGrenadesList.TryGetValue(grenade, out var Tracker))
            {
                Tracker?.CheckHeardGrenadeCollision(maxRange);
            }
        }

        private void GrenadeDangerUpdated(Grenade grenade, Vector3 Danger, float remainingTime)
        {
            if (EnemyGrenadesList.TryGetValue(grenade, out var Tracker))
            {
                Tracker.UpdateGrenadeDanger(Danger, remainingTime);
            }
        }

        private void RemoveGrenade(Throwable grenade)
        {
            if (grenade != null)
            {
                grenade.DestroyEvent -= RemoveGrenade;
                EnemyGrenadesList.Remove(grenade);
            }
        }

        public float GetReactionTime()
        {
            float reactionTime = 0.25f;
            reactionTime /= Bot.Info.Profile.DifficultyModifier;
            reactionTime *= Random.Range(0.75f, 1.25f);
            reactionTime *= Bot.Info.PersonalitySettings.General.GRENADE_REACTION_TIME_MODIFIER;
            return Mathf.Clamp(reactionTime, 0.1f, 2f);
        }

        /// <summary>
        /// 检查是否有活跃手雷威胁，供 BotDecisionManager 调用。
        /// </summary>
        public bool HasActiveGrenadeThreat(out GrenadeThreatData data)
        {
            if (DangerGrenade != null && DangerGrenade.Grenade != null && !DangerGrenade.HasExpired())
            {
                if (GlobalSettingsClass.Instance.Grenade.ENABLED)
                {
                    data = DangerGrenade.BuildThreatData();
                    return true;
                }
            }
            
            // 清理无效威胁
            if (DangerGrenade != null && (DangerGrenade.Grenade == null || DangerGrenade.HasExpired()))
            {
                DangerGrenade = null;
            }
            
            data = null;
            return false;
        }

        /// <summary>
        /// 从 EnemyGrenadesList 中选择最紧急的威胁作为 DangerGrenade。
        /// 紧急度 = 距离 × 剩余时间（越低越紧急）。
        /// </summary>
        private void UpdateDangerGrenade()
        {
            GrenadeTrackerClass mostUrgent = null;
            float bestScore = float.MaxValue;

            foreach (var tracker in EnemyGrenadesList.Values)
            {
                if (tracker?.Grenade == null || tracker.HasExpired()) continue;
                
                float score = tracker.GrenadeDistance;
                if (tracker.RemainingTime > 0f)
                    score *= Mathf.Max(0.1f, tracker.RemainingTime);
                
                if (score < bestScore)
                {
                    bestScore = score;
                    mostUrgent = tracker;
                }
            }
            DangerGrenade = mostUrgent;
        }
    }
}