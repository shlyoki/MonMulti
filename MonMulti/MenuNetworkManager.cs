using BepInEx;
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
    public class MenuNetworkManager : MonoBehaviour, INetEventListener
    {
        private static MenuNetworkManager instance;
        public static MenuNetworkManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<MenuNetworkManager>();
                    if (instance == null)
                    {
                        var go = new GameObject("MenuNetworkManager");
                        instance = go.AddComponent<MenuNetworkManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        private NetManager netManager;
        private NetPeer serverPeer;
        private bool isServer = false;
        private bool isConnected = false;
        private int PORT = 2345;
        private string myPlayerId = Guid.NewGuid().ToString();
        
        public event System.Action<NetPeer> OnPlayerConnected;
        public event System.Action<NetPeer> OnPlayerDisconnected;
        public event System.Action<string> OnConnectionStatusChanged;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                SceneManager.sceneLoaded += OnSceneLoaded;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "MainMenu")
            {
                // We're in the main menu, network manager should be available
                Debug.Log("MenuNetworkManager: Entered MainMenu scene");
            }
            else if (scene.name == "Master")
            {
                // Game scene loaded, pass control to main MonMulti plugin
                Debug.Log("MenuNetworkManager: Entered Master scene, transferring network control");
            }
        }

        public bool StartHost(string hostIP = "0.0.0.0")
        {
            try
            {
                if (netManager != null) return false; // Already running

                netManager = new NetManager(this);
                isServer = true;
                netManager.Start(PORT);
                isConnected = true;
                
                Debug.Log($"MenuNetworkManager: Hosting on {hostIP}:{PORT}");
                OnConnectionStatusChanged?.Invoke($"Hosting on port {PORT}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"MenuNetworkManager: Failed to start host: {ex.Message}");
                OnConnectionStatusChanged?.Invoke("Failed to start host");
                return false;
            }
        }

        public bool ConnectToServer(string serverIP)
        {
            try
            {
                if (netManager != null) return false; // Already running

                netManager = new NetManager(this);
                isServer = false;
                netManager.Start();
                netManager.Connect(serverIP, PORT, "SantaGoat");
                
                Debug.Log($"MenuNetworkManager: Connecting to {serverIP}:{PORT}");
                OnConnectionStatusChanged?.Invoke($"Connecting to {serverIP}...");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"MenuNetworkManager: Failed to connect: {ex.Message}");
                OnConnectionStatusChanged?.Invoke("Failed to connect");
                return false;
            }
        }

        public void Disconnect()
        {
            try
            {
                if (netManager != null)
                {
                    netManager.Stop();
                    netManager = null;
                    isConnected = false;
                    isServer = false;
                    serverPeer = null;
                    
                    Debug.Log("MenuNetworkManager: Disconnected");
                    OnConnectionStatusChanged?.Invoke("Disconnected");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"MenuNetworkManager: Error during disconnect: {ex.Message}");
            }
        }

        public NetManager GetNetManager()
        {
            return netManager;
        }

        public bool IsConnected()
        {
            return isConnected && netManager != null;
        }

        public bool IsServer()
        {
            return isServer;
        }

        public string GetPlayerId()
        {
            return myPlayerId;
        }

        private void Update()
        {
            netManager?.PollEvents();
        }

        public void SendChatMessage(string message)
        {
            if (netManager == null || !isConnected) return;

            NetDataWriter writer = new NetDataWriter();
            writer.Put("CHAT");
            writer.Put(myPlayerId);
            writer.Put(message);

            if (isServer)
            {
                foreach (var peer in netManager.ConnectedPeerList)
                    peer.Send(writer, DeliveryMethod.ReliableOrdered);
            }
            else
            {
                serverPeer?.Send(writer, DeliveryMethod.ReliableOrdered);
            }
        }

        // INetEventListener implementation
        public void OnPeerConnected(NetPeer peer)
        {
            Debug.Log($"MenuNetworkManager: Peer connected: {peer.Address}:{peer.Port}");
            
            if (!isServer)
            {
                serverPeer = peer;
                isConnected = true;
                OnConnectionStatusChanged?.Invoke("Connected to server");
            }
            else
            {
                OnConnectionStatusChanged?.Invoke($"Player connected: {peer.Address}");
            }

            OnPlayerConnected?.Invoke(peer);
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Debug.Log($"MenuNetworkManager: Peer disconnected: {peer.Address}:{peer.Port}, Reason: {disconnectInfo.Reason}");
            
            if (!isServer)
            {
                serverPeer = null;
                isConnected = false;
                OnConnectionStatusChanged?.Invoke("Disconnected from server");
            }
            else
            {
                OnConnectionStatusChanged?.Invoke($"Player disconnected: {peer.Address}");
            }

            OnPlayerDisconnected?.Invoke(peer);
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            try
            {
                string messageType = reader.GetString();
                
                if (messageType == "CHAT")
                {
                    string senderId = reader.GetString();
                    string message = reader.GetString();
                    
                    Debug.Log($"Chat from {senderId}: {message}");
                    
                    // Forward to other clients if server
                    if (isServer)
                    {
                        NetDataWriter writer = new NetDataWriter();
                        writer.Put("CHAT");
                        writer.Put(senderId);
                        writer.Put(message);

                        foreach (var p in netManager.ConnectedPeerList)
                            if (p != peer) p.Send(writer, DeliveryMethod.ReliableOrdered);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"MenuNetworkManager: Receive error: {ex.Message}");
            }
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            // Handle discovery or other unconnected messages if needed
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            // Handle latency updates if needed
        }

        public void OnConnectionRequest(ConnectionRequest request)
        {
            request.AcceptIfKey("SantaGoat");
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            Debug.LogError($"MenuNetworkManager: Network error: {socketError} at {endPoint}");
            OnConnectionStatusChanged?.Invoke($"Network error: {socketError}");
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Disconnect();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                netManager?.Stop();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                netManager?.Stop();
            }
        }
    }
}