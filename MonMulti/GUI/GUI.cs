using System;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

namespace MonMulti
{
    public class GUIManager : MonoBehaviour
    {
        private bool _showGUI = false;
        private string ipAddress = "127.0.0.1";
        private string port = "25565";

        private Client _client;
        private int _selectedTab = 0;
        private bool _isConnected = false;

        private Rect windowRect = new Rect(100, 100, 250, 250);

        public void SetClient(Client client)
        {
            _client = client;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F7))
            {
                _showGUI = !_showGUI;
            }
        }

        private void OnGUI()
        {
            if (_showGUI)
            {
                windowRect = GUI.Window(0, windowRect, DrawGUIWindow, "MonMulti Menu");
            }
        }

        private void DrawGUIWindow(int windowID)
        {
            if (GUI.Button(new Rect(10, 30, 100, 25), "Join"))
            {
                _selectedTab = 0;
            }
            if (GUI.Button(new Rect(130, 30, 100, 25), "Host"))
            {
                _selectedTab = 1;
            }

            if (_selectedTab == 0)
            {
                DrawJoinTab();
            }
            else
            {
                DrawHostTab();
            }

            GUI.DragWindow(new Rect(0, 0, 250, 30));
        }

        private void DrawJoinTab()
        {
            GUI.Label(new Rect(20, 70, 200, 20), "IP Address:");
            ipAddress = GUI.TextField(new Rect(20, 90, 210, 20), ipAddress);

            GUI.Label(new Rect(20, 120, 200, 20), "Port:");
            port = GUI.TextField(new Rect(20, 140, 210, 20), port);

            string buttonLabel = _isConnected ? "Disconnect" : "Join Game";

            if (GUI.Button(new Rect(20, 170, 210, 30), buttonLabel))
            {
                if (_isConnected)
                {
                    Debug.Log("Disconnecting from server...");
                    _client.Disconnect();
                    _isConnected = false;
                }
                else
                {
                    if (int.TryParse(port, out int PortNumber) && !string.IsNullOrEmpty(ipAddress))
                    {
                        Debug.Log($"Joining server at: {ipAddress}:{PortNumber}");
                        Task.Run(async () =>
                        {
                            bool success = await _client.ConnectToServerAsync(ipAddress, PortNumber);
                            if (success)
                            {
                                _isConnected = true;
                            }
                        });
                    }
                    else
                    {
                        Debug.LogError("Invalid IP address or port number.");
                    }
                }
            }
        }

        private void DrawHostTab()
        {
            GUI.Label(new Rect(20, 70, 200, 20), "Port:");
            port = GUI.TextField(new Rect(20, 90, 210, 20), port);

            if (GUI.Button(new Rect(20, 120, 210, 30), "Host Game"))
            {
                if (int.TryParse(port, out int PortNumber))
                {
                    Debug.Log($"Starting server at port: {PortNumber}");
                }
                else
                {
                    Debug.LogError("Invalid port number entered.");
                }
            }
        }
    }
}