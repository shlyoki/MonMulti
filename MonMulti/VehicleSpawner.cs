// VehicleSpawner.cs
using LiteNetLib;
using LiteNetLib.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace MonMulti
{
    public class VehicleSpawner
    {
        private NetManager netManager;
        private bool isServer;
        private string myPlayerId;
        
        // Vehicle tracking
        private Dictionary<string, GameObject> spawnedVehicles = new Dictionary<string, GameObject>();
        private Dictionary<string, VehicleSpawnData> vehicleSpawnData = new Dictionary<string, VehicleSpawnData>();
        
        public struct VehicleSpawnData
        {
            public Vector3 position;
            public Quaternion rotation;
            public string vehicleType;
            public string spawnerId;
            public bool isActive;
        }
        
        public VehicleSpawner(NetManager netManager, bool isServer, string playerId)
        {
            this.netManager = netManager;
            this.isServer = isServer;
            this.myPlayerId = playerId;
        }
        
        public void RegisterVehicle(string vehicleId, GameObject vehicle)
        {
            if (vehicle == null) return;
            
            spawnedVehicles[vehicleId] = vehicle;
            
            // Record spawn data
            vehicleSpawnData[vehicleId] = new VehicleSpawnData
            {
                position = vehicle.transform.position,
                rotation = vehicle.transform.rotation,
                vehicleType = GetVehicleType(vehicle),
                spawnerId = myPlayerId,
                isActive = true
            };
            
            // Notify other players about this vehicle
            if (netManager != null)
            {
                SendVehicleSpawn(vehicleId, vehicleSpawnData[vehicleId]);
            }
        }
        
        public void UnregisterVehicle(string vehicleId)
        {
            if (spawnedVehicles.ContainsKey(vehicleId))
            {
                spawnedVehicles.Remove(vehicleId);
                
                if (vehicleSpawnData.ContainsKey(vehicleId))
                {
                    var spawnData = vehicleSpawnData[vehicleId];
                    spawnData.isActive = false;
                    vehicleSpawnData[vehicleId] = spawnData;
                    
                    // Notify other players about vehicle removal
                    SendVehicleDespawn(vehicleId);
                }
            }
        }
        
        public GameObject GetVehicle(string vehicleId)
        {
            spawnedVehicles.TryGetValue(vehicleId, out GameObject vehicle);
            return vehicle;
        }
        
        public bool IsVehicleActive(string vehicleId)
        {
            if (vehicleSpawnData.TryGetValue(vehicleId, out VehicleSpawnData data))
            {
                return data.isActive;
            }
            return false;
        }
        
        private string GetVehicleType(GameObject vehicle)
        {
            if (vehicle == null) return "Unknown";
            
            string name = vehicle.name.ToUpper();
            
            if (name.Contains("KONIG")) return "KONIG";
            if (name.Contains("OLTRUCK")) return "OLTRUCK";
            if (name.Contains("SMOLLATV") || name.Contains("ATV")) return "SMOLLATV";
            if (name.Contains("BUGGY")) return "BUGGY";
            
            return name;
        }
        
        private void SendVehicleSpawn(string vehicleId, VehicleSpawnData spawnData)
        {
            NetDataWriter writer = new NetDataWriter();
            writer.Put("VEHICLE_SPAWN");
            writer.Put(vehicleId);
            writer.Put(spawnData.vehicleType);
            writer.Put(spawnData.spawnerId);
            writer.Put(spawnData.position.x);
            writer.Put(spawnData.position.y);
            writer.Put(spawnData.position.z);
            writer.Put(spawnData.rotation.x);
            writer.Put(spawnData.rotation.y);
            writer.Put(spawnData.rotation.z);
            writer.Put(spawnData.rotation.w);
            writer.Put(spawnData.isActive);
            
            SendToAllPeers(writer);
        }
        
        private void SendVehicleDespawn(string vehicleId)
        {
            NetDataWriter writer = new NetDataWriter();
            writer.Put("VEHICLE_DESPAWN");
            writer.Put(vehicleId);
            
            SendToAllPeers(writer);
        }
        
        public void HandleVehicleSpawn(NetPacketReader reader, NetPeer sender)
        {
            try
            {
                string vehicleId = reader.GetString();
                string vehicleType = reader.GetString();
                string spawnerId = reader.GetString();
                Vector3 position = new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
                Quaternion rotation = new Quaternion(reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
                bool isActive = reader.GetBool();
                
                // Don't spawn our own vehicles
                if (spawnerId == myPlayerId)
                    return;
                
                // Create or update vehicle spawn data
                vehicleSpawnData[vehicleId] = new VehicleSpawnData
                {
                    position = position,
                    rotation = rotation,
                    vehicleType = vehicleType,
                    spawnerId = spawnerId,
                    isActive = isActive
                };
                
                // Try to find existing vehicle in scene
                GameObject existingVehicle = FindVehicleInScene(vehicleType);
                if (existingVehicle != null)
                {
                    spawnedVehicles[vehicleId] = existingVehicle;
                    
                    // Update position if it's not currently owned by someone else
                    if (!IsVehicleCurrentlyDriven(existingVehicle))
                    {
                        existingVehicle.transform.position = position;
                        existingVehicle.transform.rotation = rotation;
                    }
                    
                    Debug.Log($"Registered existing vehicle {vehicleType} with ID {vehicleId}");
                }
                else
                {
                    Debug.LogWarning($"Could not find vehicle of type {vehicleType} in scene for spawn");
                }
                
                // Forward to other clients if we're the server
                if (isServer)
                {
                    NetDataWriter writer = new NetDataWriter();
                    writer.Put("VEHICLE_SPAWN");
                    writer.Put(vehicleId);
                    writer.Put(vehicleType);
                    writer.Put(spawnerId);
                    writer.Put(position.x);
                    writer.Put(position.y);
                    writer.Put(position.z);
                    writer.Put(rotation.x);
                    writer.Put(rotation.y);
                    writer.Put(rotation.z);
                    writer.Put(rotation.w);
                    writer.Put(isActive);
                    
                    foreach (var peer in netManager.ConnectedPeerList)
                    {
                        if (peer != sender)
                            peer.Send(writer, DeliveryMethod.ReliableOrdered);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error handling vehicle spawn: {e.Message}");
            }
        }
        
        public void HandleVehicleDespawn(NetPacketReader reader, NetPeer sender)
        {
            try
            {
                string vehicleId = reader.GetString();
                
                if (vehicleSpawnData.ContainsKey(vehicleId))
                {
                    var spawnData = vehicleSpawnData[vehicleId];
                    spawnData.isActive = false;
                    vehicleSpawnData[vehicleId] = spawnData;
                }
                
                // Note: We don't actually destroy the vehicle GameObject since 
                // vehicles in Mon Bazou are part of the scene, we just mark them as inactive
                
                Debug.Log($"Vehicle {vehicleId} despawned");
                
                // Forward to other clients if we're the server
                if (isServer)
                {
                    NetDataWriter writer = new NetDataWriter();
                    writer.Put("VEHICLE_DESPAWN");
                    writer.Put(vehicleId);
                    
                    foreach (var peer in netManager.ConnectedPeerList)
                    {
                        if (peer != sender)
                            peer.Send(writer, DeliveryMethod.ReliableOrdered);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error handling vehicle despawn: {e.Message}");
            }
        }
        
        private GameObject FindVehicleInScene(string vehicleType)
        {
            // Common vehicle names in Mon Bazou
            switch (vehicleType.ToUpper())
            {
                case "KONIG":
                    return GameObject.Find("Konig");
                case "OLTRUCK":
                    return GameObject.Find("OlTruck");
                case "SMOLLATV":
                    return GameObject.Find("SmollATV");
                case "BUGGY":
                    return GameObject.Find("Buggy");
                default:
                    // Try to find by partial name match
                    GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
                    foreach (GameObject obj in allObjects)
                    {
                        if (obj.name.ToUpper().Contains(vehicleType.ToUpper()))
                        {
                            return obj;
                        }
                    }
                    break;
            }
            
            return null;
        }
        
        private bool IsVehicleCurrentlyDriven(GameObject vehicle)
        {
            // Check if the player object is a child of the vehicle
            GameObject player = GameObject.Find("FirstPersonWalker_Audio");
            if (player != null && player.transform.parent == vehicle.transform)
            {
                return true;
            }
            
            // Check if vehicle has any rigidbody with significant velocity
            Rigidbody rb = vehicle.GetComponent<Rigidbody>();
            if (rb != null && rb.velocity.magnitude > 1f)
            {
                return true;
            }
            
            return false;
        }
        
        public void SyncAllVehicles()
        {
            // Send current state of all vehicles to newly connected clients
            foreach (var kvp in vehicleSpawnData)
            {
                if (kvp.Value.isActive)
                {
                    SendVehicleSpawn(kvp.Key, kvp.Value);
                }
            }
        }
        
        public void UpdateVehiclePositions()
        {
            // Update spawn data for all registered vehicles
            foreach (var kvp in spawnedVehicles)
            {
                if (kvp.Value != null && vehicleSpawnData.ContainsKey(kvp.Key))
                {
                    var spawnData = vehicleSpawnData[kvp.Key];
                    spawnData.position = kvp.Value.transform.position;
                    spawnData.rotation = kvp.Value.transform.rotation;
                    vehicleSpawnData[kvp.Key] = spawnData;
                }
            }
        }
        
        private void SendToAllPeers(NetDataWriter writer)
        {
            if (netManager == null) return;
            
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
    }
}