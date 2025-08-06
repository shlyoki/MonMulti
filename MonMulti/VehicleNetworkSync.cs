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
        private Dictionary<string, string> vehicleOwnership = new Dictionary<string, string>();
        private Dictionary<string, VehicleData> vehicleData = new Dictionary<string, VehicleData>();

        private class VehicleData
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Velocity;
            public Vector3 AngularVelocity;
            public string OwnerId;
            public float LastUpdateTime;
        }

        public VehicleNetworkSync(NetManager netManager, bool isServer, string playerId)
        {
            this.netManager = netManager;
            this.isServer = isServer;
            this.myPlayerId = playerId;
        }

        public bool IsVehicleOwner(string vehicleId)
        {
            if (isServer) return true;
            return vehicleOwnership.ContainsKey(vehicleId) && vehicleOwnership[vehicleId] == myPlayerId;
        }

        public void TakeOwnership(string vehicleId)
        {
            if (vehicleOwnership.ContainsKey(vehicleId) && vehicleOwnership[vehicleId] == myPlayerId)
                return; // Already owned

            vehicleOwnership[vehicleId] = myPlayerId;
            SendOwnershipChange(vehicleId, myPlayerId);
        }

        public void SendVehicleSync(string vehicleId, GameObject vehicleObj)
        {
            if (vehicleObj == null || netManager == null) return;

            Rigidbody rb = vehicleObj.GetComponent<Rigidbody>();
            Vector3 velocity = rb != null ? rb.velocity : Vector3.zero;
            Vector3 angularVelocity = rb != null ? rb.angularVelocity : Vector3.zero;

            NetDataWriter writer = new NetDataWriter();
            writer.Put("VEHICLE_SYNC");
            writer.Put(vehicleId);
            writer.Put(vehicleObj.transform.position.x);
            writer.Put(vehicleObj.transform.position.y);
            writer.Put(vehicleObj.transform.position.z);
            writer.Put(vehicleObj.transform.rotation.x);
            writer.Put(vehicleObj.transform.rotation.y);
            writer.Put(vehicleObj.transform.rotation.z);
            writer.Put(vehicleObj.transform.rotation.w);
            writer.Put(velocity.x);
            writer.Put(velocity.y);
            writer.Put(velocity.z);
            writer.Put(angularVelocity.x);
            writer.Put(angularVelocity.y);
            writer.Put(angularVelocity.z);

            if (isServer)
            {
                foreach (var peer in netManager.ConnectedPeerList)
                    peer.Send(writer, DeliveryMethod.Unreliable);
            }
            else
            {
                var serverPeer = GetServerPeer();
                serverPeer?.Send(writer, DeliveryMethod.Unreliable);
            }
        }

        public void HandleVehicleSync(NetPacketReader reader, NetPeer peer)
        {
            string vehicleId = reader.GetString();
            Vector3 position = new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
            Quaternion rotation = new Quaternion(reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
            Vector3 velocity = new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
            Vector3 angularVelocity = new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());

            if (!vehicleData.ContainsKey(vehicleId))
            {
                vehicleData[vehicleId] = new VehicleData();
            }

            vehicleData[vehicleId].Position = position;
            vehicleData[vehicleId].Rotation = rotation;
            vehicleData[vehicleId].Velocity = velocity;
            vehicleData[vehicleId].AngularVelocity = angularVelocity;
            vehicleData[vehicleId].LastUpdateTime = Time.time;

            // Apply to actual vehicle if it exists
            GameObject vehicle = GetVehicleById(vehicleId);
            if (vehicle != null)
            {
                ApplyVehicleData(vehicle, vehicleData[vehicleId]);
            }

            // Forward to other clients if server
            if (isServer)
            {
                NetDataWriter writer = new NetDataWriter();
                writer.Put("VEHICLE_SYNC");
                writer.Put(vehicleId);
                writer.Put(position.x); writer.Put(position.y); writer.Put(position.z);
                writer.Put(rotation.x); writer.Put(rotation.y); writer.Put(rotation.z); writer.Put(rotation.w);
                writer.Put(velocity.x); writer.Put(velocity.y); writer.Put(velocity.z);
                writer.Put(angularVelocity.x); writer.Put(angularVelocity.y); writer.Put(angularVelocity.z);

                foreach (var p in netManager.ConnectedPeerList)
                    if (p != peer) p.Send(writer, DeliveryMethod.Unreliable);
            }
        }

        public void HandleOwnershipChange(NetPacketReader reader, NetPeer peer)
        {
            string vehicleId = reader.GetString();
            string ownerId = reader.GetString();

            vehicleOwnership[vehicleId] = ownerId;

            // Forward to other clients if server
            if (isServer)
            {
                NetDataWriter writer = new NetDataWriter();
                writer.Put("VEHICLE_OWNERSHIP");
                writer.Put(vehicleId);
                writer.Put(ownerId);

                foreach (var p in netManager.ConnectedPeerList)
                    if (p != peer) p.Send(writer, DeliveryMethod.Unreliable);
            }
        }

        private void SendOwnershipChange(string vehicleId, string ownerId)
        {
            NetDataWriter writer = new NetDataWriter();
            writer.Put("VEHICLE_OWNERSHIP");
            writer.Put(vehicleId);
            writer.Put(ownerId);

            if (isServer)
            {
                foreach (var peer in netManager.ConnectedPeerList)
                    peer.Send(writer, DeliveryMethod.Unreliable);
            }
            else
            {
                var serverPeer = GetServerPeer();
                serverPeer?.Send(writer, DeliveryMethod.Unreliable);
            }
        }

        private void ApplyVehicleData(GameObject vehicle, VehicleData data)
        {
            if (Time.time - data.LastUpdateTime > 1.0f) return; // Skip old data

            vehicle.transform.position = Vector3.Lerp(vehicle.transform.position, data.Position, Time.deltaTime * 10f);
            vehicle.transform.rotation = Quaternion.Slerp(vehicle.transform.rotation, data.Rotation, Time.deltaTime * 10f);

            Rigidbody rb = vehicle.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.Lerp(rb.velocity, data.Velocity, Time.deltaTime * 5f);
                rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, data.AngularVelocity, Time.deltaTime * 5f);
            }
        }

        private GameObject GetVehicleById(string vehicleId)
        {
            switch (vehicleId.ToUpper())
            {
                case "KONIG":
                    return GameObject.Find("Konig");
                case "OLTRUCK":
                    return GameObject.Find("OlTruck");
                case "SMOLLATV":
                    return GameObject.Find("SmollATV");
                default:
                    return null;
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