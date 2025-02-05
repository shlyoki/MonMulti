using System;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using MonMulti.Networking;

namespace MonMulti
{
    public class GUIManager : MonoBehaviour
    {
        private bool _showGUI = false;
        public string ipAddress = "127.0.0.1";
        public string port = "25565";

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

            if (_isConnected && _client != null)
            {
                _ = _client.ConnectAndSendAsync("Hello, server!");
            }

            if (_isHosting && _server != null)
            {
                _ = _server.StartAsync();
            }
        }

        private void OnGUI()
        {
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.fontSize = 20;
            titleStyle.normal.textColor = Color.white;

            float screenWidth = Screen.width;
            GUI.Label(new Rect(screenWidth / 2 - 100, 10, 200, 30), "MonMulti by: antalervin19", titleStyle);

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
                    _isConnected = false;
                }
                else
                {
                    if (int.TryParse(port, out int PortNumber) && !string.IsNullOrEmpty(ipAddress))
                    {
                        Debug.Log($"Joining server at: {ipAddress}:{PortNumber}");
                        _client = new AsyncClient(ipAddress, PortNumber);
                        _isConnected = true;
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
                    _server = new AsyncServer(PortNumber);
                    _isHosting = true;
                }
                else
                {
                    Debug.LogError("Invalid port number entered.");
                }
            }
        }
    }
}