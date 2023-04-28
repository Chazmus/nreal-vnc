using UnityEngine;
using UnityEngine.UIElements;
using Zenject;

namespace InputManagement
{
    public class TestClick
    {
        [Inject]
        public TestClick(KeyboardAndMouseInput keyboardAndMouseInput)
        {
            keyboardAndMouseInput.onMouseClicked += KeyboardAndMouseInputOnonMouseClicked;
        }

        private void KeyboardAndMouseInputOnonMouseClicked(MouseButton button, bool pressed)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = new Vector3(0, 0, 2);

        }

    }
}