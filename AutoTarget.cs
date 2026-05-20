using AutoTargetMod;
using HarmonyLib;
using PugMod;
using UnityEngine;

/// <summary>
/// AutoTarget mod entry point.
/// F: auto-aim toward the nearest enemy in range (toggle).
/// T: lock onto the enemy under the cursor; press again to release.
/// Settings are saved to AutoTarget_config.json in the game's persistent data folder.
/// </summary>
public class AutoTarget : IMod
{
    private Harmony _harmony;
    private AutoTargetConfig _config;

    public void EarlyInit() { }

    public void Init()
    {
        _config = AutoTargetConfig.Load();
        AutoTargetState.autoAimActive = _config.autoAimEnabled;

        TargetManager.Initialize(_config);

        _harmony = new Harmony("com.autotarget.mod");
        AutoAimController.Apply(_harmony, _config);

        if (_config.debug)
            Debug.Log($"[AutoTarget] Ready. Auto-aim={_config.autoAimEnabled}, " +
                      $"Range={_config.targetingRange}, " +
                      $"ToggleKey={(KeyCode)_config.toggleAutoAimKeyCode}, " +
                      $"LockKey={(KeyCode)_config.lockTargetKeyCode}");
    }

    public void Shutdown()
    {
        if (_config?.debug == true) Debug.Log("[AutoTarget] Shutting down…");

        TargetManager.ClearLockedTarget();
        TargetHighlighter.ClearAll();
        TargetManager.Shutdown();
        AutoTargetState.Reset();

        _harmony?.UnpatchSelf();
        _harmony = null;

        if (_config != null)
        {
            _config.autoAimEnabled = AutoTargetState.autoAimActive;
            _config.Save();
        }
    }

    /// <summary>
    /// Per-frame update order:
    ///   1. TargetManager.Tick   - validate lock, find nearest enemy, compute aim direction
    ///   2. InputHandler.Tick    - process hotkeys
    ///   3. TargetHighlighter.Tick - update visual rings
    /// </summary>
    public void Update()
    {
        if (Manager.main.player == null)
            return;

        TargetManager.Tick(_config);
        InputHandler.Tick(_config);

        if (_config.highlightEnabled)
            TargetHighlighter.Tick(AutoTargetState.lockedTarget, AutoTargetState.nearestEnemy);
    }

    public void ModObjectLoaded(UnityEngine.Object obj) { }

    public bool CanBeUnloaded() => false;
}
