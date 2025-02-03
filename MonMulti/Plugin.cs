using BepInEx;
using HarmonyLib;
using UnityEngine;
using System;
using System.Threading.Tasks;
using MonMulti;
using Newtonsoft.Json;

namespace MonMulti
{
    [BepInPlugin(ModInfo.pluginGuid, ModInfo.pluginName, ModInfo.pluginVersion)]
    public class MonMultiMod : BaseUnityPlugin
    {
        private Harmony _harmony;
        private bool isInitialized = false;
        private Client _client;

        private void Awake()
        {
            // Initialize Harmony
            _harmony = new Harmony(ModInfo.pluginGuid);
            _harmony.PatchAll();
            Debug.Log($"{ModInfo.pluginName} has been loaded!");

            // Initialize client
            _client = new Client();

            // Initialize GUI
            GameObject guiObject = new GameObject("MonMulti_GUI");
            GUIManager guiManager = guiObject.AddComponent<GUIManager>();
            guiManager.SetClient(_client);
            DontDestroyOnLoad(guiObject);
        }

        private void Update()
        {
            if (GameData.IsGameInitialized && GameData.Player != null)
            {
                if (!isInitialized)
                {
                    OnGameInitialization();
                    isInitialized = true;
                }
            }
        }

        private void FixedUpdate()
        {
            if (!isInitialized || GameData.Player == null) { return; }

            Vector3 playerPosition = GameData.Player.transform.position;
            Quaternion playerRotation = GameData.Player.transform.rotation;

            var packet = new MonMultiPacket
            {
                PlayerPosition = new float[] { Round(playerPosition.x), Round(playerPosition.y), Round(playerPosition.z) },  // X, Y, Z (2 fpp)
                PlayerRotation = new float[] { Round(playerRotation.x), Round(playerRotation.y), Round(playerRotation.z), Round(playerRotation.w) }, // X, Y, Z, W (2 fpp)
                KonigPosition = new float[] { Round(0), Round(0), Round(0) },  // X, Y, Z (2 fpp) (placeholder)
                KonigRotation = new float[] { Round(0), Round(0), Round(0), Round(0) }, // X, Y, Z, W (2 fpp) (placeholder)
                Cash = 0, // Integer
                Time = 0  // Integer
            };

            SendJsonPacketToServerAsync(packet);
        }

        private void OnGameInitialization()
        {
            Debug.Log("Game is ready! \n Connecting to server...");
        }

        private async Task SendJsonPacketToServerAsync(MonMultiPacket packet)
        {
            string jsonMessage = JsonConvert.SerializeObject(packet);
            string response = await _client.SendJsonPacketAsync(jsonMessage);
            if (!string.IsNullOrEmpty(response))
            {
                Debug.Log($"Server responded: {response}");
            }
        }

        private void OnDestroy()
        {
            Debug.Log("Disconnecting from server...");
            _client.Disconnect();
        }

        private float Round(float value)
        {
            return (float)Math.Round(value, 2);
        }
    }
    public class MonMultiPacket
    {
        public float[] PlayerPosition { get; set; }
        public float[] PlayerRotation { get; set; }
        public float[] KonigPosition { get; set; }
        public float[] KonigRotation { get; set; }
        public int Cash { get; set; }
        public int Time { get; set; }
    }
}

foreach (var vehicle in GameData.Vehicles)
{
    Debug.Log($"Vehicle found at position: {vehicle.transform.position}");
}