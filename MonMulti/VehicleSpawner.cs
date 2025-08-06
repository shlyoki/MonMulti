using LiteNetLib;
using LiteNetLib.Utils;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MonMulti
{
    public class VehicleSpawner
    {
        private NetManager netManager;
        private bool isServer;
        private string myPlayerId;
        private Dictionary<string, VehicleInfo> registeredVehicles = new Dictionary<string, VehicleInfo>();
        private Dictionary<string, bool> vehicleSpawnStates = new Dictionary<string, bool>();

        private class VehicleInfo
        {
            public string Id;
            public GameObject GameObject;
            public Vector3 SpawnPosition;
            public Quaternion SpawnRotation;
            public bool IsActive;
            public string OwnerId;
        }

        public VehicleSpawner(NetManager netManager, bool isServer, string playerId)
        {
            this.netManager = netManager;
            this.isServer = isServer;
            this.myPlayerId = playerId;
        }

        public void Initialize()
        {
            if (SceneManager.GetActiveScene().name == "Master")
            {
                DiscoverAndRegisterVehicles();
            }
        }

        private void DiscoverAndRegisterVehicles()
        {
            // Auto-discover vehicles in the scene
            RegisterVehicle("KONIG", "Konig");
            RegisterVehicle("OLTRUCK", "OlTruck");
            RegisterVehicle("SMOLLATV", "SmollATV");
            RegisterVehicle("BUGGY", "Buggy");

            Debug.Log($"VehicleSpawner: Registered {registeredVehicles.Count} vehicles");
        }

        private void RegisterVehicle(string vehicleId, string gameObjectName)
        {
            GameObject vehicleObj = GameObject.Find(gameObjectName);
            if (vehicleObj != null)
            {
                VehicleInfo info = new VehicleInfo
                {
                    Id = vehicleId,
                    GameObject = vehicleObj,
                    SpawnPosition = vehicleObj.transform.position,
                    SpawnRotation = vehicleObj.transform.rotation,
                    IsActive = vehicleObj.activeInHierarchy,
                    OwnerId = null
                };

                registeredVehicles[vehicleId] = info;
                vehicleSpawnStates[vehicleId] = info.IsActive;

                Debug.Log($"VehicleSpawner: Registered vehicle {vehicleId} at {info.SpawnPosition}");

                // Sync initial state if server
                if (isServer)
                {
                    SendVehicleSpawn(vehicleId, info.IsActive);
                }
            }
            else
            {
                Debug.LogWarning($"VehicleSpawner: Could not find vehicle GameObject '{gameObjectName}'");
            }
        }

        public void SpawnVehicle(string vehicleId, bool isActive = true)
        {
            if (!registeredVehicles.ContainsKey(vehicleId)) return;

            var vehicle = registeredVehicles[vehicleId];
            vehicle.IsActive = isActive;
            vehicleSpawnStates[vehicleId] = isActive;

            if (vehicle.GameObject != null)
            {
                vehicle.GameObject.SetActive(isActive);
                if (isActive)
                {
                    // Reset to spawn position
                    vehicle.GameObject.transform.position = vehicle.SpawnPosition;
                    vehicle.GameObject.transform.rotation = vehicle.SpawnRotation;
                    
                    // Reset physics if available
                    var rb = vehicle.GameObject.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.velocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                }
            }

            SendVehicleSpawn(vehicleId, isActive);
        }

        public void DespawnVehicle(string vehicleId)
        {
            SpawnVehicle(vehicleId, false);
        }

        public bool IsVehicleSpawned(string vehicleId)
        {
            return vehicleSpawnStates.ContainsKey(vehicleId) && vehicleSpawnStates[vehicleId];
        }

        public GameObject GetVehicle(string vehicleId)
        {
            if (registeredVehicles.ContainsKey(vehicleId))
                return registeredVehicles[vehicleId].GameObject;
            return null;
        }

        public List<string> GetRegisteredVehicleIds()
        {
            return new List<string>(registeredVehicles.Keys);
        }

        private void SendVehicleSpawn(string vehicleId, bool isActive)
        {
            if (netManager == null) return;

            NetDataWriter writer = new NetDataWriter();
            if (isActive)
            {
                writer.Put("VEHICLE_SPAWN");
                writer.Put(vehicleId);
                
                var vehicle = registeredVehicles[vehicleId];
                writer.Put(vehicle.SpawnPosition.x);
                writer.Put(vehicle.SpawnPosition.y);
                writer.Put(vehicle.SpawnPosition.z);
                writer.Put(vehicle.SpawnRotation.x);
                writer.Put(vehicle.SpawnRotation.y);
                writer.Put(vehicle.SpawnRotation.z);
                writer.Put(vehicle.SpawnRotation.w);
            }
            else
            {
                writer.Put("VEHICLE_DESPAWN");
                writer.Put(vehicleId);
            }

            if (isServer)
            {
                foreach (var peer in netManager.ConnectedPeerList)
                    peer.Send(writer, DeliveryMethod.ReliableOrdered);
            }
            else
            {
                var serverPeer = GetServerPeer();
                serverPeer?.Send(writer, DeliveryMethod.ReliableOrdered);
            }
        }

        public void HandleVehicleSpawn(NetPacketReader reader, NetPeer peer)
        {
            string vehicleId = reader.GetString();
            Vector3 spawnPosition = new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
            Quaternion spawnRotation = new Quaternion(reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat());

            if (!registeredVehicles.ContainsKey(vehicleId))
            {
                Debug.LogWarning($"VehicleSpawner: Received spawn for unregistered vehicle: {vehicleId}");
                return;
            }

            var vehicle = registeredVehicles[vehicleId];
            vehicle.IsActive = true;
            vehicle.SpawnPosition = spawnPosition;
            vehicle.SpawnRotation = spawnRotation;
            vehicleSpawnStates[vehicleId] = true;

            if (vehicle.GameObject != null)
            {
                vehicle.GameObject.SetActive(true);
                vehicle.GameObject.transform.position = spawnPosition;
                vehicle.GameObject.transform.rotation = spawnRotation;

                // Reset physics
                var rb = vehicle.GameObject.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }

            Debug.Log($"VehicleSpawner: Spawned vehicle {vehicleId} at {spawnPosition}");

            // Forward to other clients if server
            if (isServer)
            {
                NetDataWriter writer = new NetDataWriter();
                writer.Put("VEHICLE_SPAWN");
                writer.Put(vehicleId);
                writer.Put(spawnPosition.x); writer.Put(spawnPosition.y); writer.Put(spawnPosition.z);
                writer.Put(spawnRotation.x); writer.Put(spawnRotation.y); writer.Put(spawnRotation.z); writer.Put(spawnRotation.w);

                foreach (var p in netManager.ConnectedPeerList)
                    if (p != peer) p.Send(writer, DeliveryMethod.ReliableOrdered);
            }
        }

        public void HandleVehicleDespawn(NetPacketReader reader, NetPeer peer)
        {
            string vehicleId = reader.GetString();

            if (!registeredVehicles.ContainsKey(vehicleId))
            {
                Debug.LogWarning($"VehicleSpawner: Received despawn for unregistered vehicle: {vehicleId}");
                return;
            }

            var vehicle = registeredVehicles[vehicleId];
            vehicle.IsActive = false;
            vehicleSpawnStates[vehicleId] = false;

            if (vehicle.GameObject != null)
            {
                vehicle.GameObject.SetActive(false);
            }

            Debug.Log($"VehicleSpawner: Despawned vehicle {vehicleId}");

            // Forward to other clients if server
            if (isServer)
            {
                NetDataWriter writer = new NetDataWriter();
                writer.Put("VEHICLE_DESPAWN");
                writer.Put(vehicleId);

                foreach (var p in netManager.ConnectedPeerList)
                    if (p != peer) p.Send(writer, DeliveryMethod.ReliableOrdered);
            }
        }

        public void SynchronizeVehicleStates(NetPeer newPeer)
        {
            if (!isServer) return;

            // Send current state of all vehicles to newly connected peer
            foreach (var vehicle in registeredVehicles.Values)
            {
                NetDataWriter writer = new NetDataWriter();
                if (vehicle.IsActive)
                {
                    writer.Put("VEHICLE_SPAWN");
                    writer.Put(vehicle.Id);
                    writer.Put(vehicle.SpawnPosition.x);
                    writer.Put(vehicle.SpawnPosition.y);
                    writer.Put(vehicle.SpawnPosition.z);
                    writer.Put(vehicle.SpawnRotation.x);
                    writer.Put(vehicle.SpawnRotation.y);
                    writer.Put(vehicle.SpawnRotation.z);
                    writer.Put(vehicle.SpawnRotation.w);
                }
                else
                {
                    writer.Put("VEHICLE_DESPAWN");
                    writer.Put(vehicle.Id);
                }

                newPeer.Send(writer, DeliveryMethod.ReliableOrdered);
            }
        }

        public void Update()
        {
            // Check for vehicles that have been manually activated/deactivated
            foreach (var kvp in registeredVehicles)
            {
                var vehicleId = kvp.Key;
                var vehicle = kvp.Value;

                if (vehicle.GameObject != null)
                {
                    bool currentActive = vehicle.GameObject.activeInHierarchy;
                    bool lastKnownActive = vehicleSpawnStates[vehicleId];

                    if (currentActive != lastKnownActive)
                    {
                        vehicleSpawnStates[vehicleId] = currentActive;
                        vehicle.IsActive = currentActive;

                        if (isServer)
                        {
                            SendVehicleSpawn(vehicleId, currentActive);
                        }
                    }
                }
            }
        }

        private NetPeer GetServerPeer()
        {
            if (netManager?.ConnectedPeerList?.Count > 0)
                return netManager.ConnectedPeerList[0];
            return null;
        }
    }
}