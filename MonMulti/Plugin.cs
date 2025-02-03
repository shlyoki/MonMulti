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

            Vector3 konigPosition = GameData.KonigVehicle != null ? GameData.KonigVehicle.transform.position : Vector3.zero;
            Quaternion konigRotation = GameData.KonigVehicle != null ? GameData.KonigVehicle.transform.rotation : Quaternion.identity;

            var packet = new MonMultiPacket
            {
                PlayerPosition = new float[] { Round(playerPosition.x), Round(playerPosition.y), Round(playerPosition.z) },
                PlayerRotation = new float[] { Round(playerRotation.x), Round(playerRotation.y), Round(playerRotation.z), Round(playerRotation.w) },
                KonigPosition = new float[] { Round(konigPosition.x), Round(konigPosition.y), Round(konigPosition.z) },
                KonigRotation = new float[] { Round(konigRotation.x), Round(konigRotation.y), Round(konigRotation.z), Round(konigRotation.w) },
                Cash = 0,
                Time = 0
            };

            SendJsonPacketToServerAsync(packet);
        }

        private void OnGameInitialization()
        {
            Debug.Log("Game is ready! \n Connecting to server...");

            foreach (var vehicle in GameData.Vehicles)
            {
                if (vehicle.gameObject.name == "Konig")
                {
                    GameData.KonigVehicle = vehicle.gameObject;
                    Debug.Log($"[MonMulti] Konig vehicle found and stored at position: {GameData.KonigVehicle.transform.position}");
                    break;
                }
            }
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
