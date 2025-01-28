using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MonMulti
{
    public class Client
    {
        private const string ServerAddress = "127.0.0.1"; // Localhost
        private const int Port = 25565;
        private TcpClient _tcpClient;
        private NetworkStream _networkStream;

        public async Task ConnectToServerAsync()
        {
            try
            {
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(ServerAddress, Port);
                Console.WriteLine("Connected to server");

                _networkStream = _tcpClient.GetStream();

                await SendMessageAsync("Hello Server!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        public async Task<string> SendMessageAsync(string message)
        {
            if (_networkStream != null)
            {
                byte[] messageBytes = Encoding.ASCII.GetBytes(message);
                await _networkStream.WriteAsync(messageBytes, 0, messageBytes.Length);
                Console.WriteLine($"Sent: {message}");

                byte[] buffer = new byte[1024];
                int bytesRead = await _networkStream.ReadAsync(buffer, 0, buffer.Length);
                string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"Received: {response}");

                return response;
            }

            return string.Empty;
        }

        public void Disconnect()
        {
            _networkStream?.Close();
            _tcpClient?.Close();
            Console.WriteLine("Disconnected from server.");
        }
    }
}
