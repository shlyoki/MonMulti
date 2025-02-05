using System;
using UnityEngine;
using MonMulti.Networking;

namespace MonMulti
{
    public class GUIManager : MonoBehaviour
    {
        private bool _showGUI = false;
        public string IPAddress = "127.0.0.1";
        public string Port = "25565";

        private int _selectedTab = 0;
        private bool _isConnected = false;
        private bool _isHosting = false;

        private AsyncClient _client;
        private AsyncServer _server;

        private Rect windowRect = new Rect(100, 100, 250, 250);

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F7))
            {
                _showGUI = !_showGUI;
            }
        }

        private void OnGUI()
        {
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.fontSize = 12;
            titleStyle.normal.textColor = Color.white;

            float screenWidth = Screen.width;
            GUI.Label(new Rect(screenWidth / 2 - 100, 0, 200, 20), "MonMulti by: antalervin19", titleStyle);

            if (_showGUI)
            {
                windowRect = GUI.Window(0, windowRect, DrawGUIWindow, "MonMulti Menu");
            }
        }

        private void DrawGUIWindow(int windowID)
        {
            if (_isConnected && _selectedTab != 0)
            {
                _selectedTab = 0;
            }
            else if (_isHosting && _selectedTab != 1)
            {
                _selectedTab = 1;
            }

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
            IPAddress = GUI.TextField(new Rect(20, 90, 210, 20), IPAddress);

            GUI.Label(new Rect(20, 120, 200, 20), "Port:");
            Port = GUI.TextField(new Rect(20, 140, 210, 20), Port);

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
                    if (int.TryParse(Port, out int PortNumber) && !string.IsNullOrEmpty(IPAddress))
                    {
                        Debug.Log($"Joining server at: {IPAddress}:{PortNumber}");
                        _client = new AsyncClient(IPAddress, PortNumber);
                        _ = _client.Connect();
                        _isConnected = true;
                    }
                    else
                    {
                        Debug.LogError("Invalid IP address or Port number.");
                    }
                }
            }
        }

        private void DrawHostTab()
        {
            GUI.Label(new Rect(20, 70, 200, 20), "Port:");
            Port = GUI.TextField(new Rect(20, 90, 210, 20), Port);

            string buttonLabel = _isHosting ? "Stop Hosting" : "Host Game";

            if (GUI.Button(new Rect(20, 120, 210, 30), buttonLabel))
            {
                if (_isHosting)
                {
                    Debug.Log("Stopping the server...");
                    _server.StopServer();
                    _isHosting = false;
                }
                else
                {
                    if (int.TryParse(Port, out int PortNumber))
                    {
                        Debug.Log($"Starting server at Port: {PortNumber}");
                        _server = new AsyncServer(PortNumber);
                        _ = _server.StartServer();
                        _isHosting = true;
                    }
                    else
                    {
                        Debug.LogError("Invalid Port number entered.");
                    }
                }
            }
        }
    }
}