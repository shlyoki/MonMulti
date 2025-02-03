using UnityEngine;
using System.Collections.Generic;

namespace MonMulti
{
    public static class GameData
    {
        public static PlayerInteractable Player { get; set; }
        public static bool IsGameInitialized { get; set; }
        public static int PlayerCash { get; set; } = 0;
        public static List<NWH.Vehicle> Vehicles { get; set; } = new List<NWH.Vehicle>();
        public static GameObject KonigVehicle { get; set; }
    }
}
