using System;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

public static class SaveManager
{
    private static readonly string SavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Low", "Santa Goat", "Mon Bazou", "Multiplayer");
    private static readonly string SaveFile = Path.Combine(SavePath, "saveData.json");

    static SaveManager()
    {
        if (!Directory.Exists(SavePath))
        {
            Directory.CreateDirectory(SavePath);
        }
    }

    public static void WriteData(string sector, object data)
    {
        Dictionary<string, object> saveData = new Dictionary<string, object>();

        if (File.Exists(SaveFile))
        {
            string existingJson = File.ReadAllText(SaveFile);
            saveData = JsonConvert.DeserializeObject<Dictionary<string, object>>(existingJson) ?? new Dictionary<string, object>();
        }

        saveData[sector] = data;
        File.WriteAllText(SaveFile, JsonConvert.SerializeObject(saveData, Formatting.Indented));
    }

    public static void WriteData(object data)
    {
        if (File.Exists(SaveFile))
        {
            string existingJson = File.ReadAllText(SaveFile);
            var saveData = JsonConvert.DeserializeObject<Dictionary<string, object>>(existingJson) ?? new Dictionary<string, object>();

            foreach (var entry in (Dictionary<string, object>)data)
            {
                saveData[entry.Key] = entry.Value;
            }

            File.WriteAllText(SaveFile, JsonConvert.SerializeObject(saveData, Formatting.Indented));
        }
        else
        {
            File.WriteAllText(SaveFile, JsonConvert.SerializeObject(data, Formatting.Indented));
        }
    }

    public static Dictionary<string, object> ReadData()
    {
        if (File.Exists(SaveFile))
        {
            string jsonData = File.ReadAllText(SaveFile);
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonData) ?? new Dictionary<string, object>();
        }
        return new Dictionary<string, object>();
    }

    public static T ReadData<T>(string sector)
    {
        var saveData = ReadData();
        if (saveData.ContainsKey(sector))
        {
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(saveData[sector]));
        }
        return default;
    }
}
