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
        public const string pluginVersion = "0.0.0.5";

        private Harmony _harmony;
        private Server _server;

        private bool isInitialized = false;

        private GameObject PlayerCube;
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
                    CreatePlayerCube();
                    isInitialized = true;
                }

                Vector3 playerPosition = GameData.Player.transform.position;
                Quaternion playerRotation = GameData.Player.transform.rotation;

                Debug.Log($"Player Position: {playerPosition}, Player Rotation: {playerRotation}");

                PlayerCube.transform.position = GameData.Player.transform.position + new Vector3(0, 2, 0);
            }
            else
            {
                if (!isInitialized)
                {
                    Debug.LogWarning("Game is not initialized or Player is not yet available.");
                }
            }
        }

        private void CreatePlayerCube()
        {
            PlayerCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            PlayerCube.transform.position = new Vector3(0, 10, 0);
            PlayerCube.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            Renderer renderer = PlayerCube.GetComponent<Renderer>();
            renderer.material.color = Color.white;

            DontDestroyOnLoad(PlayerCube);

            Debug.Log("Player now has a cube floating above them.");
        }
    }
}
