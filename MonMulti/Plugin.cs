using BepInEx;
using HarmonyLib;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MonMulti;
using Newtonsoft.Json;
using MonMulti.Networking;

namespace MonMulti
{
    [BepInPlugin(ModInfo.pluginGuid, ModInfo.pluginName, ModInfo.pluginVersion)]
    public class MonMultiMod : BaseUnityPlugin
    {
        private Harmony _harmony;
        private bool isInitialized = false;
        private bool DebugMode = true;

        private static AsyncServer _server;
        private static AsyncClient _client;

        private void Awake()
        {
            // Initialize Harmony
            _harmony = new Harmony(ModInfo.pluginGuid);
            _harmony.PatchAll();

            // Initialize GUI
            GameObject guiObject = new GameObject("MonMulti_GUI");
            GUIManager guiManager = guiObject.AddComponent<GUIManager>();
            DontDestroyOnLoad(guiObject);

            Debug.Log($"{ModInfo.pluginName} has been loaded!");
        }

        private void Update()
        {
            //Check if user is in a game (Might be replaced)
            if (GameData.IsGameInitialized && GameData.Player != null)
            {
                if (!isInitialized)
                {
                    OnGameInitialization();
                    isInitialized = true;
                }
            }
        }

        private void OnGameInitialization()
        {
            //Grab Konig & OITruck from Vehicles
            foreach (var vehicle in GameData.Vehicles)
            {
                if (vehicle.gameObject.name == "Konig")
                {
                    GameData.Konig = vehicle.gameObject;
                    break;
                }
                else if (vehicle.gameObject.name == "OITruck")
                {
                    GameData.OITruck = vehicle.gameObject;
                    break;
                }
            }
        }

        /*
         *   --NETWORKING-- 
         * below is the functions
         * that are used in the
         * networking.
        */

        
    }
}