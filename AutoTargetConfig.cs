using System.IO;
using UnityEngine;

namespace AutoTargetMod
{
    [System.Serializable]
    public class AutoTargetConfig
    {
        public bool autoAimEnabled = false;
        public float targetingRange = 15f;
        public bool highlightEnabled = true;
        public bool overrideMeleeFacing = true;
        public bool debug = false;

        // KeyCode int values (serializable by JsonUtility)
        public int toggleAutoAimKeyCode = (int)KeyCode.F;
        public int lockTargetKeyCode = (int)KeyCode.T;

        private const string ConfigFileName = "AutoTarget_config.json";

        public KeyCode ToggleAutoAimKey => (KeyCode)toggleAutoAimKeyCode;
        public KeyCode LockTargetKey => (KeyCode)lockTargetKeyCode;

        public static AutoTargetConfig Load()
        {
            string path = Path.Combine(Application.persistentDataPath, ConfigFileName);
            try
            {
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var loaded = JsonUtility.FromJson<AutoTargetConfig>(json);
                    if (loaded != null)
                    {
                        if (loaded.debug) Debug.Log($"[AutoTarget] Loaded config from {path}");
                        return loaded;
                    }
                }

                var defaults = new AutoTargetConfig();
                File.WriteAllText(path, JsonUtility.ToJson(defaults, true));
                if (defaults.debug) Debug.Log($"[AutoTarget] Created default config at {path}");
                return defaults;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[AutoTarget] Config load failed: {ex.Message}. Using defaults.");
                return new AutoTargetConfig();
            }
        }

        public void Save()
        {
            string path = Path.Combine(Application.persistentDataPath, ConfigFileName);
            try
            {
                File.WriteAllText(path, JsonUtility.ToJson(this, true));
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[AutoTarget] Config save failed: {ex.Message}");
            }
        }
    }
}
