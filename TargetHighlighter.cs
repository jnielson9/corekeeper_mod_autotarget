using Unity.Entities;
using UnityEngine;

namespace AutoTargetMod
{
    /// <summary>
    /// Draws a circle ring around the active auto-aim and locked targets using LineRenderer.
    /// Each ring is parented to the entity's transform and drawn once in local space, so Unity
    /// moves it automatically via the transform hierarchy with no per-frame position updates.
    /// Locked target: solid gold ring. Auto-aim target: semi-transparent cyan ring (hidden while locked).
    /// </summary>
    public static class TargetHighlighter
    {
        private const int   Segments     = 32;
        private const float LockedRadius = 0.55f;
        private const float AutoRadius   = 0.50f;
        private const float LineWidth    = 0.07f;

        private static readonly Color LockedColor = new Color(1f,  0.65f, 0f,   1f);
        private static readonly Color AutoColor   = new Color(0.2f, 0.85f, 1f, 0.75f);

        private static readonly Vector3[] _posBuffer = new Vector3[Segments];

        private static LineRenderer _lockedLR;
        private static LineRenderer _autoLR;

        private static Entity _currentLocked = Entity.Null;
        private static Entity _currentAuto   = Entity.Null;

        public static void Tick(Entity lockedTarget, Entity autoTarget)
        {
            EnsureRenderers();

            // Locked ring
            if (lockedTarget != Entity.Null)
            {
                EntityMonoBehaviour mono = Manager.memory.GetEntityMono(lockedTarget);
                if (mono != null)
                {
                    if (lockedTarget != _currentLocked)
                    {
                        AttachRing(_lockedLR, mono, LockedRadius);
                        _currentLocked = lockedTarget;
                    }
                    _lockedLR.gameObject.SetActive(true);
                }
                else
                {
                    _lockedLR.gameObject.SetActive(false);
                }
            }
            else if (_currentLocked != Entity.Null)
            {
                DetachRing(_lockedLR);
                _currentLocked = Entity.Null;
            }

            // Auto-aim ring (hidden while a manual lock is held)
            bool showAuto = AutoTargetState.autoAimActive
                         && lockedTarget == Entity.Null
                         && autoTarget   != Entity.Null;

            if (showAuto)
            {
                EntityMonoBehaviour mono = Manager.memory.GetEntityMono(autoTarget);
                if (mono != null)
                {
                    if (autoTarget != _currentAuto)
                    {
                        AttachRing(_autoLR, mono, AutoRadius);
                        _currentAuto = autoTarget;
                    }
                    _autoLR.gameObject.SetActive(true);
                }
                else
                {
                    _autoLR.gameObject.SetActive(false);
                }
            }
            else if (_currentAuto != Entity.Null)
            {
                DetachRing(_autoLR);
                _currentAuto = Entity.Null;
            }
        }

        public static void ClearAll()
        {
            if (_lockedLR != null) DetachRing(_lockedLR);
            if (_autoLR   != null) DetachRing(_autoLR);
            _currentLocked = Entity.Null;
            _currentAuto   = Entity.Null;
        }

        public static void ClearAutoAimHighlight()
        {
            if (_autoLR != null) DetachRing(_autoLR);
            _currentAuto = Entity.Null;
        }

        public static void ClearHighlight(Entity target)
        {
            if (target == _currentLocked)
            {
                if (_lockedLR != null) DetachRing(_lockedLR);
                _currentLocked = Entity.Null;
            }
            if (target == _currentAuto)
            {
                if (_autoLR != null) DetachRing(_autoLR);
                _currentAuto = Entity.Null;
            }
        }

        public static void ApplyHighlight(Entity target, bool isLocked)
        {
            // Visual is driven entirely by Tick(); no immediate action needed.
        }

        private static void EnsureRenderers()
        {
            if (_lockedLR == null || _lockedLR.gameObject == null)
            {
                _lockedLR = BuildRenderer("AutoTarget_LockedRing", LockedColor);
                _currentLocked = Entity.Null;
            }
            if (_autoLR == null || _autoLR.gameObject == null)
            {
                _autoLR = BuildRenderer("AutoTarget_AutoRing", AutoColor);
                _currentAuto = Entity.Null;
            }
        }

        /// <summary>
        /// Parents the ring to the entity's transform and draws the circle once in local space.
        /// Unity moves it automatically via the transform hierarchy.
        /// </summary>
        private static void AttachRing(LineRenderer lr, EntityMonoBehaviour mono, float radius)
        {
            lr.transform.SetParent(mono.transform, worldPositionStays: false);
            lr.transform.localPosition = Vector3.zero;
            DrawLocalCircle(lr, radius);
        }

        /// <summary>Detaches the ring from its parent and hides it.</summary>
        private static void DetachRing(LineRenderer lr)
        {
            lr.transform.SetParent(null);
            lr.gameObject.SetActive(false);
        }

        private static LineRenderer BuildRenderer(string goName, Color color)
        {
            var go = new GameObject(goName);
            var lr = go.AddComponent<LineRenderer>();
            var mat = new Material(Shader.Find("Sprites/Default"));
            lr.material = mat;

            lr.startColor    = color;
            lr.endColor      = color;
            lr.startWidth    = LineWidth;
            lr.endWidth      = LineWidth;
            lr.useWorldSpace = false;   // positions are in parent-local space
            lr.loop          = true;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows    = false;
            lr.sortingOrder      = 200;
            lr.positionCount     = Segments;

            go.SetActive(false);
            return lr;
        }

        /// <summary>
        /// Fills the position buffer with a circle in the local XZ plane and pushes it
        /// to the renderer atomically.
        /// </summary>
        private static void DrawLocalCircle(LineRenderer lr, float radius)
        {
            for (int i = 0; i < Segments; i++)
            {
                float angle = 2f * Mathf.PI * i / Segments;
                _posBuffer[i] = new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius);
            }
            lr.SetPositions(_posBuffer);
        }
    }
}
