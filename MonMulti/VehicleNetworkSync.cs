// VehicleNetworkSync.cs
using LiteNetLib;
using LiteNetLib.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace MonMulti
{
    public class VehicleNetworkSync
    {
        private NetManager netManager;
        private bool isServer;
        private string myPlayerId;
        private HashSet<string> ownedVehicles = new HashSet<string>();
        
        // Vehicle references
        private Dictionary<string, GameObject> vehicles = new Dictionary<string, GameObject>();
        
        public VehicleNetworkSync(NetManager netManager, bool isServer, string playerId)
        {
            this.netManager = netManager;
            this.isServer = isServer;
            this.myPlayerId = playerId;
        }
        
        public void RegisterVehicle(string vehicleId, GameObject vehicleObject)
        {
            if (vehicleObject != null)
            {
                vehicles[vehicleId] = vehicleObject;
            }
        }
        
        public void HandleVehicleEntry(GameObject playerObject)
        {
            string newVehicle = null;
            
            // Check which vehicle the player is currently in
            foreach (var kvp in vehicles)
            {
                if (playerObject.transform.parent == kvp.Value?.transform)
                {
                    newVehicle = kvp.Key;
                    break;
                }
            }
            
            // Handle vehicle ownership changes
            if (newVehicle != null && !ownedVehicles.Contains(newVehicle))
            {
                // Take ownership of the vehicle
                ownedVehicles.Clear(); // Only own one vehicle at a time
                ownedVehicles.Add(newVehicle);
                
                // Broadcast ownership change
                SendVehicleOwnership(newVehicle, myPlayerId, true);
            }
            else if (newVehicle == null && ownedVehicles.Count > 0)
            {
                // Release ownership when exiting vehicle
                foreach (string vehicleId in ownedVehicles)
                {
                    SendVehicleOwnership(vehicleId, myPlayerId, false);
                }
                ownedVehicles.Clear();
            }
        }
        
        public bool ShouldSyncVehicle(string vehicleId)
        {
            // Server syncs all vehicles, clients only sync owned vehicles
            return isServer || ownedVehicles.Contains(vehicleId);
        }
        
        public void SyncVehicle(string vehicleId)
        {
            if (!vehicles.TryGetValue(vehicleId, out GameObject vehicle) || vehicle == null)
                return;
                
            if (!ShouldSyncVehicle(vehicleId))
                return;
            
            Vector3 pos = vehicle.transform.position;
            Quaternion rot = vehicle.transform.rotation;
            
            // Get vehicle physics data if available
            Rigidbody rb = vehicle.GetComponent<Rigidbody>();
            Vector3 velocity = rb != null ? rb.velocity : Vector3.zero;
            Vector3 angularVelocity = rb != null ? rb.angularVelocity : Vector3.zero;
            
            NetDataWriter writer = new NetDataWriter();
            writer.Put("VEHICLE_SYNC");
            writer.Put(vehicleId);
            writer.Put(myPlayerId); // Owner ID
            writer.Put(pos.x); writer.Put(pos.y); writer.Put(pos.z);
            writer.Put(rot.x); writer.Put(rot.y); writer.Put(rot.z); writer.Put(rot.w);
            writer.Put(velocity.x); writer.Put(velocity.y); writer.Put(velocity.z);
            writer.Put(angularVelocity.x); writer.Put(angularVelocity.y); writer.Put(angularVelocity.z);
            
            SendToAllPeers(writer);
        }
        
        public void HandleVehicleSync(NetPacketReader reader, NetPeer sender)
        {
            string vehicleId = reader.GetString();
            string ownerId = reader.GetString();
            
            // Skip if this is our own vehicle
            if (ownerId == myPlayerId)
                return;
                
            if (!vehicles.TryGetValue(vehicleId, out GameObject vehicle) || vehicle == null)
                return;
            
            Vector3 pos = new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
            Quaternion rot = new Quaternion(reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
            Vector3 velocity = new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
            Vector3 angularVelocity = new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
            
            // Apply position and rotation smoothly
            vehicle.transform.position = Vector3.Lerp(vehicle.transform.position, pos, Time.deltaTime * 10f);
            vehicle.transform.rotation = Quaternion.Slerp(vehicle.transform.rotation, rot, Time.deltaTime * 10f);
            
            // Apply physics if we have a rigidbody and we're not the owner
            Rigidbody rb = vehicle.GetComponent<Rigidbody>();
            if (rb != null && !ownedVehicles.Contains(vehicleId))
            {
                rb.velocity = Vector3.Lerp(rb.velocity, velocity, Time.deltaTime * 5f);
                rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, angularVelocity, Time.deltaTime * 5f);
            }
            
            // Forward to other clients if we're the server
            if (isServer)
            {
                NetDataWriter writer = new NetDataWriter();
                writer.Put("VEHICLE_SYNC");
                writer.Put(vehicleId);
                writer.Put(ownerId);
                writer.Put(pos.x); writer.Put(pos.y); writer.Put(pos.z);
                writer.Put(rot.x); writer.Put(rot.y); writer.Put(rot.z); writer.Put(rot.w);
                writer.Put(velocity.x); writer.Put(velocity.y); writer.Put(velocity.z);
                writer.Put(angularVelocity.x); writer.Put(angularVelocity.y); writer.Put(angularVelocity.z);
                
                foreach (var peer in netManager.ConnectedPeerList)
                {
                    if (peer != sender)
                        peer.Send(writer, DeliveryMethod.Unreliable);
                }
            }
        }
        
        private void SendVehicleOwnership(string vehicleId, string ownerId, bool isOwned)
        {
            NetDataWriter writer = new NetDataWriter();
            writer.Put("VEHICLE_OWNERSHIP");
            writer.Put(vehicleId);
            writer.Put(ownerId);
            writer.Put(isOwned);
            
            SendToAllPeers(writer);
        }
        
        public void HandleVehicleOwnership(NetPacketReader reader, NetPeer sender)
        {
            string vehicleId = reader.GetString();
            string ownerId = reader.GetString();
            bool isOwned = reader.GetBool();
            
            // Forward to other clients if we're the server
            if (isServer)
            {
                NetDataWriter writer = new NetDataWriter();
                writer.Put("VEHICLE_OWNERSHIP");
                writer.Put(vehicleId);
                writer.Put(ownerId);
                writer.Put(isOwned);
                
                foreach (var peer in netManager.ConnectedPeerList)
                {
                    if (peer != sender)
                        peer.Send(writer, DeliveryMethod.Reliable);
                }
            }
        }
        
        private void SendToAllPeers(NetDataWriter writer)
        {
            if (isServer)
            {
                foreach (var peer in netManager.ConnectedPeerList)
                    peer.Send(writer, DeliveryMethod.Unreliable);
            }
            else if (netManager.FirstPeer != null)
            {
                netManager.FirstPeer.Send(writer, DeliveryMethod.Unreliable);
            }
        }
    }
}