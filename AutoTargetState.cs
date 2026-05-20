using Unity.Entities;
using Unity.Mathematics;

namespace AutoTargetMod
{
    /// <summary>
    /// Shared mod state updated each frame in AutoTarget.Update().
    /// </summary>
    public static class AutoTargetState
    {
        /// <summary>Auto-aim toward the nearest enemy is active.</summary>
        public static bool autoAimActive = false;

        /// <summary>Manually locked target. Entity.Null when nothing is locked.</summary>
        public static Entity lockedTarget = Entity.Null;

        /// <summary>Nearest detected enemy this frame. Entity.Null when none found.</summary>
        public static Entity nearestEnemy = Entity.Null;

        /// <summary>
        /// Normalised XZ direction toward the current target (locked takes priority).
        /// float3.zero when no valid target exists.
        /// </summary>
        public static float3 targetDirection = float3.zero;

        /// <summary>
        /// True when the mod should redirect attack direction this frame.
        /// Requires auto-aim or a locked target, plus a valid direction.
        /// </summary>
        public static bool ShouldOverrideAim =>
            math.lengthsq(targetDirection) > 0.001f &&
            (autoAimActive || lockedTarget != Entity.Null);

        /// <summary>The entity being targeted; locked target takes priority over nearest.</summary>
        public static Entity CurrentTarget =>
            lockedTarget != Entity.Null ? lockedTarget : nearestEnemy;

        public static void Reset()
        {
            lockedTarget = Entity.Null;
            nearestEnemy = Entity.Null;
            targetDirection = float3.zero;
        }
    }
}
