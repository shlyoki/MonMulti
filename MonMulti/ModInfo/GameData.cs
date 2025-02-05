using UnityEngine;
using System.Collections.Generic;

namespace MonMulti
{
    public static class GameData
    {
        public static PlayerInteractable Player { get; set; }
        public static bool IsGameInitialized { get; set; }

        // Vehicles
        public static List<NWH.Vehicle> Vehicles { get; set; } = new List<NWH.Vehicle>();
        public static GameObject Konig { get; set; }

        // Player Data
        public static int Cash { get; set; } = 0;
        public static int Minute { get; set; } = 0;
        public static int Hour { get; set; } = 0;
        public static int DayOfTheWeek { get; set; } = 0;
    }
}
