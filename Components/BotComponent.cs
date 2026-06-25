using EFT;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Helpers;
using SAIN.Layers;
using SAIN.Models.Enums;
using SAIN.Preset.GlobalSettings;
using SAIN.Preset.GlobalSettings.Categories;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.Classes.Debug;
using SAIN.SAINComponent.Classes.Decision;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.SAINComponent.Classes.Info;
using SAIN.SAINComponent.Classes.Memory;
using SAIN.SAINComponent.Classes.Mover;
using SAIN.SAINComponent.Classes.Search;
using SAIN.SAINComponent.Classes.Talk;
using SAIN.SAINComponent.Classes.WeaponFunction;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SAIN.Components
{
    //public enum EBotActiveState
    //{
    //    Active,
    //    Combat,
    //    Sleep,
    //    Disposed,
    //}

    public class BotComponent : BotComponentBase, ISPlayer
    {
        public Vector3 NavMeshPosition => Transform.NavData.Position;

        public float GetDistanceToPlayer(string ProfileId)
        {
            return PlayerComponent.GetDistanceToPlayer(ProfileId);
        }

        public bool IsPlayerInRange(string ProfileId, float maxDistance, out float playerDistance)
        {
            return PlayerComponent.IsPlayerInRange(ProfileId, maxDistance, out playerDistance);
        }

        public void ActivateIfBotActive(BotOwner botOwner)
        {
            if (botOwner.BotState == EBotState.Active)
            {
                Activate(botOwner);
            }
        }

        public void Activate(BotOwner botOwner)
        {
            if (_Activated)
            {
                return;
            }
            var playerComponent = botOwner.GetComponent<PlayerComponent>();
            if (playerComponent == null)
            {
#if DEBUG
                Logger.LogError("Person Null");
#endif
                return;
            }
            if (botOwner.BotState == EBotState.Active)
            {
                if (InitializeBot(playerComponent, botOwner))
                {
                    _Activated = true;
                    OnBotActivated?.Invoke(this);
                    return;
                }
                Dispose();
            }
        }

        public IBotAction CurrentAction => BotActivation.CurrentAction;

        public bool IsInCombat => BotActivation.BotInCombat;

        private bool _Activated = false;

        public event Action<BotComponent> OnBotActivated;

        public bool IsCheater { get; private set; }

        public bool BotActive => BotActivation.BotActive;
        public bool BotInStandBy => BotActivation.BotInStandBy;
        public AILimitSetting CurrentAILimit => AILimit.CurrentAILimit;

        public bool HasEnemy => Enemy.IsEnemyActive(EnemyController.GoalEnemy);
        public Enemy GoalEnemy => HasEnemy ? EnemyController.GoalEnemy : null;

        public BotGlobalEventsClass GlobalEvents { get; private set; }
        public BotBusyHandsDetector BusyHandsDetector { get; private set; }
        public SAINShootData Shoot { get; private set; }
        public BotWeightManagement WeightManagement { get; private set; }
        public SAINBotMedicalClass Medical { get; private set; }
        public SAINActivationClass BotActivation { get; private set; }
        public DoorOpener DoorOpener { get; private set; }
        public ManualShootClass ManualShoot { get; private set; }
        public CurrentTargetClass CurrentTarget { get; private set; }
        public BotBackpackDropClass BackpackDropper { get; private set; }
        public BotLightController BotLight { get; private set; }
        public SAINBotSpaceAwareness SpaceAwareness { get; private set; }
        public AimDownSightsController AimDownSightsController { get; private set; }
        public SAINAILimit AILimit { get; private set; }
        public SAINBotSuppressClass Suppression { get; private set; }
        public SAINVaultClass Vault { get; private set; }
        public SAINSearchClass Search { get; private set; }
        public SAINMemoryClass Memory { get; private set; }
        public SAINEnemyController EnemyController { get; private set; }
        public SAINNoBushESP NoBushESP { get; private set; }
        public SAINFriendlyFireClass FriendlyFire { get; private set; }
        public SAINVisionClass Vision { get; private set; }
        public SAINMoverClass Mover { get; private set; }
        public SAINBotUnstuckClass BotStuck { get; private set; }
        public SAINHearingSensorClass Hearing { get; private set; }
        public SAINBotTalkClass Talk { get; private set; }
        public SAINDecisionClass Decision { get; private set; }
        public SAINCoverClass Cover { get; private set; }
        public SAINBotInfoClass Info { get; private set; }
        public BotSquadContainer Squad { get; private set; }
        public SAINSelfActionClass SelfActions { get; private set; }
        public BotGrenadeManager Grenade { get; private set; }
        public SAINSteeringClass Steering { get; private set; }
        public SAINInterestPointClass SAINInterestPointClass { get; private set; }
        public AimClass Aim { get; private set; }

        /// <summary>
        /// Shared LootingBots integration instance for this bot. Initialized after all core classes.
        /// </summary>
        public SAINLootingBotsIntegration LootingBotsIntegration { get; private set; }

        public ESquadRole SquadRole { get; set; } = ESquadRole.Default;

        public bool IsDead => Player?.HealthController?.IsAlive != true;
        public bool GameEnding => BotActivation.GameEnding;
        public bool SAINLayersActive => BotActivation.SAINLayersActive;

        public float DistanceToAimTarget {
            get
            {
                //if (BotOwner.AimingManager.CurrentAiming != null)
                //{
                //    return BotOwner.AimingManager.CurrentAiming.LastDist2Target;
                //}
                if (EnemyController.GoalEnemy != null)
                {
                    return EnemyController.GoalEnemy.KnownPlaces.BotDistanceFromLastKnown;
                }
                return float.MaxValue;
            }
        }

        public float LastCheckVisibleTime;

        public ESAINLayer ActiveLayer {
            get
            {
                return BotActivation.ActiveLayer;
            }
            set
            {
                BotActivation.SetActiveLayer(value);
            }
        }

        public void ManualUpdate(float currentTime, float deltaTime)
        {
            BotOwner botOwner = BotOwner;
            if (botOwner != null)
            {
                Player player = botOwner.GetPlayer;
                if (player != null)
                {
                    TickClassGroup(AlwaysTickClasses, currentTime);

                    bool active = BotActive;
                    if (active)
                    {
                        TickClassGroup(TickWhenActiveClasses, currentTime);
                        CheckIdlePatrol();
                    }

                    bool inStandBy = !active || BotInStandBy;
                    if (!inStandBy)
                    {
                        TickClassGroup(TickWhenNoSleepClasses, currentTime);
                        handleDumbShit();
                    }

                    bool inCombat = active && !inStandBy && SAINLayersActive && GoalEnemy != null;
                    BotActivation.SetInCombat(inCombat);
                    if (inCombat)
                    {
                        TickClassGroup(TickWhenCombatClasses, currentTime);
                    }
                }
            }
        }

        private static void TickClassGroup(List<IBotClass> List, float CurrentTime)
        {
            for (int i = 0; i < List.Count; i++)
            {
                 List[i]?.ManualUpdate();
            }
        }

        public bool InitializeBot(PlayerComponent playerComponent, BotOwner botOwner)
        {
            base.Init(playerComponent, botOwner);
            if (!CreateClasses())
            {
                return false;
            }
            if (!AddToSquad())
            {
                return false;
            }
            if (!InitClasses())
            {
                return false;
            }

            // Initialize shared LootingBots integration singleton after other classes are created
            LootingBotsIntegration = new SAINLootingBotsIntegration(BotOwner, this);

            if (!FinishInit(playerComponent))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Tiered class creation: critical classes must succeed or the bot is discarded.
        /// Non-critical classes are wrapped in try-catch — if they fail, the bot continues
        /// with degraded functionality. This prevents non-essential features (talk, light,
        /// vault, etc.) from taking down the entire AI.
        /// </summary>
        private bool CreateClasses()
        {
            // Must be first, other classes use it
            if (!CreateCritical(() => Info = new SAINBotInfoClass(this), nameof(Info)))
                return false;

            // Non-critical: wrapped individually to allow graceful degradation
            CreateNonCritical(() => NoBushESP = new SAINNoBushESP(this), nameof(NoBushESP));
            CreateNonCritical(() => Squad = new BotSquadContainer(this), nameof(Squad));
            CreateNonCritical(() => BusyHandsDetector = new BotBusyHandsDetector(this), nameof(BusyHandsDetector));
            CreateNonCritical(() => GlobalEvents = new BotGlobalEventsClass(this), nameof(GlobalEvents));
            CreateNonCritical(() => Shoot = new SAINShootData(this), nameof(Shoot));
            CreateNonCritical(() => WeightManagement = new BotWeightManagement(this), nameof(WeightManagement));

            if (!CreateCritical(() => Memory = new SAINMemoryClass(this), nameof(Memory)))
                return false;

            CreateNonCritical(() => BotStuck = new SAINBotUnstuckClass(this), nameof(BotStuck));

            if (!CreateCritical(() => Hearing = new SAINHearingSensorClass(this), nameof(Hearing)))
                return false;

            CreateNonCritical(() => Talk = new SAINBotTalkClass(this), nameof(Talk));

            if (!CreateCritical(() => Decision = new SAINDecisionClass(this), nameof(Decision)))
                return false;

            if (!CreateCritical(() => Cover = new SAINCoverClass(this), nameof(Cover)))
                return false;

            CreateNonCritical(() => SelfActions = new SAINSelfActionClass(this), nameof(SelfActions));
            CreateNonCritical(() => Steering = new SAINSteeringClass(this), nameof(Steering));
            CreateNonCritical(() => Grenade = new BotGrenadeManager(this), nameof(Grenade));

            if (!CreateCritical(() => Mover = new SAINMoverClass(this), nameof(Mover)))
                return false;

            if (!CreateCritical(() => EnemyController = new SAINEnemyController(this), nameof(EnemyController)))
                return false;

            CreateNonCritical(() => FriendlyFire = new SAINFriendlyFireClass(this), nameof(FriendlyFire));

            if (!CreateCritical(() => Vision = new SAINVisionClass(this), nameof(Vision)))
                return false;

            CreateNonCritical(() => Search = new SAINSearchClass(this), nameof(Search));
            CreateNonCritical(() => Vault = new SAINVaultClass(this), nameof(Vault));
            CreateNonCritical(() => Suppression = new SAINBotSuppressClass(this), nameof(Suppression));
            CreateNonCritical(() => AILimit = new SAINAILimit(this), nameof(AILimit));
            CreateNonCritical(() => AimDownSightsController = new AimDownSightsController(this), nameof(AimDownSightsController));
            CreateNonCritical(() => SpaceAwareness = new SAINBotSpaceAwareness(this), nameof(SpaceAwareness));
            CreateNonCritical(() => DoorOpener = new DoorOpener(this), nameof(DoorOpener));
            CreateNonCritical(() => Medical = new SAINBotMedicalClass(this), nameof(Medical));
            CreateNonCritical(() => BotLight = new BotLightController(this), nameof(BotLight));
            CreateNonCritical(() => BackpackDropper = new BotBackpackDropClass(this), nameof(BackpackDropper));
            CreateNonCritical(() => CurrentTarget = new CurrentTargetClass(this), nameof(CurrentTarget));
            CreateNonCritical(() => ManualShoot = new ManualShootClass(this), nameof(ManualShoot));

            if (!CreateCritical(() => BotActivation = new SAINActivationClass(this), nameof(BotActivation)))
                return false;

            CreateNonCritical(() => Aim = new AimClass(this), nameof(Aim));
            CreateNonCritical(() => SAINInterestPointClass = new SAINInterestPointClass(this), nameof(SAINInterestPointClass));

            return true;
        }

        /// <summary>Create a critical class; returns false on failure (triggers bot Dispose).</summary>
        private bool CreateCritical(Action createAction, string className)
        {
            try
            {
                createAction();
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"[CRITICAL] Failed to create class [{className}], disposing bot: {ex}");
                return false;
            }
        }

        /// <summary>Create a non-critical class with individual try-catch. On failure, logs warning and continues.</summary>
        private void CreateNonCritical(Action createAction, string className)
        {
            try
            {
                createAction();
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"[NON-CRITICAL] Failed to create class [{className}], continuing with degraded functionality: {ex}");
            }
        }

        public void AddBotClass(IBotClass Class)
        {
            if (Class == null)
            {
                Logger.LogError($"Bot Class of is null, cannot add it to list!");
                return;
            }
            BotClasses.Add(Class);
        }

        public void AddBotTickClass(IBotClass Class)
        {
            if (Class.CanEverTick)
            {
                switch (Class.TickRequirement)
                {
                    case ESAINTickState.AlwaysUpdate:
                        AlwaysTickClasses.Add(Class);
                        break;

                    case ESAINTickState.OnlyBotActive:
                        TickWhenActiveClasses.Add(Class);
                        break;

                    case ESAINTickState.OnlyNoSleep:
                        TickWhenNoSleepClasses.Add(Class);
                        break;

                    case ESAINTickState.OnlyBotInCombat:
                        TickWhenCombatClasses.Add(Class);
                        break;

                    default:
                        break;
                }
            }
        }

        private bool AddToSquad()
        {
            try
            {
                Squad.SquadInfo.AddMember(this);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error adding member to squad!: {ex}");
                return false;
            }
            return true;
        }

        private bool InitClasses()
        {
            foreach (var botClass in BotClasses)
            {
                if (botClass == null) continue; // Non-critical class failed to init in CreateClasses()
                try
                {
                    botClass.Init();
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error When Initializing Class [{botClass}], Disposing... : {ex}");
                    return false;
                }
            }
            return true;
        }

        private bool FinishInit(PlayerComponent playerComponent)
        {
            try
            {
                if (!VerifyBrain(playerComponent))
                {
                    Logger.LogError("Init SAIN ERROR, Disposing...");
                    return false;
                }

                try
                {
                    BotOwner.LookSensor.MaxShootDist = float.MaxValue;
                    if (BotOwner.AIData is GClass567 aiData)
                    {
                        aiData.IsNoOffsetShooting = false;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error setting MaxShootDist during init, but continuing with initialization...: {ex}");
                }

                try
                {
                    var settings = GlobalSettingsClass.Instance.General.Jokes;
                    if (settings.RandomCheaters &&
                        (EFTMath.RandomBool(settings.RandomCheaterChance) || Player.Profile.Nickname.ToLower().Contains("solarint")))
                    {
                        IsCheater = true;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Error when initializing dumb shit for this bot, continuing anyways since its some dumb shit. Error: {ex}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error When Finishing Bot Initialization, Disposing... : {ex}");
                return false;
            }
            return true;
        }

        private bool VerifyBrain(PlayerComponent playerComp)
        {
            string assignedBrainName = playerComp.BotOwner?.Brain?.BaseBrain?.ShortName();

            if (Info.Profile.IsPMC)
            {
                IEnumerable<string> allowedBrainNames = AIBrains.GetAllowedPMCBrains().Select(brain => brain.ToString());
                return IsAssignedBrainAllowed(assignedBrainName, allowedBrainNames, "PMC") ? true : false;
            }

            if (Info.Profile.IsPlayerScav)
            {
                IEnumerable<string> allowedBrainNames = AIBrains.GetAllowedPlayerScavBrains().Select(brain => brain.ToString());
                return IsAssignedBrainAllowed(assignedBrainName, allowedBrainNames, "PlayerScav") ? true : false;
            }

            if (Info.Profile.IsScav)
            {
                IEnumerable<string> allowedBrainNames = AIBrains.GetAllowedScavBrains().Select(brain => brain.ToString());
                return IsAssignedBrainAllowed(assignedBrainName, allowedBrainNames, "Scav") ? true : false;
            }

            return true;
        }

        private bool IsAssignedBrainAllowed(string assignedBrainName, IEnumerable<string> allowedBrainNames, string botCategory)
        {
            if (allowedBrainNames.Contains(assignedBrainName))
            {
                return true;
            }

            Logger.LogAndNotifyError($"{BotOwner.name} is a ${botCategory} but does not have any of these BaseBrains: ${string.Join(", ", allowedBrainNames)}! Current Brain Assignment: [{assignedBrainName}] : SAIN Server mod is either missing or another mod is overwriting it. Destroying SAIN for this bot...");

            return false;
        }

        private void OnDisable()
        {
            BotActivation.SetActive(false);
            StopAllCoroutines();
        }

        private void OnEnable()
        {

        }

        public void LateUpdate()
        {
            //BotActivation?.LateUpdate();
            //EnemyController?.LateUpdate();
        }

        private void handleDumbShit()
        {
            if (IsCheater)
            {
                if (defaultMoveSpeed == 0)
                {
                    defaultMoveSpeed = Player.MovementContext.MaxSpeed;
                    defaultSprintSpeed = Player.MovementContext.SprintSpeed;
                }
                Player.Grounder.enabled = GoalEnemy == null;
                if (GoalEnemy != null)
                {
                    Player.MovementContext.SetCharacterMovementSpeed(350, true);
                    Player.MovementContext.SprintSpeed = 50f;
                    Player.ChangeSpeed(100f);
                    Player.UpdateSpeedLimit(100f, Player.ESpeedLimit.SurfaceNormal);
                    Player.MovementContext.ChangeSpeedLimit(100f, Player.ESpeedLimit.SurfaceNormal);
                    BotOwner.SetTargetMoveSpeed(100f);
                }
                else
                {
                    Player.MovementContext.SetCharacterMovementSpeed(defaultMoveSpeed, false);
                    Player.MovementContext.SprintSpeed = defaultSprintSpeed;
                }
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            BotActivation?.SetActive(false);
            StopAllCoroutines();

            foreach (var botClass in BotClasses)
            {
                if (botClass == null) continue;
                try
                {
                    botClass.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Dispose Class [{botClass}] Error: {ex}");
                }
            }

            // NoBushESP is now a plain class (IBotClass), disposed via BotClasses.Dispose() loop
            if (BotOwner != null)
                BotOwner.OnBotStateChange -= resetBot;

            Destroy(this);
        }

        private void resetBot(EBotState state)
        {
            Decision.ResetDecisions(false);
        }

        /// <summary>
        /// All Bot Component classes.
        /// </summary>
        private readonly List<IBotClass> BotClasses = [];

        /// <summary>
        /// Bot classes that should tick no matter what.
        /// </summary>
        private readonly List<IBotClass> AlwaysTickClasses = [];

        /// <summary>
        /// Bot classes that should tick when a bot is active.
        /// </summary>
        private readonly List<IBotClass> TickWhenActiveClasses = [];

        /// <summary>
        /// Bot classes that should tick when a bot not sleeping.
        /// </summary>
        private readonly List<IBotClass> TickWhenNoSleepClasses = [];

        /// <summary>
        /// Bot classes that should tick when a bot is in combat.
        /// </summary>
        private readonly List<IBotClass> TickWhenCombatClasses = [];

        private float defaultMoveSpeed;
        private float defaultSprintSpeed;

        // Idle patrol: force bots that have been stationary too long to move
        private Vector3 _lastIdleCheckPos;
        private float _lastIdleCheckTime;
        private float _idleSeconds;
        private const float IDLE_PATROL_THRESHOLD = 30f;
        private const float IDLE_MOVE_DIST = 30f;

        private void CheckIdlePatrol()
        {
            if (BotInStandBy) return;
            if (IsInCombat)
            {
                _idleSeconds = 0f;
                return;
            }

            float now = Time.time;
            if (_lastIdleCheckTime <= 0f)
            {
                _lastIdleCheckTime = now;
                _lastIdleCheckPos = Position;
                return;
            }

            if (now - _lastIdleCheckTime < 10f) return;
            _lastIdleCheckTime = now;

            float moved = Vector3.Distance(Position, _lastIdleCheckPos);
            if (moved < 2f)
            {
                _idleSeconds += 10f;
                if (_idleSeconds >= IDLE_PATROL_THRESHOLD)
                {
                    Vector3 randomDir = Random.onUnitSphere * IDLE_MOVE_DIST;
                    randomDir.y = 0;
                    Vector3 target = Position + randomDir;
                    if (UnityEngine.AI.NavMesh.SamplePosition(target, out var hit, IDLE_MOVE_DIST, UnityEngine.AI.NavMesh.AllAreas))
                    {
                        Mover?.WalkToPoint(hit.position);
                        _idleSeconds = 0f;
                    }
                }
            }
            else
            {
                _idleSeconds = 0f;
            }
            _lastIdleCheckPos = Position;
        }
    }
}