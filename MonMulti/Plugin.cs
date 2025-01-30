using BepInEx;
using HarmonyLib;
using UnityEngine;
using System;
using System.Threading.Tasks;

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
