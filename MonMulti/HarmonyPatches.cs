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
                Debug.Log($"[MonMulti] Player Cash: {cash}");
                GameData.PlayerCash = cash;
            }
        }
    }*/

    [HarmonyPatch(typeof(NWH.Vehicle), "Awake")]
    public class VehicleAwakePatch
    {
        static void Postfix(NWH.Vehicle __instance)
        {
            Debug.Log("Vehicle Awake() has been patched.");

            if (GameData.PlayerVehicle == null)
            {
                GameData.PlayerVehicle = __instance;
                Debug.Log("[MonMulti] Player vehicle has been stored in GameData.");
            }
        }
    }
}