using HarmonyLib;
using Pug.UnityExtensions;
using System;
using System.Reflection;
using Unity.Mathematics;
using UnityEngine;

namespace AutoTargetMod
{
    /// <summary>
    /// Harmony patches that redirect player attack direction toward the current target.
    /// Two hooks are applied:
    ///   1. PlayerController.UpdateAim (static) - overrides aimDirection for ranged attacks.
    ///   2. SendClientInputSystem.CalculateDirection (private) - overrides targetingDirection
    ///      for beams and lunges, and optionally facingDirection for melee.
    /// Both patches are no-ops when ShouldOverrideAim is false.
    /// </summary>
    public static class AutoAimController
    {
        private static AutoTargetConfig _config;

        public static void Apply(Harmony harmony, AutoTargetConfig config)
        {
            _config = config;

            // Patch 1: PlayerController.UpdateAim (static overload)
            // Signature: static void UpdateAim(ref float3 aimDirection, float3 position,
            //                                  bool isAimingBlocked, PlayerInput inputModule, AimUI aimUI)
            MethodInfo updateAimMethod = null;
            foreach (var method in typeof(PlayerController).GetMethods(
                         BindingFlags.Public | BindingFlags.Static))
            {
                if (method.Name != "UpdateAim") continue;
                var p = method.GetParameters();
                if (p.Length == 5 && p[0].ParameterType == typeof(float3).MakeByRefType())
                {
                    updateAimMethod = method;
                    break;
                }
            }

            if (updateAimMethod != null)
            {
                var postfix1 = AccessTools.Method(typeof(AutoAimController), nameof(UpdateAim_Postfix));
                harmony.Patch(updateAimMethod, postfix: new HarmonyMethod(postfix1));
                if (_config.debug) Debug.Log("[AutoTarget] Patched PlayerController.UpdateAim");
            }
            else
            {
                Debug.LogError("[AutoTarget] Could not find PlayerController.UpdateAim, ranged aim override disabled");
            }

            // Patch 2: SendClientInputSystem.CalculateDirection (private instance)
            // Signature: void CalculateDirection(ref Direction facingDirection,
            //                                    ref float3 targetingDirection, in float3 aimDirection)
            MethodInfo calcDirMethod = AccessTools.Method(typeof(SendClientInputSystem), "CalculateDirection");

            if (calcDirMethod != null)
            {
                var postfix2 = AccessTools.Method(typeof(AutoAimController), nameof(CalculateDirection_Postfix));
                harmony.Patch(calcDirMethod, postfix: new HarmonyMethod(postfix2));
                if (_config.debug) Debug.Log("[AutoTarget] Patched SendClientInputSystem.CalculateDirection");
            }
            else
            {
                Debug.LogError("[AutoTarget] Could not find SendClientInputSystem.CalculateDirection, melee/beam override disabled");
            }
        }

        // Postfix for PlayerController.UpdateAim.
        // Replaces aimDirection with the direction toward the current target.
        private static void UpdateAim_Postfix(ref float3 aimDirection)
        {
            if (!AutoTargetState.ShouldOverrideAim)
                return;

            aimDirection = AutoTargetState.targetDirection;
        }

        // Postfix for SendClientInputSystem.CalculateDirection.
        // Overrides targetingDirection for beams and lunges, and optionally facingDirection for melee.
        private static void CalculateDirection_Postfix(
            ref Direction facingDirection,
            ref float3 targetingDirection)
        {
            if (!AutoTargetState.ShouldOverrideAim)
                return;

            targetingDirection = AutoTargetState.targetDirection;

            if (_config != null && _config.overrideMeleeFacing)
                facingDirection = Direction.FromVector(AutoTargetState.targetDirection);
        }

        public static void SetConfig(AutoTargetConfig config)
        {
            _config = config;
        }
    }
}
