using EFT;
using HarmonyLib;
using SAIN.Components;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System.Reflection;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Memory
{
    public class SAINMemoryClass : BotComponentClassBase
    {
        public IPlayer LastUnderFireSource { get; private set; }
        public Enemy LastUnderFireEnemy { get; private set; }
        public Vector3 UnderFireFromPosition { get; set; }
        public int ConsecutiveUnderFireCount { get; set; }
        public float LastUnderFireTime { get; set; }

        public SAINExtract Extract { get; } = new SAINExtract();
        public HealthTracker Health { get; private set; }
        public LocationTracker Location { get; private set; }

        public bool RecentTeammateDeath { get; set; }
        public float LastTeammateDeathTime { get; set; }
        public Vector3 LastTeammateDeathPosition { get; set; }
        public float LastKillTime { get; set; }

        public SAINMemoryClass(BotComponent sain) : base(sain)
        {
            TickRequirement = ESAINTickState.OnlyNoSleep;
            Health = new HealthTracker(sain);
            Location = new LocationTracker(sain);
        }

        public override void Init()
        {
            Bot.EnemyController.Events.OnEnemyRemoved += clearEnemy;
            base.Init();
        }

        public override void ManualUpdate()
        {
            Health.ManualUpdate();
            Location.ManualUpdate();
            checkResetUnderFire();
            base.ManualUpdate();
        }

        public override void Dispose()
        {
            Bot.EnemyController.Events.OnEnemyRemoved -= clearEnemy;
            base.Dispose();
        }

        public void SetUnderFire(Enemy enemy, Vector3 position)
        {
            try
            {
                BotOwner.Memory.SetUnderFire(enemy.EnemyPlayer);
            }
            catch { }

            LastUnderFireSource = enemy.EnemyPlayer;
            UnderFireFromPosition = position;
            LastUnderFireEnemy = enemy;
            OnUnderFire(position);
        }

        public void OnUnderFire(Vector3 fromPosition)
        {
            float timeSinceLast = Time.time - LastUnderFireTime;
            if (timeSinceLast < 3f)
                ConsecutiveUnderFireCount++;
            else
                ConsecutiveUnderFireCount = 1;

            LastUnderFireTime = Time.time;
            UnderFireFromPosition = fromPosition;
        }

        private void clearEnemy(string profileId, Enemy enemy)
        {
            if (LastUnderFireEnemy == enemy)
            {
                LastUnderFireEnemy = null;
                resetUnderFire();
            }
        }

        private void checkResetUnderFire()
        {
            if (_nextCheckDeadTime < Time.time)
            {
                _nextCheckDeadTime = Time.time + 0.5f;
                resetUnderFire();
            }
        }

        private void resetUnderFire()
        {
            if (BotOwner.Memory.IsUnderFire &&
                (LastUnderFireSource == null || LastUnderFireSource.HealthController.IsAlive == false))
            {
                //Reset the UnderFireTime
                BotOwner.Memory.float_4 = Time.time;
            }
        }

        private float _nextCheckDeadTime;
    }
}