using BepInEx;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MonMulti
{
    public class Client
    {
        private TcpClient _tcpClient;
        private NetworkStream _networkStream;
        public bool DeveloperMode = false;

        public async Task ConnectToServerAsync(string ipAddress, int port)
        {
            try
            {
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(ipAddress, port);

                Debug.Log($"Connected to server at {ipAddress}:{port}");

                _networkStream = _tcpClient.GetStream();
            }
            catch (Exception ex)
            {
                if (DeveloperMode) { Debug.LogError($"Error: {ex.Message}"); }
            }
        }

        public async Task<string> SendMessageAsync(string message)
        {
            if (_networkStream != null)
            {
                try
                {
                    byte[] messageBytes = Encoding.ASCII.GetBytes(message);
                    await _networkStream.WriteAsync(messageBytes, 0, messageBytes.Length);
                    if (DeveloperMode) { Debug.Log($"Sent: {message}"); }

                    byte[] buffer = new byte[1024];
                    int bytesRead = await _networkStream.ReadAsync(buffer, 0, buffer.Length);
                    string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    if (DeveloperMode) { Debug.Log($"Received: {response}"); }

                    return response;
                }
                catch (Exception ex)
                {
                    if (DeveloperMode) { Debug.LogError($"Error while sending/receiving message: {ex.Message}"); }
                }
            }
            return string.Empty;
        }

        public void Disconnect()
        {
            if (_networkStream != null)
            {
                _networkStream.Close();
                _networkStream = null;
            }

            if (_tcpClient != null)
            {
                _tcpClient.Close();
                _tcpClient = null;
            }

            Debug.Log("Disconnected from server.");
        }

        public async Task SendPlayerPositionAsync(Vector3 playerPosition)
        {
            string message = $"CPOS:{playerPosition.x},{playerPosition.y},{playerPosition.z}";
            await SendMessageAsync(message);
        }
    }
}
