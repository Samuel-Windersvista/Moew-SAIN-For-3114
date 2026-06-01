using Comfort.Common;
using EFT;
using HarmonyLib;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Helpers;
using SAIN.Models.Structs;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SAIN.Components
{
    public class GrenadeController(BotManagerComponent controller) : BotManagerBase(controller)
    {
        public event Action<Grenade, float> OnGrenadeCollision;

        public event Action<Grenade, Vector3, string> OnGrenadeThrown;

        public event Action<Grenade, Vector3, float> OnGrenadeDangerUpdated;

        public void Init()
        {
        }

        public void Update()
        {
        }

        public void Dispose()
        {
        }

        public void Subscribe(BotEventHandler eventHandler)
        {
            eventHandler.OnGrenadeThrow += GrenadeThrown;
            eventHandler.OnGrenadeExplosive += GrenadeExplosion;
        }

        public void UnSubscribe(BotEventHandler eventHandler)
        {
            eventHandler.OnGrenadeThrow -= GrenadeThrown;
            eventHandler.OnGrenadeExplosive -= GrenadeExplosion;
        }

        public void GrenadeCollided(Grenade grenade, float maxRange)
        {
            OnGrenadeCollision?.Invoke(grenade, maxRange);
        }

        private void GrenadeExplosion(Vector3 explosionPosition, string playerProfileID, bool isSmoke, float smokeRadius, float smokeLifeTime)
        {
            if (!Singleton<BotEventHandler>.Instantiated || playerProfileID == null)
            {
                return;
            }
            Player player = GameWorldInfo.GetAlivePlayer(playerProfileID);
            if (player != null)
            {
                if (!isSmoke)
                {
                    RegisterGrenadeExplosionForSAINBots(explosionPosition, player, playerProfileID, 200f);
                }
                else
                {
                    RegisterGrenadeExplosionForSAINBots(explosionPosition, player, playerProfileID, 50f);

                    float radius = smokeRadius * HelpersGClass.SMOKE_GRENADE_RADIUS_COEF;
                    Vector3 position = player.Position;

                    if (BotController.DefaultController != null)
                        foreach (var keyValuePair in BotController.DefaultController.Groups())
                            foreach (BotsGroup botGroupClass in keyValuePair.Value.GetGroups(true))
                                botGroupClass.AddSmokePlace(explosionPosition, smokeLifeTime, radius, position);
                }
            }
        }

        private void RegisterGrenadeExplosionForSAINBots(Vector3 explosionPosition, Player player, string playerProfileID, float range)
        {
            // Play a sound with the input range.
            Singleton<BotEventHandler>.Instance?.PlaySound(player, explosionPosition, range, AISoundType.gun);
            float currentTime = Time.time;
            // We dont want bots to think the grenade explosion was a place they heard an enemy, so set this manually.
            foreach (var bot in Bots.Values)
            {
                if (bot?.BotActive == true)
                {
                    float distance = (bot.Position - explosionPosition).magnitude;
                    if (distance < range)
                    {
                        Enemy enemy = bot.EnemyController.GetEnemy(playerProfileID, true);
                        if (enemy != null)
                        {
                            float dispersion = distance / 10f;
                            Vector3 random = UnityEngine.Random.onUnitSphere * dispersion;
                            random.y = 0;
                            Vector3 estimatedThrowPosition = enemy.EnemyPosition + random;

                            SAINHearingReport report = new() {
                                position = estimatedThrowPosition,
                                soundType = SAINSoundType.GrenadeExplosion,
                                placeType = EEnemyPlaceType.Hearing,
                                isDanger = distance < 100f || enemy.InLineOfSight,
                                shallReportToSquad = true,
                            };
                            enemy.Hearing.SetHeard(report, currentTime);
                        }
                    }
                }
            }
        }

        private void GrenadeThrown(Grenade grenade, Vector3 position, Vector3 force, float mass)
        {
            if (grenade == null)
            {
                return;
            }

            Player player = GameWorldInfo.GetAlivePlayer(grenade.ProfileId);
            if (player == null)
            {
                Logger.LogError($"Player Null from ID {grenade.ProfileId}");
                return;
            }
            if (!player.HealthController.IsAlive)
            {
                return;
            }

            Vector3 dangerPoint = Vector.DangerPoint(position, force, mass);
            grenade.DestroyEvent += grenadeDestroyed;
            Singleton<BotEventHandler>.Instance?.PlaySound(player, grenade.transform.position, 20f, AISoundType.gun);
            OnGrenadeThrown?.Invoke(grenade, dangerPoint, grenade.ProfileId);
            if (GameWorldComponent.TryGetPlayerComponent(player, out PlayerComponent playerComponent))
            {
                List<PlayerComponent> RelevantPlayers = [];
                foreach (var otherPlayer in playerComponent.OtherPlayersData.DataDictionary.Values)
                {
                    if (otherPlayer.DistanceData.Distance < 125f && otherPlayer.OtherPlayerComponent.IsSAINBot)
                    {
                        RelevantPlayers.Add(otherPlayer.OtherPlayerComponent);
                    }
                }
                ActiveGrenades.Add(grenade, RelevantPlayers);
                float estimatedFuseTime = GetGrenadeFuseTime(grenade);
                BotController.StartCoroutine(GrenadeTracker(grenade, playerComponent, RelevantPlayers, dangerPoint, estimatedFuseTime));
            }
        }

        public readonly Dictionary<Throwable, List<PlayerComponent>> ActiveGrenades = [];

        private void grenadeDestroyed(Throwable Grenade)
        {
            ActiveGrenades.Remove(Grenade);
        }

        /// <summary>
        /// 获取手雷引信时间。反射优先，查表兜底。返回秒数。
        /// </summary>
        private float GetGrenadeFuseTime(Grenade grenade)
        {
            // 通过反射获取模板 ID（EFT 外部程序集类型可能无法被 LSP 解析）
            string templateId = "";
            try
            {
                var templateIdProp = AccessTools.Property(typeof(Throwable).BaseType, "TemplateId");
                if (templateIdProp != null)
                    templateId = (templateIdProp.GetValue(grenade) as string)?.ToLower() ?? "";
                if (string.IsNullOrEmpty(templateId))
                    templateId = grenade?.GetType()?.Name?.ToLower() ?? "";
            }
            catch
            {
                templateId = grenade?.GetType()?.Name?.ToLower() ?? "";
            }

            // 检查是否为碰炸手雷
            if (templateId.Contains("vog"))
                return 0f;

            // 优先：反射读取
            try
            {
                if (_destroyTimeField != null)
                {
                    return Mathf.Max(0f, (float)_destroyTimeField.GetValue(grenade) - Time.time);
                }
                if (_explosionTimeField != null)
                {
                    return Mathf.Max(0f, (float)_explosionTimeField.GetValue(grenade) - Time.time);
                }
                if (_fuseTimeField != null)
                {
                    return (float)_fuseTimeField.GetValue(grenade);
                }
            }
            catch
            {
                // 反射失败，降级到查表
            }

            // 兜底：已知引信时间表
            if (templateId.Contains("f1")) return 3.5f;
            if (templateId.Contains("rgd")) return 3.5f;
            if (templateId.Contains("m67")) return 4.0f;
            if (templateId.Contains("m18")) return 2.0f;
            if (templateId.Contains("zarya")) return 2.5f;
            if (templateId.Contains("stun")) return 2.5f;
            if (templateId.Contains("flash")) return 2.5f;
            if (templateId.Contains("smoke")) return 2.0f;

            return 3.5f;
        }

        private IEnumerator GrenadeTracker(Grenade Grenade, PlayerComponent Thrower, List<PlayerComponent> RelevantPlayers, Vector3 DangerPoint, float estimatedFuseTime)
        {
            Rigidbody Rigidbody = (Rigidbody)_rigidBodyField.GetValue(Grenade);

            if (Rigidbody == null)
            {
#if DEBUG
                Logger.LogError("RigidBody Null");
#endif
                yield break;
            }

            float thrownTime = Time.time;

            while (Grenade != null && BotController != null && Rigidbody != null)
            {
                Vector3 Velocity = Rigidbody.velocity;

                float remainingTime;
                if (estimatedFuseTime <= 0f)
                {
                    remainingTime = 0f; // 碰炸
                }
                else
                {
                    remainingTime = estimatedFuseTime - (Time.time - thrownTime);
                    if (remainingTime < 0f) remainingTime = 0f;
                }

                if (Velocity.magnitude < 0.1f)
                {
                    OnGrenadeDangerUpdated?.Invoke(Grenade, Grenade.transform.position, remainingTime);
                }
                else if (Velocity.y < 0)
                {
                    Vector3 VelocityNormal = Velocity.normalized;
                    if (Vector3.Dot(VelocityNormal, Vector3.down) > 0.5f &&
                        Physics.Raycast(Grenade.transform.position, VelocityNormal, out RaycastHit Hit, 5, LayerMaskClass.HighPolyWithTerrainMask))
                    {
                        OnGrenadeDangerUpdated?.Invoke(Grenade, Hit.point, remainingTime);
                    }
                }
                yield return null;
            }
        }

        static GrenadeController()
        {
            _rigidBodyField = AccessTools.Field(typeof(Throwable), "Rigidbody");
            _explosionTimeField = AccessTools.Field(typeof(Throwable), "_explosionTime");
            _fuseTimeField = AccessTools.Field(typeof(Throwable), "_fuseTime");
            _destroyTimeField = AccessTools.Field(typeof(Throwable), "_destroyTime");
        }

        private static FieldInfo _rigidBodyField;
        private static FieldInfo _explosionTimeField;
        private static FieldInfo _fuseTimeField;
        private static FieldInfo _destroyTimeField;
    }
}