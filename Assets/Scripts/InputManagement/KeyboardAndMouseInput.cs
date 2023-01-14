using UnityEngine;
using UnityEngine.UIElements;

namespace InputManagement
{
    public class KeyboardAndMouseInput:MonoBehaviour
    {
        void Update()
        {
            // check for all key presses
            foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(keyCode))
                {
                    // perform action for key press
                    Debug.Log("Key pressed: " + keyCode);
                }
            }

            // check for all mouse button presses
            for(int i = 0; i < 3; i++)
            {
                if (Input.GetMouseButtonDown(i))
                {
                    // perform action for mouse button press
                    Debug.Log("Mouse button pressed: " + i);
                }
            }
            // Check for mouse movement
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            if (mouseX != 0 || mouseY != 0)
            {
                // perform action for mouse movement
                Debug.Log("Mouse moved: " + mouseX + ", " + mouseY);
            }
        }
    }
}