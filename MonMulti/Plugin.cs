using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace MonMulti
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class MonMultiMod : BaseUnityPlugin
    {
        public const string pluginGuid = "monbazou.antalervin19.monmultiplayer";
        public const string pluginName = "MonMulti";
        public const string pluginVersion = "0.0.0.3";

        private Harmony _harmony;
        private bool isInitialized = false;
        private void Awake()
        {
            // Initialize Harmony
            _harmony = new Harmony(pluginGuid);
            _harmony.PatchAll();
            Debug.Log($"{pluginName} has been loaded!");
        }

        private void Update()
        {
            if (GameData.IsGameInitialized && GameData.Player != null)
            {
                if (!isInitialized)
                {
                    Debug.Log("Game is initialized and Player is available.");
                    isInitialized = true;
                }

                Vector3 playerPosition = GameData.Player.transform.position;
                Quaternion playerRotation = GameData.Player.transform.rotation;

                Debug.Log($"Player Position: {playerPosition}, Player Rotation: {playerRotation}");
            }
            else
            {
                if (!isInitialized)
                {
                    Debug.LogWarning("Game is not initialized or Player is not yet available.");
                }
            }
        }
    }
}
