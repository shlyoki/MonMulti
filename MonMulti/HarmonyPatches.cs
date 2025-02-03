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

            GameData.Player = player;
            GameData.IsGameInitialized = true;
        }
    }

    [HarmonyPatch(typeof(NWH.Vehicle), "Awake")]
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
    }

    [HarmonyPatch(typeof(PlayerData), MethodType.Constructor)]
    public static class PlayerDataPatch
    {
        [HarmonyPostfix]
        public static void Postfix(PlayerData __instance)
        {
            GameData.Cash = __instance.intCash;
            GameData.Minute = __instance.Minute;
            GameData.Hour = __instance.Hour;
            GameData.DayOfTheWeek = __instance.DayOfTheWeek;

            Debug.Log($"[MonMulti] PlayerData captured! Cash: {GameData.Cash}, Time: {GameData.Hour}:{GameData.Minute}");
        }
    }
}
