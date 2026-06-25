using EFT;
using SAIN.Components;
using SAIN.Helpers.Events;
using SAIN.Layers;
using SAIN.Models.Enums;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.SAINComponent.SubComponents;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using System;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Decision
{
    public class BotDecisionManager(SAINDecisionClass decisionClass) : BotSubClass<SAINDecisionClass>(decisionClass), IBotClass
    {
        // SAIN-3.3: Adaptive decision frequency — computed per-tick based on combat state
        private const float DECISION_FREQUENCY_FALLBACK = 1f / 10;

        public event Action<ECombatDecision, ESquadDecision, ESelfActionType, Enemy, BotComponent> OnDecisionMade;

        public ToggleEvent HasDecisionToggle { get; } = new ToggleEvent();

        public FSainBotDecision CurrentDecision { get; private set; } = new();

        public ECombatDecision CurrentCombatDecision { get; private set; }
        public ECombatDecision PreviousCombatDecision { get; private set; }
        public ESquadDecision CurrentSquadDecision { get; private set; }
        public ESquadDecision PreviousSquadDecision { get; private set; }
        public ESelfActionType CurrentSelfDecision { get; private set; }
        public ESelfActionType PreviousSelfDecision { get; private set; }

        public bool HasDecision => HasDecisionToggle.Value;

        /// <summary>活跃的手雷威胁数据。null 表示无威胁。</summary>
        private GrenadeThreatData _activeGrenadeThreat;

        public float ChangeDecisionTime { get; private set; }
        public float CombatEndTime = -1f;
        /// <summary>独立于 POST_COMBAT_RECOVERY 的战后拾取计时器，始终在战斗结束时设置。</summary>
        public float LootCombatEndTime = -1f;
        public float TimeSinceChangeDecision => Time.time - ChangeDecisionTime;

        public override void Init()
        {
            Bot.BotActivation.BotActiveToggle.OnToggle += resetDecisions;
            base.Init();
        }

        public override void ManualUpdate()
        {
            if (_nextGetDecisionTime < Time.time)
            {
                // SAIN-3.3: Adaptive decision frequency
                float interval = ComputeDecisionInterval();
                _nextGetDecisionTime = Time.time + interval;
                getDecision();
            }
        }

        /// <summary>
        /// Computes the decision update interval based on bot state and global bot density.
        /// Higher frequency during close combat, lower when idle/far from players.
        /// Scales down when bot count is high to conserve CPU cycles.
        /// </summary>
        private float ComputeDecisionInterval()
        {
            bool inCombat = Bot.IsInCombat;
            Enemy goalEnemy = Bot.GoalEnemy;

            if (inCombat)
            {
                // Adaptive: 10Hz at high bot density (>=60% of max), 20Hz at low density
                int activeBots = BotManagerComponent.Instance?.BotSpawnController?.SAINBots?.Count ?? 0;
                const int MAX_BOTS_ESTIMATE = 30;
                float ratio = (activeBots > 0) ? (float)activeBots / MAX_BOTS_ESTIMATE : 0f;
                // 0% -> 20Hz, 60% -> 10Hz, >60% -> 10Hz (clamped)
                float freq = Mathf.Lerp(20f, 10f, Mathf.Clamp01(ratio / 0.6f));
                return 1f / freq;
            }

            // Out of combat: check distance to nearest human player via AILimit cache
            if (Bot.AILimit.ClosestPlayerDistanceSqr > 0f)
            {
                float dist = Mathf.Sqrt(Bot.AILimit.ClosestPlayerDistanceSqr);
                if (dist > 100f)
                {
                    // Far from any human: 2Hz — decisions barely matter at this range
                    return 1f / 2;
                }
            }

            // Default: 10Hz
            return 1f / 10;
        }

        public override void Dispose()
        {
            Bot.BotActivation.BotActiveToggle.OnToggle -= resetDecisions;
            base.Dispose();
        }

        private void getDecision()
        {
            // 手雷威胁 — 最高优先级，即使无敌人也必须处理
            if (GlobalSettingsClass.Instance.Grenade.ENABLED 
                && Bot.Grenade.GrenadeReactionClass.HasActiveGrenadeThreat(out var grenadeData))
            {
                _activeGrenadeThreat = grenadeData;
                SetDecisions(ECombatDecision.AvoidGrenade, ESquadDecision.None, ESelfActionType.None, null);
                return;
            }

            Enemy enemy = Bot.EnemyController.ChooseEnemy();
            if (enemy == null)
            {
                // F2-4: Post-combat recovery
                if (GlobalSettingsClass.Instance.Mind.POST_COMBAT_RECOVERY && CombatEndTime > 0 && Time.time - CombatEndTime < 30f)
                {
                    bool needsHeal = Bot.Memory.Health.HealthStatus == ETagStatus.Dying
                        || Bot.Memory.Health.HealthStatus == ETagStatus.BadlyInjured;
                    bool needsReload = Bot.Decision.SelfActionDecisions.AmmoRatio < 0.5f;

                    if (needsHeal)
                    {
                        SetDecisions(ECombatDecision.None, ESquadDecision.None, ESelfActionType.FirstAid, enemy);
                        return;
                    }
                    if (Bot.Decision.CurrentSelfDecision == ESelfActionType.Surgery)
                    {
                        return;
                    }
                    if (needsReload)
                    {
                        SetDecisions(ECombatDecision.None, ESquadDecision.None, ESelfActionType.Reload, enemy);
                        return;
                    }
                    CombatEndTime = -1f;
                }

                // INT-4 / SAIN-2.3: Post-combat looting uses LootCombatEndTime (independent of POST_COMBAT_RECOVERY)
                if (LootCombatEndTime > 0 && Time.time - LootCombatEndTime > 10f)
                {
                    if (GlobalSettingsClass.Instance.Mind.POST_COMBAT_LOOTING)
                    {
                        Bot.LootingBotsIntegration?.TryTriggerPostCombatLoot();
                    }
                    LootCombatEndTime = -1f;
                }

                // F2-5: Kill confirm — maintain aim on recent kill
                if (GlobalSettingsClass.Instance.Mind.KILL_CONFIRM_ENABLED && Bot?.Memory?.LastKillTime > 0 && Time.time - Bot.Memory.LastKillTime < 2f)
                {
                    if (Bot?.GoalEnemy == null)
                    {
                        Bot.Memory.LastKillTime = -1f;
                    }
                }

                SetDecisions(ECombatDecision.None, ESquadDecision.None, ESelfActionType.None, enemy);
                return;
            }
            BaseClass.EnemyDecisions.DebugShallSearch = null;
            if (BaseClass.SelfActionDecisions.GetDecision(out ESelfActionType selfDecision, enemy))
            {
                SetDecisions(ECombatDecision.SeekCover, ESquadDecision.None, selfDecision, enemy);
                return;
            }

            // Tagilla 近战: BSG 原生 AI 自行管理武器切换（何时拔锤/何时收锤）。
            // SAIN 仅通过下文的通用 MeleeAttack 决策接管已切换近战武器后的战斗行为。

            if (enemy != null && enemy.IsZombie)
            {
                bool hasShooterContact = false;
                foreach (var knownEnemy in Bot.EnemyController.KnownEnemies)
                    if (knownEnemy?.IsZombie != true)
                        hasShooterContact = true;
                if (!hasShooterContact)
                {
                    BaseClass.SelfActionDecisions.GetDecision(out ESelfActionType zombieDecision, enemy);
                    BaseClass.SquadDecisions.GetDecision(out ESquadDecision zombieSqdDecision, enemy);
                    SetDecisions(ECombatDecision.FightZombies, zombieSqdDecision, zombieDecision, enemy);
                    return;
                }
            }
            if (Bot.Decision.DogFightDecision.DogFightActive)
            {
                SetDecisions(ECombatDecision.DogFight, ESquadDecision.None, ESelfActionType.None, enemy);
                return;
            }
            if (BotOwner.WeaponManager.IsMelee)
            {
                SetDecisions(ECombatDecision.MeleeAttack, ESquadDecision.None, ESelfActionType.None, enemy);
                return;
            }
            if (ContinueMoveToCover())
            {
                SetDecisions(ECombatDecision.SeekCover, ESquadDecision.None, Bot.Decision.CurrentSelfDecision, enemy);
                return;
            }
            if (BaseClass.SquadDecisions.GetDecision(out ESquadDecision squadDecision, enemy))
            {
                SetDecisions(ECombatDecision.None, squadDecision, ESelfActionType.None, enemy);
                return;
            }
            if (BaseClass.EnemyDecisions.GetDecision(out ECombatDecision combatDecision, enemy, Bot.EnemyController.KnownEnemies))
            {
                SetDecisions(combatDecision, ESquadDecision.None, ESelfActionType.None, enemy);
                return;
            }
            SetDecisions(ECombatDecision.None, ESquadDecision.None, ESelfActionType.None, enemy);
        }

        private void SetDecisions(ECombatDecision solo, ESquadDecision squad, ESelfActionType self, Enemy enemy)
        {
#if DEBUG
            if (SAINPlugin.DebugMode)
            {
                if (SAINPlugin.ForceSoloDecision != ECombatDecision.None)
                {
                    solo = SAINPlugin.ForceSoloDecision;
                }
                if (SAINPlugin.ForceSquadDecision != ESquadDecision.None)
                {
                    squad = SAINPlugin.ForceSquadDecision;
                }
                if (SAINPlugin.ForceSelfDecision != ESelfActionType.None)
                {
                    self = SAINPlugin.ForceSelfDecision;
                }
            }
#endif

            if (checkForNewDecision(solo, squad, self, enemy))
            {
                bool hasDecision =
                    solo != ECombatDecision.None ||
                    self != ESelfActionType.None ||
                    squad != ESquadDecision.None;

                if (hasDecision)
                {
                    BotOwner.PatrollingData.Pause();
                }

                ChangeDecisionTime = Time.time;
                HasDecisionToggle.CheckToggle(hasDecision, ChangeDecisionTime);
                OnDecisionMade?.Invoke(solo, squad, self, enemy, Bot);
            }
        }
        
        private void SetDecision(FSainBotDecision decision)
        {
#if DEBUG
            if (SAINPlugin.DebugMode)
            {
                if (SAINPlugin.ForceSoloDecision != ECombatDecision.None)
                {
                    decision.CombatDecision = SAINPlugin.ForceSoloDecision;
                }
                if (SAINPlugin.ForceSquadDecision != ESquadDecision.None)
                {
                    decision.SquadDecision = SAINPlugin.ForceSquadDecision;
                }
                if (SAINPlugin.ForceSelfDecision != ESelfActionType.None)
                {
                    decision.SelfAction = SAINPlugin.ForceSelfDecision;
                }
            }
#endif

            if (CurrentDecision != decision)
            {
                bool hasDecision = decision.CombatDecision != ECombatDecision.None || decision.SelfAction != ESelfActionType.None || decision.SquadDecision != ESquadDecision.None;

                ChangeDecisionTime = Time.time;
                HasDecisionToggle.CheckToggle(hasDecision, ChangeDecisionTime);
                CurrentDecision = decision;
                OnDecisionMade?.Invoke(decision.CombatDecision, decision.SquadDecision, decision.SelfAction, decision.Enemy, Bot);
            }
        }

        private bool checkForNewDecision(ECombatDecision newSoloDecision, ESquadDecision newSquadDecision, ESelfActionType newSelfDecision, Enemy enemy)
        {
            bool newDecision = false;

            if (_lastDecisionEnemy != enemy)
            {
                _lastDecisionEnemy = enemy;
                newDecision = true;
            }

            if (newSoloDecision != CurrentCombatDecision)
            {
                PreviousCombatDecision = CurrentCombatDecision;
                CurrentCombatDecision = newSoloDecision;
                newDecision = true;
            }

            if (newSquadDecision != CurrentSquadDecision)
            {
                PreviousSquadDecision = CurrentSquadDecision;
                CurrentSquadDecision = newSquadDecision;
                newDecision = true;
            }

            if (newSelfDecision != CurrentSelfDecision)
            {
                PreviousSelfDecision = CurrentSelfDecision;
                CurrentSelfDecision = newSelfDecision;
                newDecision = true;
            }

            return newDecision;
        }

        private Enemy _lastDecisionEnemy;

        public void ResetDecisions(bool active)
        {
            bool hasDecision = HasDecision;
            resetDecisions(false);
            if (active && hasDecision)
            {
                //BotOwner.CalcGoal();
            }
        }

        private void resetDecisions(bool value)
        {
            if (!value)
            {
                SetDecisions(ECombatDecision.None, ESquadDecision.None, ESelfActionType.None, null);
            }
        }

        private bool ContinueMoveToCover()
        {
            bool runningToCover = Bot.Decision.RunningToCover;
            if (!runningToCover) return false;
            if (!Bot.Mover.Moving) return false;
            if (Bot.Cover.HasCover) return false;

            float timeChangeDec = Bot.Decision.TimeSinceChangeDecision;
            if (timeChangeDec < 0.5f) return true;

            //if (timeChangeDec > 3 &&
            //    !Bot.BotStuck.BotHasChangedPosition)
            //{
            //    return false;
            //}

            CoverPoint coverMovingTo = Bot.Cover.CoverPoint_MovingTo;
            return coverMovingTo != null && coverMovingTo.PathDistanceStatus switch {
                CoverStatus.InCover => false,
                CoverStatus.CloseToCover => true,
                _ => !coverMovingTo.CoverData.IsBad,
            };
        }

        /// <summary>供 DodgeGrenadeAction 读取当前活跃手雷威胁数据。</summary>
        public GrenadeThreatData ActiveGrenadeThreat => _activeGrenadeThreat;

        /// <summary>由 GrenadeTrackerClass 在 CanReact 触发时调用。</summary>
        public void SetAvoidGrenade(GrenadeThreatData data)
        {
            _activeGrenadeThreat = data;
        }

        /// <summary>由 GrenadeTrackerClass 在手雷落点更新时调用。</summary>
        public void UpdateGrenadeDangerPoint(Vector3 newPoint)
        {
            if (_activeGrenadeThreat != null)
            {
                _activeGrenadeThreat.DangerPoint = newPoint;
                _activeGrenadeThreat.LastUpdateTime = Time.time;
            }
        }

        /// <summary>清除手雷威胁。由 GrenadeReactionClass 在手雷销毁/过期时调用。</summary>
        public void ClearGrenadeThreat()
        {
            _activeGrenadeThreat = null;
        }

        private float _nextGetDecisionTime;
    }
}