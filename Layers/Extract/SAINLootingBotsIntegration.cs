using EFT;
using EFT.Interactive;
using SAIN.Components;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Preset.GlobalSettings;
using UnityEngine;

namespace SAIN.Layers
{
    public class SAINLootingBotsIntegration
    {
        // SAIN-4.2: Named constants for magic numbers
        private const float VIGILANCE_CHECK_INTERVAL = 0.3f;
        private const float VISIBLE_ENEMY_THREAT_DISTANCE = 15f;
        private const float GUNSHOT_THREAT_TIMEWINDOW = 2f;
        private const float GUNSHOT_THREAT_DISTANCE = 50f;
        private const float LOOTING_PREVENTION_DURATION = 10f;
        private const float COVER_CHECK_PREVENTION_DURATION = 30f;
        private const float UPDATE_INFO_INTERVAL = 5f;
        private const float DANGER_SOUND_TIMEWINDOW = 3f;
        private const float DANGER_SOUND_DISTANCE = 30f;
        private const float KNOWN_ENEMY_SAFE_DISTANCE = 40f;
        private const float CLOSE_COVER_DISTANCE = 15f;
        private const float GRENADE_REPORT_THREAT_DISTANCE = 15f;
        private const float RANDOMIZATION_MIN = 0.75f;
        private const float RANDOMIZATION_MAX = 1.25f;

        public SAINLootingBotsIntegration(BotOwner owner, BotComponent sain)
        {
            SAIN = sain;
            BotOwner = owner;
            randomizationFactor = UnityEngine.Random.Range(RANDOMIZATION_MIN, RANDOMIZATION_MAX);
        }

        public bool FullOnLoot { get; private set; }

        public void Update()
        {
            UpdateLootingBotsInfo();
            CheckStatus();
            CheckLootingVigilance();
            CheckFullOnLootReset();
        }

        private readonly BotOwner BotOwner;
        private readonly BotComponent SAIN;

        private void CheckStatus()
        {
            if (!FullOnLoot && CanExtractFromLootValue())
            {
                Logger.LogInfo($"[{BotOwner.name}] Is Moving to Extract because because they are Full on loot. Net Loot Value: {NetLootValue}");
                FullOnLoot = true;
            }
        }

        // SAIN-2.4: Reset FullOnLoot if net loot value drops below threshold or inventory has space again
        private void CheckFullOnLootReset()
        {
            if (!FullOnLoot) return;

            bool belowThreshold = NetLootValue < GetMinNetLootValue() && NetLootValue < MinLootValException;
            bool hasSpaceAgain = !LootingBots.LootingBotsInterop.CheckIfInventoryFull(BotOwner);

            if (belowThreshold || hasSpaceAgain)
            {
                Logger.LogInfo($"[{BotOwner.name}] Reset FullOnLoot — NetLootValue {NetLootValue} below threshold or inventory has space");
                FullOnLoot = false;
            }
        }

        private bool CanExtractFromLootValue()
        {
            if (NetLootValue >= MinLootValException)
            {
                return true;
            }
            if (FullInventory && NetLootValue >= GetMinNetLootValue())
            {
                return true;
            }
            return false;
        }

        private float GetMinNetLootValue()
        {
            if (SAIN.Info.Profile.IsPMC)
            {
                return MinLootValPMC * randomizationFactor;
            }
            else if (SAIN.Info.Profile.IsScav)
            {
                return MinLootValSCAV * randomizationFactor;
            }
            else
            {
                return MinLootValOther * randomizationFactor;
            }
        }

        private void UpdateLootingBotsInfo()
        {
            if (UpdateInfoTimer < Time.time)
            {
                UpdateInfoTimer = Time.time + UPDATE_INFO_INTERVAL;
                NetLootValue = LootingBots.LootingBotsInterop.GetNetLootValue(BotOwner);
                if (NetLootValue != 0)
                {
                    //Logger.LogWarning(NetLootValue);
                }
                FullInventory = LootingBots.LootingBotsInterop.CheckIfInventoryFull(BotOwner);
            }
        }

        public float NetLootValue { get; private set; }
        public bool FullInventory { get; private set; }

        private float UpdateInfoTimer;

        private float randomizationFactor = 0;
        private float _nextVigilanceCheck;
        private float MinLootValPMC => SAINPlugin.LoadedPreset.GlobalSettings.General.LootingBots.MinLootValPMC;
        private float MinLootValSCAV => SAINPlugin.LoadedPreset.GlobalSettings.General.LootingBots.MinLootValSCAV;
        private float MinLootValOther => SAINPlugin.LoadedPreset.GlobalSettings.General.LootingBots.MinLootValOther;
        private float MinLootValException => SAINPlugin.LoadedPreset.GlobalSettings.General.LootingBots.MinLootValException;


        private void CheckLootingVigilance()
        {
            if (!GlobalSettingsClass.Instance.General.LootingBots.LOOTING_THREAT_INTERRUPT) return;

            if (_nextVigilanceCheck > Time.time) return;
            _nextVigilanceCheck = Time.time + VIGILANCE_CHECK_INTERVAL;

            if (!LootingBots.LootingBotsInterop.IsAvailable) return;

            // Check if threat nearby during looting
            bool hasThreat = false;

            // A) Visible enemy threat
            if (SAIN?.GoalEnemy != null && SAIN.GoalEnemy.RealDistance < VISIBLE_ENEMY_THREAT_DISTANCE)
                hasThreat = true;

            // B) Active suppression state
            if (SAIN?.Suppression?.IsSuppressed == true)
                hasThreat = true;

            // C) Recent gunshots
            if (!hasThreat && SAIN?.Hearing?.SoundInput?.AISoundCachedEvents_Gunshots != null)
            {
                var gunshots = SAIN.Hearing.SoundInput.AISoundCachedEvents_Gunshots;
                for (int i = 0; i < gunshots.Count; i++)
                {
                    var data = gunshots[i];
                    if (Time.time - data.Sound.TimeCreated < GUNSHOT_THREAT_TIMEWINDOW && data.PlayerDistance < GUNSHOT_THREAT_DISTANCE)
                    {
                        hasThreat = true;
                        break;
                    }
                }
            }

            // D) Recent suppressed gunshots
            if (!hasThreat && SAIN?.Hearing?.SoundInput?.AISoundCachedEvents_Gunshots_Suppressed != null)
            {
                var suppressed = SAIN.Hearing.SoundInput.AISoundCachedEvents_Gunshots_Suppressed;
                for (int i = 0; i < suppressed.Count; i++)
                {
                    var data = suppressed[i];
                    if (Time.time - data.Sound.TimeCreated < GUNSHOT_THREAT_TIMEWINDOW && data.PlayerDistance < GUNSHOT_THREAT_DISTANCE)
                    {
                        hasThreat = true;
                        break;
                    }
                }
            }

            // E) Recent suppression activity (any residual suppression value)
            if (!hasThreat && SAIN?.Suppression?.SuppressionNumber > 0f)
                hasThreat = true;

            // F) Squad grenade reports
            if (!hasThreat && SAIN?.Talk?.GroupTalk?.GetClosestRecentGrenadeReport() is { } grenadeReport)
            {
                if (Vector3.Distance(SAIN.Position, grenadeReport.DangerPoint) < GRENADE_REPORT_THREAT_DISTANCE)
                    hasThreat = true;
            }

            if (hasThreat)
            {
                LootingBots.LootingBotsInterop.TryPreventBotFromLooting(BotOwner, LOOTING_PREVENTION_DURATION);
                Logger.LogInfo($"SAIN: Interrupted looting for {BotOwner.name} — threat detected");
            }
        }

        public bool TryEnsureSafeLootingPosition()
        {
            if (!GlobalSettingsClass.Instance.General.LootingBots.LOOTING_COVER_CHECK) return true;

            if (!LootingBots.LootingBotsInterop.IsAvailable) return true;

            // Already in cover — good
            if (SAIN?.Cover?.CoverInUse != null)
                return true;

            // Check A: recent dangerous sounds within DANGER_SOUND_DISTANCE and DANGER_SOUND_TIMEWINDOW
            bool recentDangerSound = false;
            if (SAIN?.Hearing?.SoundInput?.AISoundCachedEvents_Gunshots != null)
            {
                var gunshots = SAIN.Hearing.SoundInput.AISoundCachedEvents_Gunshots;
                for (int i = 0; i < gunshots.Count; i++)
                {
                    var data = gunshots[i];
                    if (Time.time - data.Sound.TimeCreated < DANGER_SOUND_TIMEWINDOW && data.PlayerDistance < DANGER_SOUND_DISTANCE)
                    {
                        recentDangerSound = true;
                        break;
                    }
                }
            }
            if (!recentDangerSound && SAIN?.Hearing?.SoundInput?.AISoundCachedEvents_Gunshots_Suppressed != null)
            {
                var suppressed = SAIN.Hearing.SoundInput.AISoundCachedEvents_Gunshots_Suppressed;
                for (int i = 0; i < suppressed.Count; i++)
                {
                    var data = suppressed[i];
                    if (Time.time - data.Sound.TimeCreated < DANGER_SOUND_TIMEWINDOW && data.PlayerDistance < DANGER_SOUND_DISTANCE)
                    {
                        recentDangerSound = true;
                        break;
                    }
                }
            }

            // Check B: known enemies within KNOWN_ENEMY_SAFE_DISTANCE (even if not GoalEnemy)
            bool knownEnemyNearby = false;
            if (SAIN?.EnemyController?.KnownEnemies != null)
            {
                foreach (var knownEnemy in SAIN.EnemyController.KnownEnemies)
                {
                    if (knownEnemy != null && knownEnemy.RealDistance < KNOWN_ENEMY_SAFE_DISTANCE)
                    {
                        knownEnemyNearby = true;
                        break;
                    }
                }
            }

            // No cover — check if area is safe
            bool areaIsSafe = SAIN?.GoalEnemy == null && !recentDangerSound && !knownEnemyNearby;

            // Check C: if no cover in use and enemies exist, verify cover proximity
            if (!areaIsSafe)
            {
                // Check if nearest cover point is within CLOSE_COVER_DISTANCE
                bool hasCloseCover = false;
                if (SAIN?.Cover?.CoverPoints != null)
                {
                    foreach (var point in SAIN.Cover.CoverPoints)
                    {
                        if (point != null && Vector3.Distance(SAIN.Position, point.Position) < CLOSE_COVER_DISTANCE)
                        {
                            hasCloseCover = true;
                            break;
                        }
                    }
                }
                if (!hasCloseCover)
                {
                    // Far from cover and not safe — abort
                    LootingBots.LootingBotsInterop.TryPreventBotFromLooting(BotOwner, COVER_CHECK_PREVENTION_DURATION);
                    return false;
                }
            }

            // Crouch for some concealment
            SAIN?.BotOwner?.SetPose(0f);
            return true;
        }

        private int GetItemPrice(LootItem item)
        {
            float price = LootingBots.LootingBotsInterop.GetItemPriceCached(item);
            return Mathf.RoundToInt(price);
        }

        // INT-4: Trigger LootingBots after post-combat recovery
        public void TryTriggerPostCombatLoot()
        {
            if (!LootingBots.LootingBotsInterop.IsAvailable) return;
            if (SAIN?.GoalEnemy != null) return; // Still in combat
            if (SAIN?.IsInCombat == true) return;

            // Check if bot should loot: has space, not recently looted
            bool hasSpace = !LootingBots.LootingBotsInterop.CheckIfInventoryFull(BotOwner);

            if (hasSpace)
            {
                LootingBots.LootingBotsInterop.TryForceBotToScanLoot(BotOwner);
            }
        }
    }
}