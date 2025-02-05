using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MonMulti.Networking
{
    class AsyncClient
    {
        public bool DebugMode = true;

        private readonly string _ip;
        private readonly int _port;
        private int _clid;

        public AsyncClient(string ip, int port)
        {
            _ip = ip;
            _port = port;
        }

        public async Task ConnectAndSendAsync(string message)
        {
            using TcpClient client = new TcpClient();
            await client.ConnectAsync(_ip, _port);
            NetworkStream stream = client.GetStream();

            _ = Task.Run(() => ReceiveMessagesAsync(stream));

            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string serverMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            if (serverMessage.StartsWith("CLID:"))
            {
                _clid = int.Parse(serverMessage.Split(':')[1]);
                if (DebugMode) { Console.WriteLine($"Received Client ID: {_clid}"); }
            }

            await SendMessageAsync(stream, message);

            await Task.Delay(-1);
        }

        public async Task SendMessageAsync(NetworkStream stream, string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(data, 0, data.Length);
            if (DebugMode) { Console.WriteLine($"Message sent: {message}"); }
        }

        private async Task ReceiveMessagesAsync(NetworkStream stream)
        {
            byte[] buffer = new byte[1024];
            while (true)
            {
                try
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string serverMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        if (DebugMode) { Console.WriteLine($"Received from server: {serverMessage}"); }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error receiving message: {ex.Message}");
                    break;
                }
            }
        }
    }
}