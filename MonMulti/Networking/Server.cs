using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MonMulti.Networking
{
    class AsyncServer
    {
        public bool DebugMode = true;

        private readonly int _port;
        private readonly List<TcpClient> _connectedClients = new List<TcpClient>();
        private int _clientCounter = 0;

        public AsyncServer(int port)
        {
            _port = port;
        }

        public async Task StartAsync()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, _port);
            listener.Start();
            if (DebugMode) { Console.WriteLine($"Server started on port {_port}"); }

            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                _connectedClients.Add(client);
                _ = HandleClientAsync(client);
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int clid = ++_clientCounter;
            string clidMessage = $"CLID:{clid}";
            byte[] clidData = Encoding.UTF8.GetBytes(clidMessage);
            await stream.WriteAsync(clidData, 0, clidData.Length);

            if (DebugMode) { Console.WriteLine($"Client {clid} connected."); }

            await SendDataPackToAllClients();

            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                if (DebugMode) { Console.WriteLine($"Client {clid} says: {message}"); }
            }

            if (DebugMode) { Console.WriteLine($"Client {clid} disconnected."); }
            _connectedClients.Remove(client);
            client.Close();

            await SendDataPackToAllClients();
        }

        private async Task SendDataPackToAllClients()
        {
            string clientIds = string.Join(",", _connectedClients.Select(c => _connectedClients.IndexOf(c) + 1));
            int totalClients = _connectedClients.Count;
            string dataPackMessage = $"DataPack: TotalClients:{totalClients}, ClientIDs:{clientIds}";

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
                    Console.WriteLine($"Error sending DataPack to client: {ex.Message}");
                }
            }

            if (DebugMode) { Console.WriteLine($"Sent DataPack to all clients."); }
        }
    }
}