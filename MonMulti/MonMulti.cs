// MonMulti.cs
using BepInEx;
using HarmonyLib;
using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MonMulti
{
    [BepInPlugin(pluginGUID, "Mon Multi", "1.0.0")]
    public class MonMulti : BaseUnityPlugin, INetEventListener
    {
        public const string pluginGUID = "antalervin19.monbazou.multiplayer";

        private NetManager netManager;
        private NetPeer serverPeer;
        private Harmony harmony;
        private bool isServer = false;

        private GameObject playerTarget;
        private GameObject playerObject;
        private GameObject konigObject;
        private GameObject olTruckObject;
        private GameObject SmollATVObject;

        private float tickRate = 1f / 50f;
        private float tickTimer = 0f;

        private bool showMultiplayerMenu = false;

        private int PORT = 2345;
        private string hostIP = "0.0.0.0";
        private string clientIP = "127.0.0.1";

        private string myPlayerId = Guid.NewGuid().ToString();
        private string currentVehicle = null;

        private const float PlayerHeight = 1.8f;
        private const float CapsuleYOffset = (PlayerHeight / 2f) - 0.45f;

        // New components for enhanced multiplayer functionality
        private VehicleNetworkSync vehicleNetworkSync;
        private GameDataSync gameDataSync;
        private VehicleSpawner vehicleSpawner;

        private class RemoteCube
        {
            public GameObject Cube;
            public Vector3 TargetPosition;
            public Quaternion TargetRotation;
        }

        private Dictionary<string, RemoteCube> remoteCubes = new Dictionary<string, RemoteCube>();
        private Dictionary<NetPeer, string> peerToId = new Dictionary<NetPeer, string>();
        private HashSet<string> ownedVehicles = new HashSet<string>();

        private void Awake()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            harmony = new Harmony(pluginGUID);
            harmony.PatchAll();
            Logger.LogInfo("MonMulti loaded.");
        }

        [HarmonyPatch(typeof(Gameplay))]
        [HarmonyPatch("PauseGame")]
        class PauseGamePatch
        {
            static bool Prefix() => SceneManager.GetActiveScene().name != "Master";
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "Master")
            {
                AssignGameObjects();
                Logger.LogInfo("Scene loaded: Master. GameObjects assigned.");
            }
        }

        private void AssignGameObjects()
        {
            playerObject = GameObject.Find("FirstPersonWalker_Audio");
            playerTarget = playerObject;
            konigObject = GameObject.Find("Konig");
            olTruckObject = GameObject.Find("OlTruck");
            SmollATVObject = GameObject.Find("SmollATV");

            // Initialize new components if network is active
            if (netManager != null)
            {
                InitializeNetworkComponents();
            }
        }

        private void InitializeNetworkComponents()
        {
            try
            {
                vehicleNetworkSync = new VehicleNetworkSync(netManager, isServer, myPlayerId);
                gameDataSync = new GameDataSync(netManager, isServer, myPlayerId);
                vehicleSpawner = new VehicleSpawner(netManager, isServer, myPlayerId);
                vehicleSpawner.Initialize();
                
                Logger.LogInfo("Network components initialized successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to initialize network components: {ex.Message}");
            }
        }

        private void FixedUpdate()
        {
            if (playerTarget == null) return;

            tickTimer += Time.fixedDeltaTime;
            if (tickTimer >= tickRate)
            {
                tickTimer -= tickRate;

                SendPlayerSync();
                HandleVehicleEntry();

                // Use enhanced vehicle sync if available, fall back to original
                if (vehicleNetworkSync != null)
                {
                    if (ShouldSendVehicle("KONIG")) vehicleNetworkSync.SendVehicleSync("KONIG", konigObject);
                    if (ShouldSendVehicle("OLTRUCK")) vehicleNetworkSync.SendVehicleSync("OLTRUCK", olTruckObject);
                    if (ShouldSendVehicle("SMOLLATV")) vehicleNetworkSync.SendVehicleSync("SMOLLATV", SmollATVObject);
                }
                else
                {
                    // Original vehicle sync as fallback
                    if (ShouldSendVehicle("KONIG")) SendVehicleSync("KONIG", konigObject);
                    if (ShouldSendVehicle("OLTRUCK")) SendVehicleSync("OLTRUCK", olTruckObject);
                    if (ShouldSendVehicle("SMOLLATV")) SendVehicleSync("SMOLLATV", SmollATVObject);
                }
            }

            // Update new components
            gameDataSync?.Update();
            vehicleSpawner?.Update();
        }

        private void HandleVehicleEntry()
        {
            string newVehicle = null;

            if (playerObject.transform.parent == konigObject?.transform) newVehicle = "KONIG";
            else if (playerObject.transform.parent == olTruckObject?.transform) newVehicle = "OLTRUCK";
            else if (playerObject.transform.parent == SmollATVObject?.transform) newVehicle = "SMOLLATV";

            if (newVehicle != currentVehicle)
            {
                currentVehicle = newVehicle;

                if (currentVehicle != null)
                {
                    Logger.LogInfo($"Now driving: {currentVehicle}");
                    
                    // Use enhanced vehicle ownership if available
                    if (vehicleNetworkSync != null)
                    {
                        vehicleNetworkSync.TakeOwnership(currentVehicle);
                    }
                    else
                    {
                        // Original ownership logic as fallback
                        ownedVehicles.Clear();
                        ownedVehicles.Add(currentVehicle);
                    }
                }
            }
        }

        private bool ShouldSendVehicle(string vehicleTag)
        {
            if (isServer) return true;
            
            // Use enhanced vehicle ownership if available
            if (vehicleNetworkSync != null)
            {
                return vehicleNetworkSync.IsVehicleOwner(vehicleTag);
            }
            
            // Original logic as fallback
            return ownedVehicles.Contains(vehicleTag);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F9))
                showMultiplayerMenu = !showMultiplayerMenu;

            netManager?.PollEvents();

            foreach (var pair in remoteCubes)
            {
                var cube = pair.Value;
                if (cube.Cube == null) continue;
                cube.Cube.transform.position = Vector3.Lerp(cube.Cube.transform.position, cube.TargetPosition + new Vector3(0, CapsuleYOffset, 0), Time.deltaTime * 10f);
                cube.Cube.transform.rotation = Quaternion.Slerp(cube.Cube.transform.rotation, Quaternion.Euler(0, cube.TargetRotation.eulerAngles.y, 0), Time.deltaTime * 10f);
            }
        }

        private void SendPlayerSync()
        {
            if (netManager == null) return;

            Vector3 pos = playerTarget.transform.position;
            Quaternion rot = Quaternion.Euler(0, playerObject.transform.eulerAngles.y, 0);

            NetDataWriter writer = new NetDataWriter();
            writer.Put("SYNC");
            writer.Put(myPlayerId);
            writer.Put(pos.x); writer.Put(pos.y); writer.Put(pos.z);
            writer.Put(rot.x); writer.Put(rot.y); writer.Put(rot.z); writer.Put(rot.w);

            if (isServer)
            {
                foreach (var peer in netManager.ConnectedPeerList)
                    peer.Send(writer, DeliveryMethod.Unreliable);
            }
            else
            {
                serverPeer?.Send(writer, DeliveryMethod.Unreliable);
            }
        }

        private void SendVehicleSync(string tag, GameObject obj)
        {
            if (obj == null) return;

            Vector3 pos = obj.transform.position;
            Quaternion rot = obj.transform.rotation;

            NetDataWriter writer = new NetDataWriter();
            writer.Put(tag);
            writer.Put(pos.x); writer.Put(pos.y); writer.Put(pos.z);
            writer.Put(rot.x); writer.Put(rot.y); writer.Put(rot.z); writer.Put(rot.w);

            if (isServer)
            {
                foreach (var peer in netManager.ConnectedPeerList)
                    peer.Send(writer, DeliveryMethod.Unreliable);
            }
            else
            {
                serverPeer?.Send(writer, DeliveryMethod.Unreliable);
            }
        }

        public void OnPeerConnected(NetPeer peer)
        {
            Logger.LogInfo($"Peer connected: {peer.Address}:{peer.Port}");
            if (!isServer)
            {
                serverPeer = peer;
                // Initialize components when first connected as client
                if (SceneManager.GetActiveScene().name == "Master")
                {
                    InitializeNetworkComponents();
                }
            }
            else
            {
                // Server: sync vehicle states to new client
                vehicleSpawner?.SynchronizeVehicleStates(peer);
            }
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Logger.LogInfo($"Disconnected: {peer.Address}:{peer.Port}");

            if (peerToId.TryGetValue(peer, out string id))
            {
                if (remoteCubes.TryGetValue(id, out RemoteCube remoteCube))
                {
                    if (remoteCube.Cube != null) Destroy(remoteCube.Cube);
                    remoteCubes.Remove(id);
                }
                peerToId.Remove(peer);
            }

            if (!isServer)
            {
                foreach (var cube in remoteCubes.Values)
                    if (cube.Cube != null) Destroy(cube.Cube);
                remoteCubes.Clear();
            }
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            try
            {
                string tag = reader.GetString();

                if (tag == "SYNC")
                {
                    string senderId = reader.GetString();
                    Vector3 pos = new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
                    Quaternion rot = new Quaternion(reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat());

                    if (senderId != myPlayerId)
                        HandleRemotePlayer(senderId, pos, rot);

                    if (isServer)
                    {
                        NetDataWriter writer = new NetDataWriter();
                        writer.Put("SYNC");
                        writer.Put(senderId);
                        writer.Put(pos.x); writer.Put(pos.y); writer.Put(pos.z);
                        writer.Put(rot.x); writer.Put(rot.y); writer.Put(rot.z); writer.Put(rot.w);

                        foreach (var p in netManager.ConnectedPeerList)
                            if (p != peer) p.Send(writer, DeliveryMethod.Unreliable);
                    }
                }
                else if (tag == "VEHICLE_SYNC" && vehicleNetworkSync != null)
                {
                    // Use enhanced vehicle sync
                    vehicleNetworkSync.HandleVehicleSync(reader, peer);
                }
                else if (tag == "VEHICLE_OWNERSHIP" && vehicleNetworkSync != null)
                {
                    // Handle vehicle ownership changes
                    vehicleNetworkSync.HandleOwnershipChange(reader, peer);
                }
                else if (tag == "GAME_DATA_SYNC" && gameDataSync != null)
                {
                    // Handle game data synchronization
                    gameDataSync.HandleGameDataSync(reader, peer);
                }
                else if (tag == "VEHICLE_SPAWN" && vehicleSpawner != null)
                {
                    // Handle vehicle spawning
                    vehicleSpawner.HandleVehicleSpawn(reader, peer);
                }
                else if (tag == "VEHICLE_DESPAWN" && vehicleSpawner != null)
                {
                    // Handle vehicle despawning
                    vehicleSpawner.HandleVehicleDespawn(reader, peer);
                }
                else if (tag == "KONIG" || tag == "OLTRUCK" || tag == "SMOLLATV")
                {
                    // Original vehicle sync for backward compatibility
                    Vector3 pos = new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
                    Quaternion rot = new Quaternion(reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat());

                    GameObject target = tag == "KONIG" ? konigObject :
                                        tag == "OLTRUCK" ? olTruckObject :
                                        SmollATVObject;

                    if (target != null)
                    {
                        target.transform.position = Vector3.Lerp(target.transform.position, pos, Time.deltaTime * 10f);
                        target.transform.rotation = Quaternion.Slerp(target.transform.rotation, rot, Time.deltaTime * 10f);
                    }

                    if (isServer)
                    {
                        NetDataWriter writer = new NetDataWriter();
                        writer.Put(tag);
                        writer.Put(pos.x); writer.Put(pos.y); writer.Put(pos.z);
                        writer.Put(rot.x); writer.Put(rot.y); writer.Put(rot.z); writer.Put(rot.w);

                        foreach (var p in netManager.ConnectedPeerList)
                            if (p != peer) p.Send(writer, DeliveryMethod.Unreliable);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError("Receive Error: " + e);
            }
        }

        private void HandleRemotePlayer(string senderId, Vector3 pos, Quaternion rot)
        {
            if (!remoteCubes.TryGetValue(senderId, out RemoteCube remoteCube))
            {
                GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                obj.name = $"RemotePlayer_{senderId}";
                obj.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
                obj.GetComponent<Renderer>().material.color = Color.blue;
                Destroy(obj.GetComponent<Collider>());

                remoteCube = new RemoteCube
                {
                    Cube = obj,
                    TargetPosition = pos,
                    TargetRotation = rot
                };
                remoteCubes[senderId] = remoteCube;
                Logger.LogInfo($"Created remote player: {senderId}");
            }
            else
            {
                remoteCube.TargetPosition = pos;
                remoteCube.TargetRotation = rot;
            }
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }
        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
        public void OnConnectionRequest(ConnectionRequest request) => request.AcceptIfKey("SantaGoat");
        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) => Logger.LogError($"Network error: {socketError} at {endPoint}");

        private void OnGUI()
        {
            if (!showMultiplayerMenu) return;

            GUILayout.BeginArea(new Rect(20, 20, 250, 350), GUI.skin.box);
            GUILayout.Label("Mon Multi");

            GUILayout.Label("Host IP:");
            hostIP = GUILayout.TextField(hostIP);

            GUILayout.Label("Client IP:");
            clientIP = GUILayout.TextField(clientIP);

            if (netManager == null)
            {
                if (GUILayout.Button("Host Game"))
                {
                    netManager = new NetManager(this);
                    isServer = true;
                    netManager.Start(PORT);
                    Logger.LogInfo($"Hosting on 0.0.0.0:{PORT}");
                    
                    // Initialize components if in Master scene
                    if (SceneManager.GetActiveScene().name == "Master")
                    {
                        InitializeNetworkComponents();
                    }
                }

                if (GUILayout.Button("Connect"))
                {
                    netManager = new NetManager(this);
                    isServer = false;
                    netManager.Start();
                    netManager.Connect(clientIP, PORT, "SantaGoat");
                    Logger.LogInfo($"Connecting to {clientIP}:{PORT}");
                }
            }
            else
            {
                GUILayout.Label("Running: " + (isServer ? "Server" : "Client"));

                if (GUILayout.Button("Stop"))
                {
                    netManager.Stop();
                    netManager = null;
                    foreach (var cube in remoteCubes.Values)
                        if (cube.Cube != null) Destroy(cube.Cube);
                    remoteCubes.Clear();
                    peerToId.Clear();
                    
                    // Clean up new components
                    vehicleNetworkSync = null;
                    gameDataSync = null;
                    vehicleSpawner = null;
                    
                    Logger.LogInfo("Stopped network session.");
                }
            }

            GUILayout.EndArea();
        }
    }
}
