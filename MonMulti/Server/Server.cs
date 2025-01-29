using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MonMulti
{
    public class Server
    {
        private const int Port = 25565;
        private TcpListener _tcpListener;
        private List<TcpClient> _connectedClients;

        public Server()
        {
            _connectedClients = new List<TcpClient>();
        }

        public async Task StartServerAsync()
        {
            _tcpListener = new TcpListener(IPAddress.Any, Port);
            _tcpListener.Start();
            Console.WriteLine($"Server started on port {Port}");

            while (true)
            {
                var tcpClient = await _tcpListener.AcceptTcpClientAsync();
                Console.WriteLine("Client connected");

                _connectedClients.Add(tcpClient);

                _ = HandleClientAsync(tcpClient);
            }
        }

        private async Task HandleClientAsync(TcpClient tcpClient)
        {
            using (tcpClient)
            {
                var networkStream = tcpClient.GetStream();
                byte[] buffer = new byte[1024];

                while (true)
                {
                    int bytesRead = 0;

                    try
                    {
                        bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error reading from stream: {ex.Message}");
                        break;
                    }

                    if (bytesRead == 0)
                    {
                        break;
                    }

                    string receivedMessage = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"Received message: {receivedMessage}");
                }
            }

            _connectedClients.Remove(tcpClient);
        }

        public async Task SendMessageToClientAsync(TcpClient client, string message)
        {
            var networkStream = client.GetStream();
            byte[] responseBytes = Encoding.ASCII.GetBytes(message);
            await networkStream.WriteAsync(responseBytes, 0, responseBytes.Length);
        }
        public async Task SendMessageToAllClientsAsync(string message)
        {
            foreach (var client in _connectedClients)
            {
                await SendMessageToClientAsync(client, message);
            }
        }

        public void StopServer()
        {
            _tcpListener.Stop();
        }
    }
}
