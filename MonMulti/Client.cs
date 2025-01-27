using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace MonMulti
{
    public class Client
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _receiveThread;
        private bool _isConnected;

        public void ConnectToServer(string ipAddress, int port)
        {
            if (_isConnected)
            {
                Debug.Log("Already connected to the server.");
                return;
            }

            try
            {
                _client = new TcpClient(ipAddress, port);
                _stream = _client.GetStream();
                _isConnected = true;

                Debug.Log("Connected to the server!");

                _receiveThread = new Thread(ListenForServerMessages);
                _receiveThread.IsBackground = true;
                _receiveThread.Start();
            }
            catch (Exception e)
            {
                Debug.Log($"Failed to connect to server: {e.Message}");
            }
        }

        public void DisconnectFromServer()
        {
            if (!_isConnected) return;

            _isConnected = false;

            _receiveThread?.Join();
            _stream?.Close();
            _client?.Close();

            Debug.Log("Disconnected from the server.");
        }

        public void SendMessageToServer(string message)
        {
            if (!_isConnected || _client == null || !_client.Connected)
            {
                Debug.LogWarning("Cannot send message: not connected to a server.");
                return;
            }

            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                _stream.Write(data, 0, data.Length);
                Debug.Log($"Sent to server: {message}");
            }
            catch (Exception e)
            {
                Debug.Log($"Failed to send message to server: {e.Message}");
            }
        }

        private void ListenForServerMessages()
        {
            byte[] buffer = new byte[1024];

            try
            {
                while (_isConnected)
                {
                    int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Debug.Log($"Received from server: {message}");
                    }
                }
            }
            catch (Exception e)
            {
                if (_isConnected) // Avoid throwing exceptions if disconnecting
                {
                    Debug.Log($"Error while receiving data from server: {e.Message}");
                }
            }
            finally
            {
                DisconnectFromServer();
            }
        }
    }
}
