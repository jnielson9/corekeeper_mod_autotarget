using Unity.Entities;
using UnityEngine;

namespace AutoTargetMod
{
    /// <summary>
    /// Handles hotkey input for the mod. Uses UnityEngine.Input to avoid Rewired conflicts.
    /// F: toggle auto-aim. T: lock the enemy under the cursor (press again to unlock).
    /// </summary>
    public static class InputHandler
    {
        /// <summary>
        /// World-space pick radius around the cursor for target locking.
        /// 1 unit is roughly 1 tile; 1.5 gives a forgiving 3-tile-wide pick area.
        /// </summary>
        private const float CursorPickRadiusWorld = 1.5f;

        public static void Tick(AutoTargetConfig config)
        {
            if (Manager.main.player == null)
                return;

            if (SendClientInputSystem.PlayerInputBlocked())
                return;

            HandleAutoAimToggle(config);
            HandleTargetLock(config);
        }

        private static void HandleAutoAimToggle(AutoTargetConfig config)
        {
            if (!Input.GetKeyDown(config.ToggleAutoAimKey))
                return;

            AutoTargetState.autoAimActive = !AutoTargetState.autoAimActive;

            if (!AutoTargetState.autoAimActive)
            {
                TargetHighlighter.ClearAutoAimHighlight();
                AutoTargetState.nearestEnemy = Entity.Null;
                TargetDetectionService.InvalidateCache();
            }

            if (config.debug) Debug.Log($"[AutoTarget] Auto-aim {(AutoTargetState.autoAimActive ? "ON" : "OFF")}");
        }

        private static void HandleTargetLock(AutoTargetConfig config)
        {
            if (!Input.GetKeyDown(config.LockTargetKey))
                return;

            if (AutoTargetState.lockedTarget != Entity.Null)
            {
                TargetManager.ClearLockedTarget();
                return;
            }

            Entity candidate = FindEnemyUnderCursor(config);

            if (candidate != Entity.Null)
                TargetManager.SetLockedTarget(candidate);
            else
                if (config.debug) Debug.Log("[AutoTarget] No enemy under cursor to lock");
        }

        /// <summary>
        /// Returns the enemy closest to the cursor in world space, within CursorPickRadiusWorld
        /// units of the cursor and within config.targetingRange of the player.
        /// Uses Manager.ui.mouse.GetMouseGameViewPosition() for the cursor's world position.
        /// </summary>
        private static Entity FindEnemyUnderCursor(AutoTargetConfig config)
        {
            if (Manager.main.player == null || Manager.ui == null || Manager.ui.mouse == null)
                return Entity.Null;

            Vector3 mouseWorld = Manager.ui.mouse.GetMouseGameViewPosition();
            Vector3 playerPos  = Manager.main.player.transform.position;

            Entity best       = Entity.Null;
            float bestDistSq  = CursorPickRadiusWorld * CursorPickRadiusWorld;
            float rangeSq     = config.targetingRange * config.targetingRange;

            foreach (var kvp in Manager.memory.entityMonoLookUp)
            {
                EntityMonoBehaviour mono = kvp.Value;
                if (mono == null || !mono.isEnemy || mono.currentHealth <= 0)
                    continue;
                if (!mono.gameObject.activeInHierarchy)
                    continue;

                Vector3 toPlayer = mono.transform.position - playerPos;
                float playerDistSq = toPlayer.x * toPlayer.x + toPlayer.z * toPlayer.z;
                if (playerDistSq > rangeSq)
                    continue;

                Vector3 toCursor = mono.transform.position - mouseWorld;
                float cursorDistSq = toCursor.x * toCursor.x + toCursor.z * toCursor.z;

                if (cursorDistSq < bestDistSq)
                {
                    bestDistSq = cursorDistSq;
                    best       = kvp.Key;
                }
            }

            return best;
        }
    }
}
