using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.Components;
using SAIN.Layers;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.Layers.Combat.Squad
{
    /// <summary>
    /// LootingOverwatch: When a squadmate is looting, provide overwatch by facing their direction
    /// and holding near cover. If any enemy becomes visible, the action ends so combat layers take over.
    /// </summary>
    internal class LootingOverwatchAction(BotOwner bot) : BotAction(bot, nameof(LootingOverwatchAction)), IBotAction
    {
        // SAIN-4.2: Named constants
        private const float OVERWATCH_COVER_CHECK_FREQ = 2f;
        private const float OVERWATCH_CLOSE_DIST = 2f;
        private const float OVERWATCH_IDEAL_DIST_MIN = 5f;
        private const float OVERWATCH_IDEAL_DIST_MAX = 20f;
        private const float OVERWATCH_BACKOFF_DIST = 5f;
        private const float OVERWATCH_BACKOFF_NAV_DIST = 3f;
        private const float LOOTING_MEMBER_SEARCH_RANGE = 30f;
        private const float COVER_MEMBER_MAX_DIST = 30f;
        private const float COVER_SCORE_MEMBER_WEIGHT = 0.5f;

        public override void Update(CustomLayer.ActionData data)
        {
            // If a visible enemy appears, stop overwatch and let combat layers handle it
            Enemy enemy = Bot.GoalEnemy;
            if (enemy != null && (enemy.IsVisible || enemy.InLineOfSight))
            {
                return;
            }

            // Find the looting squadmate to watch over
            BotComponent lootingMember = FindLootingSquadMember();
            if (lootingMember == null)
            {
                return;
            }

            Vector3 memberPos = lootingMember.Position;
            Vector3 myPos = Bot.Position;
            float distanceToMember = Vector3.Distance(myPos, memberPos);

            // If already in cover, hold position and face the looting member
            if (Bot.Cover?.CoverInUse != null)
            {
                Bot.Mover.SetTargetPose(1f);
                Bot.Mover.SetTargetMoveSpeed(1f);
                Bot.Steering.LookToPoint(memberPos);
                return;
            }

            // Look for nearby cover close to the looting member
            if (_nextCoverCheckTime < Time.time)
            {
                _nextCoverCheckTime = Time.time + OVERWATCH_COVER_CHECK_FREQ;
                _overwatchCoverPoint = FindNearbyOverwatchCover(memberPos);
            }

            if (_overwatchCoverPoint != null)
            {
                // Move to the chosen overwatch cover position
                float coverDistance = Vector3.Distance(myPos, _overwatchCoverPoint.Position);
                if (coverDistance > OVERWATCH_CLOSE_DIST)
                {
                    Bot.Mover.SetTargetPose(1f);
                    Bot.Mover.SetTargetMoveSpeed(1f);
                    Bot.Mover.WalkToPoint(_overwatchCoverPoint.Position);
                }
                else
                {
                    // Reached cover — hold and watch
                    Bot.Mover.SetTargetPose(1f);
                    Bot.Mover.SetTargetMoveSpeed(1f);
                    Bot.Steering.LookToPoint(memberPos);
                }
            }
            else
            {
                // No cover found: stay at a moderate distance and face the looting member
                if (distanceToMember > OVERWATCH_IDEAL_DIST_MIN && distanceToMember < OVERWATCH_IDEAL_DIST_MAX)
                {
                    Bot.Mover.SetTargetPose(0.5f);
                    Bot.Mover.SetTargetMoveSpeed(0.5f);
                    Bot.Steering.LookToPoint(memberPos);
                }
                else if (distanceToMember >= OVERWATCH_IDEAL_DIST_MAX)
                {
                    // Move closer to maintain effective overwatch distance
                    Bot.Mover.SetTargetPose(1f);
                    Bot.Mover.SetTargetMoveSpeed(1f);
                    Bot.Mover.WalkToPoint(memberPos);
                }
                else
                {
                    // Too close, back off slightly
                    Vector3 awayDir = (myPos - memberPos).normalized;
                    Vector3 backupPos = myPos + awayDir * OVERWATCH_BACKOFF_DIST;
                    if (NavMesh.SamplePosition(backupPos, out var hit, OVERWATCH_BACKOFF_NAV_DIST, NavMesh.AllAreas))
                    {
                        Bot.Mover.WalkToPoint(hit.position);
                    }
                    Bot.Mover.SetTargetPose(1f);
                    Bot.Mover.SetTargetMoveSpeed(1f);
                }
            }
        }

        public override void OnSteeringTicked()
        {
            Enemy enemy = Bot.GoalEnemy;
            if (!Shoot.ShootAnyVisibleEnemies(enemy))
            {
                Bot.Suppression.TrySuppressAnyEnemy(enemy, Bot.EnemyController.KnownEnemies);
            }
            if (!Bot.Steering.SteerByPriority(enemy))
            {
                // If no enemy to steer at, look towards moving direction (or overwatch target via Update)
                Bot.Steering.LookToMovingDirection();
            }
        }

        /// <summary>
        /// Finds a looting squadmate within overwatch range.
        /// </summary>
        private BotComponent FindLootingSquadMember()
        {
            if (Bot.Squad == null || Bot.Squad.Members == null)
                return null;

            foreach (var member in Bot.Squad.Members.Values)
            {
                if (member == Bot || member?.BotOwner == null || member.BotOwner.IsDead)
                    continue;

                float dist = Vector3.Distance(Bot.Position, member.Position);
                if (dist >= LOOTING_MEMBER_SEARCH_RANGE)
                    continue;

                // Check if this member is actively looting via LootingBots API
                if (LootingBots.LootingBotsInterop.IsBotLooting(member.BotOwner))
                {
                    return member;
                }
            }
            return null;
        }

        /// <summary>
        /// Finds a nearby cover point suitable for overwatch, close to the looting member's position.
        /// </summary>
        private CoverPoint FindNearbyOverwatchCover(Vector3 memberPos)
        {
            CoverPoint bestCover = null;
            float bestScore = float.MaxValue;

            var coverPoints = Bot.Cover?.CoverPoints;
            if (coverPoints == null)
                return null;

            foreach (var cover in coverPoints)
            {
                if (cover == null)
                    continue;

                float distToMember = Vector3.Distance(cover.Position, memberPos);
                // Cover too far from the looting member is not useful for overwatch
                if (distToMember > COVER_MEMBER_MAX_DIST)
                    continue;

                float distToSelf = Vector3.Distance(cover.Position, Bot.Position);
                // Prefer cover that is close to self and close to the member
                float score = distToSelf + distToMember * COVER_SCORE_MEMBER_WEIGHT;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestCover = cover;
                }
            }

            return bestCover;
        }

        private CoverPoint _overwatchCoverPoint;
        private float _nextCoverCheckTime;
    }
}
