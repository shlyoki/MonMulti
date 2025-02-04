using BepInEx;
using HarmonyLib;
using UnityEngine;
using System;
using System.Collections.Generic;
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

        private void Awake()
        {
            // Initialize Harmony
            _harmony = new Harmony(ModInfo.pluginGuid);
            _harmony.PatchAll();

            Debug.Log($"{ModInfo.pluginName} has been loaded!");


            // Initialize GUI
            GameObject guiObject = new GameObject("MonMulti_GUI");
            GUIManager guiManager = guiObject.AddComponent<GUIManager>();
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
            Vector3 playerPosition = GameData.Player.transform.position;
            Quaternion playerRotation = GameData.Player.transform.rotation;

            Vector3 konigPosition = GameData.Konig != null ? GameData.Konig.transform.position : Vector3.zero;
            Quaternion konigRotation = GameData.Konig != null ? GameData.Konig.transform.rotation : Quaternion.identity;
        }

        private void OnGameInitialization()
        {
            foreach (var vehicle in GameData.Vehicles)
            {
                if (vehicle.gameObject.name == "Konig")
                {
                    GameData.Konig = vehicle.gameObject;
                    break;
                }
            }
        }
    }
}