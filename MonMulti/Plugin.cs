using BepInEx;
using HarmonyLib;
using UnityEngine;
using System;
using System.Threading.Tasks;

namespace MonMulti
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class MonMultiMod : BaseUnityPlugin
    {
        public const string pluginGuid = "monbazou.antalervin19.monmultiplayer";
        public const string pluginName = "MonMultiClient";
        public const string pluginVersion = "0.0.1.3";

        private Harmony _harmony;
        private bool isInitialized = false;

        private Client _client;

        private void Awake()
        {
            // Initialize Harmony
            _harmony = new Harmony(pluginGuid);
            _harmony.PatchAll();
            Debug.Log($"{pluginName} has been loaded!");

            // Initialize client
            _client = new Client();
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

            if (Time.frameCount % 50 == 0)
            {
                Vector3 playerPosition = GameData.Player.transform.position;

                string positionMessage = $"CPOS:{playerPosition.x},{playerPosition.y},{playerPosition.z}";

                SendMessageToServerAsync(positionMessage);
            }
        }

        private void OnGameInitialization()
        {
            Debug.Log("Game is ready! \n Connecting to server...");
            Task.Run(() => ConnectToServer());
        }

        private async Task ConnectToServer()
        {
            try
            {
                await _client.ConnectToServerAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error connecting to the server: {ex.Message}");
            }
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
