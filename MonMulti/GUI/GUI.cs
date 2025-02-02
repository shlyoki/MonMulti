using System;
using UnityEngine;

namespace MonMulti
{
    public class GUIManager : MonoBehaviour
    {
        private bool _showGUI = false;

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
                UnityEngine.GUI.Box(new Rect(10, 10, 200, 100), "MonMulti Menu");

                if (UnityEngine.GUI.Button(new Rect(20, 40, 180, 30), "Print Message"))
                {
                    Debug.Log("Button Clicked: Hello from MonMulti!");
                }
            }
        }
    }
}
