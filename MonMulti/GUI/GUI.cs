using System;
using System.Threading.Tasks;
using UnityEngine;

namespace MonMulti
{
    public class GUIManager : MonoBehaviour
    {
        private bool _showGUI = false;
        private string ipAddress = "";
        private string port = "";

        private Client _client;
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
                UnityEngine.GUI.Box(new Rect(10, 10, 200, 180), "MonMulti Menu");

                UnityEngine.GUI.Label(new Rect(20, 40, 180, 20), "IP Address:");
                ipAddress = UnityEngine.GUI.TextField(new Rect(20, 60, 180, 20), ipAddress);

                UnityEngine.GUI.Label(new Rect(20, 90, 180, 20), "Port:");
                port = UnityEngine.GUI.TextField(new Rect(20, 110, 180, 20), port);

                if (UnityEngine.GUI.Button(new Rect(20, 140, 180, 30), "Join Game"))
                {
                    if (int.TryParse(port, out int PortNumber))
                    {
                        if (!string.IsNullOrEmpty(ipAddress))
                        {
                            Debug.Log($"Joining server at: {ipAddress}:{PortNumber}");
                            Task.Run(() => _client.ConnectToServerAsync(ipAddress, PortNumber));
                        }
                        else
                        {
                            Debug.LogError("Please enter a valid IP address.");
                        }
                    }
                    else
                    {
                        Debug.LogError("Invalid port number entered.");
                    }
                }
            }
        }
    }
}
