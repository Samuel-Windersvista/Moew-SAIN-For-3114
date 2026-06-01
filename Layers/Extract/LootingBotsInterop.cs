using BepInEx.Bootstrap;

using EFT;
using UnityEngine;
using EFT.Interactive;

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace LootingBots
{
    internal static class LootingBotsInterop
    {
        private static bool _LootingBotsLoadedChecked = false;
        private static bool _LootingBotsInteropInited = false;

        private static bool _IsLootingBotsLoaded;
        private static Type _LootingBotsExternalType;
        private static MethodInfo _ForceBotToScanLootMethod;
        private static MethodInfo _PreventBotFromLootingMethod;
        private static MethodInfo _CheckIfInventoryFullMethod;
        private static MethodInfo _GetNetLootValueMethod;
        private static MethodInfo _GetItemPriceMethod;
        private static MethodInfo _IsBotLootingMethod;

        /**
         * Return true if Looting Bots is loaded in the client
         */
        public static bool IsLootingBotsLoaded()
        {
            // Only check for SAIN once
            if (!_LootingBotsLoadedChecked)
            {
                _LootingBotsLoadedChecked = true;
                _IsLootingBotsLoaded = Chainloader.PluginInfos.ContainsKey(
                    "me.skwizzy.lootingbots"
                );
            }

            return _IsLootingBotsLoaded;
        }

        /**
         * Initialize the Looting Bots interop class data, return true on success
         */
        public static bool Init()
        {
            if (!IsLootingBotsLoaded())
                return false;

            // Only check for the External class once
            if (!_LootingBotsInteropInited)
            {
                _LootingBotsInteropInited = true;

                _LootingBotsExternalType = Type.GetType(
                    "LootingBots.External, skwizzy.LootingBots"
                );

                // Only try to get the methods if we have the type
                if (_LootingBotsExternalType != null)
                {
                    _ForceBotToScanLootMethod = AccessTools.Method(
                        _LootingBotsExternalType,
                        "ForceBotToScanLoot"
                    );
                    _PreventBotFromLootingMethod = AccessTools.Method(
                        _LootingBotsExternalType,
                        "PreventBotFromLooting"
                    );
                    _CheckIfInventoryFullMethod = AccessTools.Method(
                        _LootingBotsExternalType,
                        "CheckIfInventoryFull"
                    );
                    _GetNetLootValueMethod = AccessTools.Method(
                        _LootingBotsExternalType,
                        "GetNetLootValue"
                    );
                    _GetItemPriceMethod = AccessTools.Method(
                        _LootingBotsExternalType,
                        "GetItemPrice"
                    );
                    _IsBotLootingMethod = AccessTools.Method(
                        _LootingBotsExternalType,
                        "IsBotLooting"
                    );
                }
            }

            // If we found the External class, at least some of the methods are (probably) available
            return _LootingBotsExternalType != null;
        }

        /**
         * Force a bot to search for loot immediately if Looting Bots is loaded. Return true if successful.
         */
        public static bool TryForceBotToScanLoot(BotOwner botOwner)
        {
            if (!Init())
                return false;
            if (_ForceBotToScanLootMethod == null)
                return false;

            return (bool)_ForceBotToScanLootMethod.Invoke(null, [botOwner]);
        }

        /**
         * Stops a bot from looting and searching for loot (until the scan timer expires) if Looting Bots is loaded. Return true if successful.
         */
        public static bool TryPreventBotFromLooting(BotOwner botOwner, float duration)
        {
            if (!Init())
                return false;
            if (_PreventBotFromLootingMethod == null)
                return false;

            return (bool)
                _PreventBotFromLootingMethod.Invoke(null, [botOwner, duration]);
        }

        /**
         * Checks if a bot's inventory is full or not
         */
        public static bool CheckIfInventoryFull(BotOwner botOwner)
        {
            if (!Init())
                return false;
            if (_CheckIfInventoryFullMethod == null)
                return false;

            return (bool)
                _CheckIfInventoryFullMethod.Invoke(null, [botOwner]);
        }

        /**
         * Gets the total value looted by a bot in this raid
         */
        public static float GetNetLootValue(BotOwner botOwner)
        {
            if (!Init())
                return 0f;
            if (_GetNetLootValueMethod == null)
            {
                return 0f;
            }

            return (float)
                _GetNetLootValueMethod.Invoke(null, [botOwner]);
        }

        /**
         * Checks the price of a loot item using LB ItemAppraiser
         */
        public static float GetItemPrice(LootItem item)
        {
            if (!Init())
                return 0f;
            if (_GetItemPriceMethod == null)
                return 0f;

            return (float)
                _GetItemPriceMethod.Invoke(null, [item]);
        }

        // INT-5: Item price cache
        private static readonly Dictionary<string, float> _itemPriceCache = new();
        private static float _cacheCleanupTime;
        private const float CACHE_EXPIRY = 60f; // 60 second cache

        public static float GetItemPriceCached(LootItem item)
        {
            string tpl = item?.Item?.TemplateId;
            if (tpl == null) return 0f;

            // Periodically clear stale cache
            if (_cacheCleanupTime < Time.time)
            {
                _cacheCleanupTime = Time.time + CACHE_EXPIRY;
                _itemPriceCache.Clear();
            }

            if (_itemPriceCache.TryGetValue(tpl, out float price))
                return price;

            price = GetItemPrice(item);
            _itemPriceCache[tpl] = price;
            return price;
        }

        /**
         * Checks if a bot is currently looting something
         */
        public static bool IsBotLooting(BotOwner botOwner)
        {
            if (!Init())
                return false;
            if (_IsBotLootingMethod == null)
                return false;

            return (bool)_IsBotLootingMethod.Invoke(null, [botOwner]);
        }
    }
}
