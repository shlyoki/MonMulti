using UnityEngine;

namespace MonMulti
{
    public static class GameData
    {
        public static PlayerInteractable Player { get; set; }
        public static bool IsGameInitialized { get; set; }
        public static int PlayerCash { get; set; } = 0;
        public static NWH.Vehicle PlayerVehicle { get; set; }
    }
}
