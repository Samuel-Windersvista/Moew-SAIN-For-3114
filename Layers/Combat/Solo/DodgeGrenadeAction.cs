using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.Models.Enums;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.Classes.Mover;
using SAIN.SAINComponent.SubComponents;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace SAIN.Layers.Combat.Solo
{
    /// <summary>
    /// SAIN 智能手雷躲避行为。
    /// 根据剩余时间、距离、手雷类型和个性参数执行不同的躲避策略。
    /// </summary>
    internal class DodgeGrenadeAction(BotOwner bot) : BotAction(bot, nameof(DodgeGrenadeAction)), IBotAction
    {
        private GrenadeThreatData _threatData;
        private Vector3 _safePoint;
        private EDodgePhase _phase = EDodgePhase.Init;
        private float _actionStartTime;
        private bool _shouldProne;
        private float _safeDistance;

        private enum EDodgePhase
        {
            Init,
            Pathfinding,
            Moving,
            Proning,
            Holding,
            Done
        }

        public override void Start()
        {
            _actionStartTime = Time.time;
            _threatData = Bot.Decision.DecisionManager.ActiveGrenadeThreat;

            if (_threatData == null)
            {
                return;
            }

            // 个性：硬扛概率
            float ignoreChance = Bot.Info.PersonalitySettings.General.GRENADE_IGNORE_CHANCE;
            if (Random.value < ignoreChance)
            {
                Bot.Decision.DecisionManager.ClearGrenadeThreat();
                return;
            }

            // 计算有效距离（垂直楼层感知）
            float effectiveDistance = CalculateEffectiveDistance(_threatData);
            _threatData.DistanceToBot = effectiveDistance;

            // 应用个性安全距离倍率
            var settings = GlobalSettingsClass.Instance.Grenade;
            _safeDistance = settings.FRAG_SAFE_DISTANCE * Bot.Info.PersonalitySettings.General.GRENADE_SAFE_DIST_MODIFIER;

            // 确定行为
            DetermineBehavior();
        }

        public override void Update(CustomLayer.ActionData data)
        {
            if (_threatData == null || !_threatData.IsValid)
            {
                _phase = EDodgePhase.Done;
                return;
            }

            switch (_phase)
            {
                case EDodgePhase.Pathfinding:
                    FindAndNavigateToSafePoint();
                    break;
                case EDodgePhase.Moving:
                    CheckArrival();
                    break;
                case EDodgePhase.Proning:
                    ExecuteProne();
                    break;
                case EDodgePhase.Holding:
                    if (_threatData.RemainingTime <= 0f || Time.time - _actionStartTime > 3f)
                        _phase = EDodgePhase.Done;
                    break;
            }
        }

        public override void Stop()
        {
            Bot.Mover.Prone.SetProne(false);
        }

        // ==================== 行为决策 ====================

        private void DetermineBehavior()
        {
            float dist = _threatData.DistanceToBot;
            float timeLeft = _threatData.RemainingTime;

            // 碰炸手雷 / 时间未知 → 紧急行为
            if (_threatData.IsImpact || timeLeft <= 0f)
            {
                if (dist < 3f) { _shouldProne = true; _phase = EDodgePhase.Proning; }
                else { SprintAway(); }
                return;
            }

            // 烟雾弹
            if (_threatData.IsSmoke)
            {
                if (dist < 8f) MoveAway(8f);
                else _phase = EDodgePhase.Done;
                return;
            }

            // 闪光弹
            if (_threatData.IsFlash)
            {
                TurnAway();
                if (dist < 5f) MoveAway(5f);
                else _phase = EDodgePhase.Done;
                return;
            }

            // 破片雷 — 时间-距离分层
            if (timeLeft > 2f)
            {
                if (dist < 10f) { _safeDistance = Mathf.Max(_safeDistance, 15f); _phase = EDodgePhase.Pathfinding; }
                else if (dist < 15f) MoveAway(15f);
                else _phase = EDodgePhase.Done;
            }
            else if (timeLeft > 1f)
            {
                if (dist < 5f) { _shouldProne = true; _phase = EDodgePhase.Pathfinding; }
                else { _safeDistance = 15f; _phase = EDodgePhase.Pathfinding; }
            }
            else
            {
                if (dist < 3f) { _shouldProne = true; _phase = EDodgePhase.Proning; }
                else SprintAway();
            }
        }

        // ==================== 寻路 ====================

        private void FindAndNavigateToSafePoint()
        {
            Vector3 awayDir = (Bot.Position - _threatData.DangerPoint).normalized;
            var settings = GlobalSettingsClass.Instance.Grenade;
            Vector3 bestPoint = Vector3.zero;
            bool foundCover = false;
            float bestScore = float.MinValue;

            // 扇形采样: 9 方向 x 3 距离
            for (int angleStep = -4; angleStep <= 4; angleStep++)
            {
                float angle = angleStep * 15f;
                Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * awayDir;

                for (int distLevel = 0; distLevel < 3; distLevel++)
                {
                    float radius = _safeDistance * (1f - distLevel * 0.25f);
                    Vector3 target = Bot.Position + dir * radius;

                    if (!Bot.Mover.CanGoToPoint(target, out NavMeshPath path))
                        continue;

                    float score = EvaluatePoint(target, ref foundCover);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestPoint = target;
                    }
                }
            }

            if (bestPoint != Vector3.zero)
            {
                _safePoint = bestPoint;
                Bot.Mover.RunToPoint(_safePoint);
                _phase = EDodgePhase.Moving;
            }
            else
            {
                // 寻路失败 → 降级扑倒
                _shouldProne = true;
                _phase = EDodgePhase.Proning;
            }
        }

        private float EvaluatePoint(Vector3 point, ref bool foundCover)
        {
            var settings = GlobalSettingsClass.Instance.Grenade;
            float score = 1f;

            // 掩体检测
            Vector3 dirToPoint = (point - _threatData.DangerPoint).normalized;
            float dist = Vector3.Distance(_threatData.DangerPoint, point);
            if (Physics.Raycast(_threatData.DangerPoint, dirToPoint, out RaycastHit hit, dist - 0.3f,
                LayerMaskClass.HighPolyWithTerrainMask))
            {
                Bounds b = hit.collider.bounds;
                float minSize = settings.MIN_COVER_SIZE;
                float minHeight = settings.MIN_COVER_HEIGHT;
                if ((b.size.x > minSize || b.size.z > minSize) && b.size.y > minHeight)
                {
                    score += 5f;
                    foundCover = true;
                }
            }

            // 距离加分
            score += Vector3.Distance(Bot.Position, point) / _safeDistance;
            return score;
        }

        // ==================== 移动执行 ====================

        private void SprintAway()
        {
            Vector3 awayPoint = Bot.Position + (Bot.Position - _threatData.DangerPoint).normalized * 15f;
            if (Bot.Mover.CanGoToPoint(awayPoint, out _))
            {
                Bot.Mover.RunToPoint(awayPoint);
                _phase = EDodgePhase.Moving;
            }
            else
            {
                _shouldProne = true;
                _phase = EDodgePhase.Proning;
            }
        }

        private void MoveAway(float distance)
        {
            Vector3 awayPoint = Bot.Position + (Bot.Position - _threatData.DangerPoint).normalized * distance;
            if (Bot.Mover.CanGoToPoint(awayPoint, out _))
            {
                Bot.Mover.WalkToPoint(awayPoint);
                _phase = EDodgePhase.Moving;
            }
            else
            {
                _phase = EDodgePhase.Done;
            }
        }

        private void TurnAway()
        {
            Vector3 lookDir = (Bot.Position - _threatData.DangerPoint).normalized;
            BotOwner.Steering.LookToPoint(Bot.Position + lookDir * 10f);
        }

        private void CheckArrival()
        {
            if (!Bot.Mover.Moving || Bot.Mover.ActivePath?.Status == EBotMoveStatus.Complete)
            {
                if (_shouldProne)
                    _phase = EDodgePhase.Proning;
                else
                    _phase = EDodgePhase.Holding;
            }
        }

        private void ExecuteProne()
        {
            Bot.Mover.Prone.SetProne(true);
            TurnAway();
            _phase = EDodgePhase.Holding;
        }

        // ==================== 辅助 ====================

        private float CalculateEffectiveDistance(GrenadeThreatData data)
        {
            Vector3 gp = data.DangerPoint;
            Vector3 bp = Bot.Position;
            float verticalDist = Mathf.Abs(gp.y - bp.y);
            float horizontalDist = new Vector2(gp.x - bp.x, gp.z - bp.z).magnitude;

            if (GlobalSettingsClass.Instance.Grenade.VERTICAL_FLOOR_FILTER
                && verticalDist > 2f && horizontalDist < 5f)
            {
                if (Bot.Mover.CanGoToPoint(gp, out NavMeshPath path))
                {
                    float pathLen = 0f;
                    for (int i = 1; i < path.corners.Length; i++)
                        pathLen += Vector3.Distance(path.corners[i - 1], path.corners[i]);
                    return pathLen;
                }
                return float.MaxValue;
            }
            return Vector3.Distance(bp, gp);
        }
    }
}
