using EFT;
using HarmonyLib;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SAIN.Components
{
    public class SAINNoBushESP : BotBase, IBotClass
    {
        static SAINNoBushESP()
        {
            Type botType = typeof(BotOwner);
            Type memoryType = AccessTools.Field(botType, "Memory").FieldType;
            GoalEnemyProp = AccessTools.Property(memoryType, "GoalEnemy");
            IsVisibleProp = AccessTools.Property(GoalEnemyProp.PropertyType, "IsVisible");
            Type shootDataType = AccessTools.Property(botType, "ShootData").PropertyType;
            CanShootByState = AccessTools.PropertySetter(shootDataType, "CanShootByState");
        }

        private static readonly PropertyInfo GoalEnemyProp;
        private static readonly PropertyInfo IsVisibleProp;
        private static readonly MethodInfo CanShootByState;
        private static LayerMask NoBushMask = 0;

        public SAINNoBushESP(BotComponent sain) : base(sain)
        {
            TickRequirement = ESAINTickState.OnlyBotInCombat;
            CanEverTick = true;
            TickInterval = Frequency > 0f ? Frequency : 0.1f;
        }

        public override void Init()
        {
            if (NoBushMask == 0)
            {
                NoBushMask = LayerMaskClass.HighPolyWithTerrainMaskAI 
                    | (1 << LayerMask.NameToLayer("PlayerSpiritAura"));
            }
            Bot?.AddBotTickClass(this);
            base.Init();
        }

        public override void ManualUpdate()
        {
            if (!UserToggle) return;

            if (NoBushTimer < Time.time)
            {
                NoBushTimer = Time.time + Frequency;
                bool active = CheckNoBushESP();
                ApplyNoBushESP(active);
            }
        }

        public bool NoBushESPActive { get; private set; } = false;
        private float NoBushTimer = 0f;
        private Vector3 HeadPosition => BotOwner.LookSensor._headPoint;

        private static NoBushESPSettings Settings => SAINPlugin.LoadedPreset?.GlobalSettings?.Look?.NoBushESP;
        private static bool UserToggle => Settings?.NoBushESPToggle ?? false;
        private static bool EnhancedChecks => Settings?.NoBushESPEnhanced ?? false;
        private static float EnhancedRatio => Settings?.NoBushESPEnhancedRatio ?? 0.5f;
        private static float Frequency => Settings?.NoBushESPFrequency ?? 0.1f;
        private static bool DebugMode => Settings?.NoBushESPDebugMode ?? false;

        public bool CheckNoBushESP()
        {
            Enemy sainEnemy = Bot?.GoalEnemy;
            var enemy = sainEnemy?.EnemyInfo ?? BotOwner?.Memory?.GoalEnemy;
            if (enemy != null && (enemy.IsVisible || enemy.CanShoot))
            {
                IPlayer person = enemy.Person;
                if (person != null && !person.IsAI)
                {
                    return EnhancedChecks ? CheckEnhanced(person) : CheckSimple(person);
                }
            }
            return false;
        }

        private bool CheckSimple(IPlayer player)
        {
            Vector3 partPos = player.MainParts[BodyPartType.body].Position;
            return RayCast(partPos, HeadPosition);
        }

        private bool CheckEnhanced(IPlayer player)
        {
            int hitCount = 0;
            int partCount = player.MainParts.Count;
            Vector3 start = HeadPosition;
            foreach (var part in player.MainParts)
            {
                if (RayCast(part.Value.Position, start)) hitCount++;
            }
            float ratio = (float)hitCount / partCount;
            return ratio >= EnhancedRatio;
        }

        private static bool RayCast(Vector3 end, Vector3 start)
        {
            Vector3 direction = end - start;
            if (Physics.Raycast(start, direction.normalized, out var hit, direction.magnitude, NoBushMask))
            {
                GameObject hitObject = hit.transform?.parent?.gameObject;
                if (hitObject != null)
                {
                    string hitName = hitObject.name?.ToLower();
                    foreach (string exclusion in ExclusionList)
                    {
                        if (hitName.Contains(exclusion)) return true;
                    }
                }
            }
            return false;
        }

        public void ApplyNoBushESP(bool blockShoot)
        {
            NoBushESPActive = blockShoot;
            if (!blockShoot) return;

            var enemy = BotOwner?.Memory?.GoalEnemy;
            if (enemy != null)
            {
                enemy.SetCanShoot(false);
                enemy.SetVisible(false);

                if (BotOwner.AimingManager.CurrentAiming is BotAimingClass aimData 
                    && aimData.aimStatus_0 != AimStatus.NoTarget)
                {
                    aimData.aimStatus_0 = AimStatus.NoTarget;
                }

                var vision = Bot?.EnemyController.GetEnemy(enemy.ProfileId, false)?.Vision;
                if (vision != null)
                {
                    vision.UpdateVisibleState(Time.time, true);
                }
            }
        }

        private static readonly List<string> ExclusionList = new() 
        { 
            "filbert", "fibert", "tree", "pine", "plant", "birch", "collider", 
            "timber", "spruce", "bush", "metal", "wood", "grass" 
        };

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
