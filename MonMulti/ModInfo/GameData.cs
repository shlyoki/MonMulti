using System.Collections.Generic;
using UnityEngine;

namespace MonMulti
{
    public static class GameData
    {
        public static PlayerInteractable Player { get; set; }
        public static bool IsGameInitialized { get; set; }
        public static int PlayerCash { get; set; } = 0;
        public static List<NWH.Vehicle> vehicles { get; private set; } = new List<NWH.Vehicle>();
    }
}
