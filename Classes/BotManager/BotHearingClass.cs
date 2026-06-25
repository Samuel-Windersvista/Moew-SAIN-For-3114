using EFT;
using SAIN.Components.PlayerComponentSpace;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace SAIN.Components.BotControllerSpace.Classes
{
    public class BotHearingClass : BotManagerBase
    {
        public event Action<EPhraseTrigger, ETagStatus, Player> PlayerTalk;

        public event Action<SAINSoundType, Vector3, PlayerComponent, float, float> AISoundPlayed;

        public event Action<EftBulletClass> BulletImpact;

        // SAIN-3.1: Queue-based delayed bot events (replaces per-sound coroutine)
        private struct DelayedBotEvent
        {
            public PlayerComponent PlayerComponent;
            public Vector3 Position;
            public float Range;
            public SAINSoundType SoundType;
            public float PlayTime;
        }

        private readonly List<DelayedBotEvent> _pendingBotEvents = new();

        public BotHearingClass(BotManagerComponent botController) : base(botController)
        {
        }

        public void BulletImpacted(EftBulletClass bullet)
        {
            //Logger.LogInfo($"Shot By: {bullet.Player?.iPlayer?.Profile.Nickname} at Time: {Time.time}");
            //DebugGizmos.Sphere(bullet.CurrentPosition);
            BulletImpact?.Invoke(bullet);
        }

        public void PlayerTalked(EPhraseTrigger phrase, ETagStatus mask, Player player)
        {
            if (phrase == EPhraseTrigger.OnDeath)
            {
                return;
            }
            if (player == null || !player.HealthController.IsAlive)
            {
                return;
            }
            
            PlayerComponent playerComponent = SAINGameWorld.PlayerTracker.GetPlayerComponent(player);
            if (playerComponent != null)
            {
                var Range = phrase switch {
                    EPhraseTrigger.OnBreath => 35,
                    EPhraseTrigger.OnBeingHurt or EPhraseTrigger.OnAgony => 70,
                    _ => (float)(mask == ETagStatus.Unaware ? 40 : 70),
                };
                playerComponent.PlayAISound(SAINSoundType.Conversation, player.Position, Range, 1, phrase, mask);
                PlayerTalk?.Invoke(phrase, mask, player);
            }
        }

        public void PlayAISound(string profileId, SAINSoundType soundType, Vector3 position, float range, float volume)
        {
            PlayerComponent playerComponent = SAINGameWorld.PlayerTracker.GetPlayerComponent(profileId);
            PlayAISound(playerComponent, soundType, position, range, volume, true);
        }

        public void PlayAISound(IPlayer Player, SAINSoundType soundType, Vector3 position, float range, float volume)
        {
            PlayerComponent playerComponent = SAINGameWorld.PlayerTracker.GetPlayerComponent(Player);
            PlayAISound(playerComponent, soundType, position, range, volume, true);
        }

        public void PlayAISound(PlayerComponent playerComponent, SAINSoundType soundType, Vector3 position, float range, float volume, bool limitFreq)
        {
            if (playerComponent == null)
            {
#if DEBUG
                Logger.LogError("Player Component Null");
#endif
                return;
            }
            if (!playerComponent.IsActive)
            {
                return;
            }
            if (!playerComponent.AIData.AISoundPlayer.ShallPlayAISound())
            {
                return;
            }
            playerComponent.PlayAISound(soundType, position, range, volume);
            AISoundPlayed?.Invoke(soundType, position, playerComponent, range, volume);
            //if (playerComponent.Player.IsYourPlayer)
            //{
            //    Logger.LogDebug($"SoundType [{soundType}] FinalRange: {range * volume} Base Range {range} : Volume: {volume}");
            //}
            // SAIN-3.1: Enqueue delayed event instead of starting a coroutine per sound
            _pendingBotEvents.Add(new DelayedBotEvent
            {
                PlayerComponent = playerComponent,
                Position = position,
                Range = range * volume,
                SoundType = soundType,
                PlayTime = Time.time + 0.1f
            });
        }

        /// <summary>
        /// Called each frame from BotManagerComponent.ManualUpdate. Processes queued delayed bot events
        /// in insertion order, removing the coroutine-per-sound overhead.
        /// Uses batch RemoveRange to avoid O(n^2) from per-item RemoveAt.
        /// </summary>
        public void Update()
        {
            float now = Time.time;
            int count = _pendingBotEvents.Count;
            if (count == 0) return;

            // Safety net: if queue is huge, force-clean expired/dead events
            const int MAX_QUEUE = 512;
            if (count > MAX_QUEUE)
            {
                _pendingBotEvents.RemoveAll(e => e.PlayTime < now || e.PlayerComponent?.Player?.HealthController?.IsAlive != true);
            }

            int processed = 0;
            for (int i = 0; i < _pendingBotEvents.Count; i++)
            {
                DelayedBotEvent evt = _pendingBotEvents[i];
                if (now >= evt.PlayTime)
                {
                    // Same null/active checks as the old coroutine
                    if (evt.PlayerComponent?.Player?.HealthController?.IsAlive == true && evt.PlayerComponent.IsActive)
                    {
                        playBotEvent(evt.PlayerComponent.Player, evt.Position, evt.Range, evt.SoundType);
                    }
                    processed++;
                }
                else
                {
                    // Events are queued in insertion order; PlayTime is monotonic, so remaining aren't ready
                    break;
                }
            }

            if (processed > 0)
            {
                _pendingBotEvents.RemoveRange(0, processed); // Single O(n) batch removal
            }
        }

        private void playBotEvent(Player player, Vector3 position, float range, SAINSoundType soundType)
        {
            AISoundType baseSoundType = getBaseSoundType(soundType);
            BotController.BotEventHandler?.PlaySound(player, position, range, baseSoundType);
        }

        private AISoundType getBaseSoundType(SAINSoundType soundType)
        {
            AISoundType baseSoundType;
            switch (soundType)
            {
                case SAINSoundType.Shot:
                    baseSoundType = AISoundType.gun;
                    break;

                case SAINSoundType.SuppressedShot:
                    baseSoundType = AISoundType.silencedGun;
                    break;

                default:
                    baseSoundType = AISoundType.step;
                    break;
            }
            return baseSoundType;
        }
    }
}