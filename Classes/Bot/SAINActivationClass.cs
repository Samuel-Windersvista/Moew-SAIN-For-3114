using Comfort.Common;
using EFT;
using SAIN.Components;
using SAIN.Helpers.Events;
using SAIN.Layers;
using SAIN.Models.Enums;
using SAIN.Preset.GlobalSettings;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class SAINActivationClass(BotComponent botComponent) : BotComponentClassBase(botComponent)
    {
        private const float ACTIVATE_STANDBY_HUMAN = 150;
        private const float ACTIVATE_STANDBY_AI = 50;
        private const float ACTIVATE_STANDBY_CHECK_FREQ = 3f;

        public ESAINLayer ActiveLayer { get; private set; }
        public IBotAction CurrentAction { get; private set; }

        public void SetCurrentAction(IBotAction action)
        {
            CurrentAction = action;
        }

        public bool BotActive => BotActiveToggle.Value;
        public ToggleEvent BotActiveToggle { get; } = new ToggleEvent();
        public bool BotInStandBy => BotStandByToggle.Value;
        public ToggleEvent BotStandByToggle { get; } = new ToggleEvent();
        public bool GameEnding => GameEndingToggle.Value;
        public ToggleEvent GameEndingToggle { get; } = new ToggleEvent();
        public bool SAINLayersActive => SAINLayersActiveToggle.Value;
        public ToggleEvent SAINLayersActiveToggle { get; } = new ToggleEvent();
        public bool BotInCombat => BotInCombatToggle.Value;
        public ToggleEvent BotInCombatToggle { get; } = new ToggleEvent();

        public void SetActive(bool botActive)
        {
            float time = Time.time;
            BotActiveToggle.CheckToggle(botActive, time);

            if (!botActive)
            {
                BotStandByToggle.CheckToggle(true, time);
                ActiveLayer = ESAINLayer.None;
                SAINLayersActiveToggle.CheckToggle(false, time);
                Bot.Mover.Stop();
                Bot.AimDownSightsController.SetADS(false, true);
                CurrentAction = null;
            }
        }

        public void SetInCombat(bool inCombat)
        {
            BotInCombatToggle.CheckToggle(inCombat, Time.time);
        }

        public void SetActiveLayer(ESAINLayer layer)
        {
            ActiveLayer = layer;
        }

        public override void ManualUpdate()
        {
            CheckGameEnding();
            CheckBotActive();
            CheckStandBy();

            bool wasActive = SAINLayersActiveToggle.Value;
            SAINLayersActiveToggle.CheckToggle(ActiveLayer != ESAINLayer.None, Time.time);
            bool activeNow = SAINLayersActiveToggle.Value;
            if (wasActive && !activeNow)
            {
                Bot.Mover.Stop();
                Bot.AimDownSightsController.SetADS(false, true);
                CurrentAction = null;
            }

            // F2-4: Detect combat end and trigger recovery
            if (_wasInCombat && !Bot.IsInCombat)
            {
                if (GlobalSettingsClass.Instance.Mind.POST_COMBAT_RECOVERY)
                {
                    Bot.Decision.DecisionManager.CombatEndTime = Time.time;
                }
                // SAIN-2.3: Always set LootCombatEndTime for post-combat looting (independent of POST_COMBAT_RECOVERY)
                Bot.Decision.DecisionManager.LootCombatEndTime = Time.time;
            }
            _wasInCombat = Bot.IsInCombat;
        }

        private void CheckBotActive()
        {
            if (GameEnding)
            {
                SetActive(false);
                return;
            }

            if (!Bot.PlayerComponent.IsActive)
            {
                SetActive(false);
                return;
            }

            if (BotOwner != null && BotOwner.BotState == EBotState.Active)
            {
                SetActive(true);
            }
        }

        private void CheckStandBy()
        {
            bool standby = BotOwner?.StandBy?.StandByType != BotStandByType.active;
            if (standby)
            {
                // SAIN-4.3: Removed old standby-auto-disable logic (commented out)
            }

            if (standby)
            {
                Bot.Mover.Stop();
                CurrentAction = null;
            }

            BotStandByToggle.CheckToggle(standby, Time.time);
        }

        private bool CheckAllEnemies()
        {
            if (_nextCheckEnemiesTime > Time.time)
            {
                return false;
            }
            _nextCheckEnemiesTime = Time.time + ACTIVATE_STANDBY_CHECK_FREQ;

            var enemies = Bot.EnemyController.Enemies.Values;
            foreach (var enemy in enemies)
                if (enemy != null &&
                    (enemy.InLineOfSight ||
                    (enemy.IsAI && enemy.RealDistance < ACTIVATE_STANDBY_AI) ||
                    (!enemy.IsAI && enemy.RealDistance < ACTIVATE_STANDBY_HUMAN)))
                    return true;

            return false;
        }

        private void CheckGameEnding()
        {
            var botGame = Singleton<IBotGame>.Instance;
            bool gameEnding = botGame == null || botGame.Status == GameStatus.Stopping;
            GameEndingToggle.CheckToggle(gameEnding, Time.time);
        }

        public override void Init()
        {
            SetActive(true);
            base.Init();
        }

        public override void Dispose()
        {
            SetActive(false);
            base.Dispose();
        }

        private float _nextCheckEnemiesTime;
        private bool _wasInCombat;
    }
}