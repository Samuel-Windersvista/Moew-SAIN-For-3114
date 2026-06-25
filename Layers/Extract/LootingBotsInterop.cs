using BepInEx.Bootstrap;

using EFT;
using UnityEngine;
using EFT.Interactive;

using HarmonyLib;
using SAIN;
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
        private static MethodInfo _IsBotInLootAnimationMethod;

        /// <summary>
        /// True only if the plugin is loaded AND all reflected methods were resolved successfully AND no invoke has failed at runtime.
        /// Checked at the start of every public method to fast-fail after a prior invoke failure.
        /// </summary>
        public static bool IsAvailable { get; private set; }

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
                    _IsBotInLootAnimationMethod = AccessTools.Method(
                        _LootingBotsExternalType,
                        "IsBotInLootAnimation"
                    );

                    // All methods must be resolved for IsAvailable to be true
                    IsAvailable = _ForceBotToScanLootMethod != null
                        && _PreventBotFromLootingMethod != null
                        && _CheckIfInventoryFullMethod != null
                        && _GetNetLootValueMethod != null
                        && _GetItemPriceMethod != null
                        && _IsBotLootingMethod != null
                        && _IsBotInLootAnimationMethod != null;
                }
            }

            return _LootingBotsExternalType != null && IsAvailable;
        }

        /**
         * Force a bot to search for loot immediately if Looting Bots is loaded. Return true if successful.
         */
        public static bool TryForceBotToScanLoot(BotOwner botOwner)
        {
            if (!IsAvailable)
                return false;
            if (!Init())
                return false;
            if (_ForceBotToScanLootMethod == null)
                return false;

            try
            {
                return (bool)_ForceBotToScanLootMethod.Invoke(null, [botOwner]);
            }
            catch (Exception ex)
            {
                SAIN.Logger.LogWarning($"[LootingBotsInterop] TryForceBotToScanLoot invoke failed: {ex.Message}");
                IsAvailable = false;
                return false;
            }
        }

        /**
         * Stops a bot from looting and searching for loot (until the scan timer expires) if Looting Bots is loaded. Return true if successful.
         */
        public static bool TryPreventBotFromLooting(BotOwner botOwner, float duration)
        {
            if (!IsAvailable)
                return false;
            if (!Init())
                return false;
            if (_PreventBotFromLootingMethod == null)
                return false;

            try
            {
                return (bool)
                    _PreventBotFromLootingMethod.Invoke(null, [botOwner, duration]);
            }
            catch (Exception ex)
            {
                SAIN.Logger.LogWarning($"[LootingBotsInterop] TryPreventBotFromLooting invoke failed: {ex.Message}");
                IsAvailable = false;
                return false;
            }
        }

        /**
         * Checks if a bot's inventory is full or not
         */
        public static bool CheckIfInventoryFull(BotOwner botOwner)
        {
            if (!IsAvailable)
                return false;
            if (!Init())
                return false;
            if (_CheckIfInventoryFullMethod == null)
                return false;

            try
            {
                return (bool)
                    _CheckIfInventoryFullMethod.Invoke(null, [botOwner]);
            }
            catch (Exception ex)
            {
                SAIN.Logger.LogWarning($"[LootingBotsInterop] CheckIfInventoryFull invoke failed: {ex.Message}");
                IsAvailable = false;
                return false;
            }
        }

        /**
         * Gets the total value looted by a bot in this raid
         */
        public static float GetNetLootValue(BotOwner botOwner)
        {
            if (!IsAvailable)
                return 0f;
            if (!Init())
                return 0f;
            if (_GetNetLootValueMethod == null)
            {
                return 0f;
            }

            try
            {
                return (float)
                    _GetNetLootValueMethod.Invoke(null, [botOwner]);
            }
            catch (Exception ex)
            {
                SAIN.Logger.LogWarning($"[LootingBotsInterop] GetNetLootValue invoke failed: {ex.Message}");
                IsAvailable = false;
                return 0f;
            }
        }

        /**
         * Checks the price of a loot item using LB ItemAppraiser
         */
        public static float GetItemPrice(LootItem item)
        {
            if (!IsAvailable)
                return 0f;
            if (!Init())
                return 0f;
            if (_GetItemPriceMethod == null)
                return 0f;

            try
            {
                return (float)
                    _GetItemPriceMethod.Invoke(null, [item]);
            }
            catch (Exception ex)
            {
                SAIN.Logger.LogWarning($"[LootingBotsInterop] GetItemPrice invoke failed: {ex.Message}");
                IsAvailable = false;
                return 0f;
            }
        }

        // INT-5 / SAIN-3.4: LRU item price cache
        private struct CacheEntry
        {
            public float Price;
            public float LastAccessTime;
        }

        private static readonly Dictionary<string, CacheEntry> _itemPriceCache = new();
        private const int CACHE_MAX_SIZE = 256;
        private const float CACHE_ENTRY_TTL = 60f;

        public static float GetItemPriceCached(LootItem item)
        {
            if (!IsAvailable)
                return 0f;

            string tpl = item?.Item?.TemplateId;
            if (tpl == null) return 0f;

            float now = Time.time;

            // Access fast path: update timestamp and return
            if (_itemPriceCache.TryGetValue(tpl, out CacheEntry entry))
            {
                entry.LastAccessTime = now;
                _itemPriceCache[tpl] = entry;
                return entry.Price;
            }

            // Cache miss: resolve price
            float price = GetItemPrice(item);

            // Eviction: if at capacity, remove oldest entry(s) first
            if (_itemPriceCache.Count >= CACHE_MAX_SIZE)
            {
                string oldestKey = null;
                float oldestTime = float.MaxValue;
                foreach (var kvp in _itemPriceCache)
                {
                    if (kvp.Value.LastAccessTime < oldestTime)
                    {
                        oldestTime = kvp.Value.LastAccessTime;
                        oldestKey = kvp.Key;
                    }
                }
                if (oldestKey != null)
                    _itemPriceCache.Remove(oldestKey);
            }

            _itemPriceCache[tpl] = new CacheEntry { Price = price, LastAccessTime = now };
            return price;
        }

        /// <summary>
        /// Called periodically (e.g. from an update loop) to evict stale entries without waiting for cache-full.
        /// </summary>
        public static void EvictStaleCacheEntries()
        {
            if (_itemPriceCache.Count == 0)
                return;

            float now = Time.time;
            // Collect stale keys
            List<string> stale = null;
            foreach (var kvp in _itemPriceCache)
            {
                if (now - kvp.Value.LastAccessTime > CACHE_ENTRY_TTL)
                {
                    stale ??= new List<string>();
                    stale.Add(kvp.Key);
                }
            }
            if (stale != null)
            {
                for (int i = 0; i < stale.Count; i++)
                    _itemPriceCache.Remove(stale[i]);
            }
        }

        /**
         * Checks if a bot is currently looting something
         */
        public static bool IsBotLooting(BotOwner botOwner)
        {
            if (!IsAvailable)
                return false;
            if (!Init())
                return false;
            if (_IsBotLootingMethod == null)
                return false;

            try
            {
                return (bool)_IsBotLootingMethod.Invoke(null, [botOwner]);
            }
            catch (Exception ex)
            {
                SAIN.Logger.LogWarning($"[LootingBotsInterop] IsBotLooting invoke failed: {ex.Message}");
                IsAvailable = false;
                return false;
            }
        }

        /**
         * Checks if a bot is currently in a loot animation via LootingBots.External.IsBotInLootAnimation
         */
        public static bool IsBotInLootAnimation(BotOwner botOwner)
        {
            if (!IsAvailable)
                return false;
            if (!Init())
                return false;
            if (_IsBotInLootAnimationMethod == null)
                return false;

            try
            {
                return (bool)_IsBotInLootAnimationMethod.Invoke(null, [botOwner]);
            }
            catch (Exception ex)
            {
                SAIN.Logger.LogWarning($"[LootingBotsInterop] IsBotInLootAnimation invoke failed: {ex.Message}");
                IsAvailable = false;
                return false;
            }
        }
    }
}
