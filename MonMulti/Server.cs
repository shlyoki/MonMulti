using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using UnityEngine;

namespace MonMulti
{
    public class Server
    {
        private TcpListener _server;
        private Thread _thread;
        private bool _isRunning;

        private readonly List<TcpClient> _clients = new List<TcpClient>();

        public void StartTCPServer(int port)
        {
            if (_isRunning)
            {
                Debug.Log("Server is already running!");
                return;
            }

            _server = new TcpListener(IPAddress.Any, port);
            _server.Start();
            _isRunning = true;

            _thread = new Thread(ListenForPlayers);
            _thread.IsBackground = true;
            _thread.Start();

            Debug.Log($"Server started on port: {port}");
        }

        public void StopTCPServer()
        {
            if (!_isRunning) return;

            _isRunning = false;

            foreach (var client in _clients)
            {
                client.Close();
            }

            _clients.Clear();
            _server.Stop();
            _thread?.Join();

            Debug.Log("Server stopped!");
        }

        public void Broadcast(string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);

            foreach (var client in _clients)
            {
                if (client.Connected)
                {
                    try
                    {
                        client.GetStream().Write(data, 0, data.Length);
                    }
                    catch (Exception e)
                    {
                        Debug.Log($"Failed to send data to client: {e.Message}");
                    }
                }
            }
        }

        private void ListenForPlayers()
        {
            while (_isRunning)
            { 
                try
                {
                    var client = _server.AcceptTcpClient();
                    _clients.Add(client);

                    Debug.Log("New player connected!");

                    Thread clientThread = new Thread(HandlePlayerCommunication);
                    clientThread.IsBackground = true;
                    clientThread.Start();
                }
                catch
                {
                    if (_isRunning)
                    {
                        Debug.Log("Socket exception occurred while accepting player connection...");
                    }
                }
            }
        }

        private void HandlePlayerCommunication(object clientObj)
        {
            var client = (TcpClient)clientObj;
            var stream = client.GetStream();
            var buffer = new byte[1024];

            try
            {
                while (_isRunning && client.Connected)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string received = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Debug.Log($"Received from player: {received}");

                        //Do smthing with player data.
                    }
                }
            }
            catch (Exception e )
            {
                Debug.Log($"Player communication error: {e.Message}");
            }
            finally
            {
                client.Close();
                _clients.Remove(client);
                Debug.Log("Player disconnected!");
            }
        }
    }
}
