using SAIN.Components;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Memory
{
    public class SAINInterestPointClass : BotBase, IBotClass
    {
        public SAINInterestPointClass(BotComponent bot) : base(bot)
        {
            bot.AddBotClass(this);
            TickRequirement = ESAINTickState.OnlyNoSleep;
            CanEverTick = false;
        }

        public override void Init() { }

        public override void ManualUpdate() { }

        public override void Dispose()
        {
            _interestPoints.Clear();
        }

        public enum InterestType
        {
            CombatSite,     // Recent combat location
            SquadDeath,     // Squadmate killed here
            Gunfire,        // Heavy gunfire heard
            LootArea,       // Potential loot location
        }

        public class InterestPoint
        {
            public Vector3 Position;
            public InterestType Type;
            public float RecordedTime;
            public float Priority => Type switch
            {
                InterestType.SquadDeath => 100f,
                InterestType.CombatSite => 70f,
                InterestType.Gunfire => 50f,
                InterestType.LootArea => 30f,
                _ => 10f,
            };
        }

        private readonly List<InterestPoint> _interestPoints = new();
        private const int MAX_POINTS = 10;
        private const float EXPIRE_TIME = 120f; // 2 minutes

        public void RecordPoint(Vector3 position, InterestType type)
        {
            // Deduplicate nearby points
            foreach (var existing in _interestPoints)
            {
                if (Vector3.Distance(existing.Position, position) < 10f)
                    return;
            }

            _interestPoints.Add(new InterestPoint
            {
                Position = position,
                Type = type,
                RecordedTime = Time.time,
            });

            // Keep only highest priority points
            if (_interestPoints.Count > MAX_POINTS)
            {
                _interestPoints.Sort((a, b) => b.Priority.CompareTo(a.Priority));
                _interestPoints.RemoveRange(MAX_POINTS, _interestPoints.Count - MAX_POINTS);
            }
        }

        public InterestPoint GetNearestInterestPoint()
        {
            CleanExpired();

            InterestPoint nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var point in _interestPoints)
            {
                float dist = Vector3.Distance(Bot.Position, point.Position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = point;
                }
            }

            return nearest;
        }

        private void CleanExpired()
        {
            float now = Time.time;
            _interestPoints.RemoveAll(p => now - p.RecordedTime > EXPIRE_TIME);
        }
    }
}
