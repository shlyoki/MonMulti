using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MonMulti.Networking
{
    class AsyncServer
    {
        public bool DebugMode = true;

        private readonly int _port;
        private readonly List<TcpClient> _connectedClients = new List<TcpClient>();
        private int _clientCounter = 0;
        private TcpListener _listener;

        public AsyncServer(int port)
        {
            _port = port;
        }

        public async Task StartServer()
        {
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();

            if (DebugMode) { Debug.Log($"Server started on port {_port}"); }

            while (true)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync();
                _connectedClients.Add(client);
                _ = HandleClient(client);
            }
        }

        private async Task HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int clid = ++_clientCounter;
            string clidMessage = $"CLID:{clid}";
            byte[] clidData = Encoding.UTF8.GetBytes(clidMessage);
            await stream.WriteAsync(clidData, 0, clidData.Length);

            if (DebugMode) { Debug.Log($"Client {clid} connected."); }

            //Create player on server side
            GameObject playerObject = new GameObject($"Player_{clid}");
            GameData.Players.Add(playerObject);

            await SendDataPack();

            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                if (DebugMode) { Debug.Log($"Client {clid} says: {message}"); }
            }

            if (DebugMode) { Debug.Log($"Client {clid} disconnected."); }
            _connectedClients.Remove(client);
            client.Close();

            //Remove player on server side
            GameData.Players.Remove(playerObject);
            UnityEngine.Object.Destroy(playerObject);

            await SendDataPack();
        }

        private async Task SendDataPack()
        {
            string clientIds = string.Join(",", _connectedClients.Select(c => _connectedClients.IndexOf(c) + 1));
            int totalClients = _connectedClients.Count;
            string dataPackMessage = $"DataPack: CL:{totalClients}, CLID:{clientIds}";

            byte[] dataPack = Encoding.UTF8.GetBytes(dataPackMessage);

            foreach (var client in _connectedClients)
            {
                try
                {
                    NetworkStream stream = client.GetStream();
                    await stream.WriteAsync(dataPack, 0, dataPack.Length);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error sending DataPack to client: {ex.Message}");
                }
            }

            if (DebugMode) { Debug.Log($"Sent DataPack to all clients."); }
        }

        public async Task StopServer()
        {
            string shutdownMessage = "ServerShuttingDown";
            byte[] shutdownData = Encoding.UTF8.GetBytes(shutdownMessage);

            foreach (var client in _connectedClients)
            {
                try
                {
                    NetworkStream stream = client.GetStream();
                    await stream.WriteAsync(shutdownData, 0, shutdownData.Length);
                    client.Close();
                }
                catch (Exception ex)
                {
                    if (DebugMode) { Debug.LogError($"Error disconnecting client: {ex.Message}"); }
                }
            }

            _connectedClients.Clear();
            _clientCounter = 0;
            _listener?.Stop();

            if (DebugMode) { Debug.Log("Server stopped."); }
        }
    }

}