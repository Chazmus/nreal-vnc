using UnityEngine;
using UnityEngine.UIElements;
using Zenject;

namespace InputManagement
{
    public class TestClick: IInitializable
    {
        [Inject]
        public TestClick(KeyboardAndMouseInput keyboardAndMouseInput)
        {
            keyboardAndMouseInput.onMouseClicked += KeyboardAndMouseInputOnonMouseClicked;
        }

        private void KeyboardAndMouseInputOnonMouseClicked(MouseButton button, bool pressed)
        {
            Debug.Log("Button:" + button + " was clicked");
        }

        public void Initialize()
        {
            // Do nothing
        }
    }
}