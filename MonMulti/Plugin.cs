using BepInEx;
using HarmonyLib;
using UnityEngine;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;

///SUMMARY:
///Added Server,Client.cs that actually work, made client and server connect.
///TODO: Create Players for every character in the game, OPTIMIZE THIS SHIT, Shit-ton of error handling.

namespace MonMulti
{
    [BepInPlugin(ModInfo.pluginGuid, ModInfo.pluginName, ModInfo.pluginVersion)]
    public class MonMultiMod : BaseUnityPlugin
    {
        private Harmony _harmony;
        private bool isInitialized = false;
        private Server _server;

        private void Awake()
        {
            // Initialize Harmony
            _harmony = new Harmony(ModInfo.pluginGuid);
            _harmony.PatchAll();
            Debug.Log($"{ModInfo.pluginName} has been loaded!");

            // Initialize server
            _server = new Server();
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
                Debug.Log(positionMessage);
                SendMessageToAllClients(positionMessage);
            }
        }

        private void OnGameInitialization()
        {
            Debug.Log("Game is ready! \n Initializing server...");
            Task.Run(() => StartServer());
        }

        private async Task StartServer()
        {
            try
            {
                await _server.StartServerAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error starting the server: {ex.Message}");
            }
        }

        private async Task SendMessageToAllClients(string message)
        {
            await _server.SendMessageToAllClientsAsync(message);
        }

        private async Task SendMessageToClient(TcpClient client, string message)
        {
            await _server.SendMessageToClientAsync(client, message);
        }

        private void OnDestroy()
        {
            Debug.Log("Stopping server...");
            _server.StopServer();
        }
    }
}
