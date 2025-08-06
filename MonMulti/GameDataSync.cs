using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MonMulti
{
    public class GameDataSync
    {
        private NetManager netManager;
        private bool isServer;
        private string myPlayerId;
        private float lastSyncTime = 0f;
        private float syncInterval = 2f; // Sync every 2 seconds
        private Dictionary<string, object> lastSentData = new Dictionary<string, object>();

        public GameDataSync(NetManager netManager, bool isServer, string playerId)
        {
            this.netManager = netManager;
            this.isServer = isServer;
            this.myPlayerId = playerId;
        }

        public void Update()
        {
            if (!isServer || Time.time - lastSyncTime < syncInterval) return;

            lastSyncTime = Time.time;
            SyncGameData();
        }

        private void SyncGameData()
        {
            try
            {
                // Sync cash/money data
                var cashData = GetCashData();
                if (cashData.HasValue && (!lastSentData.ContainsKey("cash") || !lastSentData["cash"].Equals(cashData.Value)))
                {
                    SendGameData("cash", cashData.Value);
                    lastSentData["cash"] = cashData.Value;
                }

                // Sync game time
                var timeData = GetGameTime();
                if (timeData.HasValue && (!lastSentData.ContainsKey("time") || !lastSentData["time"].Equals(timeData.Value)))
                {
                    SendGameData("time", timeData.Value);
                    lastSentData["time"] = timeData.Value;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"GameDataSync error: {ex.Message}");
            }
        }

        private float? GetCashData()
        {
            try
            {
                // Try to find cash/money in various common game objects
                var gameplayObjects = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
                
                foreach (var obj in gameplayObjects)
                {
                    var type = obj.GetType();
                    
                    // Look for common money/cash field names
                    var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    foreach (var field in fields)
                    {
                        if (field.FieldType == typeof(float) || field.FieldType == typeof(int))
                        {
                            var fieldName = field.Name.ToLower();
                            if (fieldName.Contains("cash") || fieldName.Contains("money") || fieldName.Contains("dollar"))
                            {
                                var value = field.GetValue(obj);
                                return Convert.ToSingle(value);
                            }
                        }
                    }

                    // Look for properties as well
                    var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    foreach (var property in properties)
                    {
                        if ((property.PropertyType == typeof(float) || property.PropertyType == typeof(int)) && property.CanRead)
                        {
                            var propertyName = property.Name.ToLower();
                            if (propertyName.Contains("cash") || propertyName.Contains("money") || propertyName.Contains("dollar"))
                            {
                                var value = property.GetValue(obj);
                                return Convert.ToSingle(value);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to get cash data: {ex.Message}");
            }
            return null;
        }

        private float? GetGameTime()
        {
            try
            {
                // Try to find game time objects
                var gameplayObjects = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
                
                foreach (var obj in gameplayObjects)
                {
                    var type = obj.GetType();
                    
                    // Look for time-related fields
                    var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    foreach (var field in fields)
                    {
                        if (field.FieldType == typeof(float))
                        {
                            var fieldName = field.Name.ToLower();
                            if (fieldName.Contains("time") && (fieldName.Contains("game") || fieldName.Contains("day") || fieldName.Contains("current")))
                            {
                                var value = field.GetValue(obj);
                                return Convert.ToSingle(value);
                            }
                        }
                    }

                    // Look for properties
                    var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    foreach (var property in properties)
                    {
                        if (property.PropertyType == typeof(float) && property.CanRead)
                        {
                            var propertyName = property.Name.ToLower();
                            if (propertyName.Contains("time") && (propertyName.Contains("game") || propertyName.Contains("day") || propertyName.Contains("current")))
                            {
                                var value = property.GetValue(obj);
                                return Convert.ToSingle(value);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to get time data: {ex.Message}");
            }
            return null;
        }

        private void SendGameData(string dataType, float value)
        {
            NetDataWriter writer = new NetDataWriter();
            writer.Put("GAME_DATA_SYNC");
            writer.Put(dataType);
            writer.Put(value);

            foreach (var peer in netManager.ConnectedPeerList)
                peer.Send(writer, DeliveryMethod.Reliable);
        }

        public void HandleGameDataSync(NetPacketReader reader, NetPeer peer)
        {
            try
            {
                string dataType = reader.GetString();
                float value = reader.GetFloat();

                ApplyGameData(dataType, value);

                // Forward to other clients if server
                if (isServer)
                {
                    NetDataWriter writer = new NetDataWriter();
                    writer.Put("GAME_DATA_SYNC");
                    writer.Put(dataType);
                    writer.Put(value);

                    foreach (var p in netManager.ConnectedPeerList)
                        if (p != peer) p.Send(writer, DeliveryMethod.Reliable);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"HandleGameDataSync error: {ex.Message}");
            }
        }

        private void ApplyGameData(string dataType, float value)
        {
            try
            {
                if (dataType == "cash")
                {
                    ApplyCashData(value);
                }
                else if (dataType == "time")
                {
                    ApplyTimeData(value);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to apply {dataType} data: {ex.Message}");
            }
        }

        private void ApplyCashData(float value)
        {
            var gameplayObjects = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
            
            foreach (var obj in gameplayObjects)
            {
                var type = obj.GetType();
                
                // Look for cash/money fields to update
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var field in fields)
                {
                    if (field.FieldType == typeof(float) || field.FieldType == typeof(int))
                    {
                        var fieldName = field.Name.ToLower();
                        if (fieldName.Contains("cash") || fieldName.Contains("money") || fieldName.Contains("dollar"))
                        {
                            if (field.FieldType == typeof(float))
                                field.SetValue(obj, value);
                            else
                                field.SetValue(obj, (int)value);
                            return;
                        }
                    }
                }

                // Look for properties
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var property in properties)
                {
                    if ((property.PropertyType == typeof(float) || property.PropertyType == typeof(int)) && property.CanWrite)
                    {
                        var propertyName = property.Name.ToLower();
                        if (propertyName.Contains("cash") || propertyName.Contains("money") || propertyName.Contains("dollar"))
                        {
                            if (property.PropertyType == typeof(float))
                                property.SetValue(obj, value);
                            else
                                property.SetValue(obj, (int)value);
                            return;
                        }
                    }
                }
            }
        }

        private void ApplyTimeData(float value)
        {
            var gameplayObjects = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
            
            foreach (var obj in gameplayObjects)
            {
                var type = obj.GetType();
                
                // Look for time-related fields to update
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var field in fields)
                {
                    if (field.FieldType == typeof(float))
                    {
                        var fieldName = field.Name.ToLower();
                        if (fieldName.Contains("time") && (fieldName.Contains("game") || fieldName.Contains("day") || fieldName.Contains("current")))
                        {
                            field.SetValue(obj, value);
                            return;
                        }
                    }
                }

                // Look for properties
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var property in properties)
                {
                    if (property.PropertyType == typeof(float) && property.CanWrite)
                    {
                        var propertyName = property.Name.ToLower();
                        if (propertyName.Contains("time") && (propertyName.Contains("game") || propertyName.Contains("day") || propertyName.Contains("current")))
                        {
                            property.SetValue(obj, value);
                            return;
                        }
                    }
                }
            }
        }
    }
}