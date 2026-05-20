using PugMod;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace AutoTargetMod
{
    /// <summary>
    /// Manages the manual target lock state and validates it each frame.
    /// Subscribes to despawn and world-destroyed events to clear stale locks.
    /// </summary>
    public static class TargetManager
    {
        private static AutoTargetConfig _config;

        public static void Initialize(AutoTargetConfig config)
        {
            _config = config;
            API.Client.OnObjectDespawnedOnClient += OnObjectDespawned;
            API.Client.OnWorldDestroyed += OnWorldDestroyed;
        }

        public static void Shutdown()
        {
            API.Client.OnObjectDespawnedOnClient -= OnObjectDespawned;
            API.Client.OnWorldDestroyed -= OnWorldDestroyed;
        }

        /// <summary>
        /// Locks the given target. If it is already locked, the lock is cleared (toggle).
        /// </summary>
        public static void SetLockedTarget(Entity target)
        {
            if (target == Entity.Null)
            {
                ClearLockedTarget();
                return;
            }

            if (AutoTargetState.lockedTarget == target)
            {
                ClearLockedTarget();
                return;
            }

            if (AutoTargetState.lockedTarget != Entity.Null)
                TargetHighlighter.ClearHighlight(AutoTargetState.lockedTarget);

            AutoTargetState.lockedTarget = target;
            TargetHighlighter.ApplyHighlight(target, isLocked: true);
            if (_config?.debug == true) Debug.Log($"[AutoTarget] Target locked: {target}");
        }

        public static void ClearLockedTarget()
        {
            if (AutoTargetState.lockedTarget == Entity.Null)
                return;

            TargetHighlighter.ClearHighlight(AutoTargetState.lockedTarget);
            AutoTargetState.lockedTarget = Entity.Null;
            if (_config?.debug == true) Debug.Log("[AutoTarget] Target lock cleared");
        }

        /// <summary>
        /// Validates the locked target, scans for the nearest enemy, and updates the aim direction.
        /// </summary>
        public static void Tick(AutoTargetConfig config)
        {
            PlayerController player = Manager.main.player;
            if (player == null)
            {
                AutoTargetState.targetDirection = float3.zero;
                AutoTargetState.nearestEnemy = Entity.Null;
                return;
            }

            Vector3 playerPos = player.transform.position;

            if (AutoTargetState.lockedTarget != Entity.Null
                && !TargetDetectionService.IsTargetValid(AutoTargetState.lockedTarget))
            {
                TargetHighlighter.ClearHighlight(AutoTargetState.lockedTarget);
                AutoTargetState.lockedTarget = Entity.Null;
            }

            if (AutoTargetState.autoAimActive && AutoTargetState.lockedTarget == Entity.Null)
            {
                AutoTargetState.nearestEnemy =
                    TargetDetectionService.FindNearestEnemy(playerPos, config.targetingRange);
            }
            else if (AutoTargetState.lockedTarget != Entity.Null)
            {
                // Clear nearest enemy while locked so highlights don't flicker
                AutoTargetState.nearestEnemy = Entity.Null;
            }

            Entity target = AutoTargetState.CurrentTarget;
            AutoTargetState.targetDirection = target != Entity.Null
                ? TargetDetectionService.GetDirectionTo(target, playerPos)
                : float3.zero;
        }

        private static void OnObjectDespawned(Entity entity, EntityManager entityManager, GameObject graphicalObject)
        {
            if (entity == AutoTargetState.lockedTarget)
            {
                TargetHighlighter.ClearHighlight(entity);
                AutoTargetState.lockedTarget = Entity.Null;
                AutoTargetState.targetDirection = float3.zero;
                if (_config?.debug == true) Debug.Log("[AutoTarget] Locked target despawned, lock cleared");
            }
        }

        private static void OnWorldDestroyed()
        {
            TargetHighlighter.ClearAll();
            AutoTargetState.lockedTarget = Entity.Null;
            AutoTargetState.nearestEnemy = Entity.Null;
            AutoTargetState.targetDirection = float3.zero;
            TargetDetectionService.InvalidateCache();
            if (_config?.debug == true) Debug.Log("[AutoTarget] World destroyed, target state cleared");
        }
    }
}
