using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VNCScreen;
using Zenject;

namespace InputManagement
{
    public class KeyboardAndMouseInput : ITickable
    {

        public delegate void KeyboardEventHandler(uint keysym, bool pressed);

        public event KeyboardEventHandler OnKeyboardEvent;

        public delegate void MouseClickedEventHandler(MouseButton button, bool pressed);

        public event MouseClickedEventHandler onMouseClicked;
        public delegate void MouseMoveEventHandler(uint keysym, bool pressed);

        public event MouseMoveEventHandler onMouseMoved;

        public static Dictionary<int, MouseButton> mouseButtonMap = new Dictionary<int, MouseButton>()
        {
            {0, MouseButton.LeftMouse},
            {1, MouseButton.RightMouse},
            {2, MouseButton.MiddleMouse}
        };
        public void Tick()
        {
            // check for all key presses
            foreach (var keyCode in KeyTranslator.KeyDict.Keys)
            {
                if (Input.GetKeyDown(keyCode))
                {
                    // perform action for key down
                    Debug.Log("Key down: " + keyCode);
                    OnKeyboardEvent(KeyTranslator.KeyDict[keyCode], true);
                }
                else if (Input.GetKeyUp(keyCode))
                {
                    // perform action for key up
                    Debug.Log("Key up: " + keyCode);
                    OnKeyboardEvent(KeyTranslator.KeyDict[keyCode], false);
                }
            }

            // check for all mouse button presses
            for (var i = 0; i < 3; i++)
            {
                if (Input.GetMouseButtonDown(i))
                {
                    // perform action for mouse button press
                    onMouseClicked(mouseButtonMap[i], true);
                }
            }

            // Check for mouse movement
            var mouseX = Input.GetAxis("Mouse X");
            var mouseY = Input.GetAxis("Mouse Y");
            if (mouseX != 0 || mouseY != 0)
            {
                // perform action for mouse movement
                Debug.Log("Mouse moved: " + mouseX + ", " + mouseY);
            }
        }
    }
}