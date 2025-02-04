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

    /*[HarmonyPatch(typeof(PlayerData))]
    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPatch(new Type[] {
        typeof(int), typeof(House_Location), typeof(Scrapyard), typeof(SugarShack),
        typeof(HomeGenerator), typeof(Store_TowingService), typeof(Store_Gilles),
        typeof(Store_FederationSyrup), typeof(SugarShack_SlabBuild), typeof(SugarShack_TubingPost[]),
        typeof(SugarShack_Tubing[]), typeof(Speedway), typeof(Store_PostOffice), typeof(Store_HydroSaintClin),
        typeof(Home), typeof(ComputersData[])
    })]
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
    }*/
}
