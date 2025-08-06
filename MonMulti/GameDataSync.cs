// GameDataSync.cs
using LiteNetLib;
using LiteNetLib.Utils;
using System;
using UnityEngine;

namespace MonMulti
{
    public class GameDataSync
    {
        private NetManager netManager;
        private bool isServer;
        private string myPlayerId;
        
        // Game data tracking
        private float lastCash = -1f;
        private float lastTime = -1f;
        private float syncInterval = 2f; // Sync every 2 seconds
        private float lastSyncTime = 0f;
        
        public GameDataSync(NetManager netManager, bool isServer, string playerId)
        {
            this.netManager = netManager;
            this.isServer = isServer;
            this.myPlayerId = playerId;
        }
        
        public void Update()
        {
            // Only sync game data periodically to avoid flooding
            if (Time.time - lastSyncTime > syncInterval)
            {
                lastSyncTime = Time.time;
                SyncGameData();
            }
        }
        
        private void SyncGameData()
        {
            try
            {
                float currentCash = GetPlayerCash();
                float currentTime = GetGameTime();
                
                // Only sync if values have changed significantly
                bool cashChanged = Math.Abs(currentCash - lastCash) > 0.01f;
                bool timeChanged = Math.Abs(currentTime - lastTime) > 1f; // 1 second tolerance
                
                if (cashChanged || timeChanged)
                {
                    SendGameDataSync(currentCash, currentTime);
                    lastCash = currentCash;
                    lastTime = currentTime;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"GameDataSync error: {e.Message}");
            }
        }
        
        private float GetPlayerCash()
        {
            try
            {
                // Try to find the player's cash value through common Unity patterns
                GameObject player = GameObject.Find("FirstPersonWalker_Audio");
                if (player == null) return 0f;
                
                // Look for cash-related components
                var cashComponent = player.GetComponent<UnityEngine.MonoBehaviour>();
                if (cashComponent != null)
                {
                    // Use reflection to find cash field/property
                    var cashField = cashComponent.GetType().GetField("cash") ?? 
                                   cashComponent.GetType().GetField("Cash") ??
                                   cashComponent.GetType().GetField("money") ??
                                   cashComponent.GetType().GetField("Money");
                    
                    if (cashField != null && (cashField.FieldType == typeof(float) || cashField.FieldType == typeof(int)))
                    {
                        return Convert.ToSingle(cashField.GetValue(cashComponent));
                    }
                    
                    var cashProperty = cashComponent.GetType().GetProperty("cash") ?? 
                                      cashComponent.GetType().GetProperty("Cash") ??
                                      cashComponent.GetType().GetProperty("money") ??
                                      cashComponent.GetType().GetProperty("Money");
                    
                    if (cashProperty != null && (cashProperty.PropertyType == typeof(float) || cashProperty.PropertyType == typeof(int)))
                    {
                        return Convert.ToSingle(cashProperty.GetValue(cashComponent));
                    }
                }
                
                // Try to find global game manager
                GameObject gameManager = GameObject.Find("GameManager") ?? GameObject.Find("Game Manager");
                if (gameManager != null)
                {
                    var components = gameManager.GetComponents<UnityEngine.MonoBehaviour>();
                    foreach (var component in components)
                    {
                        var cashField = component.GetType().GetField("playerCash") ?? 
                                       component.GetType().GetField("cash") ?? 
                                       component.GetType().GetField("Cash");
                        if (cashField != null)
                        {
                            return Convert.ToSingle(cashField.GetValue(component));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Could not get player cash: {e.Message}");
            }
            
            return 0f;
        }
        
        private float GetGameTime()
        {
            try
            {
                // Try to find game time from various sources
                
                // Check for day/night cycle components
                GameObject timeManager = GameObject.Find("TimeManager") ?? 
                                        GameObject.Find("Time Manager") ??
                                        GameObject.Find("DayNightCycle") ??
                                        GameObject.Find("GameManager");
                
                if (timeManager != null)
                {
                    var components = timeManager.GetComponents<UnityEngine.MonoBehaviour>();
                    foreach (var component in components)
                    {
                        var timeField = component.GetType().GetField("gameTime") ?? 
                                       component.GetType().GetField("currentTime") ??
                                       component.GetType().GetField("time") ??
                                       component.GetType().GetField("dayTime");
                        
                        if (timeField != null && (timeField.FieldType == typeof(float) || timeField.FieldType == typeof(int)))
                        {
                            return Convert.ToSingle(timeField.GetValue(component));
                        }
                        
                        var timeProperty = component.GetType().GetProperty("gameTime") ?? 
                                          component.GetType().GetProperty("currentTime") ??
                                          component.GetType().GetProperty("time") ??
                                          component.GetType().GetProperty("dayTime");
                        
                        if (timeProperty != null && (timeProperty.PropertyType == typeof(float) || timeProperty.PropertyType == typeof(int)))
                        {
                            return Convert.ToSingle(timeProperty.GetValue(component));
                        }
                    }
                }
                
                // Fallback to Unity's time
                return Time.time;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Could not get game time: {e.Message}");
                return Time.time;
            }
        }
        
        private void SendGameDataSync(float cash, float gameTime)
        {
            NetDataWriter writer = new NetDataWriter();
            writer.Put("GAME_DATA_SYNC");
            writer.Put(myPlayerId);
            writer.Put(cash);
            writer.Put(gameTime);
            
            if (isServer)
            {
                foreach (var peer in netManager.ConnectedPeerList)
                    peer.Send(writer, DeliveryMethod.ReliableOrdered);
            }
            else if (netManager.FirstPeer != null)
            {
                netManager.FirstPeer.Send(writer, DeliveryMethod.ReliableOrdered);
            }
        }
        
        public void HandleGameDataSync(NetPacketReader reader, NetPeer sender)
        {
            try
            {
                string senderId = reader.GetString();
                float cash = reader.GetFloat();
                float gameTime = reader.GetFloat();
                
                // Don't process our own data
                if (senderId == myPlayerId)
                    return;
                
                // Apply the synchronized data if we're a client
                if (!isServer)
                {
                    ApplyGameData(cash, gameTime);
                }
                
                // Forward to other clients if we're the server
                if (isServer)
                {
                    NetDataWriter writer = new NetDataWriter();
                    writer.Put("GAME_DATA_SYNC");
                    writer.Put(senderId);
                    writer.Put(cash);
                    writer.Put(gameTime);
                    
                    foreach (var peer in netManager.ConnectedPeerList)
                    {
                        if (peer != sender)
                            peer.Send(writer, DeliveryMethod.ReliableOrdered);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"HandleGameDataSync error: {e.Message}");
            }
        }
        
        private void ApplyGameData(float cash, float gameTime)
        {
            try
            {
                // Apply cash value
                SetPlayerCash(cash);
                
                // Apply game time
                SetGameTime(gameTime);
            }
            catch (Exception e)
            {
                Debug.LogError($"ApplyGameData error: {e.Message}");
            }
        }
        
        private void SetPlayerCash(float cash)
        {
            try
            {
                GameObject player = GameObject.Find("FirstPersonWalker_Audio");
                if (player == null) return;
                
                var cashComponent = player.GetComponent<UnityEngine.MonoBehaviour>();
                if (cashComponent != null)
                {
                    var cashField = cashComponent.GetType().GetField("cash") ?? 
                                   cashComponent.GetType().GetField("Cash") ??
                                   cashComponent.GetType().GetField("money") ??
                                   cashComponent.GetType().GetField("Money");
                    
                    if (cashField != null)
                    {
                        if (cashField.FieldType == typeof(float))
                            cashField.SetValue(cashComponent, cash);
                        else if (cashField.FieldType == typeof(int))
                            cashField.SetValue(cashComponent, (int)cash);
                    }
                    
                    var cashProperty = cashComponent.GetType().GetProperty("cash") ?? 
                                      cashComponent.GetType().GetProperty("Cash") ??
                                      cashComponent.GetType().GetProperty("money") ??
                                      cashComponent.GetType().GetProperty("Money");
                    
                    if (cashProperty != null && cashProperty.CanWrite)
                    {
                        if (cashProperty.PropertyType == typeof(float))
                            cashProperty.SetValue(cashComponent, cash);
                        else if (cashProperty.PropertyType == typeof(int))
                            cashProperty.SetValue(cashComponent, (int)cash);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Could not set player cash: {e.Message}");
            }
        }
        
        private void SetGameTime(float gameTime)
        {
            try
            {
                GameObject timeManager = GameObject.Find("TimeManager") ?? 
                                        GameObject.Find("Time Manager") ??
                                        GameObject.Find("DayNightCycle") ??
                                        GameObject.Find("GameManager");
                
                if (timeManager != null)
                {
                    var components = timeManager.GetComponents<UnityEngine.MonoBehaviour>();
                    foreach (var component in components)
                    {
                        var timeField = component.GetType().GetField("gameTime") ?? 
                                       component.GetType().GetField("currentTime") ??
                                       component.GetType().GetField("time") ??
                                       component.GetType().GetField("dayTime");
                        
                        if (timeField != null)
                        {
                            if (timeField.FieldType == typeof(float))
                                timeField.SetValue(component, gameTime);
                            else if (timeField.FieldType == typeof(int))
                                timeField.SetValue(component, (int)gameTime);
                        }
                        
                        var timeProperty = component.GetType().GetProperty("gameTime") ?? 
                                          component.GetType().GetProperty("currentTime") ??
                                          component.GetType().GetProperty("time") ??
                                          component.GetType().GetProperty("dayTime");
                        
                        if (timeProperty != null && timeProperty.CanWrite)
                        {
                            if (timeProperty.PropertyType == typeof(float))
                                timeProperty.SetValue(component, gameTime);
                            else if (timeProperty.PropertyType == typeof(int))
                                timeProperty.SetValue(component, (int)gameTime);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Could not set game time: {e.Message}");
            }
        }
    }
}