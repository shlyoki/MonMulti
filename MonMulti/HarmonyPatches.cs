using HarmonyLib;
using UnityEngine;

namespace MonMulti
{
    [HarmonyPatch(typeof(Gameplay), "Awake")]
    public static class GameplayAwakePatch
    {
        [HarmonyPostfix]
        public static void Postfix(Gameplay __instance)
        {
            var player = __instance.Player;

            //Get player and set GameData,
            GameData.Player = player;
            GameData.IsGameInitialized = true; //Mark the game as initialized
        }
    }

    /*[HarmonyPatch(typeof(GameManager), "GetPlayerData")]
    public static class GetPlayerDataPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref PlayerData __result)
        {
            if (__result != null)
            {
                int cash = __result.intCash;
                Debug.Log($"[MonMulti] Player Cash: {cash}");s
                GameData.PlayerCash = cash;
            }
        }
    }*/

    [HarmonyPatch(typeof(NWH.Vehicle), "Awake")]
    public class VehicleAwakePatch
    {
        static void Postfix(NWH.Vehicle __instance)
        {
            if (!GameData.vehicles.Contains(__instance))
            {
                GameData.vehicles.Add(__instance);
                Debug.Log($"[MonMulti] Added vehicle '{__instance.gameObject.name}' to GameData. Total vehicles: {GameData.vehicles.Count}");
            }
        }
    }
}