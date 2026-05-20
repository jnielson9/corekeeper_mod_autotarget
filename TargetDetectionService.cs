using PugMod;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace AutoTargetMod
{
    /// <summary>
    /// Scans the entity mono lookup for the nearest valid hostile enemy within range.
    /// Results are cached for ScanInterval seconds to avoid querying every frame.
    /// Entities are excluded if they match any of:
    ///   MinionCD / PetCD              - traditional minion or tamed pet
    ///   OwnerReferenceCD.owner == player - entity directly owned by the player
    ///   FactionCD.CanAttack returns false - same faction as the player; catches friendly
    ///                                       summons like the Phantom Spark ghost which
    ///                                       inherits the player's faction via InheritFaction
    /// </summary>
    public static class TargetDetectionService
    {
        private static float _lastScanTime = -1f;
        private const float ScanInterval = 0.1f;

        /// <summary>
        /// Returns the nearest valid enemy within range of playerPos, or Entity.Null if none found.
        /// Results are cached for ScanInterval seconds.
        /// </summary>
        public static Entity FindNearestEnemy(Vector3 playerPos, float range)
        {
            if (Time.time < _lastScanTime + ScanInterval)
                return AutoTargetState.nearestEnemy;

            _lastScanTime = Time.time;

            if (Manager.main.player == null)
                return Entity.Null;

            World world = API.Client.World;
            EntityManager em = world?.EntityManager ?? default;
            bool hasEm = world != null;

            Entity playerEntity = Manager.main.player.entity;

            // Fetch the player's faction once to filter allies such as the Phantom Spark ghost,
            // which inherits the player's faction via InheritFaction and has no OwnerReferenceCD.
            FactionCD playerFaction = default;
            bool hasPlayerFaction = hasEm &&
                EntityUtility.TryGetComponentData<FactionCD>(playerEntity, world, out playerFaction);

            float rangeSq = range * range;
            Entity nearest = Entity.Null;
            float nearestDistSq = rangeSq;

            foreach (var kvp in Manager.memory.entityMonoLookUp)
            {
                EntityMonoBehaviour mono = kvp.Value;
                if (mono == null || !mono.isEnemy || mono.currentHealth <= 0)
                    continue;

                if (!mono.gameObject.activeInHierarchy)
                    continue;

                if (hasEm)
                {
                    Entity e = kvp.Key;
                    if (!em.Exists(e)) continue;

                    if (em.HasComponent<MinionCD>(e) || em.HasComponent<PetCD>(e))
                        continue;

                    if (EntityUtility.TryGetComponentData<OwnerReferenceCD>(e, world, out OwnerReferenceCD ownerRef)
                        && ownerRef.owner == playerEntity)
                        continue;

                    if (hasPlayerFaction &&
                        EntityUtility.TryGetComponentData<FactionCD>(e, world, out FactionCD targetFaction) &&
                        !playerFaction.CanAttack(targetFaction, default))
                        continue;
                }

                Vector3 diff = mono.transform.position - playerPos;
                float distSq = diff.x * diff.x + diff.z * diff.z;
                if (distSq < nearestDistSq)
                {
                    nearestDistSq = distSq;
                    nearest = kvp.Key;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Returns the normalised XZ direction from playerPos toward target, or float3.zero if invalid.
        /// </summary>
        public static float3 GetDirectionTo(Entity target, Vector3 playerPos)
        {
            if (target == Entity.Null)
                return float3.zero;

            EntityMonoBehaviour mono = Manager.memory.GetEntityMono(target);
            if (mono == null || !mono.gameObject.activeInHierarchy)
                return float3.zero;

            Vector3 diff = mono.transform.position - playerPos;
            float3 dir = new float3(diff.x, 0f, diff.z);
            return math.normalizesafe(dir);
        }

        /// <summary>
        /// Returns true if the target is still alive and spawned.
        /// </summary>
        public static bool IsTargetValid(Entity target)
        {
            if (target == Entity.Null)
                return false;

            EntityMonoBehaviour mono = Manager.memory.GetEntityMono(target);
            return mono != null && mono.gameObject.activeInHierarchy
                   && mono.isEnemy && mono.currentHealth > 0;
        }

        public static void InvalidateCache()
        {
            _lastScanTime = -1f;
        }
    }
}
