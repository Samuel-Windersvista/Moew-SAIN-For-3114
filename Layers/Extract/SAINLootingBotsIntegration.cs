using EFT;
using EFT.Interactive;
using SAIN.Components;
using SAIN.Preset.GlobalSettings;
using UnityEngine;

namespace SAIN.Layers
{
    public class SAINLootingBotsIntegration
    {
        public SAINLootingBotsIntegration(BotOwner owner, BotComponent sain)
        {
            SAIN = sain;
            BotOwner = owner;
            randomizationFactor = UnityEngine.Random.Range(0.75f, 1.25f);
        }

        public bool FullOnLoot { get; private set; }

        public void Update()
        {
            UpdateLootingBotsInfo();
            CheckStatus();
            CheckLootingVigilance();
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
                UpdateInfoTimer = Time.time + 5f;
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
            _nextVigilanceCheck = Time.time + 0.5f;

            if (!LootingBots.LootingBotsInterop.IsLootingBotsLoaded()) return;

            // Check if threat nearby during looting
            bool hasThreat = false;

            if (SAIN?.GoalEnemy != null && SAIN.GoalEnemy.RealDistance < 15f)
                hasThreat = true;

            if (SAIN?.Suppression?.IsSuppressed == true)
                hasThreat = true;

            if (hasThreat)
            {
                LootingBots.LootingBotsInterop.TryPreventBotFromLooting(BotOwner, 10f);
                Logger.LogInfo($"SAIN: Interrupted looting for {BotOwner.name} — threat detected");
            }
        }

        public bool TryEnsureSafeLootingPosition()
        {
            if (!GlobalSettingsClass.Instance.General.LootingBots.LOOTING_COVER_CHECK) return true;

            if (!LootingBots.LootingBotsInterop.IsLootingBotsLoaded()) return true;

            // Already in cover — good
            if (SAIN?.Cover?.CoverInUse != null)
                return true;

            // No cover — check if area is safe
            bool areaIsSafe = SAIN?.GoalEnemy == null;

            if (areaIsSafe)
            {
                // Crouch for some concealment
                SAIN?.BotOwner?.SetPose(0f);
                return true;
            }

            // Not safe, abort
            LootingBots.LootingBotsInterop.TryPreventBotFromLooting(BotOwner, 30f);
            return false;
        }

        private int GetItemPrice(LootItem item)
        {
            float price = LootingBots.LootingBotsInterop.GetItemPriceCached(item);
            return Mathf.RoundToInt(price);
        }

        // INT-4: Trigger LootingBots after post-combat recovery
        public void TryTriggerPostCombatLoot()
        {
            if (!LootingBots.LootingBotsInterop.IsLootingBotsLoaded()) return;
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