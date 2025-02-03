using BepInEx;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

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

        public async Task<string> SendJsonPacketAsync(string jsonMessage)
        {
            if (_networkStream != null)
            {
                try
                {
                    byte[] messageBytes = Encoding.UTF8.GetBytes(jsonMessage);
                    await _networkStream.WriteAsync(messageBytes, 0, messageBytes.Length);

                    if (DeveloperMode) { Debug.Log($"Sent JSON: {jsonMessage}"); }

                    byte[] buffer = new byte[1024];
                    int bytesRead = await _networkStream.ReadAsync(buffer, 0, buffer.Length);
                    string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    if (DeveloperMode) { Debug.Log($"Received: {response}"); }

                    return response;
                }
                catch (Exception ex)
                {
                    if (DeveloperMode) { Debug.LogError($"Error while sending/receiving JSON: {ex.Message}"); }
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
    }
}