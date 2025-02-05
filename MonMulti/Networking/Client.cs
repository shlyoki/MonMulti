using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace MonMulti.Networking
{
    class AsyncClient
    {
        public bool DebugMode = true;

        private readonly string _ip;
        private readonly int _port;
        private TcpClient _client;
        private NetworkStream _stream;
        private CancellationTokenSource _cancellationTokenSource;

        public AsyncClient(string ip, int port)
        {
            _ip = ip;
            _port = port;
        }

        public async Task Connect()
        {
            _client = new TcpClient();
            await _client.ConnectAsync(_ip, _port);
            _stream = _client.GetStream();
            _cancellationTokenSource = new CancellationTokenSource();

            _ = Task.Run(() => ReceiveAsync(_stream, _cancellationTokenSource.Token));

            byte[] buffer = new byte[1024];
            int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
            string serverMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            if (serverMessage.StartsWith("CLID:"))
            {
                GameData.PlayerID = int.Parse(serverMessage.Split(':')[1]);
                if (DebugMode) { Debug.Log($"Current Client ID: {GameData.PlayerID}"); }
            }

            await Task.Delay(-1);
        }

        public async Task SendAsync(string message)
        {
            if (_stream == null)
            {
                Debug.LogError("Stream is not initialized.");
                return;
            }

            byte[] data = Encoding.UTF8.GetBytes($"{GameData.PlayerID}_{message}");
            await _stream.WriteAsync(data, 0, data.Length);
            if (DebugMode) { Debug.Log($"Message sent: {message}"); }
        }

        private async Task ReceiveAsync(NetworkStream stream, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[1024];
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    if (bytesRead > 0)
                    {
                        string serverMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        if (DebugMode) { Debug.Log($"Received from server: {serverMessage}"); }
                    }
                }
                catch (Exception ex)
                {
                    if (ex is TaskCanceledException)
                    {
                        break;
                    }

                    Debug.LogError($"Error receiving message: {ex.Message}");
                    break;
                }
            }
        }
        public void Disconnect()
        {
            _cancellationTokenSource?.Cancel();

            _stream?.Close();
            _client?.Close();

            if (DebugMode) { Debug.Log("Disconnected from server."); }
        }
    }
}
