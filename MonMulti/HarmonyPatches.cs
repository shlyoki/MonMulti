using HarmonyLib;
using UnityEngine;
using System;

namespace MonMulti
{
    [HarmonyPatch(typeof(Gameplay), "Awake")]
    public static class GameplayAwakePatch
    {
        [HarmonyPostfix]
        public static void Postfix(Gameplay __instance)
        {
            GameData.Player = __instance.Player;
            GameData.IsGameInitialized = true;
        }
    }

    [HarmonyPatch(typeof(Gameplay), "PauseGame")]
    public static class PauseGamePatch
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            return false;
        }
    }

    /*[HarmonyPatch(typeof(NWH.Vehicle), "Awake")]
    public class VehicleAwakePatch
    {
        static void Postfix(NWH.Vehicle __instance)
        {
            if (!GameData.Vehicles.Contains(__instance))
            {
                GameData.Vehicles.Add(__instance);
                Debug.Log($"[MonMulti] Added vehicle '{__instance.gameObject.name}' to GameData. Total vehicles: {GameData.Vehicles.Count}");
            }
        }
    }*/
}
