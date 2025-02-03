using BepInEx;
using HarmonyLib;
using UnityEngine;
using System;
using System.Threading.Tasks;
using MonMulti;

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
            if (!isInitialized) { return; }

            // Send Player Position
            Vector3 playerPosition = GameData.Player.transform.position;
            Quaternion playerRotation = GameData.Player.transform.rotation;

            string playerPositionMessage = $"PLAYER_POS:{playerPosition.x},{playerPosition.y},{playerPosition.z}";
            //string playerRotationMessage = $"PLAYER_ROT:{playerRotation.x},{playerRotation.y},{playerRotation.z},{playerRotation.w}";

            SendMessageToServerAsync(playerPositionMessage);
            //SendMessageToServerAsync(playerRotationMessage);

            foreach (var vehicle in GameData.vehicles)
            {
                if (vehicle.gameObject.name == "Konig")
                {
                    Debug.Log($"Vehicle found at position: {vehicle.transform.position}");
                }
            }

            /*
            if (GameData.PlayerVehicle != null)
            {
                Vector3 vehiclePosition = GameData.PlayerVehicle.transform.position;
                Quaternion vehicleRotation = GameData.PlayerVehicle.transform.rotation;

                string vehiclePositionMessage = $"VEHICLE_POS:{vehiclePosition.x},{vehiclePosition.y},{vehiclePosition.z}";
                string vehicleRotationMessage = $"VEHICLE_ROT:{vehicleRotation.x},{vehicleRotation.y},{vehicleRotation.z},{vehicleRotation.w}";

                SendMessageToServerAsync(vehiclePositionMessage);
                SendMessageToServerAsync(vehicleRotationMessage);
            }*/
        }

        private void OnGameInitialization()
        {
            Debug.Log("Game is ready! \n Connecting to server...");
        }

        private async Task SendMessageToServerAsync(string message)
        {
            string response = await _client.SendMessageAsync(message);
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
    }
}

/*MONMULTI COMMUNICATION PROTOCOL
 *
 *Using json:
 *PlayerPosition, XYZ 2 fpp
 *PlayerRotation, XYZQ 2 fpp
 *KonigPosition, XYZ 2 fpp   (If possible)
 *KonigRotation, XYZQ 2 fpp   (If possible)
 *Cash, intager
 *Time, intager
 *
*/