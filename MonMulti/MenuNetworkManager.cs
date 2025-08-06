// MenuNetworkManager.cs
using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MonMulti
{
    public class MenuNetworkManager : INetEventListener
    {
        private NetManager netManager;
        private NetPeer serverPeer;
        private bool isServer = false;
        private bool isConnected = false;
        
        private const int PORT = 2345;
        private string serverIP = "127.0.0.1";
        private string playerName = "Player";
        
        // Connection state
        public bool IsConnected => isConnected;
        public bool IsServer => isServer;
        public string ServerIP { get => serverIP; set => serverIP = value; }
        public string PlayerName { get => playerName; set => playerName = value; }
        
        // Events
        public event Action<bool> OnConnectionResult; // true = success, false = failure
        public event Action OnDisconnected;
        public event Action<string> OnPlayerJoined;
        public event Action<string> OnPlayerLeft;
        
        public void Initialize()
        {
            // Auto-detect current scene and handle accordingly
            string currentScene = SceneManager.GetActiveScene().name;
            Debug.Log($"MenuNetworkManager initializing in scene: {currentScene}");
        }
        
        public void StartHost()
        {
            if (netManager != null)
            {
                Stop();
            }
            
            try
            {
                netManager = new NetManager(this);
                isServer = true;
                
                bool started = netManager.Start(PORT);
                if (started)
                {
                    Debug.Log($"Server started on port {PORT}");
                    isConnected = true;
                    OnConnectionResult?.Invoke(true);
                }
                else
                {
                    Debug.LogError("Failed to start server");
                    OnConnectionResult?.Invoke(false);
                    netManager = null;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error starting host: {e.Message}");
                OnConnectionResult?.Invoke(false);
                netManager = null;
            }
        }
        
        public void ConnectToHost(string hostIP)
        {
            if (netManager != null)
            {
                Stop();
            }
            
            try
            {
                serverIP = hostIP;
                netManager = new NetManager(this);
                isServer = false;
                
                netManager.Start();
                netManager.Connect(hostIP, PORT, "SantaGoat");
                
                Debug.Log($"Attempting to connect to {hostIP}:{PORT}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error connecting to host: {e.Message}");
                OnConnectionResult?.Invoke(false);
                netManager = null;
            }
        }
        
        public void Stop()
        {
            if (netManager != null)
            {
                netManager.Stop();
                netManager = null;
            }
            
            isConnected = false;
            isServer = false;
            serverPeer = null;
            
            Debug.Log("Network manager stopped");
        }
        
        public void Update()
        {
            netManager?.PollEvents();
        }
        
        public void LoadGameScene()
        {
            if (!isConnected)
            {
                Debug.LogWarning("Cannot load game scene - not connected to network");
                return;
            }
            
            // Load the main game scene while maintaining network connection
            SceneManager.LoadScene("Master");
        }
        
        // Network event handlers
        public void OnPeerConnected(NetPeer peer)
        {
            Debug.Log($"Peer connected: {peer.Address}:{peer.Port}");
            
            if (!isServer)
            {
                serverPeer = peer;
                isConnected = true;
                OnConnectionResult?.Invoke(true);
            }
            else
            {
                // Server side - notify about new player
                OnPlayerJoined?.Invoke(peer.Address.ToString());
                
                // Send welcome message with server info
                NetDataWriter writer = new NetDataWriter();
                writer.Put("WELCOME");
                writer.Put($"Welcome to {playerName}'s server!");
                peer.Send(writer, DeliveryMethod.ReliableOrdered);
            }
        }
        
        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Debug.Log($"Peer disconnected: {peer.Address}:{peer.Port} - {disconnectInfo.Reason}");
            
            if (!isServer)
            {
                isConnected = false;
                OnDisconnected?.Invoke();
            }
            else
            {
                OnPlayerLeft?.Invoke(peer.Address.ToString());
            }
        }
        
        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            try
            {
                string messageType = reader.GetString();
                
                switch (messageType)
                {
                    case "WELCOME":
                        string welcomeMessage = reader.GetString();
                        Debug.Log($"Server: {welcomeMessage}");
                        break;
                        
                    case "CHAT":
                        string chatMessage = reader.GetString();
                        Debug.Log($"Chat: {chatMessage}");
                        break;
                        
                    case "PLAYER_INFO":
                        HandlePlayerInfo(reader, peer);
                        break;
                        
                    case "SCENE_CHANGE":
                        HandleSceneChange(reader, peer);
                        break;
                        
                    default:
                        Debug.LogWarning($"Unknown message type: {messageType}");
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error handling network message: {e.Message}");
            }
        }
        
        private void HandlePlayerInfo(NetPacketReader reader, NetPeer peer)
        {
            try
            {
                string playerInfoName = reader.GetString();
                Debug.Log($"Player info received: {playerInfoName}");
                
                if (isServer)
                {
                    // Broadcast to other clients
                    NetDataWriter writer = new NetDataWriter();
                    writer.Put("PLAYER_INFO");
                    writer.Put(playerInfoName);
                    
                    foreach (var p in netManager.ConnectedPeerList)
                    {
                        if (p != peer)
                            p.Send(writer, DeliveryMethod.ReliableOrdered);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error handling player info: {e.Message}");
            }
        }
        
        private void HandleSceneChange(NetPacketReader reader, NetPeer peer)
        {
            try
            {
                string sceneName = reader.GetString();
                Debug.Log($"Scene change request: {sceneName}");
                
                // Load the requested scene
                SceneManager.LoadScene(sceneName);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error handling scene change: {e.Message}");
            }
        }
        
        public void SendChatMessage(string message)
        {
            if (!isConnected || netManager == null)
                return;
            
            try
            {
                NetDataWriter writer = new NetDataWriter();
                writer.Put("CHAT");
                writer.Put($"{playerName}: {message}");
                
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
            catch (Exception e)
            {
                Debug.LogError($"Error sending chat message: {e.Message}");
            }
        }
        
        public void SendPlayerInfo()
        {
            if (!isConnected || netManager == null)
                return;
            
            try
            {
                NetDataWriter writer = new NetDataWriter();
                writer.Put("PLAYER_INFO");
                writer.Put(playerName);
                
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
            catch (Exception e)
            {
                Debug.LogError($"Error sending player info: {e.Message}");
            }
        }
        
        // Required INetEventListener methods
        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }
        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
        public void OnConnectionRequest(ConnectionRequest request)
        {
            if (isServer)
            {
                request.AcceptIfKey("SantaGoat");
            }
        }
        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            Debug.LogError($"Network error: {socketError} at {endPoint}");
        }
    }
}